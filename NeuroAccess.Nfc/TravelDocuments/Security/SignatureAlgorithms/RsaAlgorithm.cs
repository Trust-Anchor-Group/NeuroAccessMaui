using System;
using System.Formats.Asn1;
using System.Globalization;
using System.Numerics;
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
		/// <param name="PublicKeyKey">Public Key of the signing body.</param>
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

			byte[] Bin = RsaParameters.Modulus.ToByteArray();
			int Bits = Bin.Length << 3;
			if (Bin[0] == 0)
				Bits -= 8;

			ASN1.ReportAlgorithmUse("RSA-PKCS-" + Bits.ToString(CultureInfo.InvariantCulture));

			return this.VerifySignatureRsaPkcs1(Data, Signature,
				RsaParameters.Modulus, RsaParameters.Exponent);
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
			BigInteger Exponent)
		{
			BigInteger S = EllipticCurve.ToInt(Signature, true);
			ModulusP ModN = new(Modulus);
			BigInteger EM = 1;

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
				return false;

			int c = EncodedMessage.Length;
			if (c < K)
			{
				byte[] Padded = new byte[K];
				Buffer.BlockCopy(EncodedMessage, 0, Padded, K - c, c);
				EncodedMessage = Padded;
			}

			byte[] HashDigest = this.HashAlgorithm(Data);
			AsnWriter w = new(AsnEncodingRules.DER);

			w.PushSequence();
			w.PushSequence();
			w.WriteObjectIdentifier(this.HashAlgorithmOid);
			w.WriteNull();
			w.PopSequence();
			w.WriteOctetString(HashDigest);
			w.PopSequence();

			byte[] DigestInfo = w.Encode();

			if (c < DigestInfo.Length + 11)
				return false;

			if (EncodedMessage[0] != 0x00 || EncodedMessage[1] != 0x01)
				return false;

			int Index = 2;

			while (Index < c && EncodedMessage[Index] == 0xff)
				Index++;

			if (Index < 10 || Index >= c || EncodedMessage[Index] != 0x00)
				return false;

			Index++;

			if (c - Index + 1 != DigestInfo.Length)
				return false;

			for (int i = 0; i < DigestInfo.Length; i++)
			{
				if (EncodedMessage[Index + i] != DigestInfo[i])
					return false;
			}

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
