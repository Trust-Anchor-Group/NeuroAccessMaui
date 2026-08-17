using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Popups.Image;
using NeuroFeatures;
using Waher.Content;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// View model for displaying and managing a contract in the "View Contract" page.
	/// </summary>
	public partial class ViewContractViewModel : QrXmppViewModel
	{
		#region Fields

		private readonly ViewContractNavigationArgs? args;
		private readonly PhotosLoader attachmentLoader = new();
		private static readonly TimeSpan contractHydrationTimeout = TimeSpan.FromSeconds(45);
		private static readonly TimeSpan contractSigningTimeout = TimeSpan.FromSeconds(45);
		private static readonly TimeSpan stateChangeWaitTimeout = TimeSpan.FromSeconds(2);

		// Refresh coalescing state
		private readonly object refreshLock = new();
		private bool refreshInProgress;
		private bool refreshQueued;
		private Contract? pendingContractForRefresh;
		private ContractReference? sourceContractReference;
		private bool isUsingSavedContract;
		private bool refreshFailedUsingSaved;
		private bool initialized;
		private bool disposed;
		private long deferredUiLoadGeneration;
		private long relatedTokenLoadGeneration;

		// Bindable compatibility state for callers that complete work after navigation.
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanShowSignBar))]
		[NotifyPropertyChangedFor(nameof(CanContinueToSigning))]
		private bool isAwaitingPostCreateCompletion;

		// Suppress RefreshView.Command when setting IsRefreshing programmatically
		private bool suppressNextRefreshCommand;

		#endregion

		#region Constructor

		/// <summary>
		/// Default constructor. Retrieves navigation arguments and sets up commands.
		/// </summary>
		public ViewContractViewModel()
		{
			this.args = ServiceRef.NavigationService.PopLatestArgs<ViewContractNavigationArgs>();

			this.XmppUriClicked = this.CreateUriCommand();
			this.IotIdUriClicked = this.CreateUriCommand();
			this.IotScUriClicked = this.CreateUriCommand();
			this.NeuroFeatureUriClicked = this.CreateUriCommand();
			this.IotDiscoUriClicked = this.CreateUriCommand();
			this.EDalerUriClicked = this.CreateUriCommand();
			this.HyperlinkClicked = new Command(async p => await this.ExecuteHyperlinkClicked(p));

			this.contractSignedHandler = new EventHandlerAsync<ContractSignedEventArgs>(this.OnContractSignedAsync);
			this.contractUpdatedHandler = new EventHandlerAsync<ContractReferenceEventArgs>(this.OnContractUpdatedAsync);
		}

		#endregion

		#region Initialization and Disposal

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (!this.ValidateArgs())
				return;

			this.SubscribeToEvents();

			try
			{
				// Ensure args.Contract is loaded and displayed first
				ServiceRef.LogService.LogDebug("Contract workspace initialization started.");
				await this.LoadContractAsync();
				if (this.disposed || this.Contract is null)
					return;

				ServiceRef.LogService.LogDebug("Contract workspace structure is ready.");
				await this.InitializeUIAsync();
				if (this.disposed)
					return;

				await this.GoToStateAsync(ViewContractStep.Overview);
				ServiceRef.LogService.LogDebug("Contract workspace overview is visible.");
				await this.StartDeferredUiPreparationAsync();
				if (this.disposed)
					return;

				if (this.args?.PostCreateCompletion is not null)
				{
					// Do not await here: navigation must finish before the source page can
					// complete its post-create work and signal this compatibility task.
					this.IsAwaitingPostCreateCompletion = true;
					_ = this.ApplyPostCreateCompletionAsync(this.args.PostCreateCompletion.Task);
				}

				// Saved content makes the workspace immediately usable. Reconcile it quietly
				// after the first overview is visible so cached-first loading never looks like
				// an error and the opening interaction is not blocked by the network.
				this.initialized = true;
				if (this.isUsingSavedContract)
					this.RequestRefresh(null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				if (this.disposed)
					return;

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
				await this.GoBack();
			}
		}

		private async Task ApplyPostCreateCompletionAsync(Task<Contract?> CompletionTask)
		{
			try
			{
				Contract? Completed = await CompletionTask;
				if (Completed is not null)
					await this.RefreshContractAsync(Completed);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.IsAwaitingPostCreateCompletion = false;
			}
		}

		public override async Task OnDisposeAsync()
		{
			this.disposed = true;
			Interlocked.Increment(ref this.deferredUiLoadGeneration);
			Interlocked.Increment(ref this.relatedTokenLoadGeneration);
			this.UnsubscribeFromEvents();
			this.attachmentLoader.CancelLoadPhotos();
			this.Contract?.Dispose();
			await base.OnDisposeAsync();
		}

		#endregion

		#region Properties

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.PropertyName == nameof(this.IsBusy))
			{
				this.OnPropertyChanged(nameof(this.CanSign));
				this.OnPropertyChanged(nameof(this.CanShowSignBar));
				this.OnPropertyChanged(nameof(this.CanContinueToSigning));
				this.OnPropertyChanged(nameof(this.HasPrimaryAction));
			}
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSign))]
		[NotifyPropertyChangedFor(nameof(CanShowSignBar))]
		[NotifyPropertyChangedFor(nameof(CanContinueToSigning))]
		[NotifyPropertyChangedFor(nameof(HasContract))]
		[NotifyPropertyChangedFor(nameof(HasServerSignature))]
		[NotifyPropertyChangedFor(nameof(HasMachineReadableContent))]
		[NotifyPropertyChangedFor(nameof(MachineReadableContent))]
		[NotifyPropertyChangedFor(nameof(HasTemplateId))]
		[NotifyPropertyChangedFor(nameof(HasProvider))]
		[NotifyPropertyChangedFor(nameof(HasSignAfter))]
		[NotifyPropertyChangedFor(nameof(HasSignBefore))]
		private ObservableContract? contract;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSign))]
		[NotifyPropertyChangedFor(nameof(CanShowSignBar))]
		[NotifyPropertyChangedFor(nameof(CanContinueToSigning))]
		[NotifyPropertyChangedFor(nameof(HasPrimaryAction))]
		[NotifyCanExecuteChangedFor(nameof(SignCommand))]
		private bool isRefreshing = false;

		partial void OnIsRefreshingChanged(bool Value)
		{
			this.UpdateFreshnessPresentation();
		}

		/// <summary>
		/// Gets whether an agreement has been loaded for presentation.
		/// </summary>
		public bool HasContract => this.Contract is not null;

		/// <summary>
		/// Gets the key parameters promoted into the agreement overview.
		/// </summary>
		public ObservableCollection<ObservableParameter> KeyParameters { get; } = [];

		/// <summary>
		/// Gets the reliable activity available from the contract and local notifications.
		/// </summary>
		public ObservableCollection<ContractActivityItem> ActivityItems { get; } = [];

		/// <summary>
		/// Gets privacy-conscious summaries of the agreement's existing attachments.
		/// </summary>
		public ObservableCollection<ContractAttachmentItem> AttachmentItems { get; } = [];

		/// <summary>
		/// Gets tokens created or affected by the agreement.
		/// </summary>
		public ObservableCollection<ContractRelatedTokenItem> RelatedTokens { get; } = [];

		/// <summary>
		/// Gets whether at least one promoted key parameter is available.
		/// </summary>
		public bool HasKeyParameters => this.KeyParameters.Count > 0;

		/// <summary>
		/// Gets whether reliable agreement history is available.
		/// </summary>
		public bool HasActivity => this.ActivityItems.Count > 0;

		/// <summary>
		/// Gets whether the agreement contains existing attachments.
		/// </summary>
		public bool HasAttachments => this.AttachmentItems.Count > 0;

		/// <summary>
		/// Gets whether at least one related token identifier is available.
		/// </summary>
		public bool HasRelatedTokens => this.RelatedTokens.Count > 0;

		/// <summary>
		/// Gets whether the related-token section has useful state to present.
		/// </summary>
		public bool HasRelatedTokensSection =>
			this.HasRelatedTokens ||
			this.IsRelatedTokensLoading ||
			this.RelatedTokensUnavailable;

		/// <summary>
		/// Gets or sets whether related token references are loading.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasRelatedTokensSection))]
		private bool isRelatedTokensLoading;

		/// <summary>
		/// Gets or sets whether related token references could not be refreshed.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasRelatedTokensSection))]
		private bool relatedTokensUnavailable;

		/// <summary>
		/// Gets or sets whether all tokens created by this agreement can be browsed.
		/// </summary>
		[ObservableProperty]
		private bool canBrowseCreatedTokens;

		/// <summary>
		/// Gets whether the contract has a server signature that can be inspected.
		/// </summary>
		public bool HasServerSignature => this.Contract?.Contract.ServerSignature is not null;

		/// <summary>
		/// Gets whether machine-readable agreement content is available.
		/// </summary>
		public bool HasMachineReadableContent => !string.IsNullOrWhiteSpace(this.MachineReadableContent);

		/// <summary>
		/// Gets whether the contract includes a template identifier.
		/// </summary>
		public bool HasTemplateId => !string.IsNullOrWhiteSpace(this.Contract?.TemplateId);

		/// <summary>
		/// Gets whether the contract identifies its provider.
		/// </summary>
		public bool HasProvider => !string.IsNullOrWhiteSpace(this.Contract?.Contract.Provider);

		/// <summary>
		/// Gets whether the contract defines the earliest signing time.
		/// </summary>
		public bool HasSignAfter => this.Contract?.Contract.SignAfter.HasValue == true;

		/// <summary>
		/// Gets whether the contract defines a signing deadline.
		/// </summary>
		public bool HasSignBefore => this.Contract?.Contract.SignBefore.HasValue == true;

		/// <summary>
		/// Gets the complete machine-readable contract content for advanced inspection.
		/// </summary>
		public string MachineReadableContent =>
			this.Contract?.Contract.ForMachines?.OuterXml ?? string.Empty;

		/// <summary>
		/// Gets whether the loaded agreement has a primary review or response action.
		/// </summary>
		public bool HasPrimaryAction => this.CanSign || this.IsProposal;

		/// <summary>
		/// Gets whether the complete agreement is available and signing can continue.
		/// </summary>
		public bool CanContinueToSigning =>
			this.CanShowSignBar &&
			this.HasHumanReadableText;

		/// <summary>
		/// Gets whether a local saved reference can be removed independently of the remote contract.
		/// </summary>
		public bool CanRemoveLocalReference => this.sourceContractReference is not null;

		/// <summary>
		/// Gets the localized title used by the persistent agreement summary.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSummaryCategory))]
		private string summaryTitle = string.Empty;

		/// <summary>
		/// Gets the optional category shown beneath the agreement title.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSummaryCategory))]
		private string summaryCategory = string.Empty;

		/// <summary>
		/// Gets whether an agreement category is available.
		/// </summary>
		public bool HasSummaryCategory =>
			!string.IsNullOrWhiteSpace(this.SummaryCategory) &&
			!string.Equals(
				this.SummaryCategory,
				this.SummaryTitle,
				StringComparison.CurrentCultureIgnoreCase);

		/// <summary>
		/// Gets the localized agreement state.
		/// </summary>
		[ObservableProperty]
		private string summaryStateText = string.Empty;

		/// <summary>
		/// Gets the semantic tone for the agreement state.
		/// </summary>
		[ObservableProperty]
		private StatusPillTone summaryStateTone = StatusPillTone.Neutral;

		/// <summary>
		/// Gets the current person's role summary.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSummaryRole))]
		private string summaryRoleText = string.Empty;

		/// <summary>
		/// Gets whether the current person's role can be derived.
		/// </summary>
		public bool HasSummaryRole => !string.IsNullOrWhiteSpace(this.SummaryRoleText);

		/// <summary>
		/// Gets the agreement signature-progress summary.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSignatureProgress))]
		private string signatureProgressText = string.Empty;

		/// <summary>
		/// Gets whether signature progress is available.
		/// </summary>
		public bool HasSignatureProgress => !string.IsNullOrWhiteSpace(this.SignatureProgressText);

		/// <summary>
		/// Gets the most important agreement date or deadline.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasImportantDate))]
		private string importantDateText = string.Empty;

		/// <summary>
		/// Gets whether an important date is available.
		/// </summary>
		public bool HasImportantDate => !string.IsNullOrWhiteSpace(this.ImportantDateText);

		/// <summary>
		/// Gets the next locally valid agreement action.
		/// </summary>
		[ObservableProperty]
		private string nextActionText = string.Empty;

		/// <summary>
		/// Gets the localized primary-action label.
		/// </summary>
		[ObservableProperty]
		private string primaryActionText = string.Empty;

		/// <summary>
		/// Gets the optional warning shown when a refresh cannot replace the saved agreement.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasFreshnessWarning))]
		private string freshnessWarningText = string.Empty;

		/// <summary>
		/// Gets whether a freshness warning should be displayed.
		/// </summary>
		public bool HasFreshnessWarning => !string.IsNullOrWhiteSpace(this.FreshnessWarningText);

		/// <summary>
		/// Gets or sets whether the current contract version was fully reviewed before signing.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ReadyToSign))]
		private bool hasReviewedForSigning;

		public BindableObject? StateObject { get; set; }

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(GoToParametersCommand))]
		private bool canStateChange;

		[ObservableProperty]
		private string currentState = nameof(ViewContractStep.Loading);

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasHumanReadableText))]
		[NotifyPropertyChangedFor(nameof(CanContinueToSigning))]
		private VerticalStackLayout? humanReadableText;

		public bool HasHumanReadableText => this.HumanReadableText is not null;

		public ObservableCollection<ObservableParameter> DisplayableParameters { get; } = new();

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ReadyToSign))]
		[NotifyCanExecuteChangedFor(nameof(SignCommand))]
		private bool isContractOk;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasProposalFriendlyName))]
		[NotifyPropertyChangedFor(nameof(IsProposal))]
		private string? proposalFriendlyName;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasProposalRole))]
		private string? proposalRole;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasProposalMessage))]
		[NotifyPropertyChangedFor(nameof(IsProposal))]
		private string? proposalMessage;

		public bool IsProposal =>
			!string.IsNullOrEmpty(this.ProposalRole) ||
			!string.IsNullOrEmpty(this.ProposalMessage) ||
			!string.IsNullOrEmpty(this.ProposalFriendlyName);

		public bool HasProposalFriendlyName => !string.IsNullOrEmpty(this.ProposalFriendlyName);
		public bool HasProposalRole => !string.IsNullOrEmpty(this.ProposalRole);
		public bool HasProposalMessage => !string.IsNullOrEmpty(this.ProposalMessage);

		public string? Visibility => this.Contract?.Visibility switch
		{
			ContractVisibility.Public => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)],
			ContractVisibility.CreatorAndParts => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)],
			ContractVisibility.DomainAndParts => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)],
			ContractVisibility.PublicSearchable => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)],
			_ => string.Empty
		};

		[ObservableProperty]
		private bool canDeleteContract;

		[ObservableProperty]
		private bool canObsoleteContract;

		#endregion

		#region Signing

		public ObservableCollection<ObservableRole> SignableRoles { get; } = new();

		private bool HasSignableRoles => this.SignableRoles.Count > 0;
		private bool IsInSigningState => this.Contract?.ContractState is ContractState.Approved or ContractState.BeingSigned;
		private bool AlreadySigned => this.Contract?.Roles.Any(r => r.Parts.Any(p => p.IsMe && p.HasSigned)) == true;

		public bool CanSign =>
			!this.IsBusy &&
			!this.IsRefreshing &&
			this.HasSignableRoles &&
			this.IsInSigningState &&
			!this.AlreadySigned;

		public bool CanShowSignBar => !this.IsAwaitingPostCreateCompletion && this.CanSign;

		public bool ReadyToSign =>
			this.HasReviewedForSigning &&
			this.SelectedRole is not null &&
			this.IsContractOk;

		[ObservableProperty]
		private ObservableRole? selectedRole;

		partial void OnSelectedRoleChanged(ObservableRole? oldValue, ObservableRole? newValue)
		{
			string? MyLegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
			if (string.IsNullOrEmpty(MyLegalId))
				return;

			oldValue?.RemovePart(MyLegalId);
			_ = newValue?.AddPart(MyLegalId);

			this.OnPropertyChanged(nameof(this.ReadyToSign));
			this.SignCommand.NotifyCanExecuteChanged();
		}

		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(ReadyToSign))]
		public async Task SignAsync()
		{
			if (!this.ReadyToSign || this.Contract is null || this.SelectedRole is null)
				return;

			await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(true));
			try
			{
				ContractSigningResult SigningResult =
					await ServiceRef.XmppService.SignContractWithOutcomeAsync(
						this.Contract.Contract,
						this.SelectedRole.Name,
						false,
						contractSigningTimeout);

				await this.GoToStateAsync(ViewContractStep.Overview);
				if (SigningResult.Contract is not null &&
					(SigningResult.Outcome == ContractSigningOutcome.Confirmed ||
					SigningResult.FailureCategory == ContractSigningFailureCategory.TerminalContractState))
				{
					await this.RefreshContractAsync(SigningResult.Contract);
				}
				await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(false));

				switch (SigningResult.Outcome)
				{
					case ContractSigningOutcome.Confirmed:
						break;

					case ContractSigningOutcome.TerminalFailure:
						if (SigningResult.FailureCategory != ContractSigningFailureCategory.TerminalContractState)
							await this.RefreshContractAsync(null);

						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractSigningFailed)]);
						break;

					case ContractSigningOutcome.OutcomeUnknown:
						// Refresh is queued without extending the foreground signing wait.
						await this.RefreshContractAsync(null);
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ContractSigningOutcomeUnknownTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractSigningOutcomeUnknown)]);
						break;
				}
			}
			catch (Exception Ex)
			{
				LogRedactedFailure("Contract signing UI flow failed.", Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(false));
			}
		}

		#endregion

		#region Navigation Commands

		[RelayCommand]
		public async Task BackAsync()
		{
			ViewContractStep Step = Enum.Parse<ViewContractStep>(this.CurrentState);
			if (Step == ViewContractStep.Overview || Step == ViewContractStep.Loading)
				await base.GoBack();
			else
				await this.GoToStateAsync(ViewContractStep.Overview);
		}

		public override async Task GoBack()
		{
			await this.BackAsync();
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToParametersAsync() => this.GoToStepAsync(ViewContractStep.Parameters);

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToRolesAsync() => this.GoToStepAsync(ViewContractStep.Roles);

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToSignAsync()
		{
			if (!this.HasReviewedForSigning)
			{
				await this.GoToReviewAsync();
				return;
			}

			await this.GoToStepAsync(ViewContractStep.Sign);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!string.IsNullOrEmpty(this.ProposalRole))
					this.SelectedRole = this.SignableRoles.FirstOrDefault(r => r.Name == this.ProposalRole);
				else if (this.SignableRoles.Count == 1)
					this.SelectedRole = this.SignableRoles[0];
			});
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToReviewAsync()
		{
			if (this.Contract is null)
				return;

			this.HasReviewedForSigning = false;
			this.IsContractOk = false;
			await this.GoToStepAsync(ViewContractStep.Review, Prepare: async () =>
			{
				await this.ValidateParametersAsync();
				VerticalStackLayout? HumanReadableText = await this.Contract.Contract.ToMaui(this.Contract.Contract.DeviceLanguage());
				this.HumanReadableText = HumanReadableText;
			});
		}

		/// <summary>
		/// Continues from the complete agreement review to role selection and signing.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ContinueToSignAsync()
		{
			if (!this.CanContinueToSigning)
				return;

			this.HasReviewedForSigning = true;
			await this.GoToSignAsync();
		}

		partial void OnHasReviewedForSigningChanged(bool Value)
		{
			this.SignCommand.NotifyCanExecuteChanged();
		}

		[RelayCommand]
		private async Task OpenServerSignatureAsync()
		{
			if (this.Contract?.Contract is { } ContractObj)
				await ServiceRef.NavigationService.GoToAsync(nameof(ServerSignaturePage), new ServerSignatureNavigationArgs(ContractObj), Services.UI.BackMethod.Pop);
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenAttachmentAsync(ContractAttachmentItem? Item)
		{
			if (Item is null || !Item.CanOpen || Item.IsLoading)
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				Item.IsLoading = true;
				Item.ErrorText = string.Empty;
			});

			try
			{
				(byte[]? Data, string ContentType) =
					await this.attachmentLoader.LoadOneAttachment(
						Item.Attachment,
						SignWith.LatestApprovedIdOrCurrentKeys);
				if (Data is null)
				{
					await this.SetAttachmentUnavailableAsync(Item);
					return;
				}

				if (Item.IsImage &&
					PhotosLoader.IsSupportedImageContentType(ContentType))
				{
					await MainThread.InvokeOnMainThreadAsync(async () =>
					{
						ImagesPopup Popup = new()
						{
							BindingContext = new ImagesViewModel([Item.Attachment])
						};
						await ServiceRef.PopupService.PushAsync(Popup);
					});
					return;
				}

				string Extension = GetSafeAttachmentExtension(ContentType);
				string TemporaryPath = await PhotosLoader.GetTemporaryFile(
					Data,
					Extension);
				OpenFileRequest Request = new(
					ServiceRef.Localizer[nameof(AppResources.OpenAttachment)],
					new ReadOnlyFile(TemporaryPath, ContentType));
				bool Opened = await Launcher.Default.OpenAsync(Request);
				if (!Opened)
					await this.SetAttachmentUnavailableAsync(Item);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogWarning(
					"Contract attachment could not be opened.",
					new KeyValuePair<string, object?>(
						"FailureType",
						Ex.GetType().Name));
				await this.SetAttachmentUnavailableAsync(Item);
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(
					() => Item.IsLoading = false);
			}
		}

		#endregion

		#region Proposals

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task SendProposalToPartAsync(ObservablePart? part)
		{
			if (part is null || this.Contract is null)
				return;

			if (!part.CanSendProposal)
				return;

			try
			{
				ContactInfo? info = await ContactInfo.FindByLegalId(part.LegalId);
				if (info is null || string.IsNullOrEmpty(info.BareJid))
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkAddressOfContactUnknown)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				await ServiceRef.XmppService.ContractsClient.AuthorizeAccessToContractAsync(
					this.Contract.ContractId,
					info.BareJid,
					true);

				string? friendlyTarget = info.FriendlyName;
				if (string.IsNullOrEmpty(friendlyTarget))
					friendlyTarget = part.FriendlyName ?? info.BareJid ?? part.LegalId;

				string? proposal = await ServiceRef.UiService.DisplayPrompt(
					ServiceRef.Localizer[nameof(AppResources.Proposal)] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.EnterProposal), friendlyTarget] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.Send)] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.Cancel)] ?? string.Empty);

				if(proposal is null) // Dont send if cancelled
					return;
				if (string.IsNullOrEmpty(proposal)) // Use default if empty
					proposal = ServiceRef.Localizer[nameof(AppResources.ProposalDefaultMessage)];

				await ServiceRef.XmppService.SendContractProposal(
					this.Contract.Contract,
					part.Part.Role,
					info.BareJid,
					proposal);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.Ok)] ?? string.Empty);
			}
		}

		#endregion

		#region Contract Management Commands

		[RelayCommand]
		private async Task ObsoleteContractAsync()
		{
			if (this.Contract is null)
				return;

			if (!await this.ConfirmAsync(nameof(AppResources.AreYouSureYouWantToObsoleteContract), AuthenticationPurpose.ObsoleteContract))
				return;

			await ServiceRef.XmppService.ObsoleteContract(this.Contract.ContractId);
			await this.RefreshContractAsync(null);
			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
				ServiceRef.Localizer[nameof(AppResources.ContractHasBeenObsoleted)]);
		}

		[RelayCommand]
		private async Task DeleteContractAsync()
		{
			if (this.Contract is null)
				return;

			if (!await this.ConfirmAsync(nameof(AppResources.AreYouSureYouWantToDeleteContract), AuthenticationPurpose.DeleteContract))
				return;

			await ServiceRef.XmppService.DeleteContract(this.Contract.ContractId);
			this.Contract.Contract.State = ContractState.Deleted;
			await this.RefreshContractAsync(this.Contract.Contract);

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
				ServiceRef.Localizer[nameof(AppResources.ContractHasBeenDeleted)]);
		}

		[RelayCommand]
		private async Task RemoveLocalReferenceAsync()
		{
			if (this.sourceContractReference is null)
				return;

			bool Confirmed = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.RemoveLocalReference)],
				ServiceRef.Localizer[nameof(AppResources.RemoveLocalReferenceDescription)],
				ServiceRef.Localizer[nameof(AppResources.Yes)],
				ServiceRef.Localizer[nameof(AppResources.No)]);
			if (!Confirmed)
				return;

			await Database.Delete(this.sourceContractReference);
			this.sourceContractReference = null;
			this.OnPropertyChanged(nameof(this.CanRemoveLocalReference));
			await this.GoBack();
		}

		#endregion

		#region Clipboard and Link Commands

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ShareAsync()
		{
			if (this.Contract is null)
				return;

			try
			{
				this.GenerateQrCode(this.Contract.Contract.ContractIdUriString);
				await this.OpenQrPopup("");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

		}

		[RelayCommand]
		private async Task CopyAsync(object Item)
		{
			this.SetIsBusy(true);
			try
			{
				string Text = Item switch
				{
					string Id when Id == this.Contract?.ContractId
						=> $"{Constants.UriSchemes.IotSc}:{this.Contract.ContractId}",
					string Other => Other,
					_ => Item?.ToString() ?? string.Empty
				};

				await Clipboard.SetTextAsync(Text);

				string Key = (Item is string Id2 && Id2 == this.Contract?.ContractId)
					? nameof(AppResources.ContractIdCopiedSuccessfully)
					: nameof(AppResources.TagValueCopiedToClipboard);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[Key]);
			}
			finally { this.SetIsBusy(false); }
		}

		[RelayCommand]
		private static Task OpenContractAsync(object Item)
		{
			if (Item is string Id)
				return App.OpenUrlAsync(Constants.UriSchemes.IotSc + ":" + Id);
			return Task.CompletedTask;
		}

		[RelayCommand]
		private async Task OpenLinkAsync(object Item)
		{
			if (Item is string Url && !await App.OpenUrlAsync(Url, false))
				await this.CopyAsync(Url);
			else
				await this.CopyAsync(Item);
		}

		#endregion

		#region State Navigation Helpers

		private Command CreateUriCommand()
			=> new Command(async Parameter => await this.ExecuteUriClicked(Parameter));

		private async Task GoToStepAsync(ViewContractStep Step, Func<Task>? Prepare = null)
		{
			await this.GoToStateAsync(ViewContractStep.Loading);
			if (Prepare is not null) await Prepare();
			await this.GoToStateAsync(Step);
		}

		private async Task GoToStateAsync(ViewContractStep Step)
		{
			if (this.StateObject is null)
				return;

			string NewState = Step.ToString();
			if (NewState == this.CurrentState)
				return;

			if (!this.initialized &&
				string.Equals(
					this.CurrentState,
					nameof(ViewContractStep.Loading),
					StringComparison.Ordinal))
			{
				await this.SetStateDirectlyAsync(NewState);
				return;
			}

			DateTime WaitUntil = DateTime.UtcNow + stateChangeWaitTimeout;
			while (!this.CanStateChange && DateTime.UtcNow < WaitUntil)
				await Task.Delay(50);

			if (!this.CanStateChange)
			{
				ServiceRef.LogService.LogWarning(
					"Contract workspace animation did not become available; applying state directly.");
				await this.SetStateDirectlyAsync(NewState);
				return;
			}

			using CancellationTokenSource TransitionTimeout =
				new(stateChangeWaitTimeout);
			try
			{
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await StateContainer.ChangeStateWithAnimation(
						this.StateObject,
						NewState,
						TransitionTimeout.Token);
				});
			}
			catch (OperationCanceledException) when (TransitionTimeout.IsCancellationRequested)
			{
				ServiceRef.LogService.LogWarning(
					"Contract workspace animation timed out; applying state directly.");
				await this.SetStateDirectlyAsync(NewState);
			}
		}

		private Task SetStateDirectlyAsync(string NewState)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (this.StateObject is null)
					return;

				StateContainer.SetCurrentState(this.StateObject, NewState);
				this.CurrentState = NewState;
				this.CanStateChange = true;
			});
		}

		private Task SetCanStateChangeOnMainThreadAsync(bool value)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.CanStateChange = value;
			});
		}

		#endregion

		#region Event Wiring
		private readonly EventHandlerAsync<ContractReferenceEventArgs> contractUpdatedHandler;
		private readonly EventHandlerAsync<ContractSignedEventArgs> contractSignedHandler;

		private void SubscribeToEvents()
		{
			ServiceRef.XmppService.ContractUpdated += this.contractUpdatedHandler;
			ServiceRef.XmppService.ContractSigned += this.contractSignedHandler;
			this.SignableRoles.CollectionChanged += this.OnSignableRolesChanged;
		}

		private void UnsubscribeFromEvents()
		{
			ServiceRef.XmppService.ContractUpdated -= this.contractUpdatedHandler;
			ServiceRef.XmppService.ContractSigned -= this.contractSignedHandler;
			this.SignableRoles.CollectionChanged -= this.OnSignableRolesChanged;
		}

		private void OnSignableRolesChanged(object? sender, NotifyCollectionChangedEventArgs e)
			=> MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.CanSign));
				this.OnPropertyChanged(nameof(this.CanShowSignBar));
				this.OnPropertyChanged(nameof(this.CanContinueToSigning));
				this.OnPropertyChanged(nameof(this.HasPrimaryAction));
			});

		private async Task OnContractSignedAsync(object? sender, ContractSignedEventArgs e)
		{
			if (e.ContractId != this.Contract?.ContractId || e.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id)
				return;

			// Prefer incoming contract if it's newer than what we have locally
			Contract? Current = this.Contract?.Contract;
			Contract? Incoming = e.Contract;
			if (Current is null || Incoming is null)
				return;

			bool UseIncoming = false;
			DateTime? CurrentTs = Current.ServerSignature?.Timestamp;
			DateTime? IncomingTs = Incoming.ServerSignature?.Timestamp;

			if (IncomingTs.HasValue && CurrentTs.HasValue)
			{
				UseIncoming = IncomingTs.Value > CurrentTs.Value;
			}
			else
			{
				DateTime? CurrentUpdated = Current.Updated;
				DateTime? IncomingUpdated = Incoming.Updated;
				if (IncomingUpdated.HasValue && CurrentUpdated.HasValue)
					UseIncoming = IncomingUpdated.Value > CurrentUpdated.Value;
				else if (IncomingUpdated.HasValue && !CurrentUpdated.HasValue)
					UseIncoming = true; // Prefer newer info if local timestamp missing
				else
					UseIncoming = true; // If we can't compare reliably, accept incoming to be safe
			}

			if (UseIncoming)
				this.RequestRefresh(Incoming);

			await Task.CompletedTask;
		}

		private async Task OnContractUpdatedAsync(object? sender, ContractReferenceEventArgs e)
		{
			if (e.ContractId != this.Contract?.ContractId)
				return;

			// Coalesce refresh requests
			this.RequestRefresh(null);
			await Task.CompletedTask;
		}

		#endregion

		#region Private Helpers

		/// <summary>
		/// Validates the parameters of the contract and updates their error states.
		/// </summary>
		private async Task ValidateParametersAsync()
		{
			if (this.Contract is null)
				return;

			try
			{
				// Step 1: Get the variables and prepare the parameters to validate
				Variables Variables = [];


				Variables["Duration"] = this.Contract.Contract.Duration;

				DateTime? FirstSignature = this.Contract.Contract.FirstSignatureAt;
				if (FirstSignature.HasValue)
				{
					Variables["Now"] = FirstSignature.Value.ToLocalTime();
					Variables["NowUtc"] = FirstSignature.Value.ToUniversalTime();
				}

				foreach (ObservableParameter ParamLoop in this.Contract.Parameters)
					ParamLoop.Parameter.Populate(Variables);

				// Step 2: Prepare to collect validation results
				List<(ObservableParameter Param, bool IsValid, string ValidationText)> ValidationResults = [];

				ContractsClient? ContractsClient = null;
				try
				{
					ContractsClient = ServiceRef.XmppService.ContractsClient;
				}
				catch (Exception)
				{
					// Ignore, client might not be available currently
				}

				Task<(ObservableParameter Param, bool IsValid, string ValidationText)>[] ValidationTasks = this.Contract.Parameters.Select(async ParamToValidate =>
				{
					bool IsValid = false;
					string ValidationText = string.Empty;
					try
					{
						IsValid = await ParamToValidate.Parameter.IsParameterValid(Variables, ServiceRef.XmppService.ContractsClient).ConfigureAwait(false);
						IsValid = IsValid || ParamToValidate.Parameter.ErrorText == ContractStatus.ClientIdentityInvalid.ToString();
						ValidationText = ParamToValidate.Parameter.ErrorText;
					}
					catch (Exception Ex2)
					{
						ServiceRef.LogService.LogException(Ex2);
						IsValid = true;
					}
					return (Param: ParamToValidate, IsValid, ValidationText);
				}).ToArray();

				(ObservableParameter Param, bool IsValid, string ValidationText)[] Results = await Task.WhenAll(ValidationTasks);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
		private bool ValidateArgs()
		{
			return this.args is not null && (this.args.Contract is not null || this.args.ContractRef is not null);
		}

		private async Task LoadContractAsync()
		{
			this.sourceContractReference = this.args!.ContractRef;
			this.OnPropertyChanged(nameof(this.CanRemoveLocalReference));

			Contract? LoadedContract = this.args.Contract;
			bool DetailsDownloadFailed = false;
			bool AccessPetitionHandled = false;
			string SourceContractId = Convert.ToString(
				this.sourceContractReference?.ContractId) ?? string.Empty;

			if (LoadedContract is not null &&
				!ContractMatchesIdentifier(LoadedContract, SourceContractId))
			{
				ServiceRef.LogService.LogWarning(
					"The supplied contract did not match its saved reference; remote recovery will be attempted.");
				LoadedContract = null;
			}

			if (LoadedContract is not null)
			{
				this.isUsingSavedContract =
					this.sourceContractReference?.ContractLoaded == true &&
					!string.IsNullOrWhiteSpace(this.sourceContractReference.ContractXml);
			}
			else if (this.sourceContractReference is ContractReference SourceReference)
			{
				try
				{
					// Legacy XML parsing can be CPU-heavy and must not block the loading view.
					LoadedContract = await Task.Run(
						() => SourceReference.GetContract())
						.WaitAsync(contractHydrationTimeout);
					if (LoadedContract is not null &&
						!ContractMatchesIdentifier(LoadedContract, SourceContractId))
					{
						ServiceRef.LogService.LogWarning(
							"Saved contract content did not match its durable reference; remote recovery will be attempted.");
						LoadedContract = null;
					}
					this.isUsingSavedContract = LoadedContract is not null;
				}
				catch (Exception Ex)
				{
					// A malformed legacy cache must not remove the reference. A remote
					// recovery remains possible when the reference still has an ID.
					LogRedactedFailure("Saved contract content could not be read.", Ex);
				}

				if (LoadedContract is null && !string.IsNullOrWhiteSpace(SourceContractId))
				{
					try
					{
						LoadedContract = await this.DownloadAndPersistContractAsync(SourceContractId)
							.WaitAsync(contractHydrationTimeout);
						this.isUsingSavedContract = false;
					}
					catch (ForbiddenException)
					{
						AccessPetitionHandled = true;
						bool PetitionSent = await ServiceRef.NetworkService.TryRequest(
							() => ServiceRef.XmppService.PetitionContract(
								SourceContractId,
								Guid.NewGuid().ToString(),
								ServiceRef.Localizer[nameof(AppResources.RequestToAccessContract)]));

						if (PetitionSent)
						{
							await ServiceRef.UiService.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
								ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToTheContract)]);
						}
					}
					catch (Exception Ex)
					{
						DetailsDownloadFailed = true;
						LogRedactedFailure("Contract recovery refresh failed.", Ex);
					}
				}
			}

			if (LoadedContract is null)
			{
				if (!AccessPetitionHandled)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[
							DetailsDownloadFailed
								? nameof(AppResources.ContractDetailsDownloadFailed)
								: nameof(AppResources.ContractCouldNotBeFound)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
				await this.GoBack();
				return;
			}

			// Contract text conversion can be CPU-heavy. Participant identities are optional
			// enrichment and must not gate the first usable agreement view.
			ObservableContract ContractWrapper = await Task.Run(
				() => ObservableContract.CreateAsync(
					LoadedContract,
					true))
				.WaitAsync(contractHydrationTimeout);
			if (this.disposed)
			{
				ContractWrapper.Dispose();
				return;
			}

			this.Contract = ContractWrapper;
		}

		private async Task<Contract> DownloadAndPersistContractAsync(string ContractId)
		{
			// Keep contract parsing and metadata persistence away from the UI thread. Large
			// localized contracts can otherwise make the loading workspace appear frozen.
			ServiceRef.LogService.LogDebug("Contract reference hydration started.");
			Contract LoadedContract = await ServiceRef.XmppService.GetContract(ContractId)
				.ConfigureAwait(false);
			if (!ContractMatchesIdentifier(LoadedContract, ContractId))
				throw new InvalidOperationException("The downloaded contract did not match the requested identifier.");

			ServiceRef.LogService.LogDebug("Contract reference download completed.");
			await this.PersistContractAsync(LoadedContract).ConfigureAwait(false);
			ServiceRef.LogService.LogDebug("Contract reference persistence completed.");
			return LoadedContract;
		}

		private async Task InitializeUIAsync()
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.ProposalRole = this.args!.Role ?? string.Empty;
				this.ProposalMessage = this.args!.Proposal ?? string.Empty;
				this.PrepareDisplayableParameters();
				this.PrepareSignableRoles();

				if (!string.IsNullOrEmpty(this.ProposalRole))
					this.SelectedRole = this.SignableRoles.FirstOrDefault(r => r.Name == this.ProposalRole);
				else if (this.SignableRoles.Count == 1)
					this.SelectedRole = this.SignableRoles[0];

				this.OnPropertyChanged(nameof(this.CanSign));
				this.OnPropertyChanged(nameof(this.ReadyToSign));
				this.OnPropertyChanged(nameof(this.CanShowSignBar));
			});
			await this.PrepareWorkspaceAsync();
		}

		private async Task StartDeferredUiPreparationAsync()
		{
			if (this.disposed)
				return;

			long Generation = Interlocked.Increment(ref this.deferredUiLoadGeneration);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (Generation != Volatile.Read(ref this.deferredUiLoadGeneration))
					return;

				// Never expose management actions using eligibility from an older version.
				this.CanDeleteContract = false;
				this.CanObsoleteContract = false;
			});
			if (this.disposed ||
				Generation != Volatile.Read(ref this.deferredUiLoadGeneration))
			{
				return;
			}

			_ = this.PrepareDeferredUiAsync(Generation);
		}

		private async Task PrepareDeferredUiAsync(long Generation)
		{
			try
			{
				await Task.WhenAll(
					this.ResolveProposalFriendlyNameAndApplyAsync(Generation),
					this.PreparePropertiesAsync(Generation),
					this.PrepareActivityAsync(Generation),
					this.PrepareRelatedTokensAsync());
				ServiceRef.LogService.LogDebug("Deferred contract workspace details completed.");
			}
			catch (Exception Ex)
			{
				LogRedactedFailure("Deferred contract details could not be fully loaded.", Ex);
			}
		}

		private async Task ResolveProposalFriendlyNameAndApplyAsync(long Generation)
		{
			string FriendlyName = await Task.Run(this.ResolveProposalFriendlyNameAsync);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (Generation == Volatile.Read(ref this.deferredUiLoadGeneration))
					this.ProposalFriendlyName = FriendlyName;
			});
		}

		private async Task<string> ResolveProposalFriendlyNameAsync()
		{
			if (string.IsNullOrEmpty(this.args!.Proposal) || string.IsNullOrEmpty(this.args.FromJID))
				return string.Empty;

			try
			{
				ContactInfo Info = await ContactInfo.FindByBareJid(this.args.FromJID);
				return !string.IsNullOrEmpty(Info?.FriendlyName)
					? Info.FriendlyName
					: this.args.FromJID;
			}
			catch
			{
				return string.Empty;
			}
		}

		private void PrepareDisplayableParameters()
		{
			this.DisplayableParameters.Clear();
			if (this.Contract?.Parameters is null)
				return;

			Variables Vars = [];

			Vars["Duration"] = this.Contract.Contract.Duration;

			DateTime? FirstSignature = this.Contract.Contract.FirstSignatureAt;
			if (FirstSignature.HasValue)
			{
				Vars["Now"] = FirstSignature.Value.ToLocalTime();
				Vars["NowUtc"] = FirstSignature.Value.ToUniversalTime();
			}

			foreach (ObservableParameter Parameter in this.Contract.Parameters)
			{
				Parameter.Parameter.Populate(Vars);

				if (Parameter.Parameter is BooleanParameter or StringParameter or NumericalParameter
					or DateParameter or TimeParameter or DurationParameter
					or DateTimeParameter or CalcParameter or ContractReferenceParameter or GeoParameter)
				{
					this.DisplayableParameters.Add(Parameter);
				}
			}
		}

		private void PrepareSignableRoles()
		{
			this.SignableRoles.Clear();
			if (this.Contract is null)
				return;

			if (!string.IsNullOrEmpty(this.ProposalRole))
			{
				ObservableRole? Role = this.Contract.Roles.FirstOrDefault(r => r.Name == this.ProposalRole);
				if (Role is not null)
					this.SignableRoles.Add(Role);
			}
			else if (this.Contract.Contract.PartsMode == ContractParts.Open)
			{
				foreach (ObservableRole Role in this.Contract.Roles)
					if (!Role.HasReachedMaxCount)
						this.SignableRoles.Add(Role);
			}
			else
			{
				foreach (ObservableRole Role in this.Contract.Roles)
					if (Role.Parts.Any(p => p.IsMe))
						this.SignableRoles.Add(Role);
			}
		}

		private async Task PreparePropertiesAsync(long Generation)
		{
			ObservableContract? ContractSnapshot = this.Contract;
			if (ContractSnapshot is null)
				return;

			bool CanObsolete = false;
			foreach (ObservableRole? Role in ContractSnapshot.Roles.Where(R => R.Parts.Any(P => P.IsMe)))
			{
				if (Role.Role.CanRevoke)
				{
					CanObsolete = ContractSnapshot.ContractState is
						ContractState.Approved or
						ContractState.BeingSigned or
						ContractState.Signed;
				}
			}

			bool CanDelete = false;
			if (this.args is not null)
			{
				try
				{
					bool Binding = await Task.Run(
						() => ContractSnapshot.Contract.IsLegallyBinding(
							true,
							ServiceRef.XmppService.ContractsClient));
					CanDelete =
						!this.args.IsReadOnly &&
						!Binding &&
						ContractSnapshot.ContractState is not
							(ContractState.Deleted or ContractState.Obsoleted);
				}
				catch (Exception Ex)
				{
					LogRedactedFailure("Contract management eligibility could not be determined.", Ex);
				}
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (Generation != Volatile.Read(ref this.deferredUiLoadGeneration) ||
					!ReferenceEquals(this.Contract, ContractSnapshot))
				{
					return;
				}

				this.CanDeleteContract = CanDelete;
				this.CanObsoleteContract = CanObsolete;
			});
		}

		private async Task PrepareWorkspaceAsync()
		{
			if (this.Contract is null)
				return;

			Contract Agreement = this.Contract.Contract;
			string FriendlyName = await ContractModel.GetName(Agreement);
			string ContractId = Agreement.ContractId ?? string.Empty;
			string ShortId = ContractId.Length > 16
				? ContractId[..8] + "…" + ContractId[^6..]
				: ContractId;

			this.SummaryCategory = this.Contract.Category ?? string.Empty;
			this.SummaryTitle = FirstNonEmpty(
				FriendlyName,
				this.sourceContractReference?.Name,
				this.SummaryCategory,
				ShortId,
				ServiceRef.Localizer[nameof(AppResources.UntitledContract)]);
			this.SummaryStateText = GetContractStateText(Agreement.State);
			this.SummaryStateTone = GetContractStateTone(Agreement.State);

			string RoleName = string.Join(
				", ",
				this.Contract.Roles
					.Where(Role => Role.Parts.Any(Part => Part.IsMe))
					.Select(Role => Role.Name)
					.Distinct(StringComparer.CurrentCultureIgnoreCase));
			if (string.IsNullOrWhiteSpace(RoleName))
				RoleName = this.ProposalRole ?? string.Empty;

			this.SummaryRoleText = string.IsNullOrWhiteSpace(RoleName)
				? string.Empty
				: string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.ContractYourRoleFormat)],
					RoleName);

			int RequiredSignatures = (Agreement.Roles ?? [])
				.Sum(Role => Math.Max(0, Role.MinCount));
			int SignedCount = Math.Min(
				Agreement.ClientSignatures?.Length ?? 0,
				RequiredSignatures);
			this.SignatureProgressText = RequiredSignatures == 0
				? string.Empty
				: string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.SignatureProgressFormat)],
					SignedCount,
					RequiredSignatures);

			this.ImportantDateText = GetImportantDateText(Agreement);
			this.NextActionText = this.IsProposal
				? ServiceRef.Localizer[nameof(AppResources.RespondToProposal)]
				: this.CanSign
					? ServiceRef.Localizer[nameof(AppResources.ReviewAndSign)]
					: string.Empty;
			this.PrimaryActionText = this.NextActionText;

			this.KeyParameters.Clear();
			foreach (ObservableParameter Parameter in this.DisplayableParameters.Take(3))
				this.KeyParameters.Add(Parameter);

			this.OnPropertyChanged(nameof(this.HasKeyParameters));
			await this.PrepareAttachmentsAsync();
			this.UpdateFreshnessPresentation();
			this.NotifyContractPresentationChanged();
		}

		private async Task PrepareActivityAsync(long Generation)
		{
			if (this.Contract is null)
				return;

			Contract Agreement = this.Contract.Contract;
			List<ContractActivityItem> Items = [];
			if (Agreement.Created != DateTime.MinValue)
			{
				Items.Add(new ContractActivityItem(
					ServiceRef.Localizer[nameof(AppResources.Created)],
					string.Empty,
					Agreement.Created));
			}

			if (Agreement.Updated != DateTime.MinValue &&
				Agreement.Updated != Agreement.Created)
			{
				Items.Add(new ContractActivityItem(
					ServiceRef.Localizer[nameof(AppResources.Updated)],
					string.Empty,
					Agreement.Updated));
			}

			foreach (ClientSignature Signature in Agreement.ClientSignatures ?? [])
			{
				Items.Add(new ContractActivityItem(
					ServiceRef.Localizer[nameof(AppResources.ContractSigned)],
					Signature.Role ?? string.Empty,
					Signature.Timestamp));
			}

			if (Agreement.ServerSignature is not null)
			{
				Items.Add(new ContractActivityItem(
					ServiceRef.Localizer[nameof(AppResources.ServerSignatures)],
					string.Empty,
					Agreement.ServerSignature.Timestamp));
			}

			try
			{
				INotificationServiceV2 NotificationService =
					ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				IReadOnlyList<NotificationRecord> Records = await NotificationService.GetAsync(
					new NotificationQuery
					{
						Channels = [Constants.PushChannels.Contracts],
						Limit = 100
					},
					CancellationToken.None);

				foreach (NotificationRecord Record in Records.Where(
					Record => string.Equals(
						Record.EntityId,
						Agreement.ContractId,
						StringComparison.OrdinalIgnoreCase)))
				{
					Items.Add(new ContractActivityItem(
						Record.Title,
						Record.Body ?? string.Empty,
						Record.TimestampCreated));
				}
			}
			catch (Exception Ex)
			{
				// Contract timestamps remain useful when notification history is unavailable.
				LogRedactedFailure("Contract activity history could not be loaded.", Ex);
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (Generation != Volatile.Read(ref this.deferredUiLoadGeneration) ||
					!string.Equals(
						this.Contract?.ContractId,
						Agreement.ContractId,
						StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				this.ActivityItems.Clear();
				foreach (ContractActivityItem Item in Items
					.OrderByDescending(Item => Item.Timestamp)
					.GroupBy(Item => new { Item.Title, Item.Timestamp })
					.Select(Group => Group.First()))
				{
					this.ActivityItems.Add(Item);
				}

				this.OnPropertyChanged(nameof(this.HasActivity));
			});
		}

		private async Task PrepareRelatedTokensAsync()
		{
			Contract? Agreement = this.Contract?.Contract;
			if (Agreement is null)
				return;

			long Generation = Interlocked.Increment(
				ref this.relatedTokenLoadGeneration);
			string ContractId = Agreement.ContractId ?? string.Empty;
			bool IsTokenContract = string.Equals(
				Agreement.ForMachinesNamespace,
				NeuroFeaturesClient.NamespaceNeuroFeatures,
				StringComparison.Ordinal);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.RelatedTokens.Clear();
				this.CanBrowseCreatedTokens = false;
				this.RelatedTokensUnavailable = false;
				this.IsRelatedTokensLoading = IsTokenContract;
				this.OnPropertyChanged(nameof(this.HasRelatedTokens));
				this.OnPropertyChanged(nameof(this.HasRelatedTokensSection));
			});

			Dictionary<string, bool> RelatedIds =
				new(StringComparer.OrdinalIgnoreCase);
			bool ReferencesUnavailable = false;
			bool CanBrowseCreatedTokens = false;

			if (IsTokenContract &&
				Agreement.PartsMode != ContractParts.TemplateOnly &&
				!string.IsNullOrWhiteSpace(ContractId))
			{
				try
				{
					string[] CreatedTokenIds =
						await ServiceRef.XmppService.GetNeuroFeatureReferencesForContract(
							ContractId,
							0,
							5);

					foreach (string TokenId in CreatedTokenIds ?? [])
					{
						string NormalizedTokenId = TokenId?.Trim() ?? string.Empty;
						if (!string.IsNullOrEmpty(NormalizedTokenId))
							RelatedIds[NormalizedTokenId] = true;
					}

					CanBrowseCreatedTokens = RelatedIds.Count > 0;
				}
				catch (Exception Ex)
				{
					ReferencesUnavailable = true;
					ServiceRef.LogService.LogWarning(
						"Related token references could not be loaded.",
						new KeyValuePair<string, object?>(
							"FailureType",
							Ex.GetType().Name));
				}
			}

			bool ExplicitIdsAreCreated = string.Equals(
				Agreement.ForMachinesLocalName,
				"Create",
				StringComparison.OrdinalIgnoreCase);
			foreach (string TokenId in ExtractExplicitTokenIds(Agreement))
			{
				if (!RelatedIds.ContainsKey(TokenId))
					RelatedIds[TokenId] = ExplicitIdsAreCreated;
			}

			List<ContractRelatedTokenItem> Items = RelatedIds
				.Select(Pair => new ContractRelatedTokenItem(
					Pair.Key,
					Pair.Value
						? ServiceRef.Localizer[nameof(AppResources.TokenCreatedByAgreement)]
						: ServiceRef.Localizer[nameof(AppResources.TokenAffectedByAgreement)]))
				.ToList();

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (Generation != Volatile.Read(ref this.relatedTokenLoadGeneration) ||
					!string.Equals(
						this.Contract?.ContractId,
						ContractId,
						StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				this.RelatedTokens.Clear();
				foreach (ContractRelatedTokenItem Item in Items)
					this.RelatedTokens.Add(Item);

				this.CanBrowseCreatedTokens = CanBrowseCreatedTokens;
				this.RelatedTokensUnavailable = ReferencesUnavailable;
				this.IsRelatedTokensLoading = false;
				this.OnPropertyChanged(nameof(this.HasRelatedTokens));
				this.OnPropertyChanged(nameof(this.HasRelatedTokensSection));
			});
		}

		private static IEnumerable<string> ExtractExplicitTokenIds(
			Contract Agreement)
		{
			XmlElement? MachineReadable = Agreement.ForMachines;
			if (MachineReadable is null)
				yield break;

			XmlNodeList? Nodes = MachineReadable.SelectNodes(".//*");
			if (Nodes is null)
				yield break;

			HashSet<string> TokenIds = new(StringComparer.OrdinalIgnoreCase);
			foreach (XmlNode Node in Nodes)
			{
				if (Node is not XmlElement Element)
					continue;

				bool IsTokenElement =
					string.Equals(
						Element.LocalName,
						"TokenID",
						StringComparison.OrdinalIgnoreCase) ||
					(string.Equals(
						Element.LocalName,
						"Tag",
						StringComparison.OrdinalIgnoreCase) &&
					string.Equals(
						Element.GetAttribute("name"),
						"TokenID",
						StringComparison.OrdinalIgnoreCase));
				if (!IsTokenElement)
					continue;

				XmlNodeList? ParameterReferences =
					Element.SelectNodes(".//*[local-name()='ParameterReference']");
				if (ParameterReferences is not null)
				{
					foreach (XmlNode ParameterNode in ParameterReferences)
					{
						if (ParameterNode is not XmlElement ParameterElement)
							continue;

						string ParameterName =
							ParameterElement.GetAttribute("parameter");
						Parameter? Parameter = (Agreement.Parameters ?? [])
							.FirstOrDefault(Item => string.Equals(
								Item.Name,
								ParameterName,
								StringComparison.OrdinalIgnoreCase));
						string Value = Convert.ToString(
							Parameter?.ObjectValue,
							CultureInfo.InvariantCulture) ?? string.Empty;
						if (IsLikelyTokenId(Value))
							TokenIds.Add(Value.Trim());
					}
				}

				XmlNode? StringValue =
					Element.SelectSingleNode(".//*[local-name()='String']");
				if (StringValue is not null && IsLikelyTokenId(StringValue.InnerText))
					TokenIds.Add(StringValue.InnerText.Trim());
			}

			foreach (string TokenId in TokenIds)
				yield return TokenId;
		}

		private static bool IsLikelyTokenId(string? Value)
		{
			if (string.IsNullOrWhiteSpace(Value) ||
				Value.Length > 1024 ||
				Value.Any(char.IsWhiteSpace))
			{
				return false;
			}

			int SeparatorIndex = Value.IndexOf('@');
			return SeparatorIndex > 0 && SeparatorIndex < Value.Length - 1;
		}

		private async Task PrepareAttachmentsAsync()
		{
			Attachment[] Attachments =
				this.Contract?.Contract.Attachments ?? [];
			List<ContractAttachmentItem> Items = [];
			for (int Index = 0; Index < Attachments.Length; Index++)
			{
				Attachment? Attachment = Attachments[Index];
				if (Attachment is null)
					continue;

				try
				{
					Items.Add(new ContractAttachmentItem(
						Attachment,
						Index + 1));
				}
				catch (Exception Ex)
				{
					// A malformed attachment must not prevent the agreement itself
					// from loading. Do not include attachment metadata in diagnostics.
					ServiceRef.LogService.LogWarning(
						"Contract attachment metadata could not be presented.",
						new KeyValuePair<string, object?>(
							"FailureType",
							Ex.GetType().Name));
				}
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.AttachmentItems.Clear();
				foreach (ContractAttachmentItem Item in Items)
					this.AttachmentItems.Add(Item);

				this.OnPropertyChanged(nameof(this.HasAttachments));
			});
		}

		private Task SetAttachmentUnavailableAsync(ContractAttachmentItem Item)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				Item.ErrorText =
					ServiceRef.Localizer[nameof(AppResources.ContractAttachmentUnavailable)];
			});
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenRelatedTokenAsync(ContractRelatedTokenItem? Item)
		{
			if (Item is null || string.IsNullOrWhiteSpace(Item.TokenId))
				return;

			await ServiceRef.NeuroWalletOrchestratorService.OpenTokenAsync(Item.TokenId);
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task BrowseCreatedTokensAsync()
		{
			string ContractId = this.Contract?.ContractId ?? string.Empty;
			if (!this.CanBrowseCreatedTokens || string.IsNullOrWhiteSpace(ContractId))
				return;

			MyTokensNavigationArgs Args = new(ContractId);
			await ServiceRef.NavigationService.GoToAsync(
				nameof(MyTokensPage),
				Args,
				Services.UI.BackMethod.Pop);
		}

		private static string GetSafeAttachmentExtension(string ContentType)
		{
			string MediaType = ContentType.Split(';', 2)[0].Trim();
			string Extension = InternetContent.GetFileExtension(MediaType);
			if (string.IsNullOrWhiteSpace(Extension) ||
				Extension.Length > 10 ||
				Extension.Any(Character =>
					!char.IsLetterOrDigit(Character)))
			{
				return "bin";
			}

			return Extension.ToLowerInvariant();
		}

		private void UpdateFreshnessPresentation()
		{
			if (!this.IsRefreshing && this.refreshFailedUsingSaved)
			{
				this.FreshnessWarningText =
					ServiceRef.Localizer[nameof(AppResources.ContractRefreshFailedUsingSaved)];
			}
			else
				this.FreshnessWarningText = string.Empty;
		}

		private void NotifyContractPresentationChanged()
		{
			this.OnPropertyChanged(nameof(this.Visibility));
			this.OnPropertyChanged(nameof(this.CanSign));
			this.OnPropertyChanged(nameof(this.CanShowSignBar));
			this.OnPropertyChanged(nameof(this.CanContinueToSigning));
			this.OnPropertyChanged(nameof(this.HasPrimaryAction));
			this.OnPropertyChanged(nameof(this.HasServerSignature));
			this.OnPropertyChanged(nameof(this.HasMachineReadableContent));
			this.OnPropertyChanged(nameof(this.MachineReadableContent));
			this.OnPropertyChanged(nameof(this.HasTemplateId));
			this.OnPropertyChanged(nameof(this.HasProvider));
			this.OnPropertyChanged(nameof(this.HasSignAfter));
			this.OnPropertyChanged(nameof(this.HasSignBefore));
		}

		private async Task PersistContractAsync(Contract Contract)
		{
			ContractReference? Reference = this.sourceContractReference;
			if (Reference is null)
			{
				Reference = await Database.FindFirstIgnoreRest<ContractReference>(
					new FilterFieldEqualTo("ContractId", Contract.ContractId));
			}

			if (Reference is null)
				return;

			string ReferenceContractId = Convert.ToString(Reference.ContractId) ?? string.Empty;
			if (!ContractMatchesIdentifier(Contract, ReferenceContractId))
				throw new InvalidOperationException("The contract did not match the saved reference identifier.");

			await Reference.SetContract(Contract);
			await Database.Update(Reference);
			this.sourceContractReference = Reference;
			await MainThread.InvokeOnMainThreadAsync(
				() => this.OnPropertyChanged(nameof(this.CanRemoveLocalReference)));
		}

		private static bool ContractMatchesIdentifier(
			Contract? Contract,
			string ExpectedContractId)
		{
			return Contract is not null &&
				(string.IsNullOrWhiteSpace(ExpectedContractId) ||
				 string.Equals(
					 Contract.ContractId,
					 ExpectedContractId,
					 StringComparison.OrdinalIgnoreCase));
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

		private static string GetImportantDateText(Contract Contract)
		{
			if (Contract.SignBefore is DateTime SignBefore &&
				Contract.State is ContractState.Approved or ContractState.BeingSigned)
			{
				return string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.ContractSignByFormat)],
					SignBefore.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
			}

			DateTime Timestamp = Contract.Updated != DateTime.MinValue
				? Contract.Updated
				: Contract.Created;
			return Timestamp == DateTime.MinValue
				? string.Empty
				: string.Format(
					CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.LastUpdatedFormat)],
					Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
		}

		private static string GetContractStateText(ContractState State)
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
				_ => State.ToString()
			};
		}

		private static StatusPillTone GetContractStateTone(ContractState State)
		{
			return State switch
			{
				ContractState.Signed => StatusPillTone.Success,
				ContractState.Rejected or
				ContractState.Failed or
				ContractState.Obsoleted or
				ContractState.Deleted => StatusPillTone.Danger,
				ContractState.Proposed or
				ContractState.Approved or
				ContractState.BeingSigned => StatusPillTone.Information,
				_ => StatusPillTone.Neutral
			};
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task RefreshContractAsync(Contract? newContract)
		{
			// If the command was triggered by our programmatic IsRefreshing change, ignore once
			if (this.suppressNextRefreshCommand)
			{
				this.suppressNextRefreshCommand = false;
				return Task.CompletedTask;
			}

			this.RequestRefresh(newContract);
			return Task.CompletedTask;
		}

		// Coalesced refresh entry
		private void RequestRefresh(Contract? newContract)
		{
			lock (this.refreshLock)
			{
				if (newContract is not null)
					this.pendingContractForRefresh = newContract;

				this.refreshQueued = true;

				// Wait until initialized (first UI shown) before allowing refreshes to run
				if (this.refreshInProgress)
					return;

				this.refreshInProgress = true;
			}

			_ = this.ProcessRefreshQueueAsync();
		}

		private async Task ProcessRefreshQueueAsync()
		{
			try
			{
				// Ensure initial contract displayed
				while (!this.disposed && (!this.initialized || this.Contract is null))
					await Task.Delay(50);
				if (this.disposed)
					return;

				while (true)
				{
					if (this.disposed)
						return;

					Contract? ToUse;
					lock (this.refreshLock)
					{
						if (!this.refreshQueued)
						{
							this.refreshInProgress = false;
							return;
						}

						this.refreshQueued = false;
						ToUse = this.pendingContractForRefresh;
						this.pendingContractForRefresh = null;
					}

					await this.DoRefreshAsync(ToUse);
				}
			}
			catch (Exception Ex)
			{
				LogRedactedFailure("Queued contract refresh failed.", Ex);
			}
			finally
			{
				lock (this.refreshLock)
				{
					this.refreshInProgress = false;
					this.refreshQueued = false;
					this.pendingContractForRefresh = null;
				}
			}
		}

		private async Task DoRefreshAsync(Contract? newContract)
		{
			if (this.disposed || this.Contract is null)
				return;

			Interlocked.Increment(ref this.deferredUiLoadGeneration);
			Interlocked.Increment(ref this.relatedTokenLoadGeneration);
			ServiceRef.LogService.LogDebug("Contract refresh started.");
			bool PreviousStateChange = this.CanStateChange;
			await this.SetCanStateChangeOnMainThreadAsync(false);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!this.IsRefreshing)
				{
					this.suppressNextRefreshCommand = true;
					this.IsRefreshing = true;
				}
			});

			try
			{
				try
				{
					if (newContract is null)
					{
						Task<Contract> RefreshTask =
							ServiceRef.XmppService.GetContract(this.Contract.ContractId);
						try
						{
							newContract = await RefreshTask.WaitAsync(contractHydrationTimeout);
						}
						catch (TimeoutException)
						{
							_ = ObserveLateContractRefreshAsync(RefreshTask);
							throw;
						}
					}
				}
				catch (ForbiddenException)
				{
					bool Petition = await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.RefreshContract_Forbidden_Title)],
						ServiceRef.Localizer[nameof(AppResources.RefreshContract_Forbidden_Description)],
						ServiceRef.Localizer[nameof(AppResources.Yes)],
						ServiceRef.Localizer[nameof(AppResources.No)]);
					if (Petition)
					{
						await ServiceRef.NetworkService.TryRequest(
							() => ServiceRef.XmppService.PetitionContract(
								this.Contract.ContractId,
								Guid.NewGuid().ToString(),
								ServiceRef.Localizer[nameof(AppResources.RequestToAccessContract)]));
					}

					this.MarkRefreshFailureUsingSavedContract();
					return;
				}
				catch (ItemNotFoundException Ex)
				{
					LogRedactedFailure("Contract refresh could not find the requested agreement.", Ex);
					this.MarkRefreshFailureUsingSavedContract();
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeFound)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}
				catch (Exception Ex)
				{
					LogRedactedFailure("Contract refresh failed.", Ex);
					this.MarkRefreshFailureUsingSavedContract();
					return;
				}

				if (newContract is null)
				{
					this.MarkRefreshFailureUsingSavedContract();
					return;
				}
				if (!ContractMatchesIdentifier(
					newContract,
					this.Contract.ContractId))
				{
					ServiceRef.LogService.LogWarning(
						"Contract refresh returned a different durable identifier and was ignored.");
					this.MarkRefreshFailureUsingSavedContract();
					return;
				}

				await this.PersistContractAsync(newContract);
				this.isUsingSavedContract = false;
				this.refreshFailedUsingSaved = false;

				if (ContractsRepresentSameVersion(
					newContract,
					this.Contract.Contract))
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						this.PrepareDisplayableParameters();
						this.PrepareSignableRoles();
					});
					await this.PrepareWorkspaceAsync();
					await this.StartDeferredUiPreparationAsync();
					this.UpdateFreshnessPresentation();
					ServiceRef.LogService.LogDebug(
						"RefreshContractAsync completed (already current)");
					return;
				}

				ObservableContract Wrapper = await Task.Run(
					() => ObservableContract.CreateAsync(
						newContract,
						true))
					.WaitAsync(contractHydrationTimeout);
				ViewContractStep CurrentStep =
					Enum.TryParse(this.CurrentState, out ViewContractStep ParsedStep)
						? ParsedStep
						: ViewContractStep.Overview;
				if (CurrentStep is ViewContractStep.Review or ViewContractStep.Sign)
					CurrentStep = ViewContractStep.Overview;

				ObservableContract PreviousContract = this.Contract;
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.SelectedRole = null;
					this.HasReviewedForSigning = false;
					this.IsContractOk = false;
					this.HumanReadableText = null;
					this.Contract = Wrapper;
					this.PrepareDisplayableParameters();
					this.PrepareSignableRoles();
				});
				PreviousContract.Dispose();

				await this.PrepareWorkspaceAsync();
				await this.SetCanStateChangeOnMainThreadAsync(true);
				await this.GoToStateAsync(CurrentStep);
				await this.StartDeferredUiPreparationAsync();
				ServiceRef.LogService.LogDebug("Contract refresh completed.");
			}
			finally
			{
				await this.SetCanStateChangeOnMainThreadAsync(PreviousStateChange);
				await MainThread.InvokeOnMainThreadAsync(() => this.IsRefreshing = false);
			}
		}

		private void MarkRefreshFailureUsingSavedContract()
		{
			this.isUsingSavedContract = true;
			this.refreshFailedUsingSaved = true;
			this.UpdateFreshnessPresentation();
		}

		private static void LogRedactedFailure(string Message, Exception Exception)
		{
			// Contract IDs, XML, identities, signatures, and parameter values are deliberately omitted.
			ServiceRef.LogService.LogWarning(
				Message,
				new KeyValuePair<string, object?>(
					"FailureType",
					Exception.GetType().Name));
		}

		private static async Task ObserveLateContractRefreshAsync(Task<Contract> RefreshTask)
		{
			try
			{
				await RefreshTask.ConfigureAwait(false);
			}
			catch (Exception Ex)
			{
				LogRedactedFailure("Late contract refresh failed.", Ex);
			}
		}

		private static bool ContractsRepresentSameVersion(
			Contract First,
			Contract Second)
		{
			DateTime? FirstSignature = First.ServerSignature?.Timestamp;
			DateTime? SecondSignature = Second.ServerSignature?.Timestamp;
			if (FirstSignature.HasValue || SecondSignature.HasValue)
			{
				return FirstSignature == SecondSignature &&
					First.Updated == Second.Updated &&
					First.State == Second.State &&
					(First.ClientSignatures?.Length ?? 0) ==
					(Second.ClientSignatures?.Length ?? 0);
			}

			return First.Updated == Second.Updated &&
				First.State == Second.State &&
				(First.ClientSignatures?.Length ?? 0) ==
				(Second.ClientSignatures?.Length ?? 0);
		}

		private async Task<bool> ConfirmAsync(string resourceKey, AuthenticationPurpose purpose)
		{
			if (!await AreYouSure(ServiceRef.Localizer[resourceKey]))
				return false;
			return await ServiceRef.AuthenticationService.AuthenticateUserAsync(purpose, true);
		}

		#endregion

		#region ILinkableView Implementation

		public override string? Link { get; }
		public override Task<string> Title => ContractModel.GetName(this.Contract?.Contract);

		#endregion

		#region Markdown Link Handlers

		public Command XmppUriClicked { get; }
		public Command IotIdUriClicked { get; }
		public Command IotScUriClicked { get; }
		public Command NeuroFeatureUriClicked { get; }
		public Command IotDiscoUriClicked { get; }
		public Command EDalerUriClicked { get; }
		public Command HyperlinkClicked { get; }

		private async Task ExecuteUriClicked(object? parameter)
		{
			if (parameter is string Uri)
				await App.OpenUrlAsync(Uri);
		}

		private async Task ExecuteHyperlinkClicked(object? parameter)
		{
			if (parameter is string Url)
				await App.OpenUrlAsync(Url);
		}

		#endregion
	}
}
