using System.Security.Cryptography.X509Certificates;

namespace NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms
{
	/// <summary>
	/// Interface for signature algorithms, as defined in RFC 5280.
	/// </summary>
	public interface ISignatureAlgorithm : ISecurityObject
	{
		/// <summary>
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="Certificate">Certificate of the signing body.</param>
		/// <returns>If the digital signature is correct.</returns>
		bool VerifySignature(byte[] Data, byte[] Signature, X509Certificate2 Certificate);
	}
}
