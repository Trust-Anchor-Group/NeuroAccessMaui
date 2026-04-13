using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys
{
	/// <summary>
	/// Interface for public keys.
	/// </summary>
	public interface IPublicKey : ISecurityObject
	{
		/// <summary>
		/// Verifies a digital signature.
		/// </summary>
		/// <param name="Data">Data being signed.</param>
		/// <param name="Signature">Digital signature.</param>
		/// <param name="SignatureAlgorithm">Algorithm used to sign the data.</param>
		/// <param name="Client">Optional client reference.</param>
		/// <returns>If the digital signature is correct.</returns>
		bool VerifySignature(byte[] Data, byte[] Signature, ISignatureAlgorithm SignatureAlgorithm,
			ICommunicationLayer? Client);

		/// <summary>
		/// Sets the public key, based on the security information provided.
		/// </summary>
		/// <param name="PublicKey">Security information used to set the public key.</param>
		/// <returns>If successful in setting the public key.</returns>
		bool SetPublicKey(object? PublicKey);
	}
}
