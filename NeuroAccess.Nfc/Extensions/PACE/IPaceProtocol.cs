using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.Extensions.PACE
{
	/// <summary>
	/// Basic interface for PACE protocols.
	/// </summary>
	public interface IPaceProtocol : IProcessingSupport<string>
	{
		/// <summary>
		/// OID identifying the PACE protocol.
		/// </summary>
		string Oid { get; }

		/// <summary>
		/// Security strength mapped as a grade.
		/// </summary>
		Grade SecurityStrength { get; }
	}
}
