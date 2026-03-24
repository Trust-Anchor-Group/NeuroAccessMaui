using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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
		/// <returns>If the digital signature is correct.</returns>
		public override bool VerifySignature(byte[] Data, byte[] Signature, X509Certificate2 Certificate)
		{
			using RSA? Rsa = Certificate.GetRSAPublicKey();

			if (Rsa is null)
				return false;

			return Rsa.VerifyData(Data, Signature, this.HashAlgorithmName, RSASignaturePadding.Pkcs1);
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public abstract HashAlgorithmName HashAlgorithmName { get; }
	}
}
