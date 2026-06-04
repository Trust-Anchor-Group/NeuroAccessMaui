using System.Collections.Generic;

namespace NeuroAccess.Nfc.TravelDocuments.Security.PublicKeys
{
	/// <summary>
	/// Interface for public keys.
	/// </summary>
	public interface IPublicKey : ISecurityObject
	{
		/// <summary>
		/// Sets the public key, based on the security information provided.
		/// </summary>
		/// <param name="PublicKey">Security information used to set the public key.</param>
		/// <returns>If successful in setting the public key.</returns>
		bool SetPublicKey(object? PublicKey);

		/// <summary>
		/// Gets parsed parameters from the public key definition, if available.
		/// </summary>
		/// <param name="Parameters">Dictionary to receive parsed parameters.</param>
		public abstract void GetParsedParameters(Dictionary<string, object?> Parameters);
	}
}
