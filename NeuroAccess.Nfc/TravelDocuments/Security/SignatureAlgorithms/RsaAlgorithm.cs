using System;
using System.Formats.Asn1;
using System.Globalization;
using System.Numerics;
using System.Text;
using NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys;
using Waher.Networking;
using Waher.Security;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// RSA Signature algorithm
	/// </summary>
	public abstract class RsaAlgorithm : SignatureAlgorithm
	{
		/// <summary>
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="PublicKey">Public Key of the signing body.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public override bool VerifySignature(byte[] Data, byte[] Signature, IPublicKey PublicKey,
			ICommunicationLayer? Client)
		{
			if (PublicKey is not RsaPublicKey RsaParameters)
			{
				Client?.Error("No RSA public key provided or found.");
				return false;
			}

			int Bits = (int)RsaParameters.Modulus.GetBitLength();

			ASN1.ReportAlgorithmUse("RSA-PKCS-" + Bits.ToString(CultureInfo.InvariantCulture));

			return this.VerifySignatureRsaPkcs1(Data, Signature,
				RsaParameters.Modulus, RsaParameters.Exponent, Client);
		}

		/// <summary>
		/// Verifies a digital signature, using RSA PKCS#1 v1.5.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="Modulus">Modulus parameter.</param>
		/// <param name="Exponent">Exponent parameter.</param>
		/// <param name="HashFunction">Hash function to use, if defined.</param>
		/// <returns>If the digital signature is correct.</returns>
		public bool VerifySignatureRsaPkcs1(byte[] Data, byte[] Signature, BigInteger Modulus,
			BigInteger Exponent, ICommunicationLayer? Client)
		{
			BigInteger S = EllipticCurve.ToInt(Signature, true);
			ModulusP ModN = new(Modulus);
			BigInteger EM = 1;
			bool HasSniffer = Client?.HasSniffers ?? false;
			StringBuilder? Msg = HasSniffer ? new StringBuilder() : null;

			if (HasSniffer)
			{
				Msg!.AppendLine("RSS-PKCS1 signature verification parameters:");
				Msg.Append("n (Modulus, dec): ");
				Msg.AppendLine(Modulus.ToString(CultureInfo.InvariantCulture));
				Msg.Append("e (Exponent, dec): ");
				Msg.AppendLine(Exponent.ToString(CultureInfo.InvariantCulture));
				Msg.Append("S (Signature, dec): ");
				Msg.AppendLine(S.ToString(CultureInfo.InvariantCulture));
				Msg.Append("Message (hex): ");
				Msg.AppendLine(Hashes.BinaryToString(Data));
			}

			while (!Exponent.IsZero)
			{
				if (!Exponent.IsEven)
					EM = ModN.Multiply(EM, S);

				Exponent >>= 1;
				S = ModN.Multiply(S, S);
			}

			int K = Modulus.GetByteCount(true);
			byte[] EncodedMessage = EM.ToByteArray(true, true);

			if (EncodedMessage.Length > K)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Encoded message too long. Invalid signature.");
				}

				return false;
			}

			int c = EncodedMessage.Length;
			if (c < K)
			{
				byte[] Padded = new byte[K];
				Buffer.BlockCopy(EncodedMessage, 0, Padded, K - c, c);
				EncodedMessage = Padded;
			}

			if (HasSniffer)
			{
				Msg!.Append("EM = S^e (hex): ");
				Msg.AppendLine(Hashes.BinaryToString(EncodedMessage));
			}

			byte[] HashDigest = this.HashAlgorithm(Data);

			if (HasSniffer)
			{
				Msg!.Append("Hash Digest (hex): ");
				Msg.AppendLine(Hashes.BinaryToString(HashDigest));
			}

			AsnWriter w = new(AsnEncodingRules.DER);

			w.PushSequence();
			w.PushSequence();
			w.WriteObjectIdentifier(this.HashAlgorithmOid);
			w.WriteNull();
			w.PopSequence();
			w.WriteOctetString(HashDigest);
			w.PopSequence();

			byte[] DigestInfo = w.Encode();

			if (HasSniffer)
			{
				Msg!.Append("DigestInfo (hex): ");
				Msg.AppendLine(Hashes.BinaryToString(DigestInfo));
			}

			if (c < DigestInfo.Length + 11)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Encoded message & DigestInfo length mismatch. Invalid signature.");
				}

				return false;
			}

			if (EncodedMessage[0] != 0x00 || EncodedMessage[1] != 0x01)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Encoded message not prefixed correctly. Invalid signature.");
				}

				return false;
			}

			int Index = 2;

			while (Index < c && EncodedMessage[Index] == 0xff)
				Index++;

			if (Index < 10 || Index >= c || EncodedMessage[Index] != 0x00)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Encoded message not padded correctly. Invalid signature.");
				}

				return false;
			}

			Index++;

			if (c - Index + 1 != DigestInfo.Length)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Padding length not correct. Invalid signature.");
				}

				return false;
			}

			for (int i = 0; i < DigestInfo.Length; i++)
			{
				if (EncodedMessage[Index + i] != DigestInfo[i])
				{
					if (HasSniffer)
					{
						Client!.Information(Msg!.ToString());
						Client.Error("DigestInfo not encoded correctly into encoded message. Invalid signature.");
					}

					return false;
				}
			}

			if (HasSniffer)
				Client!.Information(Msg!.ToString());

			return true;
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public abstract HashFunctionArray HashAlgorithm { get; }

		/// <summary>
		/// OID of Hash algorithm to use.
		/// </summary>
		public abstract string HashAlgorithmOid { get; }
	}
}
