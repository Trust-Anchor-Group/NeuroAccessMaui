using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Persistence;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// Reads contracts from the server and stores them through <see cref="IContractStore"/>.
	/// </summary>
	internal class ContractSyncService : IContractSyncService, IDisposable
	{
		private const string LastSyncKey = "Contracts.LastSync";
		private const int ReferencePageSize = 50;
		private const int MaxReferencePages = 100;
		private const int BatchSize = 25;
		private const int MaxParallelRefreshes = 4;

		private static readonly TimeSpan minSyncInterval = TimeSpan.FromMinutes(5);

		private readonly SemaphoreSlim syncLock = new(1, 1);
		private DateTime lastSync = DateTime.MinValue;
		private bool lastSyncRestored;
		private volatile bool isSyncing;
		private bool disposed;

		/// <summary>
		/// Reads contracts from the server and stores them through <see cref="IContractStore"/>.
		/// </summary>
		public ContractSyncService()
		{
		}

		/// <inheritdoc/>
		public bool IsSyncing => this.isSyncing;

		/// <inheritdoc/>
		public async Task<bool> SyncAsync(bool Force, CancellationToken CancellationToken)
		{
			DateTime Requested = DateTime.UtcNow;
			bool Ran = false;
			bool Completed = false;

			try
			{
				await this.syncLock.WaitAsync(CancellationToken);
			}
			catch (OperationCanceledException)
			{
				return false;
			}

			try
			{
				if (!this.lastSyncRestored)
				{
					this.lastSync = await ServiceRef.SettingsService.RestoreDateTimeState(LastSyncKey, DateTime.MinValue);
					this.lastSyncRestored = true;
				}

				if (this.lastSync >= Requested)
					return true;    // Another sync finished while this one waited.

				if (!Force && Requested - this.lastSync < minSyncInterval)
					return true;

				if (!ServiceRef.XmppService.IsOnline || ServiceRef.TagProfile.LegalIdentity is null)
					return false;

				Ran = true;
				this.isSyncing = true;
				try
				{
					Completed = await this.RunSyncAsync(CancellationToken);
				}
				finally
				{
					this.isSyncing = false;
				}

				if (Completed)
				{
					this.lastSync = DateTime.UtcNow;
					await ServiceRef.SettingsService.SaveState(LastSyncKey, this.lastSync);
				}
			}
			catch (OperationCanceledException)
			{
				Completed = false;
			}
			catch (Exception Ex)
			{
				Completed = false;

				if (!IsConnectionError(Ex))
					ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.syncLock.Release();
			}

			if (Ran)
				await ServiceRef.ContractStore.RaiseSyncCompletedAsync();

			return Completed;
		}

		/// <inheritdoc/>
		public Task<ContractRefreshResult> RefreshAsync(CaseInsensitiveString ContractId, CancellationToken CancellationToken)
		{
			return this.RefreshAsync(ContractId, ContractOrigin.Event, false, CancellationToken);
		}

		/// <inheritdoc/>
		public async Task<ContractRefreshResult> RefreshAsync(CaseInsensitiveString ContractId, ContractOrigin Origin, bool IsCreator, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(ContractId?.Value))
				return new ContractRefreshResult(ContractRefreshStatus.Failed, null, null);

			Contract Contract;

			try
			{
				CancellationToken.ThrowIfCancellationRequested();
				Contract = await ServiceRef.XmppService.GetContract(ContractId);
			}
			catch (ItemNotFoundException)
			{
				await ServiceRef.ContractStore.MarkDeletedAsync(ContractId);
				return new ContractRefreshResult(ContractRefreshStatus.Deleted, null, await ServiceRef.ContractStore.GetAsync(ContractId));
			}
			catch (ForbiddenException)
			{
				return new ContractRefreshResult(ContractRefreshStatus.Forbidden, null, await ServiceRef.ContractStore.GetAsync(ContractId));
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ContractReference? Row = await ServiceRef.ContractStore.GetAsync(ContractId);

				if (IsConnectionError(Ex))
					return new ContractRefreshResult(ContractRefreshStatus.Offline, null, Row);

				ServiceRef.LogService.LogException(Ex);
				return new ContractRefreshResult(ContractRefreshStatus.Failed, null, Row);
			}

			if (Contract is null)
				return new ContractRefreshResult(ContractRefreshStatus.Failed, null, await ServiceRef.ContractStore.GetAsync(ContractId));

			if (Contract.State == ContractState.Deleted)
			{
				await ServiceRef.ContractStore.MarkDeletedAsync(ContractId);
				return new ContractRefreshResult(ContractRefreshStatus.Deleted, Contract, await ServiceRef.ContractStore.GetAsync(ContractId));
			}

			ContractReference? Existing = await ServiceRef.ContractStore.GetAsync(ContractId);
			bool Changed = await HasChangedAsync(Existing, Contract);
			bool Keep =
				Existing is not null ||
				IsCreator ||
				Contract.PartsMode == ContractParts.TemplateOnly ||
				IsStakeholder(Contract) ||
				(Origin == ContractOrigin.Event && await ServiceRef.XmppService.IsContractCreatorAsync(ContractId));

			if (!Keep)
				return new ContractRefreshResult(ContractRefreshStatus.Unchanged, Contract, null);

			ContractReference Stored = await ServiceRef.ContractStore.UpsertAsync(Contract, Origin, IsCreator);
			return new ContractRefreshResult(Changed ? ContractRefreshStatus.Updated : ContractRefreshStatus.Unchanged, Contract, Stored);
		}

		/// <summary>
		/// Runs the steps of a full sync.
		/// </summary>
		/// <param name="CancellationToken">Cancels the sync.</param>
		/// <returns>If the sync ran to the end. False if the connection was lost.</returns>
		private async Task<bool> RunSyncAsync(CancellationToken CancellationToken)
		{
			ContractsClient Client = ServiceRef.XmppService.ContractsClient;

			// Collect the IDs of contracts the user created or signed.

			string[] CreatedIds = await GetReferencesAsync(Client, true, CancellationToken);
			string[] SignedIds = await GetReferencesAsync(Client, false, CancellationToken);

			HashSet<string> Created = new(CreatedIds, StringComparer.OrdinalIgnoreCase);
			HashSet<string> Listed = new(StringComparer.OrdinalIgnoreCase);
			List<string> Ids = [];

			foreach (string Id in CreatedIds.Concat(SignedIds))
			{
				if (!string.IsNullOrEmpty(Id) && Listed.Add(Id))
					Ids.Add(Id);
			}

			Dictionary<string, ContractReference> Rows = new(StringComparer.OrdinalIgnoreCase);
			foreach (ContractReference Row in await ServiceRef.ContractStore.GetAllContractsAsync())
			{
				string? Id = Row.ContractId?.Value;
				if (!string.IsNullOrEmpty(Id))
					Rows.TryAdd(Id, Row);
			}

			// Fetch listed contracts in batches, and store the ones that changed.

			for (int Offset = 0; Offset < Ids.Count; Offset += BatchSize)
			{
				CancellationToken.ThrowIfCancellationRequested();

				string[] Batch = [.. Ids.Skip(Offset).Take(BatchSize)];
				HashSet<string> Returned = new(StringComparer.OrdinalIgnoreCase);

				ContractsEventArgs Result = await Client.GetContractsAsync(Batch);

				if (Result.Ok && Result.Contracts is not null)
				{
					foreach (Contract Contract in Result.Contracts)
					{
						if (Contract is null || string.IsNullOrEmpty(Contract.ContractId))
							continue;

						Returned.Add(Contract.ContractId);
						bool IsCreator = Created.Contains(Contract.ContractId);
						Rows.TryGetValue(Contract.ContractId, out ContractReference? Row);

						bool Changed =
							Row is null ||
							!Row.ContractLoaded ||
							Row.IsDeletedOnServer ||
							Row.State != Contract.State ||
							!SameTime(Row.Updated, Contract.Updated) ||
							(IsCreator && !Row.IsCreator);

						if (!Changed)
							await ServiceRef.ContractStore.RecomputeAsync(Contract.ContractId, IsCreator);
						else if (Contract.HasEncryptedParameters)
						{
							ContractRefreshResult Refreshed = await this.RefreshAsync(Contract.ContractId, ContractOrigin.Synced, IsCreator, CancellationToken);
							if (Refreshed.Status == ContractRefreshStatus.Offline)
								return false;
						}
						else
							await ServiceRef.ContractStore.UpsertAsync(Contract, ContractOrigin.Synced, IsCreator);
					}
				}
				else if (!ServiceRef.XmppService.IsOnline)
					return false;

				foreach (string Id in Batch)
				{
					if (Returned.Contains(Id))
						continue;

					ContractRefreshResult Refreshed = await this.RefreshAsync(Id, ContractOrigin.Synced, Created.Contains(Id), CancellationToken);
					if (Refreshed.Status == ContractRefreshStatus.Offline)
						return false;
				}
			}

			// Refresh stored contracts the server didn't list: open invitations, contracts the user takes part in without
			// having signed or created them, and contracts that may have been deleted.

			List<string> Others = [];
			foreach (KeyValuePair<string, ContractReference> P in Rows)
			{
				if (!Listed.Contains(P.Key) && !P.Value.IsTemplate && !P.Value.IsDeletedOnServer)
					Others.Add(P.Key);
			}

			for (int Offset = 0; Offset < Others.Count; Offset += MaxParallelRefreshes)
			{
				CancellationToken.ThrowIfCancellationRequested();

				Task<ContractRefreshResult>[] Tasks = [.. Others.Skip(Offset).Take(MaxParallelRefreshes)
					.Select(Id => this.RefreshAsync(Id, ContractOrigin.Synced, false, CancellationToken))];

				ContractRefreshResult[] Results = await Task.WhenAll(Tasks);
				if (Results.Any(R => R.Status == ContractRefreshStatus.Offline))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Pages through the server's list of created or signed contracts until a page is short.
		/// </summary>
		private static async Task<string[]> GetReferencesAsync(ContractsClient Client, bool CreatedList, CancellationToken CancellationToken)
		{
			List<string> Result = [];

			for (int Page = 0; Page < MaxReferencePages; Page++)
			{
				CancellationToken.ThrowIfCancellationRequested();

				int Offset = Page * ReferencePageSize;
				string[] Ids = CreatedList ?
					await Client.GetCreatedContractReferencesAsync(Offset, ReferencePageSize) :
					await Client.GetSignedContractReferencesAsync(Offset, ReferencePageSize);

				if (Ids is null)
					break;

				Result.AddRange(Ids);

				if (Ids.Length < ReferencePageSize)
					break;
			}

			return [.. Result];
		}

		/// <summary>
		/// Checks if the stored copy differs from the contract on the server.
		/// </summary>
		private static async Task<bool> HasChangedAsync(ContractReference? Existing, Contract Contract)
		{
			if (Existing is null || !Existing.ContractLoaded || Existing.IsDeletedOnServer)
				return true;

			if (Existing.State != Contract.State || !SameTime(Existing.Updated, Contract.Updated))
				return true;

			Contract? Stored = await Existing.GetContract();
			return (Stored?.ClientSignatures?.Length ?? 0) != (Contract.ClientSignatures?.Length ?? 0);
		}

		/// <summary>
		/// Checks if the user takes part in a contract: as a listed part, or as a signatory.
		/// </summary>
		private static bool IsStakeholder(Contract Contract)
		{
			string? LegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
			string? BareJid = ServiceRef.XmppService.BareJid;

			if (Contract.ClientSignatures is not null)
			{
				foreach (ClientSignature Signature in Contract.ClientSignatures)
				{
					if ((!string.IsNullOrEmpty(LegalId) && string.Equals(Signature.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)) ||
						(!string.IsNullOrEmpty(BareJid) && string.Equals(Signature.BareJid, BareJid, StringComparison.OrdinalIgnoreCase)))
					{
						return true;
					}
				}
			}

			if (!string.IsNullOrEmpty(LegalId) && Contract.Parts is not null)
			{
				foreach (Part Part in Contract.Parts)
				{
					if (string.Equals(Part.LegalId, LegalId, StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Releases the resources owned by the service.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases the resources owned by the service.
		/// </summary>
		/// <param name="Disposing">If managed resources should be released.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.disposed)
				return;

			if (Disposing)
				this.syncLock.Dispose();

			this.disposed = true;
		}

		private static bool SameTime(DateTime A, DateTime B)
		{
			if (A.Kind == DateTimeKind.Local)
				A = A.ToUniversalTime();

			if (B.Kind == DateTimeKind.Local)
				B = B.ToUniversalTime();

			return A.Ticks == B.Ticks;
		}

		/// <summary>
		/// Checks if an error was caused by the connection, rather than by the request.
		/// </summary>
		/// <param name="Error">The error.</param>
		/// <returns>If the error was caused by the connection.</returns>
		internal static bool IsConnectionError(Exception Error)
		{
			Exception Inner = Error;
			while (Inner is AggregateException Aggregate && Aggregate.InnerException is not null)
				Inner = Aggregate.InnerException;

			return Inner is TimeoutException || Inner is RemoteServerTimeoutException || !ServiceRef.XmppService.IsOnline;
		}
	}
}
