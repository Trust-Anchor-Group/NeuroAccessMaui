using NeuroAccess.Nfc.TravelDocuments.Security.SignatureAlgorithms;
using Waher.Networking;

namespace NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys
{
	/// <summary>
	/// Abstract base class for public keys.
	/// </summary>
	public abstract class PublicKey : SecurityObject, IPublicKey
	{
		/// <summary>
		/// Sets the public key, based on the security information provided.
		/// </summary>
		/// <param name="PublicKey">Security information used to set the public key.</param>
		/// <returns>If successful in setting the public key.</returns>
		public abstract bool SetPublicKey(object? PublicKey);
	}
}
