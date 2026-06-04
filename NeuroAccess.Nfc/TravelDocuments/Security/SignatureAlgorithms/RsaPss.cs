using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using NeuroAccess.Nfc.TravelDocuments.Security.MaskGenerationFunctions;
using NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys;
using Waher.Networking;
using Waher.Security.EllipticCurves;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// RSASSA-PSS algorithm, as defined in RFCs 3447, 4055 and 4056:
	/// https://www.rfc-editor.org/rfc/rfc3447
	/// https://www.rfc-editor.org/rfc/rfc4055
	/// https://www.rfc-editor.org/rfc/rfc4056
	/// </summary>
	public class RsaPss : RsaAlgorithm
	{
		private static readonly HashFunction defaultHashFunction = new Sha1();
		private static readonly MGF1 defaultMaskGenerationFunction = new(defaultHashFunction);

		private HashFunction hashFunction = defaultHashFunction;
		private MaskGenerationFunction maskGenerationFunction = defaultMaskGenerationFunction;
		private int saltLength = 20;
		private int trailerField = 1;
		private bool configured = false;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.10";

		/// <summary>
		/// If the object has been configured.
		/// </summary>
		public override bool IsConfigured => this.configured;

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		public override bool Configure(Vector SecurityInfo)
		{
			if (SecurityInfo.Length < 2)
			{
				this.configured = true;
				return true;
			}

			if (SecurityInfo[1] is not Vector RsaSsaPssParameters)
			{
				return false;
			}

			int c = RsaSsaPssParameters.Length;

			for (int i = 0; i < c; i++)
			{
				object? Parameter = RsaSsaPssParameters[i];

				if (Parameter is ContextSpecific TaggedParameter)
				{
					switch (TaggedParameter.Tag)
					{
						case 0:
							if (!TryGetHashFunction(TaggedParameter, out HashFunction HashFunction))
								return false;

							this.hashFunction = HashFunction;
							break;

						case 1:
							if (!TryGetMaskGenerationFunction(TaggedParameter, out MaskGenerationFunction MaskGenerationFunction))
								return false;

							this.maskGenerationFunction = MaskGenerationFunction;
							break;

						case 2:
							if (!TryGetInteger(TaggedParameter, out int SaltLength))
								return false;

							this.saltLength = SaltLength;
							break;

						case 3:
							if (!TryGetInteger(TaggedParameter, out int TrailerField))
								return false;

							this.trailerField = TrailerField;
							break;

						default:
							return false;
					}

					continue;
				}

				if (i == 0)
				{
					if (!TryGetHashFunction(Parameter, out HashFunction HashFunction))
						return false;

					this.hashFunction = HashFunction;
				}
				else if (i == 1)
				{
					if (!TryGetMaskGenerationFunction(Parameter, out MaskGenerationFunction MaskGenerationFunction))
						return false;

					this.maskGenerationFunction = MaskGenerationFunction;
				}
				else if (i == 2)
				{
					if (!TryGetInteger(Parameter, out int SaltLength))
						return false;

					this.saltLength = SaltLength;
				}
				else if (i == 3)
				{
					if (!TryGetInteger(Parameter, out int TrailerField))
						return false;

					this.trailerField = TrailerField;
				}
				else
					return false;
			}

			this.configured = true;

			return true;
		}

		private static bool TryGetHashFunction(object? Value, out HashFunction HashFunction)
		{
			if (Value is HashFunction HashFunction2)
			{
				HashFunction = HashFunction2;
				return true;
			}

			if (Value is Vector HashVector &&
				HashVector.Length >= 1)
			{
				return TryGetHashFunction(HashVector[0], out HashFunction);
			}

			HashFunction = defaultHashFunction;
			return false;
		}

		private static bool TryGetMaskGenerationFunction(object? Value, out MaskGenerationFunction MaskGenerationFunction)
		{
			if (Value is MaskGenerationFunction MaskGenerationFunction2)
			{
				MaskGenerationFunction = MaskGenerationFunction2;
				return true;
			}

			if (Value is Vector MaskGenerationFunctionVector &&
				MaskGenerationFunctionVector.Length >= 1)
			{
				return TryGetMaskGenerationFunction(MaskGenerationFunctionVector[0], out MaskGenerationFunction);
			}

			MaskGenerationFunction = defaultMaskGenerationFunction;
			return false;
		}

		private static bool TryGetInteger(object? Value, out int Integer)
		{
			if (Value is Vector IntegerVector &&
				IntegerVector.Length >= 1)
			{
				Value = IntegerVector[0];
			}

			if (Value is BigInteger BigIntegerValue &&
				BigIntegerValue >= int.MinValue &&
				BigIntegerValue <= int.MaxValue)
			{
				Integer = (int)BigIntegerValue;
				return true;
			}

			Integer = 0;
			return false;
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public override Waher.Security.HashFunctionArray HashAlgorithm => this.hashFunction.ComputeHash;

		/// <summary>
		/// OID of Hash algorithm to use.
		/// </summary>
		public override string HashAlgorithmOid => this.hashFunction.Oid;

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
			if (PublicKey is not RsaPublicKey RsaPublicKey)
			{
				Client?.Error("Unable to get RSA public key from certificate.");
				return false;
			}

			int Bits = (int)RsaPublicKey.Modulus.GetBitLength();

			ASN1.ReportAlgorithmUse("RSA-PSS-" + Bits.ToString(CultureInfo.InvariantCulture));

			BigInteger S = EllipticCurve.ToInt(Signature, true);

			return Verify(this.hashFunction, this.maskGenerationFunction,
				RsaPublicKey.Modulus, RsaPublicKey.Exponent, Data, S, this.saltLength, Client);
		}

		/// <summary>
		/// Verifies an RSA-PSS signature.
		/// </summary>
		/// <param name="H">Hash function</param>
		/// <param name="Mgf">Mask Generation function</param>
		/// <param name="n">Modulus</param>
		/// <param name="e">Exponent</param>
		/// <param name="Message">Signed message</param>
		/// <param name="S">Signature</param>
		/// <param name="SaltLen">Length of salt.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the signature is valid.</returns>
		public static bool Verify(HashFunction H, MaskGenerationFunction Mgf, BigInteger n,
			BigInteger e, byte[] Message, BigInteger S, int SaltLen, ICommunicationLayer? Client)
		{
			// Encoded Message EM = S^e mod n

			bool HasSniffer = Client?.HasSniffers ?? false;
			StringBuilder? Msg = HasSniffer ? new StringBuilder() : null;

			if (HasSniffer)
			{
				Msg!.AppendLine("RSS-PSS signature verification parameters:");
				Msg.Append("n (Modulus, dec): ");
				Msg.AppendLine(n.ToString(CultureInfo.InvariantCulture));
				Msg.Append("e (Exponent, dec): ");
				Msg.AppendLine(e.ToString(CultureInfo.InvariantCulture));
				Msg.Append("S (Signature, dec): ");
				Msg.AppendLine(S.ToString(CultureInfo.InvariantCulture));
				Msg.Append("Salt length (dec): ");
				Msg.AppendLine(SaltLen.ToString(CultureInfo.InvariantCulture));
				Msg.Append("Hash function: ");
				Msg.AppendLine(H.ToString());
				Msg.Append("MGF function: ");
				Msg.AppendLine(Mgf.ToString());
				Msg.Append("Message (hex): ");
				Msg.AppendLine(Waher.Security.Hashes.BinaryToString(Message));
			}

			if (n <= BigInteger.Zero ||
				e <= BigInteger.Zero)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Invalid RSA public key parameters.");
				}

				return false;
			}

			if (S < BigInteger.Zero ||
				S >= n)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("RSA signature representative out of range.");
				}

				return false;
			}

			if (SaltLen < 0)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Invalid RSA-PSS salt length.");
				}

				return false;
			}

			BigInteger EM = BigInteger.ModPow(S, e, n);
			byte[] EMBin = EM.ToByteArray(true, true);
			int EMLen = (int)(n.GetBitLength() + 7) / 8;

			if (EMBin.Length < EMLen)
			{
				byte[] EMBin2 = new byte[EMLen];
				Buffer.BlockCopy(EMBin, 0, EMBin2, EMLen - EMBin.Length, EMBin.Length);
				EMBin = EMBin2;
			}

			if (HasSniffer)
			{
				Msg!.Append("EM = S^e (hex): ");
				Msg.AppendLine(Waher.Security.Hashes.BinaryToString(EMBin));
			}

			if (EMBin[^1] != 0xbc)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("S^e does not end with BC. Invalid signature.");
				}

				return false;
			}

			int HLen = H.HashLength;
			int MaskedDBLen = EMBin.Length - 1 - HLen;

			if (MaskedDBLen <= 0)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("Encoded RSA-PSS message too short.");
				}

				return false;
			}

			byte[] MaskedDB = new byte[MaskedDBLen];
			byte[] HashDigest = new byte[HLen];

			Buffer.BlockCopy(EMBin, 0, MaskedDB, 0, MaskedDBLen);
			Buffer.BlockCopy(EMBin, MaskedDBLen, HashDigest, 0, HLen);

			if (HasSniffer)
			{
				Msg!.Append("Masked DB (hex): ");
				Msg.AppendLine(Waher.Security.Hashes.BinaryToString(MaskedDB));
				Msg!.Append("Hash Digest 1 (hex): ");
				Msg.AppendLine(Waher.Security.Hashes.BinaryToString(HashDigest));
			}

			byte[] DBMask = Mgf.CalculateMask(HashDigest, MaskedDBLen);

			if (HasSniffer)
			{
				Msg!.Append("DB Mask (hex): ");
				Msg.AppendLine(Waher.Security.Hashes.BinaryToString(DBMask));
			}

			byte[] DB = TravelDocumentsClient.XOR(MaskedDB, DBMask);

			DB[0] &= 0x7f;  // MSB can be 1, as EMBits=bitlen(n)-1

			if (HasSniffer)
			{
				Msg!.Append("DB (hex): ");
				Msg.AppendLine(Waher.Security.Hashes.BinaryToString(DB));
			}

			int i = 0;
			int c = DB.Length;

			while (i < c && DB[i] == 0)
				i++;

			if (i >= c ||
				DB[i++] != 1 ||
				c - i != SaltLen)
			{
				if (HasSniffer)
				{
					Client!.Information(Msg!.ToString());
					Client.Error("DB invalid. Invalid signature.");
				}

				return false;
			}

			byte[] Digest = H.ComputeHash(Message);
			byte[] H2 = new byte[8 + HLen + SaltLen];

			Buffer.BlockCopy(Digest, 0, H2, 8, HLen);
			Buffer.BlockCopy(DB, i, H2, 8 + HLen, SaltLen);

			Digest = H.ComputeHash(H2);

			if (HasSniffer)
			{
				Msg!.Append("Hash Digest 2 (hex): ");
				Msg.AppendLine(Waher.Security.Hashes.BinaryToString(Digest));
			}

			for (i = 0; i < HLen; i++)
			{
				if (Digest[i] != HashDigest[i])
				{
					if (HasSniffer)
					{
						Client!.Information(Msg!.ToString());
						Client.Error("Hash digests do not match. Invalid signature.");
					}

					return false;
				}
			}

			if (HasSniffer)
				Client!.Information(Msg!.ToString());

			return true;
		}

	}
}
