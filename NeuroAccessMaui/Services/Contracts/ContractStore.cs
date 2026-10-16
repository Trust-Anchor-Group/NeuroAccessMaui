using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using NeuroAccessMaui.Services.Contacts;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// The only writer of stored contracts (<see cref="ContractReference"/> rows).
	/// </summary>
	internal class ContractStore : IContractStore
	{
		private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// The only writer of stored contracts (<see cref="ContractReference"/> rows).
		/// </summary>
		public ContractStore()
		{
		}

		/// <inheritdoc/>
		public event EventHandlerAsync<ContractChangedEventArgs>? ContractChanged;

		/// <inheritdoc/>
		public async Task<ContractReference?> GetAsync(CaseInsensitiveString ContractId)
		{
			if (string.IsNullOrEmpty(ContractId?.Value))
				return null;

			return await Database.FindFirstIgnoreRest<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));
		}

		/// <inheritdoc/>
		public Task<ContractReference> UpsertAsync(Contract Contract, ContractOrigin Origin)
		{
			return this.UpsertAsync(Contract, Origin, false);
		}

		/// <inheritdoc/>
		public async Task<ContractReference> UpsertAsync(Contract Contract, ContractOrigin Origin, bool IsCreator)
		{
			CaseInsensitiveString ContractId = Contract.ContractId;
			ContractReference? Ref;
			bool IsNew;

			SemaphoreSlim Lock = this.GetLock(ContractId);
			await Lock.WaitAsync();
			try
			{
				Ref = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));
				IsNew = Ref is null;
				Ref ??= new ContractReference()
				{
					ContractId = ContractId
				};

				await Ref.SetContract(Contract);

				if (IsCreator || Origin == ContractOrigin.Created)
					Ref.IsCreator = true;

				if (Contract.State != ContractState.Deleted)
					Ref.IsDeletedOnServer = false;

				await ComputeDerivedFieldsAsync(Ref, Contract);

				switch (Origin)
				{
					case ContractOrigin.Created:
					case ContractOrigin.Signed:
					case ContractOrigin.Opened:
						MarkAsViewed(Ref);
						break;

					case ContractOrigin.Synced:
						if (Ref.LastViewed == DateTime.MinValue)
							Ref.LastViewed = Ref.LastChanged;
						break;
				}

				if (IsNew)
					await Database.Insert(Ref);
				else
					await Database.Update(Ref);
			}
			finally
			{
				Lock.Release();
			}

			ServiceRef.TagProfile.CheckContractReference(Ref);
			await this.RaiseAsync(new ContractChangedEventArgs(ContractId, IsNew ? ContractChangeKind.Added : ContractChangeKind.Updated, Ref, Contract));

			return Ref;
		}

		/// <inheritdoc/>
		public async Task<ContractReference> RecordInvitationAsync(ContractInvitation Invitation, Contract? Contract)
		{
			CaseInsensitiveString ContractId = Invitation.ContractId;
			ContractReference? Ref;
			bool IsNew;

			SemaphoreSlim Lock = this.GetLock(ContractId);
			await Lock.WaitAsync();
			try
			{
				Ref = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));
				IsNew = Ref is null;
				Ref ??= new ContractReference()
				{
					ContractId = ContractId,
					Created = Invitation.ReceivedAt,
					Updated = Invitation.ReceivedAt
				};

				if (Contract is not null)
					await Ref.SetContract(Contract);

				Ref.InvitedBy = Invitation.FromBareJid;
				Ref.InvitedRole = Invitation.Role;
				Ref.InvitationMessage = Invitation.Message;
				Ref.InvitedAt = Invitation.ReceivedAt;

				if (!Ref.ContractLoaded && !string.IsNullOrEmpty(Invitation.FromBareJid))
					Ref.Name = await ContactInfo.GetFriendlyName(Invitation.FromBareJid);

				await ComputeDerivedFieldsAsync(Ref, Contract ?? await Ref.GetContract());

				if (IsNew)
					await Database.Insert(Ref);
				else
					await Database.Update(Ref);
			}
			finally
			{
				Lock.Release();
			}

			ServiceRef.TagProfile.CheckContractReference(Ref);
			await this.RaiseAsync(new ContractChangedEventArgs(ContractId, IsNew ? ContractChangeKind.Added : ContractChangeKind.Updated, Ref, Contract));

			return Ref;
		}

		/// <inheritdoc/>
		public async Task MarkDeletedAsync(CaseInsensitiveString ContractId)
		{
			ContractReference? Kept = null;

			SemaphoreSlim Lock = this.GetLock(ContractId);
			await Lock.WaitAsync();
			try
			{
				ContractReference? Ref = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));

				if (Ref is not null)
				{
					if (Ref.HasSigned)
					{
						if (!Ref.IsDeletedOnServer)
						{
							Ref.IsDeletedOnServer = true;
							Ref.State = ContractState.Deleted;
							await ComputeDerivedFieldsAsync(Ref, await Ref.GetContract());

							DateTime UtcNow = DateTime.UtcNow;
							if (Ref.LastChanged < UtcNow)
								Ref.LastChanged = UtcNow;

							await Database.Update(Ref);
						}

						Kept = Ref;
					}
					else
						await Database.Delete(Ref);
				}
			}
			finally
			{
				Lock.Release();
			}

			await this.RaiseAsync(new ContractChangedEventArgs(ContractId, ContractChangeKind.Deleted, Kept, null));
		}

		/// <inheritdoc/>
		public async Task RemoveAsync(CaseInsensitiveString ContractId)
		{
			SemaphoreSlim Lock = this.GetLock(ContractId);
			await Lock.WaitAsync();
			try
			{
				await Database.FindDelete<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));
			}
			finally
			{
				Lock.Release();
			}

			await this.RaiseAsync(new ContractChangedEventArgs(ContractId, ContractChangeKind.Removed, null, null));
		}

		/// <inheritdoc/>
		public async Task MarkViewedAsync(CaseInsensitiveString ContractId)
		{
			ContractReference? Ref;

			SemaphoreSlim Lock = this.GetLock(ContractId);
			await Lock.WaitAsync();
			try
			{
				Ref = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));
				if (Ref is null || !Ref.IsUnread)
					return;

				MarkAsViewed(Ref);
				await Database.Update(Ref);
			}
			finally
			{
				Lock.Release();
			}

			await this.RaiseAsync(new ContractChangedEventArgs(ContractId, ContractChangeKind.Viewed, Ref, null));
		}

		/// <inheritdoc/>
		public async Task<bool> RecomputeAsync(CaseInsensitiveString ContractId, bool IsCreator)
		{
			ContractReference? Ref;

			SemaphoreSlim Lock = this.GetLock(ContractId);
			await Lock.WaitAsync();
			try
			{
				Ref = await Database.FindFirstDeleteRest<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));
				if (Ref is null)
					return false;

				string Before = DerivedFieldsSnapshot(Ref);

				if (IsCreator)
					Ref.IsCreator = true;

				await ComputeDerivedFieldsAsync(Ref, await Ref.GetContract());

				if (Ref.LastViewed == DateTime.MinValue)
					Ref.LastViewed = Ref.LastChanged;

				if (DerivedFieldsSnapshot(Ref) == Before)
					return false;

				await Database.Update(Ref);
			}
			finally
			{
				Lock.Release();
			}

			await this.RaiseAsync(new ContractChangedEventArgs(ContractId, ContractChangeKind.Updated, Ref, null));
			return true;
		}

		/// <inheritdoc/>
		public async Task<IReadOnlyList<ContractReference>> QueryAsync(ContractListQuery Query)
		{
			List<Filter> Filters =
			[
				new FilterFieldEqualTo("IsTemplate", Query.IsTemplate)
			];

			if (Query.Group.HasValue)
				Filters.Add(new FilterFieldEqualTo("ListGroup", Query.Group.Value));

			if (Query.IsTokenCreationTemplate.HasValue)
				Filters.Add(new FilterFieldEqualTo("IsTokenCreationTemplate", Query.IsTokenCreationTemplate.Value));

			if (Query.IsTemplate)
				Filters.Add(new FilterFieldEqualTo("ContractLoaded", true));

			string? SearchText = Query.SearchText?.Trim();
			if (!string.IsNullOrEmpty(SearchText))
				Filters.Add(new FilterFieldLikeRegEx("SearchText", ".*" + Regex.Escape(SearchText.ToLowerInvariant()) + ".*"));

			Filter Filter = Filters.Count == 1 ? Filters[0] : new FilterAnd([.. Filters]);

			IEnumerable<ContractReference> Rows = await Database.Find<ContractReference>(Query.Offset, Query.MaxCount, Filter, "-LastChanged");
			return [.. Rows];
		}

		/// <inheritdoc/>
		public async Task<IReadOnlyList<ContractReference>> GetAllContractsAsync()
		{
			IEnumerable<ContractReference> Rows = await Database.Find<ContractReference>(new FilterFieldEqualTo("IsTemplate", false));
			return [.. Rows];
		}

		/// <inheritdoc/>
		public Task RaiseSyncCompletedAsync()
		{
			return this.RaiseAsync(new ContractChangedEventArgs(null, ContractChangeKind.SyncCompleted, null, null));
		}

		private SemaphoreSlim GetLock(CaseInsensitiveString ContractId)
		{
			return this.locks.GetOrAdd(ContractId?.Value ?? string.Empty, _ => new SemaphoreSlim(1, 1));
		}

		private async Task RaiseAsync(ContractChangedEventArgs e)
		{
			try
			{
				await this.ContractChanged.Raise(this, e);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private static void MarkAsViewed(ContractReference Ref)
		{
			DateTime UtcNow = DateTime.UtcNow;
			Ref.LastViewed = Ref.LastChanged > UtcNow ? Ref.LastChanged : UtcNow;
		}

		/// <summary>
		/// Computes the fields that are derived from the contract and the user's identity.
		/// </summary>
		/// <param name="Ref">The row to update.</param>
		/// <param name="Contract">The contract, if the row has one.</param>
		private static async Task ComputeDerivedFieldsAsync(ContractReference Ref, Contract? Contract)
		{
			string? MyLegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
			string? MyBareJid = ServiceRef.XmppService.BareJid;
			SortedSet<string> MyLegalIds = new(StringComparer.OrdinalIgnoreCase);
			SortedSet<string> MyRoles = new(StringComparer.OrdinalIgnoreCase);
			bool HasSigned = false;
			DateTime LastChanged = Max(ToUtc(Ref.Created), ToUtc(Ref.Updated));

			if (Contract is not null)
			{
				if (Contract.ClientSignatures is not null)
				{
					foreach (ClientSignature Signature in Contract.ClientSignatures)
					{
						bool IsMine =
							(!string.IsNullOrEmpty(MyLegalId) && string.Equals(Signature.LegalId, MyLegalId, StringComparison.OrdinalIgnoreCase)) ||
							(!string.IsNullOrEmpty(MyBareJid) && string.Equals(Signature.BareJid, MyBareJid, StringComparison.OrdinalIgnoreCase));

						if (IsMine)
						{
							HasSigned = true;
							MyLegalIds.Add(Signature.LegalId);
							MyRoles.Add(Signature.Role);
						}

						LastChanged = Max(LastChanged, ToUtc(Signature.Timestamp));
					}
				}

				if (!string.IsNullOrEmpty(MyLegalId))
					MyLegalIds.Add(MyLegalId);

				if (Contract.Parts is not null)
				{
					foreach (Part Part in Contract.Parts)
					{
						if (MyLegalIds.Contains(Part.LegalId))
							MyRoles.Add(Part.Role);
					}
				}

				MyLegalIds.RemoveWhere(Id => !IsListed(Contract, Id));
			}

			if (Ref.InvitedAt.HasValue)
				LastChanged = Max(LastChanged, ToUtc(Ref.InvitedAt.Value));

			Ref.MyLegalIds = [.. MyLegalIds];
			Ref.MyRoles = [.. MyRoles];
			Ref.HasSigned = HasSigned;
			Ref.LastChanged = LastChanged;
			Ref.PartyNames = Contract is null ? Ref.Name : await GetPartyNamesAsync(Contract, MyLegalId, MyBareJid);
			Ref.SearchText = BuildSearchText(Ref);
			Ref.ListGroup = GetListGroup(Ref, Contract, MyLegalId, DateTime.UtcNow);
		}

		private static bool IsListed(Contract Contract, string LegalId)
		{
			if (Contract.ClientSignatures?.Any(Signature => string.Equals(Signature.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)) == true)
				return true;

			return Contract.Parts?.Any(Part => string.Equals(Part.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)) == true;
		}

		/// <summary>
		/// Gets the display names of the other parties: listed parts and signatories that aren't the user.
		/// </summary>
		private static async Task<string> GetPartyNamesAsync(Contract Contract, string? MyLegalId, string? MyBareJid)
		{
			List<string> LegalIds = [];

			if (Contract.Parts is not null)
			{
				foreach (Part Part in Contract.Parts)
					LegalIds.Add(Part.LegalId);
			}

			if (Contract.ClientSignatures is not null)
			{
				foreach (ClientSignature Signature in Contract.ClientSignatures)
				{
					if (!string.IsNullOrEmpty(MyBareJid) && string.Equals(Signature.BareJid, MyBareJid, StringComparison.OrdinalIgnoreCase))
						LegalIds.RemoveAll(Id => string.Equals(Id, Signature.LegalId, StringComparison.OrdinalIgnoreCase));
					else if (!LegalIds.Contains(Signature.LegalId, StringComparer.OrdinalIgnoreCase))
						LegalIds.Add(Signature.LegalId);
				}
			}

			StringBuilder? Names = null;
			HashSet<string> Seen = new(StringComparer.OrdinalIgnoreCase);

			foreach (string LegalId in LegalIds)
			{
				if (string.IsNullOrEmpty(LegalId) || string.Equals(LegalId, MyLegalId, StringComparison.OrdinalIgnoreCase))
					continue;

				string Name = await ContactInfo.GetFriendlyName(LegalId);
				if (!Seen.Add(Name))
					continue;

				if (Names is null)
					Names = new StringBuilder(Name);
				else
				{
					Names.Append(", ");
					Names.Append(Name);
				}
			}

			return Names?.ToString() ?? string.Empty;
		}

		private static string BuildSearchText(ContractReference Ref)
		{
			StringBuilder Text = new();

			foreach (string? Value in new string?[] { Ref.Category, Ref.Name, Ref.PartyNames, Ref.ContractId?.Value })
			{
				if (string.IsNullOrEmpty(Value))
					continue;

				if (Text.Length > 0)
					Text.Append(' ');

				Text.Append(Value);
			}

			return Text.ToString().ToLowerInvariant();
		}

		/// <summary>
		/// Decides the group a contract is listed under. The rules are applied in order: Ended, NeedsAction, Signed, InProgress.
		/// </summary>
		private static ContractListGroup GetListGroup(ContractReference Ref, Contract? Contract, string? MyLegalId, DateTime UtcNow)
		{
			if (Ref.IsDeletedOnServer)
				return ContractListGroup.Ended;

			if (!Ref.ContractLoaded || Contract is null)
				return Ref.HasOpenInvitation ? ContractListGroup.NeedsAction : ContractListGroup.InProgress;

			switch (Ref.State)
			{
				case ContractState.Obsoleted:
				case ContractState.Failed:
				case ContractState.Rejected:
				case ContractState.Deleted:
					return ContractListGroup.Ended;

				case ContractState.Signed:
					if (Contract.To != DateTime.MaxValue && ToUtc(Contract.To) < UtcNow)
						return ContractListGroup.Ended;
					break;

				default:
					if (Ref.SignBefore.HasValue && ToUtc(Ref.SignBefore.Value) < UtcNow)
						return ContractListGroup.Ended;
					break;
			}

			if (Ref.HasOpenInvitation)
				return ContractListGroup.NeedsAction;

			if (!Ref.HasSigned &&
				(Ref.State is ContractState.Approved or ContractState.BeingSigned) &&
				ContractActionRules.CanSign(Contract, MyLegalId, UtcNow))
			{
				return ContractListGroup.NeedsAction;
			}

			if (Ref.State == ContractState.Signed)
				return ContractListGroup.Signed;

			return ContractListGroup.InProgress;
		}

		private static string DerivedFieldsSnapshot(ContractReference Ref)
		{
			return string.Join('\u001f',
				Ref.IsCreator,
				Ref.HasSigned,
				Ref.ListGroup,
				Ref.LastChanged.Ticks,
				Ref.LastViewed.Ticks,
				Ref.PartyNames,
				Ref.SearchText,
				string.Join(',', Ref.MyLegalIds ?? []),
				string.Join(',', Ref.MyRoles ?? []));
		}

		private static DateTime ToUtc(DateTime Value)
		{
			return Value.Kind switch
			{
				DateTimeKind.Utc => Value,
				DateTimeKind.Local => Value.ToUniversalTime(),
				_ => DateTime.SpecifyKind(Value, DateTimeKind.Utc)
			};
		}

		private static DateTime Max(DateTime A, DateTime B)
		{
			return A > B ? A : B;
		}
	}
}
