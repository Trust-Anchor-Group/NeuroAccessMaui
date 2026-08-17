namespace NeuroAccessMaui.Services.Xmpp
{
	/// <summary>
	/// Categorizes a contract-signing failure without exposing protocol error content.
	/// </summary>
	public enum ContractSigningFailureCategory
	{
		/// <summary>
		/// No failure was recorded.
		/// </summary>
		None,

		/// <summary>
		/// A signing, reconciliation, or persistence operation returned a fault.
		/// </summary>
		OperationFault,

		/// <summary>
		/// The authoritative contract entered a failed or rejected state.
		/// </summary>
		TerminalContractState,

		/// <summary>
		/// The foreground deadline elapsed before the outcome was confirmed.
		/// </summary>
		Timeout
	}
}
