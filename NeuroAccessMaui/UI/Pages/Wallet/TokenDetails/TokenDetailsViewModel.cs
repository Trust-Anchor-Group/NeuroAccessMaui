using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;
using NeuroAccessMaui.UI.Pages.Wallet.MachineVariables;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails.ObjectModels;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using NeuroFeatures.Events;
using NeuroFeatures.Tags;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Web;
using System.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Persistence;
using Waher.Security;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of a token.
	/// </summary>
	public partial class TokenDetailsViewModel : QrXmppViewModel
	{
		private readonly TokenDetailsNavigationArgs? navigationArguments;
		private readonly ITokenNoteCommandService tokenNoteCommandService;
		private Token? currentToken;
		private long activityLoadGeneration;
		private long noteCommandLoadGeneration;

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenDetailsViewModel"/> class.
		/// </summary>
		/// <param name="Args">Optional navigation arguments containing a token identifier and immediate data.</param>
		public TokenDetailsViewModel(TokenDetailsNavigationArgs? Args)
		{
			this.navigationArguments = Args;
			this.tokenNoteCommandService =
				ServiceRef.Provider.GetRequiredService<ITokenNoteCommandService>();
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ServiceRef.XmppService.NeuroFeatureAdded += this.XmppService_NeuroFeatureAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved += this.XmppService_NeuroFeatureRemoved;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.XmppService_NeuroFeatureStateUpdated;
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.XmppService_NeuroFeatureVariablesUpdated;

			Token? InitialToken = this.navigationArguments?.InitialToken;
			string RequestedTokenId = this.navigationArguments?.TokenId?.Trim() ??
				InitialToken?.TokenId?.Trim() ??
				string.Empty;

			if (InitialToken is not null &&
				!string.IsNullOrWhiteSpace(InitialToken.TokenId) &&
				(string.IsNullOrEmpty(RequestedTokenId) ||
					string.Equals(InitialToken.TokenId, RequestedTokenId, StringComparison.Ordinal)))
			{
				await this.ApplyTokenAsync(InitialToken);
			}
			else if (!string.IsNullOrEmpty(RequestedTokenId))
				await this.LoadTokenAsync(RequestedTokenId, true);
			else
			{
				this.ShowUnavailableState(ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)]);
				return;
			}

			if (this.currentToken is not null)
			{
				await Task.WhenAll(
					this.LoadActivityAsync(this.currentToken.TokenId),
					this.LoadTokenNoteCommandsAsync(this.currentToken));
			}
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			Interlocked.Increment(ref this.activityLoadGeneration);
			Interlocked.Increment(ref this.noteCommandLoadGeneration);
			ServiceRef.XmppService.NeuroFeatureAdded -= this.XmppService_NeuroFeatureAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved -= this.XmppService_NeuroFeatureRemoved;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.XmppService_NeuroFeatureStateUpdated;
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.XmppService_NeuroFeatureVariablesUpdated;
			return base.OnDisposeAsync();
		}

		private async Task<bool> LoadTokenAsync(string TokenId, bool IsInitialLoad)
		{
			if (string.IsNullOrWhiteSpace(TokenId))
			{
				this.ShowUnavailableState(ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)]);
				return false;
			}

			this.IsRefreshing = true;
			this.IsInitialLoading = IsInitialLoad && this.currentToken is null;
			this.HasLoadError = false;
			this.LoadErrorMessage = string.Empty;

			try
			{
				Token Token = await ServiceRef.XmppService.GetNeuroFeature(TokenId);
				if (Token is null ||
					string.IsNullOrWhiteSpace(Token.TokenId) ||
					!string.Equals(Token.TokenId, TokenId, StringComparison.Ordinal))
				{
					throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.InvalidNeuroFeatureToken)]);
				}

				await this.ApplyTokenAsync(Token);
				return true;
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token detail load failed.", ex);
				if (this.currentToken is null)
					this.ShowUnavailableState(ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)]);
				else
				{
					this.HasLoadError = true;
					this.LoadErrorMessage = ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)];
				}

				return false;
			}
			finally
			{
				this.IsInitialLoading = false;
				this.IsRefreshing = false;
			}
		}

		private Task ApplyTokenAsync(Token Token)
		{
			return MainThread.InvokeOnMainThreadAsync(async () =>
			{
				NotificationEvent[] Events;
				if (!ServiceRef.NotificationService.TryGetNotificationEvents(
					NotificationEventType.Wallet,
					Token.TokenId,
					out NotificationEvent[]? CurrentEvents))
				{
					Events = [];
				}
				else
					Events = CurrentEvents;

				TokenSummaryItem Summary = new(
					Token,
					Events,
					ServiceRef.XmppService.BareJid,
					null);

				this.currentToken = Token;
				this.Created = Token.Created;
				this.Updated = Token.Updated;
				this.Expires = Token.Expires;
				this.SignatureTimestamp = Token.SignatureTimestamp;
				this.Signature = Token.Signature;
				this.DefinitionSchemaDigest = Token.DefinitionSchemaDigest;
				this.DefinitionSchemaHashFunction = Token.DefinitionSchemaHashFunction;
				this.FriendlyName = Summary.DisplayName;
				this.Category = Summary.Category;
				this.DescriptionText = Normalize(Token.Description);
				this.Description = null;
				if (!string.IsNullOrEmpty(this.DescriptionText))
				{
					try
					{
						this.Description = await this.DescriptionText.MarkdownToParsedXaml();
					}
					catch (Exception ex)
					{
						LogRedactedFailure("Token description rendering failed.", ex);
					}
				}

				this.HasDescription = !string.IsNullOrEmpty(this.DescriptionText);
				this.HasRichDescription = this.Description is not null;
				this.Ordinal = Token.Ordinal;
				this.Value = Token.Value;
				this.TokenIdMethod = Token.TokenIdMethod;
				this.TokenId = Normalize(Token.TokenId);
				this.ShortTokenId = Normalize(Token.ShortId);
				this.ShortDisplayTokenId = Summary.ShortTokenId;
				this.Visibility = Token.Visibility;
				this.Creator = Normalize(Token.Creator);
				this.CreatorFriendlyName = await GetFriendlyNameAsync(this.Creator);
				this.CreatorJid = Normalize(Token.CreatorJid);
				this.Owner = Normalize(Token.Owner);
				this.OwnerFriendlyName = await GetFriendlyNameAsync(this.Owner);
				this.OwnerJid = Normalize(Token.OwnerJid);
				this.BatchSize = Token.BatchSize;
				this.TrustProvider = Normalize(Token.TrustProvider);
				this.TrustProviderFriendlyName = await GetFriendlyNameAsync(this.TrustProvider);
				this.TrustProviderJid = Normalize(Token.TrustProviderJid);
				this.Currency = Normalize(Token.Currency);
				this.Reference = Normalize(Token.Reference);
				this.Definition = Normalize(Token.Definition);
				this.HasMachineReadableDefinition = !string.IsNullOrEmpty(this.Definition);
				this.DefinitionNamespace = Normalize(Token.DefinitionNamespace);
				this.CreationContract = Normalize(Token.CreationContract);
				this.OwnershipContract = Normalize(Token.OwnershipContract);
				this.GlyphImage = Summary.GlyphImage;
				this.HasGlyphImage = Summary.HasGlyphImage;
				this.FallbackGlyphText = Summary.FallbackGlyphText;
				try
				{
					this.TokenXml = Token.ToXml();
				}
				catch (Exception ex)
				{
					LogRedactedFailure("Token proof serialization failed.", ex);
					this.TokenXml = string.Empty;
				}
				this.IsMyToken = Summary.IsOwner;
				this.CanAddTokenUpdate = Summary.IsOwner;
				this.HasStateMachine = Token.HasStateMachine;
				this.HasEmbeddedLayout = Token.HasEmbeddedLayout;
				this.StatusText = Summary.StatusText;
				this.StatusTone = Summary.StatusTone;
				this.OwnershipText = Summary.OwnershipText;
				this.ValueText = Summary.ValueText;
				this.ExpiryText = Summary.ExpiryText;
				this.UpdatedText = Summary.UpdatedText;
				this.HasValue = Summary.HasValue;
				this.HasExpiry = Summary.HasExpiry;
				this.NeedsAttention = Summary.NeedsAttention;
				this.AttentionText = Summary.NeedsAttention
					? ServiceRef.Localizer[nameof(AppResources.NeedsAttention)]
					: Summary.StatusText;
				this.DefinitionSchemaUrl = BuildDefinitionSchemaUrl(Token);

				if (!string.IsNullOrEmpty(this.TokenId))
					this.GenerateQrCode(Constants.UriSchemes.CreateTokenUri(this.TokenId));

				await this.PopulateRelationshipsAsync(Token);
				this.BuildPresentationCollections(Token);

				this.HasContent = true;
				this.HasLoadError = false;
				this.LoadErrorMessage = string.Empty;
			});
		}

		private async Task<bool> LoadActivityAsync(string TokenId)
		{
			if (string.IsNullOrWhiteSpace(TokenId))
				return false;

			long LoadGeneration = Interlocked.Increment(ref this.activityLoadGeneration);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsActivityLoading = true;
				this.HasActivityError = false;
				this.ActivityErrorMessage = string.Empty;
				this.ShowActivityEmpty = false;
			});

			try
			{
				TokenEvent[] Events = await ServiceRef.XmppService.GetNeuroFeatureEvents(TokenId);
				TokenActivityItem[] Items = await Task.WhenAll(
					(Events ?? []).Select(TokenActivityItem.CreateAsync));
				TokenActivityItem[] OrderedItems =
				[.. Items.OrderByDescending(Item => Item.Timestamp)];

				if (LoadGeneration != Volatile.Read(ref this.activityLoadGeneration))
					return false;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (LoadGeneration != Volatile.Read(ref this.activityLoadGeneration))
						return;

					this.ActivityItems.Clear();
					foreach (TokenActivityItem Item in OrderedItems)
						this.ActivityItems.Add(Item);

					this.HasActivityItems = this.ActivityItems.Count > 0;
					this.ShowActivityEmpty = !this.HasActivityItems;
					this.HasActivityError = false;
					this.ActivityErrorMessage = string.Empty;
				});
				return LoadGeneration == Volatile.Read(ref this.activityLoadGeneration);
			}
			catch (Exception ex)
			{
				if (LoadGeneration != Volatile.Read(ref this.activityLoadGeneration))
					return false;

				LogRedactedFailure("Token activity load failed.", ex);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (LoadGeneration != Volatile.Read(ref this.activityLoadGeneration))
						return;

					this.HasActivityError = true;
					this.ActivityErrorMessage =
						ServiceRef.Localizer[nameof(AppResources.TokensUnavailableDescription)];
					this.HasActivityItems = this.ActivityItems.Count > 0;
					this.ShowActivityEmpty = false;
				});
				return false;
			}
			finally
			{
				if (LoadGeneration == Volatile.Read(ref this.activityLoadGeneration))
				await MainThread.InvokeOnMainThreadAsync(() => this.IsActivityLoading = false);
			}
		}

		private async Task<bool> LoadTokenNoteCommandsAsync(Token Token)
		{
			long LoadGeneration = Interlocked.Increment(ref this.noteCommandLoadGeneration);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsTokenCommandsLoading = true;
				this.IsTokenCommandContextCurrent = false;
				this.TokenNoteCommands.Clear();
				this.TokenDefinedActions.Clear();
				this.HasTokenNoteCommands = false;
				this.HasTokenCommandError = false;
				this.TokenCommandErrorMessage = string.Empty;
			});

			try
			{
				IReadOnlyList<TokenNoteCommandDescriptor> Commands =
					await this.tokenNoteCommandService.DiscoverAsync(
						Token,
						CultureInfo.CurrentUICulture.Name);

				if (LoadGeneration != Volatile.Read(ref this.noteCommandLoadGeneration))
					return false;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (LoadGeneration != Volatile.Read(ref this.noteCommandLoadGeneration))
						return;

					this.TokenNoteCommands.Clear();
					this.TokenDefinedActions.Clear();
					foreach (TokenNoteCommandDescriptor Command in Commands)
					{
						this.TokenNoteCommands.Add(Command);
						this.TokenDefinedActions.Add(new TokenActionItem(
							Command.Title,
							Command.HelpText,
							null,
							TokenActionProminence.Secondary,
							false,
							true,
							string.Empty,
							new AsyncRelayCommand(() =>
							{
								this.OpenTokenNoteCommand(Command);
								return Task.CompletedTask;
							})));
					}

					this.HasTokenNoteCommands = this.TokenNoteCommands.Count > 0;
					this.HasTokenCommandError = false;
					this.TokenCommandErrorMessage = string.Empty;
					this.IsTokenCommandContextCurrent = true;
					this.ReconcileSelectedTokenCommand();
				});

				return LoadGeneration == Volatile.Read(ref this.noteCommandLoadGeneration);
			}
			catch (Exception Ex)
			{
				if (LoadGeneration != Volatile.Read(ref this.noteCommandLoadGeneration))
					return false;

				ServiceRef.LogService.LogWarning(
					"Token-defined actions could not be loaded.",
					new KeyValuePair<string, object?>(
						"FailureType",
						Ex.GetType().Name));
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (LoadGeneration != Volatile.Read(ref this.noteCommandLoadGeneration))
						return;

					this.TokenNoteCommands.Clear();
					this.TokenDefinedActions.Clear();
					this.HasTokenNoteCommands = false;
					this.HasTokenCommandError = true;
					this.TokenCommandErrorMessage =
						ServiceRef.Localizer[nameof(AppResources.TokenDefinedActionsUnavailable)];
					this.IsTokenCommandContextCurrent = false;
					if (this.SelectedTokenNoteCommand is not null)
						this.SelectedTokenCommandUnavailable = true;
				});

				return false;
			}
			finally
			{
				if (LoadGeneration == Volatile.Read(ref this.noteCommandLoadGeneration))
				await MainThread.InvokeOnMainThreadAsync(() => this.IsTokenCommandsLoading = false);
			}
		}

		private void ReconcileSelectedTokenCommand()
		{
			TokenNoteCommandDescriptor? Selected = this.SelectedTokenNoteCommand;
			if (Selected is null)
				return;

			TokenNoteCommandDescriptor? Current = this.TokenNoteCommands.FirstOrDefault(Command =>
				Command.CommandIndex == Selected.CommandIndex &&
				string.Equals(Command.Id, Selected.Id, StringComparison.Ordinal));

			if (Current is null ||
				Current.IsOwnerContext != Selected.IsOwnerContext ||
				!HasMatchingParameterSchema(
					Current.Parameters,
					this.SelectedTokenCommandParameters))
			{
				this.SelectedTokenCommandUnavailable = true;
				this.HasTokenCommandExecutionFeedback = true;
				this.TokenCommandExecutionFeedback =
					ServiceRef.Localizer[nameof(AppResources.TokenActionUnavailable)];
				return;
			}

			this.SelectedTokenNoteCommand = Current;
			this.SelectedTokenCommandUnavailable = false;
			this.UpdateSelectedTokenCommandContextCopy();
		}

		private static bool HasMatchingParameterSchema(
			IReadOnlyList<Parameter> Current,
			IReadOnlyList<ObservableParameter> Editors)
		{
			if (Current.Count != Editors.Count)
				return false;

			for (int i = 0; i < Current.Count; i++)
			{
				if (!string.Equals(
						Current[i].Name,
						Editors[i].Parameter.Name,
						StringComparison.Ordinal) ||
					Current[i].GetType() != Editors[i].Parameter.GetType())
				{
					return false;
				}
			}

			return true;
		}

		private async Task<(bool TokenLoaded, bool ActivityLoaded)> RefreshWorkspaceAsync(
			string TokenId,
			bool IsInitialLoad)
		{
			Task<bool> TokenLoad = this.LoadTokenAsync(TokenId, IsInitialLoad);
			Task<bool> ActivityLoad = this.LoadActivityAsync(TokenId);
			bool TokenLoaded = await TokenLoad;

			Token? TokenForCommands = this.currentToken;
			if (TokenForCommands is not null &&
				string.Equals(TokenForCommands.TokenId, TokenId, StringComparison.Ordinal))
			{
				await Task.WhenAll(
					ActivityLoad,
					this.LoadTokenNoteCommandsAsync(TokenForCommands));
			}
			else
				await ActivityLoad;

			return (TokenLoaded, await ActivityLoad);
		}

		private async Task<TokenUpdateSubmissionResult> SubmitManualTokenUpdateAsync(
			TokenUpdateDraft Draft)
		{
			string CurrentTokenId = this.TokenId ?? string.Empty;
			if (string.IsNullOrWhiteSpace(CurrentTokenId) || !this.CanAddTokenUpdate)
			{
				return new TokenUpdateSubmissionResult(
					TokenUpdateSubmissionStatus.Failed,
					ServiceRef.Localizer[nameof(AppResources.TokenActionUnavailable)],
					false);
			}

			int MatchingEventsBeforeSubmission = this.CountMatchingManualUpdates(Draft);

			try
			{
				if (Draft.ContentType == TokenUpdateContentType.Xml)
				{
					await ServiceRef.XmppService.AddNeuroFeatureXmlNote(
						CurrentTokenId,
						Draft.Content,
						Draft.Personal);
				}
				else
				{
					await ServiceRef.XmppService.AddNeuroFeatureTextNote(
						CurrentTokenId,
						Draft.Content,
						Draft.Personal);
				}
			}
			catch (Exception ex)
			{
				// Note content is intentionally excluded from diagnostics.
				LogRedactedFailure("Token update submission failed.", ex);

				if (IsDefiniteManualUpdateFailure(ex))
				{
					return new TokenUpdateSubmissionResult(
						TokenUpdateSubmissionStatus.Failed,
						GetManualUpdateFailureMessage(ex),
						CanRetryManualUpdate(ex));
				}

				(bool TokenLoaded, bool ActivityLoaded) Verification =
					await this.RefreshWorkspaceAsync(CurrentTokenId, false);
				bool UpdateAppeared = Verification.ActivityLoaded &&
					this.CountMatchingManualUpdates(Draft) > MatchingEventsBeforeSubmission;

				if (UpdateAppeared)
				{
					return new TokenUpdateSubmissionResult(
						Verification.TokenLoaded
							? TokenUpdateSubmissionStatus.Confirmed
							: TokenUpdateSubmissionStatus.ConfirmedRefreshFailed,
						Verification.TokenLoaded
							? ServiceRef.Localizer[nameof(AppResources.UpdateSent)]
							: ServiceRef.Localizer[nameof(AppResources.UpdateSentRefreshFailed)],
						false);
				}

				// An unconfirmed send is never immediately retryable; a separate refresh prevents duplicates.
				return new TokenUpdateSubmissionResult(
					TokenUpdateSubmissionStatus.OutcomeUncertain,
					ServiceRef.Localizer[nameof(AppResources.UpdateOutcomeUncertain)],
					false);
			}

			(bool TokenLoaded, bool ActivityLoaded) Refresh =
				await this.RefreshWorkspaceAsync(CurrentTokenId, false);
			bool RefreshSucceeded = Refresh.TokenLoaded && Refresh.ActivityLoaded;

			return new TokenUpdateSubmissionResult(
				RefreshSucceeded
					? TokenUpdateSubmissionStatus.Confirmed
					: TokenUpdateSubmissionStatus.ConfirmedRefreshFailed,
				RefreshSucceeded
					? ServiceRef.Localizer[nameof(AppResources.UpdateSent)]
					: ServiceRef.Localizer[nameof(AppResources.UpdateSentRefreshFailed)],
				false);
		}

		private int CountMatchingManualUpdates(TokenUpdateDraft Draft)
		{
			return this.ActivityItems.Count(Item =>
				Item.Event.Personal == Draft.Personal &&
				string.Equals(Item.CompleteContent, Draft.Content, StringComparison.Ordinal) &&
				(Draft.ContentType == TokenUpdateContentType.Xml
					? Item.Event is NoteXml
					: Item.Event is NoteText));
		}

		private static bool IsDefiniteManualUpdateFailure(Exception Exception)
		{
			Exception Current = Exception;
			while (Current.InnerException is not null &&
				Current is AggregateException)
			{
				Current = Current.InnerException;
			}

			if (Current is ArgumentException)
				return true;

			return Current is XmppException XmppException &&
				XmppException.Stanza is not null;
		}

		private static string GetManualUpdateFailureMessage(Exception Exception)
		{
			Exception Current = Exception;
			while (Current.InnerException is not null && Current is AggregateException)
				Current = Current.InnerException;

			if (Current is ArgumentException)
				return ServiceRef.Localizer[nameof(AppResources.InvalidXmlUpdate)];

			if (Current is StanzaWaitExceptionException)
				return ServiceRef.Localizer[nameof(AppResources.PleaseTryAgain)];

			if (Current is XmppException)
				return ServiceRef.Localizer[nameof(AppResources.TokenActionUnavailable)];

			return ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)];
		}

		private static bool CanRetryManualUpdate(Exception Exception)
		{
			Exception Current = Exception;
			while (Current.InnerException is not null && Current is AggregateException)
				Current = Current.InnerException;

			return Current is ArgumentException or StanzaWaitExceptionException;
		}

		private async Task PopulateRelationshipsAsync(Token Token)
		{
			this.Relationships.Clear();
			this.Participants.Clear();

			this.AddRelationship(
				ServiceRef.Localizer[nameof(AppResources.Owner)],
				this.Owner,
				this.OwnerJid,
				this.OwnerFriendlyName,
				"Owner");
			this.AddRelationship(
				ServiceRef.Localizer[nameof(AppResources.Creator)],
				this.Creator,
				this.CreatorJid,
				this.CreatorFriendlyName,
				"Creator");
			this.AddRelationship(
				ServiceRef.Localizer[nameof(AppResources.TrustProvider)],
				this.TrustProvider,
				this.TrustProviderJid,
				this.TrustProviderFriendlyName,
				"TrustProvider");

			await this.PopulateParticipantsAsync(
				ServiceRef.Localizer[nameof(AppResources.Witness)],
				Token.Witness,
				null);
			await this.PopulateParticipantsAsync(
				ServiceRef.Localizer[nameof(AppResources.Certifier)],
				Token.Certifier,
				Token.CertifierJids);
			await this.PopulateParticipantsAsync(
				ServiceRef.Localizer[nameof(AppResources.Valuator)],
				Token.Valuator,
				null);
			await this.PopulateParticipantsAsync(
				ServiceRef.Localizer[nameof(AppResources.Assessor)],
				Token.Assessor,
				null);
		}

		private void AddRelationship(
			string Label,
			string? LegalId,
			string? Jid,
			string? FriendlyName,
			string ChatCommandParameter)
		{
			if (string.IsNullOrWhiteSpace(LegalId) &&
				string.IsNullOrWhiteSpace(Jid) &&
				string.IsNullOrWhiteSpace(FriendlyName))
			{
				return;
			}

			this.Relationships.Add(new PartItem(
				Label,
				LegalId,
				Jid,
				FriendlyName,
				ChatCommandParameter,
				this.ViewIdCommand,
				this.OpenChatCommand));
		}

		private async Task PopulateParticipantsAsync(
			string Label,
			string[]? LegalIds,
			string[]? Jids)
		{
			int LegalIdCount = LegalIds?.Length ?? 0;
			int JidCount = Jids?.Length ?? 0;

			for (int i = 0; i < LegalIdCount; i++)
			{
				string LegalId = Normalize(LegalIds![i]);
				string Jid = i < JidCount ? Normalize(Jids![i]) : string.Empty;
				string FriendlyName = await GetFriendlyNameAsync(LegalId);
				string ChatParameter = string.Concat(Jid, " | ", LegalId, " | ", FriendlyName);
				PartItem Item = new(
					Label,
					LegalId,
					Jid,
					FriendlyName,
					ChatParameter,
					this.ViewIdCommand,
					this.OpenChatCommand);

				this.Participants.Add(Item);
			}
		}

		private void BuildPresentationCollections(Token Token)
		{
			this.PrimaryActions.Clear();
			this.SecondaryActions.Clear();
			this.MarketplaceActions.Clear();
			this.RelatedContracts.Clear();
			this.TechnicalItems.Clear();
			this.PermissionItems.Clear();
			this.TagItems.Clear();
			this.RawItems.Clear();

			string MachineId =
				Convert.ToString(Token.MachineId, CultureInfo.InvariantCulture) ?? string.Empty;
			string CreationContractTemplate =
				Convert.ToString(
					Token.CreationContractTemplate,
					CultureInfo.InvariantCulture) ?? string.Empty;

			if (this.HasEmbeddedLayout)
			{
				this.PrimaryActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.Open)],
					ServiceRef.Localizer[nameof(AppResources.ViewEmbeddedLayout)],
					null,
					TokenActionProminence.Primary,
					false,
					true,
					string.Empty,
					this.PresentEmbeddedLayoutCommand));
			}

			if (this.HasQrCode)
			{
				this.SecondaryActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.Share)],
					ServiceRef.Localizer[nameof(AppResources.TokenShareActionDescription)],
					null,
					TokenActionProminence.Secondary,
					false,
					true,
					string.Empty,
					this.ShareCommand));
			}
			else
			{
				this.SecondaryActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.Share)],
					ServiceRef.Localizer[nameof(AppResources.TokenShareActionDescription)],
					null,
					TokenActionProminence.Secondary,
					false,
					false,
					ServiceRef.Localizer[nameof(AppResources.TokenSharePayloadUnavailable)],
					null));
			}

			if (!string.IsNullOrEmpty(this.TokenXml))
			{
				this.SecondaryActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.SendToContact)],
					ServiceRef.Localizer[nameof(AppResources.TokenSendActionDescription)],
					null,
					TokenActionProminence.Secondary,
					false,
					true,
					string.Empty,
					this.SendToContactCommand));
			}
			else
			{
				this.SecondaryActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.SendToContact)],
					ServiceRef.Localizer[nameof(AppResources.TokenSendActionDescription)],
					null,
					TokenActionProminence.Secondary,
					false,
					false,
					ServiceRef.Localizer[nameof(AppResources.TokenSharePayloadUnavailable)],
					null));
			}

			bool HasMarketplaceIdentity = HasApprovedMarketplaceIdentity();
			string MarketplaceExplanation = HasMarketplaceIdentity
				? string.Empty
				: ServiceRef.Localizer[nameof(AppResources.MarketplaceIdentityRequired)];
			if (this.IsMyToken)
			{
				this.MarketplaceActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.PublishMarketplace)],
					ServiceRef.Localizer[nameof(AppResources.PublishMarketplaceDescription)],
					null,
					TokenActionProminence.Advanced,
					false,
					HasMarketplaceIdentity,
					MarketplaceExplanation,
					HasMarketplaceIdentity ? this.PublishMarketplaceCommand : null));
				this.MarketplaceActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.OfferToSell)],
					ServiceRef.Localizer[nameof(AppResources.OfferToSellDescription)],
					null,
					TokenActionProminence.Advanced,
					false,
					HasMarketplaceIdentity,
					MarketplaceExplanation,
					HasMarketplaceIdentity ? this.OfferToSellCommand : null));
			}
			else
			{
				this.MarketplaceActions.Add(new TokenActionItem(
					ServiceRef.Localizer[nameof(AppResources.OfferToBuy)],
					ServiceRef.Localizer[nameof(AppResources.OfferToBuyDescription)],
					null,
					TokenActionProminence.Advanced,
					false,
					HasMarketplaceIdentity,
					MarketplaceExplanation,
					HasMarketplaceIdentity ? this.OfferToBuyCommand : null));
			}

			this.AddRelatedContract(
				ServiceRef.Localizer[nameof(AppResources.CreationContract)],
				this.CreationContract);
			this.AddRelatedContract(
				ServiceRef.Localizer[nameof(AppResources.OwnershipContract)],
				this.OwnershipContract);

			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.TokenId)],
				BuildShortIdentifier(this.TokenId),
				this.TokenId,
				false);
			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.ShortId)],
				this.ShortTokenId,
				this.ShortTokenId,
				false);
			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.TokenIdMethod)],
				this.TokenIdMethod?.ToString(),
				null,
				false);
			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.MachineId)],
				BuildShortIdentifier(MachineId),
				MachineId,
				false);
			if ((this.BatchSize ?? 0) > 0)
			{
				this.AddTechnicalItem(
					this.TechnicalItems,
					ServiceRef.Localizer[nameof(AppResources.Ordinal)],
					string.Format(
						CultureInfo.CurrentCulture,
						"{0} / {1}",
						this.Ordinal ?? 0,
						this.BatchSize),
					null,
					false);
			}
			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.Created)],
				FormatDate(this.Created),
				null,
				false);
			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.Updated)],
				FormatDate(this.Updated),
				null,
				false);
			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.Expires)],
				FormatDate(this.Expires),
				null,
				false);
			this.AddTechnicalLink(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.Reference)],
				this.Reference,
				false);
			this.AddTechnicalItem(
				this.TechnicalItems,
				ServiceRef.Localizer[nameof(AppResources.CreationContractTemplate)],
				BuildShortIdentifier(CreationContractTemplate),
				CreationContractTemplate,
				false,
				string.IsNullOrWhiteSpace(CreationContractTemplate)
					? null
					: new AsyncRelayCommand(
						() => this.ViewContract(CreationContractTemplate)));

			this.AddTechnicalItem(
				this.PermissionItems,
				ServiceRef.Localizer[nameof(AppResources.TokenVisibility)],
				FormatVisibility(this.Visibility),
				null,
				false);
			this.AddPermission(
				ServiceRef.Localizer[nameof(AppResources.CreatorCanDestroy)],
				Token.CreatorCanDestroy);
			this.AddPermission(
				ServiceRef.Localizer[nameof(AppResources.OwnerCanDestroyBatch)],
				Token.OwnerCanDestroyBatch);
			this.AddPermission(
				ServiceRef.Localizer[nameof(AppResources.OwnerCanDestroyIndividual)],
				Token.OwnerCanDestroyIndividual);
			this.AddPermission(
				ServiceRef.Localizer[nameof(AppResources.CertifierCanDestroy)],
				Token.CertifierCanDestroy);
			this.AddTechnicalItem(
				this.PermissionItems,
				ServiceRef.Localizer[nameof(AppResources.ArchiveRequired)],
				Token.ArchiveRequired?.ToString(),
				null,
				false);
			this.AddTechnicalItem(
				this.PermissionItems,
				ServiceRef.Localizer[nameof(AppResources.ArchiveOptional)],
				Token.ArchiveOptional?.ToString(),
				null,
				false);

			try
			{
				foreach (TokenTag Tag in Token.Tags ?? [])
				{
					string TagName = Normalize(Tag.Name);
					string TagValue = Tag.Value?.ToString()?.Trim() ?? string.Empty;
					if (string.IsNullOrEmpty(TagName) || string.IsNullOrEmpty(TagValue))
						continue;

					this.AddTechnicalLink(this.TagItems, TagName, TagValue, true);
				}
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token tag presentation failed.", ex);
			}

			this.AddTechnicalItem(
				this.RawItems,
				ServiceRef.Localizer[nameof(AppResources.Signature)],
				BuildShortIdentifier(ToBase64(this.Signature)),
				ToBase64(this.Signature),
				true);
			this.AddTechnicalItem(
				this.RawItems,
				ServiceRef.Localizer[nameof(AppResources.Timestamp)],
				FormatDate(this.SignatureTimestamp),
				null,
				false);
			this.AddTechnicalLink(
				this.RawItems,
				ServiceRef.Localizer[nameof(AppResources.DefinitionNamespace)],
				this.DefinitionNamespace,
				true);
			this.AddTechnicalItem(
				this.RawItems,
				ServiceRef.Localizer[nameof(AppResources.DefinitionSchemaDigest)],
				BuildShortIdentifier(ToBase64(this.DefinitionSchemaDigest)),
				ToBase64(this.DefinitionSchemaDigest),
				true,
				string.IsNullOrEmpty(this.DefinitionSchemaUrl)
					? null
					: new AsyncRelayCommand(() => OpenLink(this.DefinitionSchemaUrl)));
			this.AddTechnicalItem(
				this.RawItems,
				ServiceRef.Localizer[nameof(AppResources.HashFunction)],
				this.DefinitionSchemaHashFunction?.ToString(),
				null,
				false);
			this.AddTechnicalItem(
				this.RawItems,
				ServiceRef.Localizer[nameof(AppResources.MachineReadableText)],
				BuildShortIdentifier(this.TokenXml),
				this.TokenXml,
				true);

			this.HasPrimaryActions = this.PrimaryActions.Count > 0;
			this.HasRelationships = this.Relationships.Count > 0;
			this.HasRelatedContracts = this.RelatedContracts.Count > 0;
			this.HasParticipants = this.Participants.Count > 0;
			this.HasPermissions = this.PermissionItems.Count > 0;
			this.HasTags = this.TagItems.Count > 0;
			this.HasMarketplaceActions = this.MarketplaceActions.Count > 0;
			this.HasAdvancedPresentations = this.HasStateMachine || this.HasEmbeddedLayout;
			this.HasRawDetails =
				this.RawItems.Count > 0 ||
				this.HasMachineReadableDefinition;
			this.NextActionExplanation = this.HasPrimaryActions
				? string.Empty
				: string.Join(
					" · ",
					new[] { this.StatusText, this.OwnershipText, this.ExpiryText }
						.Where(Value => !string.IsNullOrWhiteSpace(Value)));
		}

		private void AddRelatedContract(string Label, string? ContractId)
		{
			string NormalizedContractId = Normalize(ContractId);
			if (string.IsNullOrEmpty(NormalizedContractId))
				return;

			this.RelatedContracts.Add(new TokenTechnicalItem(
				Label,
				BuildShortIdentifier(NormalizedContractId),
				false,
				new AsyncRelayCommand(() => CopyRawValueAsync(NormalizedContractId)),
				new AsyncRelayCommand(() => ViewContract(NormalizedContractId))));
		}

		private void AddPermission(string Label, bool Value)
		{
			this.PermissionItems.Add(new TokenTechnicalItem(
				Label,
				ServiceRef.Localizer[
					Value
						? nameof(AppResources.Yes)
						: nameof(AppResources.No)],
				false));
		}

		private static bool HasApprovedMarketplaceIdentity()
		{
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			return Identity?.IsApproved() == true &&
				!string.IsNullOrWhiteSpace(Identity.Id);
		}

		private void AddTechnicalLink(
			ObservableCollection<TokenTechnicalItem> Target,
			string Label,
			string? Value,
			bool IsSensitive)
		{
			string NormalizedValue = Normalize(Value);
			if (string.IsNullOrEmpty(NormalizedValue))
				return;

			IAsyncRelayCommand? OpenCommand = IsSafeWebUri(NormalizedValue)
				? new AsyncRelayCommand(() => OpenLink(NormalizedValue))
				: null;
			this.AddTechnicalItem(
				Target,
				Label,
				BuildShortIdentifier(NormalizedValue),
				NormalizedValue,
				IsSensitive,
				OpenCommand);
		}

		private void AddTechnicalItem(
			ObservableCollection<TokenTechnicalItem> Target,
			string Label,
			string? DisplayValue,
			string? CompleteValue,
			bool IsSensitive,
			IAsyncRelayCommand? OpenCommand = null)
		{
			string NormalizedDisplayValue = Normalize(DisplayValue);
			if (string.IsNullOrEmpty(NormalizedDisplayValue))
				return;

			string NormalizedCompleteValue = Normalize(CompleteValue);
			IAsyncRelayCommand? CopyCommand = string.IsNullOrEmpty(NormalizedCompleteValue)
				? null
				: new AsyncRelayCommand(() => CopyToClipboard(NormalizedCompleteValue));
			Target.Add(new TokenTechnicalItem(
				Label,
				NormalizedDisplayValue,
				IsSensitive,
				CopyCommand,
				OpenCommand));
		}

		private void ShowUnavailableState(string Message)
		{
			this.HasContent = this.currentToken is not null;
			this.HasLoadError = true;
			this.LoadErrorMessage = Message;
			this.StatusText = ServiceRef.Localizer[nameof(AppResources.Unavailable)];
			this.StatusTone = StatusPillTone.Neutral;
			this.AttentionText = ServiceRef.Localizer[nameof(AppResources.NeedsAttention)];
			this.NeedsAttention = true;
			this.PrimaryActions.Clear();
			this.SecondaryActions.Clear();
			this.MarketplaceActions.Clear();
			this.TokenNoteCommands.Clear();
			this.TokenDefinedActions.Clear();
			this.HasTokenNoteCommands = false;
			this.IsTokenCommandContextCurrent = false;
			if (this.SelectedTokenNoteCommand is not null)
				this.SelectedTokenCommandUnavailable = true;
			this.HasPrimaryActions = false;
			this.HasMarketplaceActions = false;
			this.HasStateMachine = false;
			this.HasEmbeddedLayout = false;
			this.HasAdvancedPresentations = false;
			this.HasMachineReadableDefinition = false;
			this.HasRawDetails = this.RawItems.Count > 0;
			this.CanAddTokenUpdate = false;
			this.NextActionExplanation = ServiceRef.Localizer[nameof(AppResources.TokenActionUnavailable)];
		}

		private async Task XmppService_NeuroFeatureAdded(object? Sender, TokenEventArgs e)
		{
			string CurrentTokenId = this.TokenId ?? this.navigationArguments?.TokenId ?? string.Empty;
			if (!string.Equals(e.Token.TokenId, CurrentTokenId, StringComparison.Ordinal))
				return;

			await this.ApplyTokenAsync(e.Token);
			await Task.WhenAll(
				this.LoadActivityAsync(CurrentTokenId),
				this.LoadTokenNoteCommandsAsync(e.Token));
		}

		private Task XmppService_NeuroFeatureStateUpdated(
			object? Sender,
			NewStateEventArgs e)
		{
			return this.RefreshTokenNoteCommandsForStateChangeAsync(e.TokenId);
		}

		private Task XmppService_NeuroFeatureVariablesUpdated(
			object? Sender,
			VariablesUpdatedEventArgs e)
		{
			return this.RefreshTokenNoteCommandsForStateChangeAsync(e.TokenId);
		}

		private Task RefreshTokenNoteCommandsForStateChangeAsync(string TokenId)
		{
			Token? Token = this.currentToken;
			if (Token is null ||
				!string.Equals(Token.TokenId, TokenId, StringComparison.Ordinal))
			{
				return Task.CompletedTask;
			}

			return this.LoadTokenNoteCommandsAsync(Token);
		}

		private Task XmppService_NeuroFeatureRemoved(object? Sender, TokenEventArgs e)
		{
			string CurrentTokenId = this.TokenId ?? this.navigationArguments?.TokenId ?? string.Empty;
			if (!string.Equals(e.Token.TokenId, CurrentTokenId, StringComparison.Ordinal))
				return Task.CompletedTask;

			return MainThread.InvokeOnMainThreadAsync(() =>
				this.ShowUnavailableState(ServiceRef.Localizer[nameof(AppResources.TokenActionUnavailable)]));
		}

		private static async Task<string> GetFriendlyNameAsync(string? LegalId)
		{
			string NormalizedLegalId = Normalize(LegalId);
			if (string.IsNullOrEmpty(NormalizedLegalId))
				return string.Empty;

			try
			{
				string? FriendlyName = await ContactInfo.GetFriendlyName(NormalizedLegalId);
				return string.IsNullOrWhiteSpace(FriendlyName)
					? BuildShortIdentifier(NormalizedLegalId)
					: FriendlyName.Trim();
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token identity lookup failed.", ex);
				return BuildShortIdentifier(NormalizedLegalId);
			}
		}

		private static string BuildDefinitionSchemaUrl(Token Token)
		{
			string TokenId = Normalize(Token.TokenId);
			string DefinitionNamespace = Normalize(Token.DefinitionNamespace);
			byte[]? Digest = Token.DefinitionSchemaDigest;
			// The legacy schema endpoint is only exposed when the token ID yields a valid parent host.
			int AtIndex = TokenId.IndexOf('@', StringComparison.Ordinal);
			string ComponentDomain = AtIndex >= 0 && AtIndex < TokenId.Length - 1
				? TokenId[(AtIndex + 1)..]
				: string.Empty;
			int DotIndex = ComponentDomain.IndexOf('.', StringComparison.Ordinal);
			string Domain = DotIndex > 0 && DotIndex < ComponentDomain.Length - 1
				? ComponentDomain[(DotIndex + 1)..]
				: string.Empty;

			if (string.IsNullOrWhiteSpace(Domain) ||
				Uri.CheckHostName(Domain) == UriHostNameType.Unknown ||
				string.IsNullOrWhiteSpace(DefinitionNamespace) ||
				Digest is null ||
				Digest.Length == 0)
			{
				return string.Empty;
			}

			return string.Concat(
				"https://",
				Domain,
				"/ValidationSchema.md?NS=",
				HttpUtility.UrlEncode(DefinitionNamespace),
				"&H=",
				HttpUtility.UrlEncode(Convert.ToBase64String(Digest)),
				"&Download=1");
		}

		private static string FormatVisibility(ContractVisibility? Visibility)
		{
			return Visibility switch
			{
				ContractVisibility.CreatorAndParts =>
					ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)],
				ContractVisibility.DomainAndParts =>
					ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)],
				ContractVisibility.Public =>
					ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)],
				ContractVisibility.PublicSearchable =>
					ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)],
				_ => ServiceRef.Localizer[nameof(AppResources.Unknown)]
			};
		}

		private static string FormatDate(DateTime? Value)
		{
			return Value.HasValue &&
				Value.Value > DateTime.MinValue &&
				Value.Value < DateTime.MaxValue
					? Value.Value.ToString("g", CultureInfo.CurrentCulture)
					: string.Empty;
		}

		private static string ToBase64(byte[]? Value)
		{
			return Value is null || Value.Length == 0
				? string.Empty
				: Convert.ToBase64String(Value);
		}

		private static string Normalize(string? Value)
		{
			return Value?.Trim() ?? string.Empty;
		}

		private static void LogRedactedFailure(string Message, Exception Exception)
		{
			// Token IDs, identity values, XML, note content, and generated action data are deliberately omitted.
			ServiceRef.LogService.LogWarning(
				Message,
				new KeyValuePair<string, object?>(
					"FailureType",
					Exception.GetType().Name));
		}

		private static string BuildShortIdentifier(string? Value)
		{
			string NormalizedValue = Normalize(Value);
			if (NormalizedValue.Length <= 24)
				return NormalizedValue;

			return NormalizedValue[..10] + "…" + NormalizedValue[^8..];
		}

		private static bool IsSafeWebUri(string Value)
		{
			return Uri.TryCreate(Value, UriKind.Absolute, out Uri? ParsedUri) &&
				(ParsedUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
					ParsedUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
		}

		private static async Task CopyRawValueAsync(string Value)
		{
			try
			{
				await Clipboard.SetTextAsync(Value);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token clipboard operation failed.", ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Token ID
		/// </summary>
		[ObservableProperty]
		private string? definitionSchemaUrl;

		#region Properties

		/// <summary>
		/// Gets the identity relationships shown in the overview.
		/// </summary>
		public ObservableCollection<PartItem> Relationships { get; } = [];

		/// <summary>
		/// Gets additional witnesses, certifiers, valuators, and assessors.
		/// </summary>
		public ObservableCollection<PartItem> Participants { get; } = [];

		/// <summary>
		/// Gets the actions that may be emphasized as the token's next step.
		/// </summary>
		public ObservableCollection<TokenActionItem> PrimaryActions { get; } = [];

		/// <summary>
		/// Gets routine secondary token actions.
		/// </summary>
		public ObservableCollection<TokenActionItem> SecondaryActions { get; } = [];

		/// <summary>
		/// Gets advanced marketplace actions.
		/// </summary>
		public ObservableCollection<TokenActionItem> MarketplaceActions { get; } = [];

		/// <summary>
		/// Gets related creation and ownership contract links.
		/// </summary>
		public ObservableCollection<TokenTechnicalItem> RelatedContracts { get; } = [];

		/// <summary>
		/// Gets progressively disclosed technical metadata.
		/// </summary>
		public ObservableCollection<TokenTechnicalItem> TechnicalItems { get; } = [];

		/// <summary>
		/// Gets progressively disclosed destruction and permission values.
		/// </summary>
		public ObservableCollection<TokenTechnicalItem> PermissionItems { get; } = [];

		/// <summary>
		/// Gets progressively disclosed custom token tags.
		/// </summary>
		public ObservableCollection<TokenTechnicalItem> TagItems { get; } = [];

		/// <summary>
		/// Gets progressively disclosed raw proof and machine-readable values.
		/// </summary>
		public ObservableCollection<TokenTechnicalItem> RawItems { get; } = [];

		/// <summary>
		/// Gets the normalized activity timeline in reverse chronological order.
		/// </summary>
		public ObservableCollection<TokenActivityItem> ActivityItems { get; } = [];

		/// <summary>
		/// Gets token-defined actions applicable to the current role, state, and variables.
		/// </summary>
		public ObservableCollection<TokenNoteCommandDescriptor> TokenNoteCommands { get; } = [];

		/// <summary>
		/// Gets page-ready cards for applicable token-defined actions.
		/// </summary>
		public ObservableCollection<TokenActionItem> TokenDefinedActions { get; } = [];

		/// <summary>
		/// Gets parameter editors for the currently selected token-defined action.
		/// </summary>
		public ObservableCollection<ObservableParameter> SelectedTokenCommandParameters { get; } = [];

		/// <summary>
		/// When token was created.
		/// </summary>
		[ObservableProperty]
		private DateTime? created;

		/// <summary>
		/// When token was last updated.
		/// </summary>
		[ObservableProperty]
		private DateTime? updated;

		/// <summary>
		/// When token expires.
		/// </summary>
		[ObservableProperty]
		private DateTime? expires;

		/// <summary>
		/// Signature timestamp
		/// </summary>
		[ObservableProperty]
		private DateTime? signatureTimestamp;

		/// <summary>
		/// Token signature
		/// </summary>
		[ObservableProperty]
		private byte[]? signature;

		/// <summary>
		/// Digest of schema used to validate token definition XML.
		/// </summary>
		[ObservableProperty]
		private byte[]? definitionSchemaDigest;

		/// <summary>
		/// Hash function used to compute <see cref="DefinitionSchemaDigest"/>.
		/// </summary>
		[ObservableProperty]
		private HashFunction? definitionSchemaHashFunction;

		/// <summary>
		/// Friendly name of token.
		/// </summary>
		[ObservableProperty]
		private string? friendlyName;

		/// <summary>
		/// Friendly name of token.
		/// </summary>
		[ObservableProperty]
		private string? category;

		/// <summary>
		/// Description of token.
		/// </summary>
		[ObservableProperty]
		private object? description;

		/// <summary>
		/// Ordinal of token, within batch.
		/// </summary>
		[ObservableProperty]
		private int? ordinal;

		/// <summary>
		/// (Last) Value of token
		/// </summary>
		[ObservableProperty]
		private decimal? value;

		/// <summary>
		/// Method of assigning the Token ID.
		/// </summary>
		[ObservableProperty]
		private TokenIdMethod? tokenIdMethod;

		/// <summary>
		/// Token ID
		/// </summary>
		[ObservableProperty]
		private string? tokenId;

		/// <summary>
		/// ShortToken ID
		/// </summary>
		[ObservableProperty]
		private string? shortTokenId;

		/// <summary>
		/// Token XML
		/// </summary>
		[ObservableProperty]
		private string? tokenXml;

		/// <summary>
		/// Visibility of token
		/// </summary>
		[ObservableProperty]
		private ContractVisibility? visibility;

		/// <summary>
		/// Creator of token
		/// </summary>
		[ObservableProperty]
		private string? creator;

		/// <summary>
		/// CreatorFriendlyName of token
		/// </summary>
		[ObservableProperty]
		private string? creatorFriendlyName;

		/// <summary>
		/// JID of <see cref="Creator"/>.
		/// </summary>
		[ObservableProperty]
		private string? creatorJid;

		/// <summary>
		/// Current owner
		/// </summary>
		[ObservableProperty]
		private string? owner;

		/// <summary>
		/// Current owner
		/// </summary>
		[ObservableProperty]
		private string? ownerFriendlyName;

		/// <summary>
		/// JID of owner
		/// </summary>
		[ObservableProperty]
		private string? ownerJid;

		/// <summary>
		/// Number of tokens in batch being created.
		/// </summary>
		[ObservableProperty]
		private int? batchSize;

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		[ObservableProperty]
		private string? trustProvider;

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		[ObservableProperty]
		private string? trustProviderFriendlyName;

		/// <summary>
		/// JID of <see cref="TrustProvider"/>
		/// </summary>
		[ObservableProperty]
		private string? trustProviderJid;

		/// <summary>
		/// Currency of <see cref="Value"/>.
		/// </summary>
		[ObservableProperty]
		private string? currency;

		/// <summary>
		/// Any reference provided by the token creator.
		/// </summary>
		[ObservableProperty]
		private string? reference;

		/// <summary>
		/// XML Definition of token.
		/// </summary>
		[ObservableProperty]
		private string? definition;

		/// <summary>
		/// XML Namespace used in the <see cref="Definition"/>
		/// </summary>
		[ObservableProperty]
		private string? definitionNamespace;

		/// <summary>
		/// Contract used to create the contract.
		/// </summary>
		[ObservableProperty]
		private string? creationContract;

		/// <summary>
		/// Contract used to define the current ownership
		/// </summary>
		[ObservableProperty]
		private string? ownershipContract;

		/// <summary>
		/// Gets or sets the image representing the glyph.
		/// </summary>
		[ObservableProperty]
		private ImageSource? glyphImage;

		/// <summary>
		/// Gets or sets the value representing of a glyph is available or not.
		/// </summary>
		[ObservableProperty]
		private bool hasGlyphImage;

		/// <summary>
		/// If the token belongs to the user.
		/// </summary>
		[ObservableProperty]
		private bool isMyToken;

		/// <summary>
		/// If the token is associated with a state-machine.
		/// </summary>
		[ObservableProperty]
		private bool hasStateMachine;

		/// <summary>
		/// If the token have an embedded layout.
		/// </summary>
		[ObservableProperty]
		private bool hasEmbeddedLayout;

		/// <summary>
		/// Gets or sets the plain-text token description used as a safe fallback.
		/// </summary>
		[ObservableProperty]
		private string descriptionText = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether a description is available.
		/// </summary>
		[ObservableProperty]
		private bool hasDescription;

		/// <summary>
		/// Gets or sets a value indicating whether the description has a rich rendered representation.
		/// </summary>
		[ObservableProperty]
		private bool hasRichDescription;

		/// <summary>
		/// Gets or sets the short identifier displayed outside Advanced.
		/// </summary>
		[ObservableProperty]
		private string shortDisplayTokenId = string.Empty;

		/// <summary>
		/// Gets or sets the fallback glyph character.
		/// </summary>
		[ObservableProperty]
		private string fallbackGlyphText = "•";

		/// <summary>
		/// Gets or sets the localized token status.
		/// </summary>
		[ObservableProperty]
		private string statusText = string.Empty;

		/// <summary>
		/// Gets or sets the theme-driven status tone.
		/// </summary>
		[ObservableProperty]
		private StatusPillTone statusTone = StatusPillTone.Neutral;

		/// <summary>
		/// Gets or sets the localized ownership relationship.
		/// </summary>
		[ObservableProperty]
		private string ownershipText = string.Empty;

		/// <summary>
		/// Gets or sets the localized value summary.
		/// </summary>
		[ObservableProperty]
		private string valueText = string.Empty;

		/// <summary>
		/// Gets or sets the localized expiry summary.
		/// </summary>
		[ObservableProperty]
		private string expiryText = string.Empty;

		/// <summary>
		/// Gets or sets the localized update summary.
		/// </summary>
		[ObservableProperty]
		private string updatedText = string.Empty;

		/// <summary>
		/// Gets or sets the plain-language attention summary.
		/// </summary>
		[ObservableProperty]
		private string attentionText = string.Empty;

		/// <summary>
		/// Gets or sets the explanation shown when there is no primary token action.
		/// </summary>
		[ObservableProperty]
		private string nextActionExplanation = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether a meaningful value is available.
		/// </summary>
		[ObservableProperty]
		private bool hasValue;

		/// <summary>
		/// Gets or sets a value indicating whether meaningful expiry information is available.
		/// </summary>
		[ObservableProperty]
		private bool hasExpiry;

		/// <summary>
		/// Gets or sets a value indicating whether the token needs attention.
		/// </summary>
		[ObservableProperty]
		private bool needsAttention;

		/// <summary>
		/// Gets or sets a value indicating whether the page has token content.
		/// </summary>
		[ObservableProperty]
		private bool hasContent;

		/// <summary>
		/// Gets or sets a value indicating whether the first token load is running.
		/// </summary>
		[ObservableProperty]
		private bool isInitialLoading;

		/// <summary>
		/// Gets or sets a value indicating whether a token refresh is running.
		/// </summary>
		[ObservableProperty]
		private bool isRefreshing;

		/// <summary>
		/// Gets or sets a value indicating whether a token load or refresh error is visible.
		/// </summary>
		[ObservableProperty]
		private bool hasLoadError;

		/// <summary>
		/// Gets or sets the localized token load or refresh error.
		/// </summary>
		[ObservableProperty]
		private string loadErrorMessage = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether token activity is loading.
		/// </summary>
		[ObservableProperty]
		private bool isActivityLoading;

		/// <summary>
		/// Gets or sets a value indicating whether token activity could not be loaded.
		/// </summary>
		[ObservableProperty]
		private bool hasActivityError;

		/// <summary>
		/// Gets or sets the localized token activity error message.
		/// </summary>
		[ObservableProperty]
		private string activityErrorMessage = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether the activity timeline has entries.
		/// </summary>
		[ObservableProperty]
		private bool hasActivityItems;

		/// <summary>
		/// Gets or sets a value indicating whether the successful empty activity state is visible.
		/// </summary>
		[ObservableProperty]
		private bool showActivityEmpty;

		/// <summary>
		/// Gets or sets a value indicating whether the current owner can add a manual token update.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(AddTokenUpdateCommand))]
		private bool canAddTokenUpdate;

		/// <summary>
		/// Gets or sets a value indicating whether token-defined actions are loading.
		/// </summary>
		[ObservableProperty]
		private bool isTokenCommandsLoading;

		/// <summary>
		/// Gets or sets a value indicating whether applicable token-defined actions are available.
		/// </summary>
		[ObservableProperty]
		private bool hasTokenNoteCommands;

		/// <summary>
		/// Gets or sets a value indicating whether token-defined actions could not be loaded.
		/// </summary>
		[ObservableProperty]
		private bool hasTokenCommandError;

		/// <summary>
		/// Gets or sets the localized token-defined action loading error.
		/// </summary>
		[ObservableProperty]
		private string tokenCommandErrorMessage = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether action eligibility reflects live state and variables.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinueTokenCommand))]
		[NotifyCanExecuteChangedFor(nameof(ContinueTokenCommandCommand))]
		private bool isTokenCommandContextCurrent;

		/// <summary>
		/// Gets or sets the action currently being reviewed.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinueTokenCommand))]
		[NotifyCanExecuteChangedFor(nameof(ContinueTokenCommandCommand))]
		private TokenNoteCommandDescriptor? selectedTokenNoteCommand;

		/// <summary>
		/// Gets or sets a value indicating whether an action is currently being reviewed.
		/// </summary>
		[ObservableProperty]
		private bool hasSelectedTokenNoteCommand;

		/// <summary>
		/// Gets or sets a value indicating whether the selected action has parameter editors.
		/// </summary>
		[ObservableProperty]
		private bool hasSelectedTokenCommandParameters;

		/// <summary>
		/// Gets or sets a value indicating whether the selected action became inapplicable while open.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinueTokenCommand))]
		[NotifyCanExecuteChangedFor(nameof(ContinueTokenCommandCommand))]
		private bool selectedTokenCommandUnavailable;

		/// <summary>
		/// Gets or sets a value indicating whether a token-defined action is being validated or submitted.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinueTokenCommand))]
		[NotifyPropertyChangedFor(nameof(CanCancelTokenCommand))]
		[NotifyCanExecuteChangedFor(nameof(ContinueTokenCommandCommand))]
		[NotifyCanExecuteChangedFor(nameof(CancelTokenCommandCommand))]
		private bool isExecutingTokenCommand;

		/// <summary>
		/// Gets or sets a value indicating whether action feedback is visible.
		/// </summary>
		[ObservableProperty]
		private bool hasTokenCommandExecutionFeedback;

		/// <summary>
		/// Gets or sets localized action validation or submission feedback.
		/// </summary>
		[ObservableProperty]
		private string tokenCommandExecutionFeedback = string.Empty;

		/// <summary>
		/// Gets or sets the localized role context for the selected action.
		/// </summary>
		[ObservableProperty]
		private string selectedTokenCommandRoleText = string.Empty;

		/// <summary>
		/// Gets or sets the localized visibility consequence for the selected action.
		/// </summary>
		[ObservableProperty]
		private string selectedTokenCommandVisibilityText = string.Empty;

		/// <summary>
		/// Gets a value indicating whether the selected action can continue to validation.
		/// </summary>
		public bool CanContinueTokenCommand =>
			this.SelectedTokenNoteCommand is not null &&
			!this.SelectedTokenCommandUnavailable &&
			this.IsTokenCommandContextCurrent &&
			!this.IsExecutingTokenCommand;

		/// <summary>
		/// Gets a value indicating whether the selected action can be cancelled.
		/// </summary>
		public bool CanCancelTokenCommand => !this.IsExecutingTokenCommand;

		/// <summary>
		/// Gets or sets a value indicating whether Overview is selected.
		/// </summary>
		[ObservableProperty]
		private bool isOverviewSelected = true;

		/// <summary>
		/// Gets or sets a value indicating whether Activity is selected.
		/// </summary>
		[ObservableProperty]
		private bool isActivitySelected;

		/// <summary>
		/// Gets or sets a value indicating whether Advanced is selected.
		/// </summary>
		[ObservableProperty]
		private bool isAdvancedSelected;

		/// <summary>
		/// Gets or sets a value indicating whether a primary next-step action is available.
		/// </summary>
		[ObservableProperty]
		private bool hasPrimaryActions;

		/// <summary>
		/// Gets or sets a value indicating whether identity relationships are available.
		/// </summary>
		[ObservableProperty]
		private bool hasRelationships;

		/// <summary>
		/// Gets or sets a value indicating whether related contracts are available.
		/// </summary>
		[ObservableProperty]
		private bool hasRelatedContracts;

		/// <summary>
		/// Gets or sets a value indicating whether additional participants are available.
		/// </summary>
		[ObservableProperty]
		private bool hasParticipants;

		/// <summary>
		/// Gets or sets a value indicating whether permission metadata is available.
		/// </summary>
		[ObservableProperty]
		private bool hasPermissions;

		/// <summary>
		/// Gets or sets a value indicating whether custom tags are available.
		/// </summary>
		[ObservableProperty]
		private bool hasTags;

		/// <summary>
		/// Gets or sets a value indicating whether marketplace actions are available.
		/// </summary>
		[ObservableProperty]
		private bool hasMarketplaceActions;

		/// <summary>
		/// Gets or sets a value indicating whether embedded layout or state-machine presentations are available.
		/// </summary>
		[ObservableProperty]
		private bool hasAdvancedPresentations;

		/// <summary>
		/// Gets or sets a value indicating whether machine-readable token definition content is available.
		/// </summary>
		[ObservableProperty]
		private bool hasMachineReadableDefinition;

		/// <summary>
		/// Gets or sets a value indicating whether raw proof or definition details are available.
		/// </summary>
		[ObservableProperty]
		private bool hasRawDetails;


		#endregion

		#region Commands

		/// <summary>
		/// Opens an applicable token-defined action for parameter entry and review.
		/// </summary>
		/// <param name="Descriptor">Action selected by the user.</param>
		[RelayCommand]
		private void OpenTokenNoteCommand(TokenNoteCommandDescriptor Descriptor)
		{
			if (Descriptor is null ||
				this.IsExecutingTokenCommand ||
				!this.IsTokenCommandContextCurrent)
			{
				return;
			}

			this.SelectedTokenCommandParameters.Clear();
			foreach (Parameter Parameter in Descriptor.Parameters)
			{
				this.SelectedTokenCommandParameters.Add(
					CreateTokenCommandParameterEditor(Parameter));
			}

			this.SelectedTokenNoteCommand = Descriptor;
			this.HasSelectedTokenNoteCommand = true;
			this.HasSelectedTokenCommandParameters =
				this.SelectedTokenCommandParameters.Count > 0;
			this.SelectedTokenCommandUnavailable = false;
			this.HasTokenCommandExecutionFeedback = false;
			this.TokenCommandExecutionFeedback = string.Empty;
			this.UpdateSelectedTokenCommandContextCopy();
		}

		/// <summary>
		/// Validates, confirms, and executes the selected token-defined action.
		/// </summary>
		[RelayCommand(
			AllowConcurrentExecutions = false,
			CanExecute = nameof(CanContinueTokenCommand))]
		private async Task ContinueTokenCommand()
		{
			Token? Token = this.currentToken;
			TokenNoteCommandDescriptor? Descriptor = this.SelectedTokenNoteCommand;
			if (Token is null || Descriptor is null)
				return;

			this.IsExecutingTokenCommand = true;
			this.HasTokenCommandExecutionFeedback = false;
			this.TokenCommandExecutionFeedback = string.Empty;
			this.ResetTokenCommandParameterValidation();

			try
			{
				IReadOnlyDictionary<string, object?> ParameterValues =
					this.CollectTokenCommandParameterValues();
				TokenNoteCommandResult Validation =
					await this.tokenNoteCommandService.ValidateAsync(
						Token,
						Descriptor,
						ParameterValues,
						CultureInfo.CurrentUICulture.Name);

				if (!Validation.IsReady)
				{
					this.HandleTokenCommandFailure(Validation);
					return;
				}

				string Confirmation = this.BuildTokenCommandConfirmation(Descriptor);
				bool Confirmed = await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ConfirmTokenAction)],
					Confirmation,
					ServiceRef.Localizer[nameof(AppResources.Confirm)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]);
				if (!Confirmed)
					return;

				TokenNoteCommandResult Result =
					await this.tokenNoteCommandService.ExecuteAsync(
						Token,
						Descriptor,
						ParameterValues,
						CultureInfo.CurrentUICulture.Name);

				if (!Result.Succeeded)
				{
					if (Result.Status == TokenNoteCommandStatus.OutcomeUncertain)
					{
						await this.RefreshWorkspaceAsync(Token.TokenId, false);
						this.SelectedTokenCommandUnavailable = true;
					}

					this.HandleTokenCommandFailure(Result);
					return;
				}

				(bool TokenLoaded, bool ActivityLoaded) Refresh =
					await this.RefreshWorkspaceAsync(Token.TokenId, false);
				bool RefreshSucceeded =
					Refresh.TokenLoaded &&
					Refresh.ActivityLoaded &&
					this.IsTokenCommandContextCurrent;
				string SuccessMessage = string.IsNullOrWhiteSpace(Result.Message)
					? ServiceRef.Localizer[nameof(AppResources.UpdateSent)]
					: Result.Message;
				if (!RefreshSucceeded)
				{
					SuccessMessage = string.Join(
						Environment.NewLine + Environment.NewLine,
						SuccessMessage,
						ServiceRef.Localizer[nameof(AppResources.UpdateSentRefreshFailed)]);
				}

				this.CloseTokenCommandEditor();
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					SuccessMessage);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogWarning(
					"Token-defined action UI flow failed.",
					new KeyValuePair<string, object?>(
						"FailureType",
						Ex.GetType().Name));
				this.SetTokenCommandFeedback(
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
			}
			finally
			{
				this.IsExecutingTokenCommand = false;
			}
		}

		/// <summary>
		/// Cancels the selected token-defined action without submitting an update.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanCancelTokenCommand))]
		private void CancelTokenCommand()
		{
			this.CloseTokenCommandEditor();
		}

		private static ObservableParameter CreateTokenCommandParameterEditor(
			Parameter Parameter)
		{
			return Parameter switch
			{
				BooleanParameter BooleanParameter =>
					new ObservableBooleanParameter(BooleanParameter),
				DateParameter DateParameter =>
					new ObservableDateParameter(DateParameter),
				DateTimeParameter DateTimeParameter =>
					new ObservableDateTimeParameter(DateTimeParameter),
				NumericalParameter NumericalParameter =>
					new ObservableNumericalParameter(NumericalParameter),
				StringParameter StringParameter =>
					new ObservableStringParameter(StringParameter),
				TimeParameter TimeParameter =>
					new ObservableTimeParameter(TimeParameter),
				DurationParameter DurationParameter =>
					new ObservableDurationParameter(DurationParameter),
				_ => throw new NotSupportedException()
			};
		}

		private IReadOnlyDictionary<string, object?> CollectTokenCommandParameterValues()
		{
			Dictionary<string, object?> Result = new(StringComparer.Ordinal);
			foreach (ObservableParameter Parameter in this.SelectedTokenCommandParameters)
				Result[Parameter.Parameter.Name] = Parameter.Parameter.ObjectValue;

			return Result;
		}

		private void ResetTokenCommandParameterValidation()
		{
			foreach (ObservableParameter Parameter in this.SelectedTokenCommandParameters)
			{
				Parameter.IsValid = true;
				Parameter.ValidationText = string.Empty;
			}
		}

		private void ApplyTokenCommandParameterErrors(
			IReadOnlyDictionary<string, string> Errors)
		{
			foreach (ObservableParameter Parameter in this.SelectedTokenCommandParameters)
			{
				if (!Errors.ContainsKey(Parameter.Parameter.Name))
					continue;

				Parameter.IsValid = false;
				Parameter.ValidationText = string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.TokenCommandParameterInvalidFormat)],
					Parameter.Label);
			}
		}

		private string BuildTokenCommandConfirmation(
			TokenNoteCommandDescriptor Descriptor)
		{
			List<string> Sections = [];
			string Consequence = Descriptor.ConfirmationText;
			if (string.IsNullOrWhiteSpace(Consequence))
			{
				Consequence = string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.TokenCommandDefaultConfirmationFormat)],
					Descriptor.Title,
					this.FriendlyName ?? this.ShortDisplayTokenId);
			}

			Sections.Add(Consequence);
			Sections.Add(this.SelectedTokenCommandRoleText);
			if (Descriptor.HasCurrentState)
			{
				Sections.Add(string.Concat(
					ServiceRef.Localizer[nameof(AppResources.CurrentState)],
					": ",
					Descriptor.CurrentState));
			}
			Sections.Add(this.SelectedTokenCommandVisibilityText);

			return string.Join(
				Environment.NewLine + Environment.NewLine,
				Sections.Where(Section => !string.IsNullOrWhiteSpace(Section)));
		}

		private void HandleTokenCommandFailure(TokenNoteCommandResult Result)
		{
			if (Result.Status == TokenNoteCommandStatus.ValidationFailed)
				this.ApplyTokenCommandParameterErrors(Result.ParameterErrors);

			string Message = Result.Status switch
			{
				TokenNoteCommandStatus.ValidationFailed =>
					ServiceRef.Localizer[nameof(AppResources.TokenCommandInvalidParameters)],
				TokenNoteCommandStatus.NotAvailable =>
					ServiceRef.Localizer[nameof(AppResources.TokenActionUnavailable)],
				TokenNoteCommandStatus.Unsafe =>
					ServiceRef.Localizer[nameof(AppResources.UnsafeTokenAction)],
				TokenNoteCommandStatus.UnsupportedResult =>
					ServiceRef.Localizer[nameof(AppResources.TokenCommandUnsupportedResult)],
				TokenNoteCommandStatus.OutcomeUncertain =>
					ServiceRef.Localizer[nameof(AppResources.UpdateOutcomeUncertain)],
				_ => string.IsNullOrWhiteSpace(Result.Message)
					? ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]
					: Result.Message
			};

			if (Result.Status is
				TokenNoteCommandStatus.NotAvailable or
				TokenNoteCommandStatus.Unsafe or
				TokenNoteCommandStatus.UnsupportedResult or
				TokenNoteCommandStatus.OutcomeUncertain)
			{
				this.SelectedTokenCommandUnavailable = true;
			}

			this.SetTokenCommandFeedback(Message);
		}

		private void SetTokenCommandFeedback(string Message)
		{
			this.TokenCommandExecutionFeedback = Message;
			this.HasTokenCommandExecutionFeedback =
				!string.IsNullOrWhiteSpace(Message);
		}

		private void UpdateSelectedTokenCommandContextCopy()
		{
			TokenNoteCommandDescriptor? Descriptor = this.SelectedTokenNoteCommand;
			if (Descriptor is null)
			{
				this.SelectedTokenCommandRoleText = string.Empty;
				this.SelectedTokenCommandVisibilityText = string.Empty;
				return;
			}

			this.SelectedTokenCommandRoleText = ServiceRef.Localizer[
				Descriptor.IsOwnerContext
					? nameof(AppResources.TokenCommandOwnerContext)
					: nameof(AppResources.TokenCommandExternalContext)];
			this.SelectedTokenCommandVisibilityText = ServiceRef.Localizer[
				Descriptor.Personal
					? nameof(AppResources.PersonalUpdateDescription)
					: nameof(AppResources.TokenCommandPublicUpdateDescription)];
		}

		private void CloseTokenCommandEditor()
		{
			this.SelectedTokenNoteCommand = null;
			this.SelectedTokenCommandParameters.Clear();
			this.HasSelectedTokenNoteCommand = false;
			this.HasSelectedTokenCommandParameters = false;
			this.SelectedTokenCommandUnavailable = false;
			this.HasTokenCommandExecutionFeedback = false;
			this.TokenCommandExecutionFeedback = string.Empty;
			this.SelectedTokenCommandRoleText = string.Empty;
			this.SelectedTokenCommandVisibilityText = string.Empty;
		}

		/// <summary>
		/// Opens the explicit manual token update composer.
		/// </summary>
		[RelayCommand(
			AllowConcurrentExecutions = false,
			CanExecute = nameof(CanAddTokenUpdate))]
		private async Task AddTokenUpdate()
		{
			TokenUpdateComposerViewModel ViewModel = new(this.SubmitManualTokenUpdateAsync);
			TokenUpdateComposerPopup Popup = new(ViewModel);

			await ServiceRef.PopupService.PushAsync(
				Popup,
				PopupOptions.CreateModal(closeOnBackButton: false));
			TokenUpdateSubmissionResult? Result = await ViewModel.Result;
			if (Result is null || !Result.ShouldClose)
				return;

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
				Result.Message);
		}

		/// <summary>
		/// Refreshes the current token from the authoritative server source.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RefreshToken()
		{
			string TokenId = this.TokenId ??
				this.navigationArguments?.TokenId ??
				string.Empty;

			if (string.IsNullOrWhiteSpace(TokenId))
				return;

			await this.RefreshWorkspaceAsync(TokenId, this.currentToken is null);
		}

		/// <summary>
		/// Selects the human-readable token overview.
		/// </summary>
		[RelayCommand]
		private void ShowOverview()
		{
			this.IsOverviewSelected = true;
			this.IsActivitySelected = false;
			this.IsAdvancedSelected = false;
		}

		/// <summary>
		/// Selects token activity.
		/// </summary>
		[RelayCommand]
		private void ShowActivity()
		{
			this.IsOverviewSelected = false;
			this.IsActivitySelected = true;
			this.IsAdvancedSelected = false;
		}

		/// <summary>
		/// Selects progressively disclosed advanced token information.
		/// </summary>
		[RelayCommand]
		private void ShowAdvanced()
		{
			this.IsOverviewSelected = false;
			this.IsActivitySelected = false;
			this.IsAdvancedSelected = true;
		}

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		[RelayCommand]
		public static async Task CopyToClipboard(object Parameter)
		{
			try
			{
				string s = Parameter?.ToString() ?? string.Empty;
				int i = s.IndexOf('@');

				if (i > 0 && Guid.TryParse(s[..i], out _))
				{
					await Clipboard.SetTextAsync(Constants.UriSchemes.NeuroFeature + ":" + s);
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]);
				}
				else
				{
					await Clipboard.SetTextAsync(s);
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
				}
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token clipboard operation failed.", ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to view a Legal ID.
		/// </summary>
		[RelayCommand]
		private static async Task ViewId(object Parameter)
		{
			string? LegalId = Parameter?.ToString();
			if (string.IsNullOrEmpty(LegalId))
				return;

			try
			{
				await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(LegalId, ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)]);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to view a smart contract.
		/// </summary>
		[RelayCommand]
		private static async Task ViewContract(object Parameter)
		{
			string? ContractId = Parameter?.ToString();
			if (string.IsNullOrEmpty(ContractId))
				return;

			try
			{
				await ServiceRef.ContractOrchestratorService.OpenContract(ContractId, ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)], null);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to open a chat page.
		/// </summary>
		[RelayCommand]
		private async Task OpenChat(object Parameter)
		{
			string? s = Parameter?.ToString();
			if (string.IsNullOrEmpty(s))
				return;

			string? BareJid;
			string? LegalId;
			string? FriendlyName;

			switch (s)
			{
				case "Owner":
					BareJid = this.OwnerJid;
					LegalId = this.Owner;
					FriendlyName = this.OwnerFriendlyName;
					break;

				case "Creator":
					BareJid = this.CreatorJid;
					LegalId = this.Creator;
					FriendlyName = this.CreatorFriendlyName;
					break;

				case "TrustProvider":
					BareJid = this.TrustProviderJid;
					LegalId = this.TrustProvider;
					FriendlyName = this.TrustProviderFriendlyName;
					break;

				default:
					string[] Parts = s.Split(" | ");
					if (Parts.Length != 3)
						return;

					BareJid = Parts[0];
					LegalId = Parts[1];
					FriendlyName = Parts[2];
					break;
			}

			try
			{
				ChatNavigationArgs Args = new(LegalId, BareJid, FriendlyName);
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Inherited, BareJid);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to open a link.
		/// </summary>
		[RelayCommand]
		private static async Task OpenLink(object Parameter)
		{
			if (Parameter is not null)
				await App.OpenUrlAsync(Parameter.ToString()!);
		}

		/// <summary>
		/// Command to show machine-readable details of token.
		/// </summary>
		[RelayCommand]
		private async Task ShowM2mInfo()
		{
			if (this.Definition is null)
				return;

			bool Confirmed = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ShowM2MInfo)],
				ServiceRef.Localizer[nameof(AppResources.MachineReadableInfoTextToken)],
				ServiceRef.Localizer[nameof(AppResources.Open)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);
			if (!Confirmed)
				return;

			try
			{
				byte[] Bin = Encoding.UTF8.GetBytes(this.Definition);
				HttpFileUploadEventArgs e = await ServiceRef.XmppService.RequestUploadSlotAsync(this.TokenId + ".xml", "text/xml; charset=utf-8", Bin.Length);

				if (e.Ok)
				{
					await e.PUT(Bin, "text/xml", (int)Constants.Timeouts.UploadFile.TotalMilliseconds);
					await App.OpenUrlAsync(e.GetUrl);
				}
				else
					await ServiceRef.UiService.DisplayException(e.StanzaError ?? new Exception(e.ErrorText));
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to send token to contact
		/// </summary>
		[RelayCommand]
		private async Task SendToContact()
		{
			TaskCompletionSource<ContactInfoModel?> Selected = new();
			ContactListNavigationArgs ContactListArgs = new(ServiceRef.Localizer[nameof(AppResources.SendInformationTo)], Selected)
			{
				CanScanQrCode = true,
			};

			await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), ContactListArgs, BackMethod.Pop);

			ContactInfoModel? Contact = await Selected.Task;
			if (Contact is null)
				return;

			string Recipient = Normalize(Contact.FriendlyName);
			if (string.IsNullOrEmpty(Recipient))
				Recipient = Contact.BareJid?.ToString() ?? string.Empty;

			string Preview = string.Format(
				CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.TokenSendPreviewFormat)],
				this.FriendlyName ?? this.ShortDisplayTokenId,
				Recipient,
				this.QrCodeUri ?? Constants.UriSchemes.CreateTokenUri(this.TokenId ?? string.Empty));
			bool Confirmed = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.TokenSendPreviewTitle)],
				Preview,
				ServiceRef.Localizer[nameof(AppResources.Send)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);
			if (!Confirmed)
				return;

			StringBuilder Markdown = new();

			Markdown.Append("```");
			Markdown.AppendLine(Constants.UriSchemes.NeuroFeature);
			Markdown.AppendLine(this.TokenXml);
			Markdown.AppendLine("```");

			await ChatViewModel.ExecuteSendMessage(string.Empty, Markdown.ToString(), Contact.BareJid);

			if (Contact.Contact is not null)
			{
				await Task.Delay(100);  // Otherwise, page doesn't show properly. (Underlying timing issue. TODO: Find better solution.)

				ChatNavigationArgs ChatArgs = new(Contact.Contact);
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), ChatArgs, BackMethod.Inherited, Contact.BareJid);
			}
		}

		/// <summary>
		/// Command to share token with other applications
		/// </summary>
		[RelayCommand]
		private async Task Share()
		{
			try
			{
				if (this.QrCodeBin is null)
					return;

				await this.OpenQrPopup(this.FriendlyName);
			}
			catch (Exception ex)
			{
				LogRedactedFailure("Token sharing preview failed.", ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to publish the token on the marketplace, for sale.
		/// </summary>
		[RelayCommand]
		private async Task PublishMarketplace()
		{
			if (!await this.EnsureMarketplaceIdentityAsync())
				return;

			try
			{
				CreationAttributesEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
				Contract Template = await ServiceRef.XmppService.GetContract(Constants.ContractTemplates.TokenConsignmentTemplate);
				Template.Visibility = ContractVisibility.Public;
				NewContractNavigationArgs NewContractArgs = new(Template, true,
					new Dictionary<CaseInsensitiveString, object>()
					{
						{ "TokenID", this.TokenId ?? string.Empty },
						{ "Category", this.Category ?? string.Empty },
						{ "FriendlyName", this.FriendlyName ?? string.Empty },
						{ "CommissionPercent", e.Commission },
						{ "Currency", e.Currency }
					});

				Template.Parts = 
				[
					new()
					{
						Role = "Seller",
						LegalId = ServiceRef.TagProfile.LegalIdentity?.Id
					},
					new()
					{
						Role = "Auctioneer",
						LegalId = e.TrustProviderId
					}
				];

				NewContractArgs.SuppressProposal(e.TrustProviderId);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to offer the token for sale.
		/// </summary>
		[RelayCommand]
		private async Task OfferToSell()
		{
			if (!await this.EnsureMarketplaceIdentityAsync())
				return;

			try
			{
				Dictionary<CaseInsensitiveString, object> Parameters = [];
				string? TrustProviderId = null;
				Contract Template = await ServiceRef.XmppService.GetContract(Constants.ContractTemplates.TransferTokenTemplate);
				Template.Visibility = ContractVisibility.Public;

				if (Template.ForMachinesLocalName == "Transfer" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(Template.ForMachines.OuterXml);

					TrustProviderId = e.TrustProviderId;

					XmlNamespaceManager NamespaceManager = new(Doc.NameTable);
					NamespaceManager.AddNamespace("nft", NeuroFeaturesClient.NamespaceNeuroFeatures);

					string? SellerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Seller/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TrustProviderRole = Doc.SelectSingleNode("/nft:Transfer/nft:TrustProvider/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TokenIdParameter = Doc.SelectSingleNode("/nft:Transfer/nft:TokenID/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CurrencyParameter = Doc.SelectSingleNode("/nft:Transfer/nft:Currency/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CommissionParameter = Doc.SelectSingleNode("/nft:Transfer/nft:CommissionPercent/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? OwnershipContractParameter = Doc.SelectSingleNode("/nft:Transfer/nft:OwnershipContract/nft:ParameterReference/@parameter", NamespaceManager)?.Value;

					if (Template.Parts is null)
					{
						List<Part> Parts = [];

						if (!string.IsNullOrEmpty(SellerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = ServiceRef.TagProfile.LegalIdentity?.Id,
								Role = SellerRole
							});
						}

						if (!string.IsNullOrEmpty(TrustProviderRole))
						{
							Parts.Add(new Part()
							{
								LegalId = e.TrustProviderId,
								Role = TrustProviderRole
							});
						}

						Template.Parts = [.. Parts];
						Template.PartsMode = ContractParts.ExplicitlyDefined;
					}
					else
					{
						foreach (Part Part in Template.Parts)
						{
							if (Part.Role == SellerRole)
								Part.LegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
							else if (Part.Role == TrustProviderRole)
								Part.LegalId = e.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(TokenIdParameter))
						Parameters[TokenIdParameter] = this.TokenId ?? string.Empty;

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Parameters[CurrencyParameter] = e.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Parameters[CommissionParameter] = e.Commission;

					if (!string.IsNullOrEmpty(OwnershipContractParameter))
						Parameters[OwnershipContractParameter] = this.OwnershipContract ?? string.Empty;
				}

				NewContractNavigationArgs NewContractArgs = new(Template, true, Parameters);

				if (!string.IsNullOrEmpty(TrustProviderId))
					NewContractArgs.SuppressProposal(TrustProviderId);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to offer to buy the token.
		/// </summary>
		[RelayCommand]
		private async Task OfferToBuy()
		{
			if (!await this.EnsureMarketplaceIdentityAsync())
				return;

			try
			{
				Dictionary<CaseInsensitiveString, object> Parameters = [];
				string? TrustProviderId = null;
				Contract Template = await ServiceRef.XmppService.GetContract(Constants.ContractTemplates.TransferTokenTemplate);
				Template.Visibility = ContractVisibility.Public;

				if (Template.ForMachinesLocalName == "Transfer" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(Template.ForMachines.OuterXml);

					TrustProviderId = e.TrustProviderId;

					XmlNamespaceManager NamespaceManager = new(Doc.NameTable);
					NamespaceManager.AddNamespace("nft", NeuroFeaturesClient.NamespaceNeuroFeatures);

					string? BuyerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Buyer/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? SellerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Seller/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TrustProviderRole = Doc.SelectSingleNode("/nft:Transfer/nft:TrustProvider/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TokenIdParameter = Doc.SelectSingleNode("/nft:Transfer/nft:TokenID/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CurrencyParameter = Doc.SelectSingleNode("/nft:Transfer/nft:Currency/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CommissionParameter = Doc.SelectSingleNode("/nft:Transfer/nft:CommissionPercent/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? OwnershipContractParameter = Doc.SelectSingleNode("/nft:Transfer/nft:OwnershipContract/nft:ParameterReference/@parameter", NamespaceManager)?.Value;

					if (Template.Parts is null)
					{
						List<Part> Parts = [];

						if (!string.IsNullOrEmpty(BuyerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = ServiceRef.TagProfile.LegalIdentity?.Id,
								Role = BuyerRole
							});
						}

						if (!string.IsNullOrEmpty(SellerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = this.Owner,
								Role = SellerRole
							});
						}

						if (!string.IsNullOrEmpty(TrustProviderRole))
						{
							Parts.Add(new Part()
							{
								LegalId = e.TrustProviderId,
								Role = TrustProviderRole
							});
						}

						Template.Parts = [.. Parts];
						Template.PartsMode = ContractParts.ExplicitlyDefined;
					}
					else
					{
						foreach (Part Part in Template.Parts)
						{
							if (Part.Role == BuyerRole)
								Part.LegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
							else if (Part.Role == SellerRole)
								Part.LegalId = this.Owner;
							else if (Part.Role == TrustProviderRole)
								Part.LegalId = e.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(TokenIdParameter))
						Parameters[TokenIdParameter] = this.TokenId ?? string.Empty;

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Parameters[CurrencyParameter] = e.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Parameters[CommissionParameter] = e.Commission;

					if (!string.IsNullOrEmpty(OwnershipContractParameter))
						Parameters[OwnershipContractParameter] = this.OwnershipContract ?? string.Empty;
				}

				NewContractNavigationArgs NewContractArgs = new(Template, true, Parameters);

				if (!string.IsNullOrEmpty(TrustProviderId))
					NewContractArgs.SuppressProposal(TrustProviderId);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand]
		private async Task PresentEmbeddedLayout()
		{
			if (this.TokenId is null || this.currentToken is null)
				return;

			EmbeddedLayoutNavigationArgs Args = new(this.currentToken);

			await ServiceRef.NavigationService.GoToAsync(nameof(EmbeddedLayoutPage), Args, BackMethod.Pop);
		}

		/// <summary>
		/// Command to display the present state report of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task PresentReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenPresentReport(this.TokenId));
		}

		/// <summary>
		/// Command to display the historic states report of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task HistoryReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenHistoryReport(this.TokenId));
		}

		/// <summary>
		/// Command to display the state diagram of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task StatesReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenStateDiagramReport(this.TokenId));
		}

		/// <summary>
		/// Command to display a profiling report of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task ProfilingReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenProfilingReport(this.TokenId));
		}

		/// <summary>
		/// Command to display the current states of variables of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task VariablesReport()
		{
			if (this.TokenId is null)
				return;

			try
			{
				CurrentStateEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCurrentState(this.TokenId);
				if (e.Ok)
				{
					MachineVariablesNavigationArgs Args = new(
						this.TokenId,
						e.Running,
						e.Ended,
						e.CurrentState,
						e.Variables);

					await ServiceRef.NavigationService.GoToAsync(nameof(MachineVariablesPage), Args, BackMethod.Pop);
				}
				else
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], e.ErrorText);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async Task ShowReport(TokenReport Report)
		{
			try
			{
				MachineReportNavigationArgs Args = new(Report);

				await ServiceRef.NavigationService.GoToAsync(nameof(MachineReportPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
			}
		}

		private async Task<bool> EnsureMarketplaceIdentityAsync()
		{
			if (HasApprovedMarketplaceIdentity())
				return true;

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.Unavailable)],
				ServiceRef.Localizer[nameof(AppResources.MarketplaceIdentityRequired)]);
			return false;
		}

		#endregion

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(this.FriendlyName ?? string.Empty);

		#endregion


	}
}
