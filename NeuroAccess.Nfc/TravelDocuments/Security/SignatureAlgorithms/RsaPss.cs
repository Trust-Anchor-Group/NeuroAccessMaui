using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using NeuroAccess.Nfc.TravelDocuments.Security.HashFunctions;
using NeuroAccess.Nfc.TravelDocuments.Security.MaskGenerationFunctions;
using Waher.Networking;

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
			bool Result = false;	// TODO

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
	}
}
