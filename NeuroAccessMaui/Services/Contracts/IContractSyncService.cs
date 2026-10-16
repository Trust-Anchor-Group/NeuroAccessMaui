using Waher.Persistence;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Reads contracts from the server and stores them through <see cref="IContractStore"/>.
	/// </summary>
	public interface IContractSyncService
	{
		/// <summary>
		/// If a full sync is running.
		/// </summary>
		bool IsSyncing { get; }

		/// <summary>
		/// Reads every contract the user created or signed, and refreshes stored contracts the server didn't list.
		/// Only one sync runs at a time. Connection failures stop the sync quietly.
		/// </summary>
		/// <param name="Force">If the sync runs even if one finished recently.</param>
		/// <param name="CancellationToken">Cancels the sync.</param>
		/// <returns>If the sync ran to the end.</returns>
		Task<bool> SyncAsync(bool Force, CancellationToken CancellationToken);

		/// <summary>
		/// Reads one contract from the server, decrypted, and stores it if the user takes part in it, it is a template,
		/// or it is already stored.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		/// <param name="CancellationToken">Cancels the refresh.</param>
		/// <returns>The result.</returns>
		Task<ContractRefreshResult> RefreshAsync(CaseInsensitiveString ContractId, CancellationToken CancellationToken);

		/// <summary>
		/// Reads one contract from the server, decrypted, and stores it if the user takes part in it, it is a template,
		/// or it is already stored.
		/// </summary>
		/// <param name="ContractId">The contract ID.</param>
		/// <param name="Origin">Why the contract is read.</param>
		/// <param name="IsCreator">If the user's account is known to have created the contract.</param>
		/// <param name="CancellationToken">Cancels the refresh.</param>
		/// <returns>The result.</returns>
		Task<ContractRefreshResult> RefreshAsync(CaseInsensitiveString ContractId, ContractOrigin Origin, bool IsCreator, CancellationToken CancellationToken);
	}
}
