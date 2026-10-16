namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// The kind of change reported by <see cref="IContractStore.ContractChanged"/>.
	/// </summary>
	public enum ContractChangeKind
	{
		/// <summary>
		/// A new row was stored.
		/// </summary>
		Added,

		/// <summary>
		/// An existing row was updated.
		/// </summary>
		Updated,

		/// <summary>
		/// The contract was deleted on the server. A read-only copy may have been kept.
		/// </summary>
		Deleted,

		/// <summary>
		/// The row was removed by the user.
		/// </summary>
		Removed,

		/// <summary>
		/// The user opened the contract, and its unread marker was cleared.
		/// </summary>
		Viewed,

		/// <summary>
		/// A full sync finished. Lists should reload once.
		/// </summary>
		SyncCompleted
	}
}
