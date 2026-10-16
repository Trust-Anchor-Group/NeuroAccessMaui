using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Event arguments for <see cref="IContractStore.ContractChanged"/>.
	/// </summary>
	/// <param name="ContractId">The ID of the contract that changed. Null for <see cref="ContractChangeKind.SyncCompleted"/>.</param>
	/// <param name="Kind">The kind of change.</param>
	/// <param name="Reference">The stored row after the change, if one remains.</param>
	/// <param name="Contract">The decrypted contract, when the change came with one.</param>
	public class ContractChangedEventArgs(CaseInsensitiveString? ContractId, ContractChangeKind Kind, ContractReference? Reference, Contract? Contract) : EventArgs
	{
		/// <summary>
		/// The ID of the contract that changed. Null for <see cref="ContractChangeKind.SyncCompleted"/>.
		/// </summary>
		public CaseInsensitiveString? ContractId { get; } = ContractId;

		/// <summary>
		/// The kind of change.
		/// </summary>
		public ContractChangeKind Kind { get; } = Kind;

		/// <summary>
		/// The stored row after the change, if one remains.
		/// </summary>
		public ContractReference? Reference { get; } = Reference;

		/// <summary>
		/// The decrypted contract, when the change came with one.
		/// </summary>
		public Contract? Contract { get; } = Contract;

		/// <summary>
		/// Checks if the change concerns a given contract.
		/// </summary>
		/// <param name="Id">The contract ID to compare with.</param>
		/// <returns>If the change concerns the contract.</returns>
		public bool IsFor(string? Id)
		{
			return !string.IsNullOrEmpty(Id) &&
				this.ContractId is not null &&
				string.Equals(this.ContractId.Value, Id, StringComparison.OrdinalIgnoreCase);
		}
	}
}
