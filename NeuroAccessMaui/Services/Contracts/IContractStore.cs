using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// The only writer of stored contracts (<see cref="ContractReference"/> rows). Writes for one contract are serialized,
	/// and every write raises <see cref="ContractChanged"/>.
	/// </summary>
	public interface IContractStore
	{
		/// <summary>
		/// Raised after every write.
		/// </summary>
		event EventHandlerAsync<ContractChangedEventArgs>? ContractChanged;

		/// <summary>
		/// Reads one stored contract.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		/// <returns>The stored row, or null if there is none.</returns>
		Task<ContractReference?> GetAsync(CaseInsensitiveString ContractId);

		/// <summary>
		/// Adds or updates a stored contract from a decrypted contract, and recomputes its derived fields.
		/// </summary>
		/// <param name="Contract">The decrypted contract.</param>
		/// <param name="Origin">Where the contract came from.</param>
		/// <returns>The stored row.</returns>
		Task<ContractReference> UpsertAsync(Contract Contract, ContractOrigin Origin);

		/// <summary>
		/// Adds or updates a stored contract from a decrypted contract, and recomputes its derived fields.
		/// </summary>
		/// <param name="Contract">The decrypted contract.</param>
		/// <param name="Origin">Where the contract came from.</param>
		/// <param name="IsCreator">If the user's account is known to have created the contract. A stored true is never cleared.</param>
		/// <returns>The stored row.</returns>
		Task<ContractReference> UpsertAsync(Contract Contract, ContractOrigin Origin, bool IsCreator);

		/// <summary>
		/// Saves an invitation to sign a contract, with or without the contract.
		/// </summary>
		/// <param name="Invitation">The invitation.</param>
		/// <param name="Contract">The decrypted contract, if it could be read.</param>
		/// <returns>The stored row.</returns>
		Task<ContractReference> RecordInvitationAsync(ContractInvitation Invitation, Contract? Contract);

		/// <summary>
		/// Handles a contract that was deleted on the server. Keeps a read-only copy if the user signed it, and removes the row otherwise.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		Task MarkDeletedAsync(CaseInsensitiveString ContractId);

		/// <summary>
		/// Removes a stored contract, for example a declined invitation or a deleted copy.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		Task RemoveAsync(CaseInsensitiveString ContractId);

		/// <summary>
		/// Clears the unread marker of a stored contract.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		Task MarkViewedAsync(CaseInsensitiveString ContractId);

		/// <summary>
		/// Recomputes the derived fields of a stored contract from its saved copy, and saves the row if they changed.
		/// Time alone can move a row to another group.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		/// <param name="IsCreator">If the user's account is known to have created the contract.</param>
		/// <returns>If the row changed.</returns>
		Task<bool> RecomputeAsync(CaseInsensitiveString ContractId, bool IsCreator);

		/// <summary>
		/// Lists stored contracts, sorted by last change, newest first.
		/// </summary>
		/// <param name="Query">The query.</param>
		/// <returns>The matching rows.</returns>
		Task<IReadOnlyList<ContractReference>> QueryAsync(ContractListQuery Query);

		/// <summary>
		/// Lists every stored contract that isn't a template.
		/// </summary>
		/// <returns>The rows.</returns>
		Task<IReadOnlyList<ContractReference>> GetAllContractsAsync();

		/// <summary>
		/// Raises one <see cref="ContractChangeKind.SyncCompleted"/> change, after a full sync.
		/// </summary>
		Task RaiseSyncCompletedAsync();
	}
}
