namespace NeuroAccessMaui.Services.Xmpp
{
	/// <summary>
	/// Describes the confirmed foreground outcome of a contract-signing operation.
	/// </summary>
	public enum ContractSigningOutcome
	{
		/// <summary>
		/// The signature was confirmed by the contracts service.
		/// </summary>
		Confirmed,

		/// <summary>
		/// The request faulted or the authoritative contract entered a failed or rejected state.
		/// </summary>
		TerminalFailure,

		/// <summary>
		/// The foreground deadline elapsed before an authoritative outcome was available.
		/// </summary>
		OutcomeUnknown
	}
}
