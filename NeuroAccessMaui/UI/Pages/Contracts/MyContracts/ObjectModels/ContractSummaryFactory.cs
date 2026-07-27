using System.Globalization;
using System.Text.Json;
using System.Xml;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Contracts;
using NeuroAccessMaui.UI.Controls;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// Creates contract-card summaries exclusively from persisted reference data and saved XML.
	/// </summary>
	internal static class ContractSummaryFactory
	{
		/// <summary>
		/// Creates a failure-tolerant contract summary without performing network requests.
		/// </summary>
		/// <param name="ContractRef">Persisted contract reference.</param>
		/// <param name="Events">Related legacy notification events.</param>
		/// <param name="NotificationRecords">Related current notification records.</param>
		/// <param name="Mode">Collection mode in which the reference is presented.</param>
		/// <returns>A locally derived contract summary.</returns>
		internal static async Task<ContractModel> CreateAsync(
			ContractReference ContractRef,
			NotificationEvent[] Events,
			NotificationRecord[] NotificationRecords,
			ContractsListMode Mode)
		{
			Contract? Contract = await ParseSavedContractAsync(ContractRef.ContractXml).ConfigureAwait(false);
			bool HasLocalContract = Contract is not null;
			bool CanOpen = !string.IsNullOrWhiteSpace(Convert.ToString(ContractRef.ContractId));
			ContractState State = Contract?.State ?? ContractRef.State;
			bool HasStoredState =
				Contract is not null ||
				ContractRef.ContractLoaded ||
				!string.IsNullOrWhiteSpace(ContractRef.ContractXml);
			string ParsedCategory = string.Empty;
			if (Contract is not null && string.IsNullOrWhiteSpace(ContractRef.Category))
			{
				try
				{
					ParsedCategory = await ContractModel.GetCategory(Contract).ConfigureAwait(false) ??
						string.Empty;
				}
				catch
				{
					ParsedCategory = string.Empty;
				}
			}

			string Category = FirstNonEmpty(ContractRef.Category, ParsedCategory);
			string ContractId = Convert.ToString(ContractRef.ContractId) ?? string.Empty;
			string Title = FirstNonEmpty(
				ContractRef.Name,
				Category,
				ShortenIdentifier(ContractId),
				ServiceRef.Localizer[nameof(AppResources.UntitledContract)]);

			(
				bool HasProposal,
				string ProposalRole,
				string? ProposalMessage,
				string? ProposalFromJid) = GetProposalContext(Events, NotificationRecords);
			string RoleName = Contract is null ? string.Empty : GetCurrentRole(Contract);
			if (string.IsNullOrWhiteSpace(RoleName))
				RoleName = ProposalRole;

			string RoleText = string.IsNullOrWhiteSpace(RoleName)
				? string.Empty
				: string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.ContractYourRoleFormat)],
					RoleName);

			int PartyCount = Contract is null ? 0 : GetPartyCount(Contract);
			string PartiesText = PartyCount switch
			{
				0 => string.Empty,
				1 => ServiceRef.Localizer[nameof(AppResources.ContractPartiesSingular)],
				_ => string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.ContractPartiesPluralFormat)],
					PartyCount)
			};

			string SignatureProgressText = Contract is null
				? string.Empty
				: GetSignatureProgress(Contract);
			string TimeContextText = GetTimeContext(ContractRef, Contract, State);
			string RecoveryText = GetRecoveryText(ContractRef, Contract, CanOpen);
			bool CanSign = Contract is not null &&
				CanCurrentUserSign(Contract, State, RoleName, HasProposal);
			bool ProposalStateIsActionable =
				!HasStoredState ||
				State is ContractState.Proposed or
					ContractState.Approved or
					ContractState.BeingSigned;
			bool HasActionableProposal = HasProposal && ProposalStateIsActionable;
			bool NeedsAttention =
				Mode == ContractsListMode.Contracts &&
				CanOpen &&
				(HasActionableProposal || CanSign);
			string NextActionText = GetNextAction(
				Contract,
				Mode,
				CanOpen,
				HasActionableProposal,
				CanSign);
			ContractSummaryGroup SummaryGroup = GetSummaryGroup(
				Mode,
				State,
				HasStoredState,
				NeedsAttention);

			return new ContractModel(
				ContractRef,
				Events,
				GetAdditionalNotificationCount(NotificationRecords),
				SummaryGroup,
				Title,
				Category,
				HasStoredState
					? GetStateText(State)
					: ServiceRef.Localizer[nameof(AppResources.Unavailable)],
				HasStoredState ? GetStateTone(State) : StatusPillTone.Neutral,
				GetTemplateKindText(Mode),
				RoleText,
				PartiesText,
				SignatureProgressText,
				TimeContextText,
				NextActionText,
				RecoveryText,
				HasLocalContract,
				CanOpen,
				HasActionableProposal ? ProposalRole : null,
				HasActionableProposal ? ProposalMessage : null,
				HasActionableProposal ? ProposalFromJid : null,
				Contract);
		}

		private static async Task<Contract?> ParseSavedContractAsync(string? ContractXml)
		{
			if (string.IsNullOrWhiteSpace(ContractXml))
				return null;

			try
			{
				XmlDocument Document = new()
				{
					PreserveWhitespace = true
				};
				Document.LoadXml(ContractXml);

				if (Document.DocumentElement is null)
					return null;

				// A null client is intentional: collection summaries must never turn local
				// parameter validation into an N+1 contract or schema request.
				ParsedContract? Parsed = await Contract.Parse(Document.DocumentElement, null!, false).ConfigureAwait(false);
				return Parsed?.Contract;
			}
			catch
			{
				// Remote contract content is untrusted and may be old or malformed. The
				// persisted reference remains recoverable and is summarized from metadata.
				return null;
			}
		}

		private static string GetCurrentRole(Contract Contract)
		{
			HashSet<string> Roles = new(StringComparer.OrdinalIgnoreCase);
			string LegalId = ServiceRef.TagProfile.LegalIdentity?.Id ?? string.Empty;
			string BareJid = ServiceRef.XmppService.BareJid ?? string.Empty;

			foreach (Part Part in Contract.Parts ?? [])
			{
				if (!string.IsNullOrWhiteSpace(LegalId) &&
					string.Equals(Part.LegalId, LegalId, StringComparison.OrdinalIgnoreCase) &&
					!string.IsNullOrWhiteSpace(Part.Role))
				{
					Roles.Add(Part.Role);
				}
			}

			foreach (ClientSignature Signature in Contract.ClientSignatures ?? [])
			{
				bool IsCurrentSigner =
					(!string.IsNullOrWhiteSpace(LegalId) &&
					 string.Equals(Signature.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)) ||
					(!string.IsNullOrWhiteSpace(BareJid) &&
					 string.Equals(Signature.BareJid, BareJid, StringComparison.OrdinalIgnoreCase));

				if (IsCurrentSigner && !string.IsNullOrWhiteSpace(Signature.Role))
					Roles.Add(Signature.Role);
			}

			return string.Join(", ", Roles.OrderBy(Role => Role, StringComparer.CurrentCultureIgnoreCase));
		}

		private static int GetPartyCount(Contract Contract)
		{
			HashSet<string> Parties = new(StringComparer.OrdinalIgnoreCase);

			foreach (Part Part in Contract.Parts ?? [])
			{
				if (!string.IsNullOrWhiteSpace(Part.LegalId))
					Parties.Add(Part.LegalId);
			}

			foreach (ClientSignature Signature in Contract.ClientSignatures ?? [])
			{
				if (!string.IsNullOrWhiteSpace(Signature.LegalId))
					Parties.Add(Signature.LegalId);
			}

			return Parties.Count;
		}

		private static string GetSignatureProgress(Contract Contract)
		{
			int RequiredSignatures = 0;
			foreach (Role Role in Contract.Roles ?? [])
				RequiredSignatures += Math.Max(0, Role.MinCount);

			if (RequiredSignatures == 0)
				return string.Empty;

			int SignedCount = Math.Min(Contract.ClientSignatures?.Length ?? 0, RequiredSignatures);
			return string.Format(
				CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.SignatureProgressFormat)],
				SignedCount,
				RequiredSignatures);
		}

		private static string GetTimeContext(
			ContractReference ContractRef,
			Contract? Contract,
			ContractState State)
		{
			if (Contract?.SignBefore is DateTime SignBefore &&
				State is ContractState.Approved or ContractState.BeingSigned)
			{
				return string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.ContractSignByFormat)],
					SignBefore.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
			}

			DateTime Updated = Contract?.Updated ?? ContractRef.Updated;
			if (Updated == DateTime.MinValue)
				Updated = Contract?.Created ?? ContractRef.Created;

			if (Updated == DateTime.MinValue)
				return string.Empty;

			return string.Format(
				CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.LastUpdatedFormat)],
				Updated.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
		}

		private static string GetRecoveryText(
			ContractReference ContractRef,
			Contract? Contract,
			bool CanOpen)
		{
			if (Contract is null)
			{
				return CanOpen
					? ServiceRef.Localizer[nameof(AppResources.ContractSavedDetailsUnavailable)]
					: ServiceRef.Localizer[nameof(AppResources.ContractReferenceUnavailable)];
			}

			bool MetadataIsNewerThanXml =
				ContractRef.Updated != DateTime.MinValue &&
				Contract.Updated != DateTime.MinValue &&
				ContractRef.Updated > Contract.Updated;
			bool LoadedBeforeKnownUpdate =
				ContractRef.Loaded != DateTime.MinValue &&
				ContractRef.Updated != DateTime.MinValue &&
				ContractRef.Loaded < ContractRef.Updated;
			bool FreshnessTimestampMissing =
				ContractRef.Loaded == DateTime.MinValue &&
				ContractRef.Updated != DateTime.MinValue;

			return MetadataIsNewerThanXml || LoadedBeforeKnownUpdate || FreshnessTimestampMissing
				? ServiceRef.Localizer[nameof(AppResources.SavedContractMayBeOutOfDate)]
				: string.Empty;
		}

		private static string GetNextAction(
			Contract? Contract,
			ContractsListMode Mode,
			bool CanOpen,
			bool HasProposal,
			bool CanSign)
		{
			if (!CanOpen)
				return ServiceRef.Localizer[nameof(AppResources.Unavailable)];

			if (Mode is ContractsListMode.ContractTemplates or ContractsListMode.TokenCreationTemplates)
				return ServiceRef.Localizer[nameof(AppResources.Create)];

			if (HasProposal)
				return ServiceRef.Localizer[nameof(AppResources.RespondToProposal)];

			if (Contract is null)
				return ServiceRef.Localizer[nameof(AppResources.Refresh)];

			if (CanSign)
				return ServiceRef.Localizer[nameof(AppResources.ReviewAndSign)];

			return ServiceRef.Localizer[nameof(AppResources.ViewContract)];
		}

		private static bool CanCurrentUserSign(
			Contract Contract,
			ContractState State,
			string RoleName,
			bool HasProposal)
		{
			if (State is not (ContractState.Approved or ContractState.BeingSigned) ||
				HasCurrentSignature(Contract))
			{
				return false;
			}

			if (HasProposal && !string.IsNullOrWhiteSpace(RoleName))
				return true;

			if (Contract.PartsMode == ContractParts.Open)
			{
				foreach (Role Role in Contract.Roles ?? [])
				{
					int CurrentParts = (Contract.Parts ?? []).Count(Part =>
						string.Equals(Part.Role, Role.Name, StringComparison.OrdinalIgnoreCase));
					if (CurrentParts < Role.MaxCount)
						return true;
				}
			}

			return !string.IsNullOrWhiteSpace(RoleName);
		}

		private static bool HasCurrentSignature(Contract Contract)
		{
			string LegalId = ServiceRef.TagProfile.LegalIdentity?.Id ?? string.Empty;
			string BareJid = ServiceRef.XmppService.BareJid ?? string.Empty;

			return (Contract.ClientSignatures ?? []).Any(Signature =>
				(!string.IsNullOrWhiteSpace(LegalId) &&
				 string.Equals(Signature.LegalId, LegalId, StringComparison.OrdinalIgnoreCase)) ||
				(!string.IsNullOrWhiteSpace(BareJid) &&
				 string.Equals(Signature.BareJid, BareJid, StringComparison.OrdinalIgnoreCase)));
		}

		private static (
			bool HasProposal,
			string ProposalRole,
			string? ProposalMessage,
			string? ProposalFromJid) GetProposalContext(
				NotificationEvent[] Events,
				NotificationRecord[] NotificationRecords)
		{
			ContractProposalNotificationEvent? LegacyProposal = Events
				.OfType<ContractProposalNotificationEvent>()
				.OrderByDescending(Event => Event.Received)
				.FirstOrDefault();

			bool HasProposal = LegacyProposal is not null;
			string ProposalRole = LegacyProposal?.Role ?? string.Empty;
			string? ProposalMessage = LegacyProposal?.Message;
			string? ProposalFromJid = LegacyProposal?.FromJID;

			foreach (NotificationRecord Record in NotificationRecords)
			{
				if (!TryGetProposalExtras(
					Record.ExtrasJson,
					out string RecordRole,
					out string? RecordFromJid))
				{
					continue;
				}

				HasProposal = true;
				if (string.IsNullOrWhiteSpace(ProposalRole))
					ProposalRole = RecordRole;

				if (string.IsNullOrWhiteSpace(ProposalFromJid))
					ProposalFromJid = RecordFromJid;

				break;
			}

			return (HasProposal, ProposalRole, ProposalMessage, ProposalFromJid);
		}

		private static bool TryGetProposalExtras(
			string? ExtrasJson,
			out string Role,
			out string? FromJid)
		{
			Role = string.Empty;
			FromJid = null;
			if (string.IsNullOrWhiteSpace(ExtrasJson))
				return false;

			try
			{
				using JsonDocument Document = JsonDocument.Parse(ExtrasJson);
				JsonElement Root = Document.RootElement;
				bool HasRole = Root.TryGetProperty("role", out JsonElement RoleElement);
				bool HasFromJid = Root.TryGetProperty("fromJid", out JsonElement FromJidElement);
				if (!HasRole && !HasFromJid)
					return false;

				Role = HasRole ? RoleElement.GetString() ?? string.Empty : string.Empty;
				FromJid = HasFromJid ? FromJidElement.GetString() : null;
				return true;
			}
			catch (Exception Ex) when (Ex is JsonException or InvalidOperationException)
			{
				return false;
			}
		}

		private static int GetAdditionalNotificationCount(NotificationRecord[] NotificationRecords)
		{
			int Count = 0;
			foreach (NotificationRecord Record in NotificationRecords)
			{
				int Occurrences = Math.Max(1, Record.OccurrenceCount);
				Count = Count > int.MaxValue - Occurrences
					? int.MaxValue
					: Count + Occurrences;
			}

			return Count;
		}

		private static ContractSummaryGroup GetSummaryGroup(
			ContractsListMode Mode,
			ContractState State,
			bool HasStoredState,
			bool NeedsAttention)
		{
			if (NeedsAttention)
				return ContractSummaryGroup.NeedsAttention;

			if (Mode != ContractsListMode.Contracts || !HasStoredState)
				return ContractSummaryGroup.Other;

			return State switch
			{
				ContractState.Proposed or
				ContractState.Approved or
				ContractState.BeingSigned => ContractSummaryGroup.InProgress,
				ContractState.Signed => ContractSummaryGroup.Completed,
				_ => ContractSummaryGroup.Other
			};
		}

		private static string GetTemplateKindText(ContractsListMode Mode)
		{
			return Mode switch
			{
				ContractsListMode.ContractTemplates =>
					ServiceRef.Localizer[nameof(AppResources.ContractTemplates)],
				ContractsListMode.TokenCreationTemplates =>
					ServiceRef.Localizer[nameof(AppResources.TokenCreationTemplates)],
				_ => string.Empty
			};
		}

		private static string GetStateText(ContractState State)
		{
			return State switch
			{
				ContractState.Proposed => ServiceRef.Localizer[nameof(AppResources.Proposed)],
				ContractState.Rejected => ServiceRef.Localizer[nameof(AppResources.Rejected)],
				ContractState.Approved => ServiceRef.Localizer[nameof(AppResources.Approved)],
				ContractState.BeingSigned => ServiceRef.Localizer[nameof(AppResources.BeingSigned)],
				ContractState.Signed => ServiceRef.Localizer[nameof(AppResources.Signed)],
				ContractState.Failed => ServiceRef.Localizer[nameof(AppResources.Failed)],
				ContractState.Obsoleted => ServiceRef.Localizer[nameof(AppResources.Obsoleted)],
				ContractState.Deleted => ServiceRef.Localizer[nameof(AppResources.Deleted)],
				_ => ServiceRef.Localizer[nameof(AppResources.Unavailable)]
			};
		}

		private static StatusPillTone GetStateTone(ContractState State)
		{
			return State switch
			{
				ContractState.Proposed => StatusPillTone.Information,
				ContractState.Approved => StatusPillTone.Information,
				ContractState.BeingSigned => StatusPillTone.Warning,
				ContractState.Signed => StatusPillTone.Success,
				ContractState.Rejected => StatusPillTone.Danger,
				ContractState.Failed => StatusPillTone.Danger,
				ContractState.Obsoleted => StatusPillTone.Neutral,
				ContractState.Deleted => StatusPillTone.Neutral,
				_ => StatusPillTone.Neutral
			};
		}

		private static string FirstNonEmpty(params string?[] Values)
		{
			foreach (string? Value in Values)
			{
				if (!string.IsNullOrWhiteSpace(Value))
					return Value.Trim();
			}

			return string.Empty;
		}

		private static string ShortenIdentifier(string ContractId)
		{
			if (string.IsNullOrWhiteSpace(ContractId))
				return string.Empty;

			const int VisibleCharacters = 8;
			return ContractId.Length <= VisibleCharacters * 2 + 1
				? ContractId
				: ContractId[..VisibleCharacters] + "…" + ContractId[^VisibleCharacters..];
		}
	}
}
