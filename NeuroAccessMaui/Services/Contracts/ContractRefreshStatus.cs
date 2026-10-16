namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// The outcome of <see cref="IContractSyncService.RefreshAsync(Waher.Persistence.CaseInsensitiveString, CancellationToken)"/>.
	/// </summary>
	public enum ContractRefreshStatus
	{
		/// <summary>
		/// The contract was read, and it changed since it was stored.
		/// </summary>
		Updated,

		/// <summary>
		/// The contract was read, and it hasn't changed.
		/// </summary>
		Unchanged,

		/// <summary>
		/// The contract no longer exists on the server.
		/// </summary>
		Deleted,

		/// <summary>
		/// The user isn't allowed to read the contract.
		/// </summary>
		Forbidden,

		/// <summary>
		/// The server couldn't be reached.
		/// </summary>
		Offline,

		/// <summary>
		/// The contract couldn't be read for another reason. The error has been logged.
		/// </summary>
		Failed
	}
}
