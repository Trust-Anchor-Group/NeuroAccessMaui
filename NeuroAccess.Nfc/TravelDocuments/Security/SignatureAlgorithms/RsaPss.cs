using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using NeuroAccess.Nfc.TravelDocuments.Security.MaskGenerationFunctions;
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
	public class RsaPss : SignatureAlgorithm
	{
		private static readonly HashFunction defaultHashFunction = new Sha1();
		private static readonly MGF1 defaultMaskGenerationFunction = new(defaultHashFunction);

		private HashFunction hashFunction = defaultHashFunction;
		private MaskGenerationFunction maskGenerationFunction = defaultMaskGenerationFunction;
		private int saltLength = 20;
		private int trailerField = 1;

		/// <summary>
		/// OID identifying the type of object.
		/// </summary>
		public override string Oid => "1.2.840.113549.1.1.10";

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

			return true;
		}
		/// <summary>
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="Certificate">Certificate of the signing body.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public override bool VerifySignature(byte[] Data, byte[] Signature, X509Certificate2 Certificate,
			ICommunicationLayer? Client)
		{
			using RSA? Rsa = Certificate.GetRSAPublicKey();
			if (Rsa is null)
			{
				Client?.Error("Unable to get RSA public key from certificate.");
				return false;
			}

			RSAParameters P = Rsa.ExportParameters(false);
			BigInteger S = EllipticCurve.ToInt(Signature, true);
			BigInteger e = EllipticCurve.ToInt(P.Exponent, true);
			BigInteger n = EllipticCurve.ToInt(P.Modulus, true);

			bool Result = Verify(this.hashFunction, this.maskGenerationFunction, n, e, Data, S, this.saltLength);

			if (!Result && (Client?.HasSniffers ?? false))
			{
				Client?.Warning("Type: " + this.GetType().FullName);
				Client?.Warning("Hash Function: " + this.hashFunction.ToString());
				Client?.Warning("Mask Generation Function: " + this.maskGenerationFunction.ToString());
				Client?.Warning("Salt Length: " + this.saltLength.ToString(CultureInfo.InvariantCulture));
				Client?.Warning("Trailer Field: " + this.trailerField.ToString(CultureInfo.InvariantCulture));
				Client?.Warning("Signature: " + Convert.ToBase64String(Signature));
				Client?.Warning("Data: " + Convert.ToBase64String(Data));
				Client?.Warning("Valid: " + Result.ToString());
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
