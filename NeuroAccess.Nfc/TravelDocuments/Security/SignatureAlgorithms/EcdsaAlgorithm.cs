using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// ECDSA Signature algorithm
	/// </summary>
	public abstract class EcdsaAlgorithm : SignatureAlgorithm
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
			using ECDsa? Ecdsa = Certificate.GetECDsaPublicKey();

			if (Ecdsa is null)
				return false;

			return Ecdsa.VerifyData(Data, Signature, this.HashAlgorithmName, DSASignatureFormat.Rfc3279DerSequence);
		}

		/// <summary>
		/// Hash algorithm to use.
		/// </summary>
		public abstract HashAlgorithmName HashAlgorithmName { get; }
	}
}
