using System;
using System.Globalization;
using System.Numerics;
using System.Text;
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
			if (SecurityInfo.Length < 2 ||
				SecurityInfo[1] is not Vector RsaSsaPssParameters)
			{
				return false;
			}

			int c = RsaSsaPssParameters.Length;

			if (c >= 1)
			{
				if (RsaSsaPssParameters[0] is not Vector HashVector ||
					HashVector.Length < 1 ||
					HashVector[0] is not HashFunction HashFunction)
				{
					return false;
				}

				this.hashFunction = HashFunction;

				if (c >= 2)
				{
					if (RsaSsaPssParameters[1] is not Vector MaskGenerationFunctionVector ||
						MaskGenerationFunctionVector.Length < 1 ||
						MaskGenerationFunctionVector[0] is not MaskGenerationFunction MaskGenerationFunction)
					{
						return false;
					}

					this.maskGenerationFunction = MaskGenerationFunction;
				}

				if (c >= 3)
				{
					if (RsaSsaPssParameters[2] is not Vector SaltLengthVector ||
						SaltLengthVector.Length < 1 ||
						SaltLengthVector[0] is not System.Numerics.BigInteger SaltLength ||
						SaltLength < int.MinValue ||
						SaltLength > int.MaxValue)
					{
						return false;
					}

					this.saltLength = (int)SaltLength;

					if (c >= 4)
					{
						if (RsaSsaPssParameters[3] is not Vector TrailerFieldVector ||
							TrailerFieldVector.Length < 1 ||
							TrailerFieldVector[0] is not System.Numerics.BigInteger TrailerField ||
							TrailerField < int.MinValue ||
							TrailerField > int.MaxValue)
						{
							return false;
						}

						this.trailerField = (int)TrailerField;

						if (c > 4)
							return false;
					}
				}
			}

			this.configured = true;

			return true;
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
		/// <param name="PublicKeyKey">Public Key of the signing body.</param>
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

			BigInteger S = EllipticCurve.ToInt(Signature, true);

			bool Result = Verify(this.hashFunction, this.maskGenerationFunction,
				RsaPublicKey.Modulus, RsaPublicKey.Exponent, Data, S, this.saltLength);

			if (!Result && (Client?.HasSniffers ?? false))
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("Type: ");
				sb.AppendLine(this.GetType().FullName);
				sb.Append("Hash Function: ");
				sb.AppendLine(this.hashFunction.ToString());
				sb.Append("Mask Generation Function: ");
				sb.AppendLine(this.maskGenerationFunction.ToString());
				sb.Append("Salt Length: ");
				sb.AppendLine(this.saltLength.ToString(CultureInfo.InvariantCulture));
				sb.Append("Trailer Field: ");
				sb.AppendLine(this.trailerField.ToString(CultureInfo.InvariantCulture));
				sb.Append("Signature: ");
				sb.AppendLine(S.ToString(CultureInfo.InvariantCulture));
				sb.Append("Modulus: ");
				sb.AppendLine(RsaPublicKey.Modulus.ToString(CultureInfo.InvariantCulture));
				sb.Append("Exponent: ");
				sb.AppendLine(RsaPublicKey.Exponent.ToString(CultureInfo.InvariantCulture));
				sb.Append("Data: ");
				sb.AppendLine(Convert.ToBase64String(Data));
				sb.Append("Valid: ");
				sb.AppendLine(Result.ToString());

				Client.Warning(sb.ToString());
			}

			return Result;
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
		/// <returns>If the signature is valid.</returns>
		public static bool Verify(HashFunction H, MaskGenerationFunction Mgf, BigInteger n,
			BigInteger e, byte[] Message, BigInteger S, int SaltLen)
		{
			// Encoded Message EM = S^e mod n

			ModulusP ModN = new(n); // n not a prime, so not a Field (i.e. has zero-divisors), but addition and multiplication mod n work.
			BigInteger EM = 1;

			while (!e.IsZero)
			{
				if (!e.IsEven)
					EM = ModN.Multiply(EM, S);

				e >>= 1;
				S = ModN.Multiply(S, S);
			}

			byte[] EMBin = EM.ToByteArray(true, true);
			if (EMBin[^1] != 0xbc)
				return false;

			int HLen = H.HashLength;
			int MaskedDBLen = EMBin.Length - 1 - HLen;
			byte[] MaskedDB = new byte[MaskedDBLen];
			byte[] HashDigest = new byte[HLen];

			Buffer.BlockCopy(EMBin, 0, MaskedDB, 0, MaskedDBLen);
			Buffer.BlockCopy(EMBin, MaskedDBLen, HashDigest, 0, HLen);

			byte[] DBMask = Mgf.CalculateMask(HashDigest, MaskedDBLen);
			byte[] DB = TravelDocumentsClient.XOR(MaskedDB, DBMask);
			int i = 0;
			int c = DB.Length;

			DB[0] &= 0x7f;  // MSB can be 1, as EMBits=bitlen(n)-1
			while (i < c && DB[i] == 0)
				i++;

			if (i >= c)
				return false;

			if (DB[i++] != 1)
				return false;

			if (c - i != SaltLen)
				return false;

			byte[] Digest = H.ComputeHash(Message);
			byte[] H2 = new byte[8 + HLen + SaltLen];

			Buffer.BlockCopy(Digest, 0, H2, 8, HLen);
			Buffer.BlockCopy(DB, i, H2, 8 + HLen, SaltLen);

			Digest = H.ComputeHash(H2);

			for (i = 0; i < HLen; i++)
			{
				if (Digest[i] != HashDigest[i])
					return false;
			}

			return true;
		}

	}
}
