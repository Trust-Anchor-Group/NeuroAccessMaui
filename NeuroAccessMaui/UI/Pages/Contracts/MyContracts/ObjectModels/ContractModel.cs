using System.ComponentModel;
using System.Text;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Controls;
using Waher.Content.Markdown;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.BlockElements;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// Defines the display priority of a contract summary in the local collection.
	/// </summary>
	internal enum ContractSummaryGroup
	{
		/// <summary>
		/// The current person has a locally derivable action to perform.
		/// </summary>
		NeedsAttention = 0,

		/// <summary>
		/// The agreement is progressing without a locally derivable user action.
		/// </summary>
		InProgress = 1,

		/// <summary>
		/// The agreement has completed successfully.
		/// </summary>
		Completed = 2,

		/// <summary>
		/// The agreement is a template, unavailable, or in another terminal state.
		/// </summary>
		Other = 3
	}

	/// <summary>
	/// Presents a locally derived, failure-tolerant summary of a saved contract reference.
	/// </summary>
	public class ContractModel : IUniqueItem, INotifyPropertyChanged
	{
		private readonly ContractReference contractRef;
		private readonly int additionalNotificationCount;
		private NotificationEvent[] events;
		private bool isOpening;

		/// <summary>
		/// Initializes a compatibility summary from persisted reference metadata.
		/// </summary>
		/// <param name="ContractRef">Persisted contract reference.</param>
		/// <param name="Events">Related notification events.</param>
		public ContractModel(ContractReference ContractRef, NotificationEvent[] Events)
			: this(
				ContractRef,
				Events,
				0,
				ContractSummaryGroup.Other,
				ContractRef.Name ?? string.Empty,
				ContractRef.Category ?? string.Empty,
				ContractRef.State.ToString(),
				StatusPillTone.Neutral,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				string.Empty,
				false,
				!string.IsNullOrWhiteSpace(ContractRef.ContractId),
				null,
				null,
				null,
				null)
		{
		}

		internal ContractModel(
			ContractReference ContractRef,
			NotificationEvent[] Events,
			int AdditionalNotificationCount,
			ContractSummaryGroup SummaryGroup,
			string Title,
			string Category,
			string StateText,
			StatusPillTone StateTone,
			string TemplateKindText,
			string RoleText,
			string PartiesText,
			string SignatureProgressText,
			string TimeContextText,
			string NextActionText,
			string RecoveryText,
			bool HasLocalContract,
			bool CanOpen,
			string? ProposalRole,
			string? ProposalMessage,
			string? ProposalFromJid,
			Contract? LocalContract)
		{
			this.contractRef = ContractRef;
			this.events = Events;
			this.additionalNotificationCount = AdditionalNotificationCount;
			this.SummaryGroup = SummaryGroup;
			this.Title = Title;
			this.Category = Category;
			this.StateText = StateText;
			this.StateTone = StateTone;
			this.TemplateKindText = TemplateKindText;
			this.RoleText = RoleText;
			this.PartiesText = PartiesText;
			this.SignatureProgressText = SignatureProgressText;
			this.TimeContextText = TimeContextText;
			this.NextActionText = NextActionText;
			this.RecoveryText = RecoveryText;
			this.HasLocalContract = HasLocalContract;
			this.CanOpen = CanOpen;
			this.ProposalRole = ProposalRole;
			this.ProposalMessage = ProposalMessage;
			this.ProposalFromJid = ProposalFromJid;
			this.LocalContract = LocalContract;
		}

		/// <summary>
		/// Derives the legacy persisted display name when a contract reference is first stored.
		/// </summary>
		/// <param name="Contract">Contract being persisted.</param>
		/// <returns>A friendly name based on the other locally known parties.</returns>
		public static async Task<string> GetName(Contract? Contract)
		{
			if (Contract?.Parts is null)
				return string.Empty;

			Dictionary<string, ClientSignature> Signatures = [];
			StringBuilder? Builder = null;

			foreach (ClientSignature Signature in Contract.ClientSignatures ?? [])
				Signatures[Signature.LegalId] = Signature;

			foreach (Part Part in Contract.Parts)
			{
				bool IsCurrentUser =
					Part.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id ||
					(Signatures.TryGetValue(Part.LegalId, out ClientSignature? Signature) &&
					 string.Equals(
						 Signature.BareJid,
						 ServiceRef.XmppService.BareJid,
						 StringComparison.OrdinalIgnoreCase));

				if (IsCurrentUser)
					continue;

				string FriendlyName = await ContactInfo.GetFriendlyName(Part.LegalId);
				if (Builder is null)
					Builder = new StringBuilder(FriendlyName);
				else
				{
					Builder.Append(", ");
					Builder.Append(FriendlyName);
				}
			}

			return Builder?.ToString() ?? string.Empty;
		}

		/// <summary>
		/// Derives the legacy persisted category from the localized contract heading.
		/// </summary>
		/// <param name="Contract">Contract being persisted.</param>
		/// <returns>The localized category, or <see langword="null"/> when no heading exists.</returns>
		public static async Task<string?> GetCategory(Contract Contract)
		{
			HumanReadableText[] Localizations = Contract.ForHumans;
			string Language = Contract.DeviceLanguage();

			foreach (HumanReadableText Localization in Localizations ?? [])
			{
				if (!string.Equals(
					Localization.Language,
					Language,
					StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				foreach (BlockElement Block in Localization.Body)
				{
					if (Block is not Section Section)
						continue;

					MarkdownOutput Markdown = new();
					foreach (InlineElement Item in Section.Header)
					{
						await Item.GenerateMarkdown(
							Markdown,
							1,
							0,
							new Waher.Networking.XMPP.Contracts.HumanReadable.MarkdownSettings(
								Contract,
								MarkdownType.ForRendering));
					}

					MarkdownDocument Document =
						await MarkdownDocument.CreateAsync(Markdown.ToString());
					return (await Document.GeneratePlainText()).Trim();
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the persisted contract identifier, or an empty string when the legacy reference has none.
		/// </summary>
		public string ContractId => Convert.ToString(this.contractRef.ContractId) ?? string.Empty;

		/// <inheritdoc/>
		public string UniqueName => !string.IsNullOrEmpty(this.ContractId)
			? this.ContractId
			: this.contractRef.ObjectId ?? string.Empty;

		/// <summary>
		/// Gets the contract identifier URI used for sharing.
		/// </summary>
		public string ContractIdUriString => string.IsNullOrEmpty(this.ContractId)
			? string.Empty
			: Constants.UriSchemes.IotSc + ":" + this.ContractId;

		/// <summary>
		/// Gets the best locally known contract timestamp.
		/// </summary>
		public DateTime Timestamp => this.contractRef.Updated != DateTime.MinValue
			? this.contractRef.Updated
			: this.contractRef.Created;

		/// <summary>
		/// Gets the persisted contract reference.
		/// </summary>
		public ContractReference ContractRef => this.contractRef;

		/// <summary>
		/// Gets the recognizable title derived from local contract data.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets the legacy display name.
		/// </summary>
		public string Name => this.contractRef.Name ?? string.Empty;

		/// <summary>
		/// Gets the title used by existing callers.
		/// </summary>
		public string NameOrCategory => this.Title;

		/// <summary>
		/// Gets the locally stored contract category.
		/// </summary>
		public string Category { get; }

		/// <summary>
		/// Gets whether a category is available.
		/// </summary>
		public bool HasCategory =>
			!string.IsNullOrWhiteSpace(this.Category) &&
			!string.Equals(
				this.Category,
				this.Title,
				StringComparison.CurrentCultureIgnoreCase);

		/// <summary>
		/// Gets the localized contract state.
		/// </summary>
		public string StateText { get; }

		/// <summary>
		/// Gets the semantic tone for the contract state.
		/// </summary>
		public StatusPillTone StateTone { get; }

		/// <summary>
		/// Gets the localized template-kind label.
		/// </summary>
		public string TemplateKindText { get; }

		/// <summary>
		/// Gets whether the template-kind label is available.
		/// </summary>
		public bool HasTemplateKind => !string.IsNullOrWhiteSpace(this.TemplateKindText);

		/// <summary>
		/// Gets the current person's locally derivable role summary.
		/// </summary>
		public string RoleText { get; }

		/// <summary>
		/// Gets whether a role summary is available.
		/// </summary>
		public bool HasRole => !string.IsNullOrWhiteSpace(this.RoleText);

		/// <summary>
		/// Gets the locally derivable party summary.
		/// </summary>
		public string PartiesText { get; }

		/// <summary>
		/// Gets whether a party summary is available.
		/// </summary>
		public bool HasParties => !string.IsNullOrWhiteSpace(this.PartiesText);

		/// <summary>
		/// Gets the locally derivable signature progress.
		/// </summary>
		public string SignatureProgressText { get; }

		/// <summary>
		/// Gets whether signature progress is available.
		/// </summary>
		public bool HasSignatureProgress => !string.IsNullOrWhiteSpace(this.SignatureProgressText);

		/// <summary>
		/// Gets the most relevant locally known updated or signing-deadline context.
		/// </summary>
		public string TimeContextText { get; }

		/// <summary>
		/// Gets whether time context is available.
		/// </summary>
		public bool HasTimeContext => !string.IsNullOrWhiteSpace(this.TimeContextText);

		/// <summary>
		/// Gets the next action that can be inferred without contacting the server.
		/// </summary>
		public string NextActionText { get; }

		/// <summary>
		/// Gets whether a next-action label is available.
		/// </summary>
		public bool HasNextAction => !string.IsNullOrWhiteSpace(this.NextActionText);

		/// <summary>
		/// Gets a safe recovery or freshness explanation for incomplete local data.
		/// </summary>
		public string RecoveryText { get; }

		/// <summary>
		/// Gets whether a recovery or freshness explanation is available.
		/// </summary>
		public bool HasRecoveryText => !string.IsNullOrWhiteSpace(this.RecoveryText);

		/// <summary>
		/// Gets whether this is a recovered reference whose contract details have not been downloaded yet.
		/// </summary>
		public bool NeedsDetailsDownload => !this.HasLocalContract && this.CanOpen;

		/// <summary>
		/// Gets whether the recovery text represents a warning about saved contract data.
		/// </summary>
		public bool HasRecoveryWarning => this.HasRecoveryText && !this.NeedsDetailsDownload;

		/// <summary>
		/// Gets whether the saved XML was parsed into a local contract summary.
		/// </summary>
		public bool HasLocalContract { get; }

		/// <summary>
		/// Gets whether the reference has enough identity information to be opened or refreshed.
		/// </summary>
		public bool CanOpen { get; }

		/// <summary>
		/// Gets or sets whether this contract is currently opening.
		/// </summary>
		public bool IsOpening
		{
			get => this.isOpening;
			internal set
			{
				if (this.isOpening == value)
					return;

				this.isOpening = value;
				this.OnPropertyChanged(nameof(this.IsOpening));
			}
		}

		/// <summary>
		/// Gets whether the contract has a locally derivable action for the current person.
		/// </summary>
		public bool IsNeedsAttention => this.SummaryGroup == ContractSummaryGroup.NeedsAttention;

		/// <summary>
		/// Gets the collection priority used to keep actionable agreements before passive states.
		/// </summary>
		internal ContractSummaryGroup SummaryGroup { get; }

		/// <summary>
		/// Gets the proposal role needed to preserve proposal-response navigation.
		/// </summary>
		internal string? ProposalRole { get; }

		/// <summary>
		/// Gets the proposal message needed to preserve proposal-response navigation.
		/// </summary>
		internal string? ProposalMessage { get; }

		/// <summary>
		/// Gets the proposal sender needed to preserve proposal-response navigation.
		/// </summary>
		internal string? ProposalFromJid { get; }

		/// <summary>
		/// Gets the parsed saved contract used to derive the summary.
		/// </summary>
		internal Contract? LocalContract { get; }

		/// <summary>
		/// Gets whether the contract has associated notification events.
		/// </summary>
		public bool HasEvents => this.NrEvents > 0;

		/// <summary>
		/// Gets the number of associated notification events.
		/// </summary>
		public int NrEvents
		{
			get
			{
				int LegacyCount = this.events?.Length ?? 0;
				return this.additionalNotificationCount > int.MaxValue - LegacyCount
					? int.MaxValue
					: LegacyCount + this.additionalNotificationCount;
			}
		}

		/// <summary>
		/// Gets the associated notification events.
		/// </summary>
		public NotificationEvent[] Events => this.events;

		/// <summary>
		/// Raises a property-change notification.
		/// </summary>
		/// <param name="PropertyName">Changed property name.</param>
		public void OnPropertyChanged(string PropertyName)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Replaces the notification events associated with the summary.
		/// </summary>
		/// <param name="Events">Updated notification events.</param>
		public void NotificationsUpdated(NotificationEvent[] Events)
		{
			this.events = Events;
			this.OnPropertyChanged(nameof(this.Events));
			this.OnPropertyChanged(nameof(this.HasEvents));
			this.OnPropertyChanged(nameof(this.NrEvents));
		}

		/// <summary>
		/// Adds a notification event if it is not already represented.
		/// </summary>
		/// <param name="Event">Notification event to add.</param>
		/// <returns><see langword="true"/> if the event was added.</returns>
		public bool AddEvent(NotificationEvent Event)
		{
			if (this.events is null)
				this.NotificationsUpdated([Event]);
			else
			{
				foreach (NotificationEvent ExistingEvent in this.events)
				{
					if (ExistingEvent.ObjectId == Event.ObjectId)
						return false;
				}

				NotificationEvent[] NewEvents = new NotificationEvent[this.events.Length + 1];
				Array.Copy(this.events, NewEvents, this.events.Length);
				NewEvents[^1] = Event;
				this.NotificationsUpdated(NewEvents);
			}

			return true;
		}

		/// <summary>
		/// Removes a represented notification event.
		/// </summary>
		/// <param name="Event">Notification event to remove.</param>
		/// <returns><see langword="true"/> if the event was found and removed.</returns>
		public bool RemoveEvent(NotificationEvent Event)
		{
			for (int i = 0; i < this.events.Length; i++)
			{
				if (this.events[i].ObjectId != Event.ObjectId)
					continue;

				NotificationEvent[] NewEvents = new NotificationEvent[this.events.Length - 1];
				if (i > 0)
					Array.Copy(this.events, 0, NewEvents, 0, i);

				if (i < this.events.Length - 1)
					Array.Copy(this.events, i + 1, NewEvents, i, this.events.Length - i - 1);

				this.NotificationsUpdated(NewEvents);
				return true;
			}

			return false;
		}
	}
}
