using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Interface for security objects.
	/// </summary>
	public interface ISecurityObject : IProcessingSupport<string>
	{
		/// <summary>
		/// OID identifying the object.
		/// </summary>
		string Oid { get; }

		/// <summary>
		/// If the object can be configured by the security information provided.
		/// </summary>
		/// <param name="SecurityInfo">Security information.</param>
		/// <returns>If the object can be configured, given the security information.</returns>
		bool Configure(Vector SecurityInfo);
	}
}
