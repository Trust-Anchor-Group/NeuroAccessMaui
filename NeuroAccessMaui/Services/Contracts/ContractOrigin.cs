namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Where a stored contract came from. Decides which fields of the stored row are updated.
	/// </summary>
	public enum ContractOrigin
	{
		/// <summary>
		/// The user created the contract.
		/// </summary>
		Created,

		/// <summary>
		/// The user signed the contract.
		/// </summary>
		Signed,

		/// <summary>
		/// The contract was read during a sync with the server.
		/// </summary>
		Synced,

		/// <summary>
		/// The contract was read after a contract event from the server.
		/// </summary>
		Event,

		/// <summary>
		/// The contract came with an invitation to sign it.
		/// </summary>
		Invitation,

		/// <summary>
		/// The user opened the contract.
		/// </summary>
		Opened
	}
}
