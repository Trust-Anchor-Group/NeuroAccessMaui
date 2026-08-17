using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroFeatures.Events;
using System.Globalization;
using System.Text;
using System.Xml;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails.ObjectModels
{
	/// <summary>
	/// Represents one normalized, page-ready entry in a token activity timeline.
	/// </summary>
	public sealed class TokenActivityItem
	{
		private const int maximumPreviewLength = 480;
		private const long maximumXmlCharacters = 1024 * 1024;

		private TokenActivityItem(
			TokenEvent Event,
			string Title,
			StatusPillTone StatusTone,
			string ActorLabel,
			string ActorId,
			string ActorName,
			string OwnerId,
			string OwnerName,
			string ValueText,
			string RelatedContractId,
			string Source,
			string ContentLabel,
			string ContentPreview,
			string CompleteContent,
			string RawDetails)
		{
			this.Event = Event;
			this.Title = Title;
			this.StatusTone = StatusTone;
			this.Timestamp = Event.Timestamp;
			this.TimestampText = FormatDate(Event.Timestamp);
			this.ActorLabel = ActorLabel;
			this.ActorId = ActorId;
			this.ActorName = ActorName;
			this.OwnerId = OwnerId;
			this.OwnerName = OwnerName;
			this.ValueText = ValueText;
			this.RelatedContractId = RelatedContractId;
			this.RelatedContractDisplayId = BuildShortIdentifier(RelatedContractId);
			this.Source = Source;
			this.ContentLabel = ContentLabel;
			this.ContentPreview = ContentPreview;
			this.CompleteContent = CompleteContent;
			this.RawDetails = RawDetails;
			this.PrivacyText = Event.Personal
				? ServiceRef.Localizer[nameof(AppResources.PersonalUpdate)]
				: string.Empty;

			List<string> SummaryParts = [];
			if (this.HasTimestamp)
				SummaryParts.Add(this.TimestampText);
			if (this.HasActor)
			{
				SummaryParts.Add(string.IsNullOrWhiteSpace(this.ActorLabel)
					? this.ActorName
					: string.Concat(this.ActorLabel, ": ", this.ActorName));
			}
			else if (this.HasOwner)
			{
				SummaryParts.Add(string.Concat(
					ServiceRef.Localizer[nameof(AppResources.Owner)],
					": ",
					this.OwnerName));
			}
			if (this.HasValue)
				SummaryParts.Add(this.ValueText);
			if (this.IsPersonal)
				SummaryParts.Add(this.PrivacyText);

			this.SummaryText = string.Join(" · ", SummaryParts);

			if (!string.IsNullOrEmpty(this.ActorId))
				this.OpenActorCommand = new AsyncRelayCommand(() => OpenIdentityAsync(this.ActorId));

			if (!string.IsNullOrEmpty(this.OwnerId))
				this.OpenOwnerCommand = new AsyncRelayCommand(() => OpenIdentityAsync(this.OwnerId));

			if (!string.IsNullOrEmpty(this.RelatedContractId))
			{
				this.OpenRelatedContractCommand = new AsyncRelayCommand(
					() => OpenContractAsync(this.RelatedContractId));
			}

			if (!string.IsNullOrEmpty(this.Source))
				this.OpenSourceCommand = new AsyncRelayCommand(() => OpenSourceAsync(this.Source));

			if (!string.IsNullOrEmpty(this.CompleteContent))
			{
				this.CopyContentCommand = new AsyncRelayCommand(
					() => CopyToClipboardAsync(this.CompleteContent));
			}

			if (!string.IsNullOrEmpty(this.RawDetails))
			{
				this.CopyRawDetailsCommand = new AsyncRelayCommand(
					() => CopyToClipboardAsync(this.RawDetails));
			}
		}

		/// <summary>
		/// Gets the original event retained for normalized activity behavior and future actions.
		/// </summary>
		public TokenEvent Event { get; }

		/// <summary>
		/// Gets the localized, human-readable activity title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets the theme-driven semantic tone for the activity.
		/// </summary>
		public StatusPillTone StatusTone { get; }

		/// <summary>
		/// Gets the event timestamp used for deterministic ordering.
		/// </summary>
		public DateTime Timestamp { get; }

		/// <summary>
		/// Gets the event timestamp formatted for the current culture.
		/// </summary>
		public string TimestampText { get; }

		/// <summary>
		/// Gets the compact metadata summary shown while the activity entry is collapsed.
		/// </summary>
		public string SummaryText { get; }

		/// <summary>
		/// Gets a value indicating whether a meaningful timestamp is available.
		/// </summary>
		public bool HasTimestamp => HasMeaningfulDate(this.Timestamp);

		/// <summary>
		/// Gets the localized label describing the activity actor.
		/// </summary>
		public string ActorLabel { get; }

		/// <summary>
		/// Gets the legal identity of the activity actor, when available.
		/// </summary>
		public string ActorId { get; }

		/// <summary>
		/// Gets the friendly activity actor name.
		/// </summary>
		public string ActorName { get; }

		/// <summary>
		/// Gets a value indicating whether activity actor information is available.
		/// </summary>
		public bool HasActor => !string.IsNullOrEmpty(this.ActorName);

		/// <summary>
		/// Gets a value indicating whether the activity actor identity can be opened.
		/// </summary>
		public bool CanOpenActor => this.OpenActorCommand is not null;

		/// <summary>
		/// Gets the legal identity of the owner after the activity, when available.
		/// </summary>
		public string OwnerId { get; }

		/// <summary>
		/// Gets the friendly name of the owner after the activity.
		/// </summary>
		public string OwnerName { get; }

		/// <summary>
		/// Gets a value indicating whether owner information is available.
		/// </summary>
		public bool HasOwner => !string.IsNullOrEmpty(this.OwnerName);

		/// <summary>
		/// Gets a value indicating whether the owner identity can be opened.
		/// </summary>
		public bool CanOpenOwner => this.OpenOwnerCommand is not null;

		/// <summary>
		/// Gets the culture-aware value and currency summary.
		/// </summary>
		public string ValueText { get; }

		/// <summary>
		/// Gets a value indicating whether value information is available.
		/// </summary>
		public bool HasValue => !string.IsNullOrEmpty(this.ValueText);

		/// <summary>
		/// Gets the complete related ownership contract identifier.
		/// </summary>
		public string RelatedContractId { get; }

		/// <summary>
		/// Gets a shortened related contract identifier for display.
		/// </summary>
		public string RelatedContractDisplayId { get; }

		/// <summary>
		/// Gets a value indicating whether a related contract is available.
		/// </summary>
		public bool HasRelatedContract => !string.IsNullOrEmpty(this.RelatedContractId);

		/// <summary>
		/// Gets the external source of the activity, when available.
		/// </summary>
		public string Source { get; }

		/// <summary>
		/// Gets a value indicating whether an external source is available.
		/// </summary>
		public bool HasSource => !string.IsNullOrEmpty(this.Source);

		/// <summary>
		/// Gets the localized content kind.
		/// </summary>
		public string ContentLabel { get; }

		/// <summary>
		/// Gets a safe, bounded plain-text preview of event content.
		/// </summary>
		public string ContentPreview { get; }

		/// <summary>
		/// Gets the complete event content used only by the explicit copy action.
		/// </summary>
		public string CompleteContent { get; }

		/// <summary>
		/// Gets a value indicating whether event content is available.
		/// </summary>
		public bool HasContent => !string.IsNullOrEmpty(this.ContentPreview);

		/// <summary>
		/// Gets a value indicating whether complete event content can be copied from advanced details.
		/// </summary>
		public bool HasCompleteContent => !string.IsNullOrEmpty(this.CompleteContent);

		/// <summary>
		/// Gets the localized privacy label for a personal activity.
		/// </summary>
		public string PrivacyText { get; }

		/// <summary>
		/// Gets a value indicating whether this activity is personal to the current owner.
		/// </summary>
		public bool IsPersonal => this.Event.Personal;

		/// <summary>
		/// Gets safe protocol metadata for progressive disclosure.
		/// </summary>
		public string RawDetails { get; }

		/// <summary>
		/// Gets a value indicating whether safe protocol metadata is available.
		/// </summary>
		public bool HasRawDetails => !string.IsNullOrEmpty(this.RawDetails);

		/// <summary>
		/// Gets the command that opens the activity actor identity.
		/// </summary>
		public IAsyncRelayCommand? OpenActorCommand { get; }

		/// <summary>
		/// Gets the command that opens the owner identity.
		/// </summary>
		public IAsyncRelayCommand? OpenOwnerCommand { get; }

		/// <summary>
		/// Gets the command that opens the related contract.
		/// </summary>
		public IAsyncRelayCommand? OpenRelatedContractCommand { get; }

		/// <summary>
		/// Gets the command that opens the external activity source.
		/// </summary>
		public IAsyncRelayCommand? OpenSourceCommand { get; }

		/// <summary>
		/// Gets the command that copies complete event content.
		/// </summary>
		public IAsyncRelayCommand? CopyContentCommand { get; }

		/// <summary>
		/// Gets the command that copies safe protocol metadata.
		/// </summary>
		public IAsyncRelayCommand? CopyRawDetailsCommand { get; }

		/// <summary>
		/// Normalizes a protocol event into a human-readable activity item.
		/// </summary>
		/// <param name="Event">Protocol event to normalize.</param>
		/// <returns>A task that resolves friendly identity names and returns the activity item.</returns>
		public static async Task<TokenActivityItem> CreateAsync(TokenEvent Event)
		{
			ArgumentNullException.ThrowIfNull(Event);

			string Title;
			StatusPillTone StatusTone;
			string ActorLabel = string.Empty;
			string ActorId = string.Empty;
			string ActorFallback = string.Empty;
			string Source = string.Empty;
			string ContentLabel = string.Empty;
			string CompleteContent = string.Empty;
			bool IsXmlContent = false;

			switch (Event)
			{
				case Created Created:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityCreated)];
					StatusTone = StatusPillTone.Success;
					ActorLabel = ServiceRef.Localizer[nameof(AppResources.Creator)];
					ActorId = Normalize(Created.Creator);
					break;

				case Transferred Transferred:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityTransferred)];
					StatusTone = StatusPillTone.Information;
					ActorLabel = ServiceRef.Localizer[nameof(AppResources.From)];
					ActorId = Normalize(Transferred.Seller);
					break;

				case Donated Donated:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityDonated)];
					StatusTone = StatusPillTone.Information;
					ActorLabel = ServiceRef.Localizer[nameof(AppResources.From)];
					ActorId = Normalize(Donated.Donor);
					break;

				case Destroyed:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityDestroyed)];
					StatusTone = StatusPillTone.Danger;
					break;

				case Killed Killed:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityKilled)];
					StatusTone = StatusPillTone.Danger;
					ActorLabel = ServiceRef.Localizer[nameof(AppResources.KillerUser)];
					ActorId = Normalize(Killed.LegalId);
					ActorFallback = Normalize(Killed.User);
					break;

				case ExternalNoteText ExternalNoteText:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityExternalUpdate)];
					StatusTone = StatusPillTone.Information;
					Source = Normalize(ExternalNoteText.Source);
					ContentLabel = ServiceRef.Localizer[nameof(AppResources.TextUpdate)];
					CompleteContent = NormalizeContent(ExternalNoteText.Note);
					break;

				case ExternalNoteXml ExternalNoteXml:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityExternalUpdate)];
					StatusTone = StatusPillTone.Information;
					Source = Normalize(ExternalNoteXml.Source);
					ContentLabel = ServiceRef.Localizer[nameof(AppResources.XmlUpdate)];
					CompleteContent = NormalizeContent(ExternalNoteXml.Note);
					IsXmlContent = true;
					break;

				case NoteText NoteText:
					Title = Event.Personal
						? ServiceRef.Localizer[nameof(AppResources.TokenActivityPersonalUpdate)]
						: ServiceRef.Localizer[nameof(AppResources.TextUpdate)];
					StatusTone = Event.Personal
						? StatusPillTone.Information
						: StatusPillTone.Neutral;
					ContentLabel = ServiceRef.Localizer[nameof(AppResources.TextUpdate)];
					CompleteContent = NormalizeContent(NoteText.Note);
					break;

				case NoteXml NoteXml:
					Title = Event.Personal
						? ServiceRef.Localizer[nameof(AppResources.TokenActivityPersonalUpdate)]
						: ServiceRef.Localizer[nameof(AppResources.XmlUpdate)];
					StatusTone = Event.Personal
						? StatusPillTone.Information
						: StatusPillTone.Neutral;
					ContentLabel = ServiceRef.Localizer[nameof(AppResources.XmlUpdate)];
					CompleteContent = NormalizeContent(NoteXml.Note);
					IsXmlContent = true;
					break;

				default:
					Title = ServiceRef.Localizer[nameof(AppResources.TokenActivityUnsupportedEvent)];
					StatusTone = StatusPillTone.Warning;
					break;
			}

			TokenOwnershipEvent? OwnershipEvent = Event as TokenOwnershipEvent;
			string OwnerId = Normalize(OwnershipEvent?.Owner);
			string OwnerName = await ResolveFriendlyNameAsync(OwnerId, string.Empty);
			string ActorName = await ResolveFriendlyNameAsync(ActorId, ActorFallback);
			string ValueText = Event is TokenValueEvent ValueEvent &&
				(ValueEvent.Value != 0 || !string.IsNullOrWhiteSpace(ValueEvent.Currency))
					? FormatValue(ValueEvent.Value, ValueEvent.Currency)
					: string.Empty;
			string RelatedContractId = Normalize(OwnershipEvent?.OwnershipContract);
			string ContentPreview = IsXmlContent
				? BuildXmlPreview(CompleteContent)
				: BuildPreview(CompleteContent);
			string RawDetails = BuildRawDetails(Event);

			return new TokenActivityItem(
				Event,
				Title,
				StatusTone,
				ActorLabel,
				ActorId,
				ActorName,
				OwnerId,
				OwnerName,
				ValueText,
				RelatedContractId,
				Source,
				ContentLabel,
				ContentPreview,
				CompleteContent,
				RawDetails);
		}

		private static async Task<string> ResolveFriendlyNameAsync(string Id, string Fallback)
		{
			if (string.IsNullOrEmpty(Id))
			{
				return string.IsNullOrEmpty(Fallback)
					? string.Empty
					: Fallback;
			}

			try
			{
				string? FriendlyName = await ContactInfo.GetFriendlyName(Id);
				return string.IsNullOrWhiteSpace(FriendlyName)
					? FirstNonEmpty(Fallback, BuildShortIdentifier(Id))
					: FriendlyName.Trim();
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token activity identity lookup failed.", ex);
				return FirstNonEmpty(Fallback, BuildShortIdentifier(Id));
			}
		}

		private static string FormatValue(decimal Value, string? Currency)
		{
			string Amount = Value.ToString("0.############################", CultureInfo.CurrentCulture);
			string NormalizedCurrency = Normalize(Currency);

			return string.IsNullOrEmpty(NormalizedCurrency)
				? Amount
				: string.Concat(Amount, " ", NormalizedCurrency);
		}

		private static string BuildXmlPreview(string Xml)
		{
			if (string.IsNullOrEmpty(Xml))
				return string.Empty;

			try
			{
				XmlReaderSettings Settings = new()
				{
					DtdProcessing = DtdProcessing.Prohibit,
					IgnoreComments = true,
					IgnoreProcessingInstructions = true,
					MaxCharactersInDocument = maximumXmlCharacters,
					XmlResolver = null
				};
				StringBuilder Text = new();

				using StringReader Input = new(string.Concat("<root>", Xml, "</root>"));
				using XmlReader Reader = XmlReader.Create(Input, Settings);
				while (Reader.Read())
				{
					if (Reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.SignificantWhitespace)
						Text.Append(Reader.Value).Append(' ');
				}

				string Preview = BuildPreview(Text.ToString());
				return string.IsNullOrEmpty(Preview)
					? BuildPreview(Xml)
					: Preview;
			}
			catch (XmlException)
			{
				// Malformed XML remains plain text so it can be inspected without being rendered as markup.
				return BuildPreview(Xml);
			}
		}

		private static string BuildPreview(string Value)
		{
			string Normalized = NormalizeWhitespace(Value);
			if (Normalized.Length <= maximumPreviewLength)
				return Normalized;

			return Normalized[..(maximumPreviewLength - 1)] + "…";
		}

		private static string NormalizeWhitespace(string Value)
		{
			if (string.IsNullOrWhiteSpace(Value))
				return string.Empty;

			StringBuilder Result = new(Value.Length);
			bool PreviousWasWhitespace = false;

			foreach (char Character in Value.Trim())
			{
				if (char.IsWhiteSpace(Character))
				{
					if (!PreviousWasWhitespace)
						Result.Append(' ');

					PreviousWasWhitespace = true;
				}
				else
				{
					Result.Append(Character);
					PreviousWasWhitespace = false;
				}
			}

			return Result.ToString();
		}

		private static string BuildRawDetails(TokenEvent Event)
		{
			List<string> Lines =
			[
				Event.GetType().FullName ?? Event.GetType().Name
			];

			if (HasMeaningfulDate(Event.Timestamp))
			{
				Lines.Add(string.Concat(
					ServiceRef.Localizer[nameof(AppResources.Timestamp)],
					": ",
					FormatDate(Event.Timestamp)));
			}

			if (HasMeaningfulDate(Event.Expires))
			{
				Lines.Add(string.Concat(
					ServiceRef.Localizer[nameof(AppResources.Expires)],
					": ",
					FormatDate(Event.Expires)));
			}

			string TokenId = Normalize(Event.TokenId);
			if (!string.IsNullOrEmpty(TokenId))
			{
				Lines.Add(string.Concat(
					ServiceRef.Localizer[nameof(AppResources.TokenId)],
					": ",
					TokenId));
			}

			if (Event.Personal)
			{
				Lines.Add(string.Concat(
					ServiceRef.Localizer[nameof(AppResources.PersonalUpdate)],
					": ",
					ServiceRef.Localizer[nameof(AppResources.Yes)]));
			}

			return string.Join(Environment.NewLine, Lines);
		}

		private static string FormatDate(DateTime Value)
		{
			if (!HasMeaningfulDate(Value))
				return string.Empty;

			DateTime DisplayValue = Value.Kind == DateTimeKind.Utc
				? Value.ToLocalTime()
				: Value;
			return DisplayValue.ToString("g", CultureInfo.CurrentCulture);
		}

		private static bool HasMeaningfulDate(DateTime Value)
		{
			return Value > DateTime.MinValue && Value < DateTime.MaxValue;
		}

		private static string BuildShortIdentifier(string? Value)
		{
			string Normalized = Normalize(Value);
			if (Normalized.Length <= 22)
				return Normalized;

			return Normalized[..10] + "…" + Normalized[^8..];
		}

		private static string FirstNonEmpty(params string[] Values)
		{
			foreach (string Value in Values)
			{
				if (!string.IsNullOrWhiteSpace(Value))
					return Value.Trim();
			}

			return string.Empty;
		}

		private static string Normalize(string? Value)
		{
			return Value?.Trim() ?? string.Empty;
		}

		private static string NormalizeContent(string? Value)
		{
			return Value ?? string.Empty;
		}

		private static async Task CopyToClipboardAsync(string Value)
		{
			try
			{
				await Clipboard.SetTextAsync(Value);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.SuccessfullyCopiedToClipboard)]);
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token activity clipboard operation failed.", ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private static void LogRedactedFailure(string Message, Exception Exception)
		{
			// Identity values, note content, XML, and token identifiers are deliberately omitted.
			ServiceRef.LogService.LogWarning(
				Message,
				new KeyValuePair<string, object?>(
					"FailureType",
					Exception.GetType().Name));
		}

		private static Task OpenIdentityAsync(string IdentityId)
		{
			return ServiceRef.ContractOrchestratorService.OpenLegalIdentity(
				IdentityId,
				ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)]);
		}

		private static Task OpenContractAsync(string ContractId)
		{
			return ServiceRef.ContractOrchestratorService.OpenContract(
				ContractId,
				ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)],
				null);
		}

		private static async Task OpenSourceAsync(string Source)
		{
			try
			{
				int SeparatorIndex = Source.IndexOf('@');
				if (SeparatorIndex < 0)
				{
					if (Source.IndexOf(':') < 0)
						Source = string.Concat("https://", Source);
				}
				else
				{
					string Account = Source[..SeparatorIndex];
					if (Guid.TryParse(Account, out _))
					{
						Source = string.Concat(Constants.UriSchemes.IotId, ":", Source);
					}
					else
					{
						ContactInfo? Contact = await ContactInfo.FindByBareJid(Source);
						ChatNavigationArgs Args = new(
							Contact?.LegalId,
							Contact?.BareJid ?? Source,
							Contact?.FriendlyName ?? Source);

						await ServiceRef.NavigationService.GoToAsync(
							nameof(ChatPage),
							Args,
							BackMethod.Inherited,
							Contact?.BareJid ?? Source);
						return;
					}
				}

				await App.OpenUrlAsync(Source);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}
	}
}
