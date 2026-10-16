namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// A query for stored contracts, used by <see cref="IContractStore.QueryAsync"/>. Results are sorted by last change, newest first.
	/// </summary>
	public class ContractListQuery
	{
		/// <summary>
		/// If templates are listed, rather than contracts.
		/// </summary>
		public bool IsTemplate { get; set; }

		/// <summary>
		/// Only rows in this group, if set.
		/// </summary>
		public ContractListGroup? Group { get; set; }

		/// <summary>
		/// Only token creation templates (true) or other rows (false), if set.
		/// </summary>
		public bool? IsTokenCreationTemplate { get; set; }

		/// <summary>
		/// Text the rows must contain, if set. Matched against the stored search text, ignoring case.
		/// </summary>
		public string? SearchText { get; set; }

		/// <summary>
		/// Number of rows to skip.
		/// </summary>
		public int Offset { get; set; }

		/// <summary>
		/// Maximum number of rows to return.
		/// </summary>
		public int MaxCount { get; set; } = 20;
	}
}
