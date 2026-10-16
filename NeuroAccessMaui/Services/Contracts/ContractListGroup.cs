namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// The group a contract is listed under in My contracts. The order of the values is the order of the groups.
	/// </summary>
	public enum ContractListGroup
	{
		/// <summary>
		/// The user has an open invitation, or can sign and hasn't.
		/// </summary>
		NeedsAction = 0,

		/// <summary>
		/// The contract is waiting for others, or for the operator.
		/// </summary>
		InProgress = 1,

		/// <summary>
		/// The contract is signed.
		/// </summary>
		Signed = 2,

		/// <summary>
		/// The contract is withdrawn, failed, rejected, deleted or expired.
		/// </summary>
		Ended = 3
	}
}
