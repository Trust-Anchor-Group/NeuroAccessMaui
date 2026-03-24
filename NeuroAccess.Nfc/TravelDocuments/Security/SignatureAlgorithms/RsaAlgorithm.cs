using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Waher.Networking;

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
		/// <param name="Certificate">Certificate of the signing body.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		public override bool VerifySignature(byte[] Data, byte[] Signature, X509Certificate2 Certificate,
			ICommunicationLayer? Client)
		{
			using RSA? Rsa = Certificate.GetRSAPublicKey();

			if (Rsa is null)
				return false;

			bool Result = Rsa.VerifyData(Data, Signature, this.HashAlgorithmName, RSASignaturePadding.Pkcs1);

			//if (!Result && (Client?.HasSniffers ?? false))
			{
				Client?.Warning("Type: " + this.GetType().FullName);
				Client?.Warning("Public Key: " + Convert.ToBase64String(Rsa.ExportSubjectPublicKeyInfo()));
				Client?.Warning("Signature: " + Convert.ToBase64String(Signature));
				Client?.Warning("Data: " + Convert.ToBase64String(Data));
				Client?.Warning("Valid: " + Result.ToString());
			}

			return Result;
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public abstract HashAlgorithmName HashAlgorithmName { get; }
	}
}
