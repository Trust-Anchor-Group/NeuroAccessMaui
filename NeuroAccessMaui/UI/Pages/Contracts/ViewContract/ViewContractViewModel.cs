using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // For MainThread marshaling
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contracts.InviteParts;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using NeuroAccessMaui.UI.Pages.Contracts.SigningBlockers;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// View model for displaying and managing a contract in the "View Contract" page.
	/// </summary>
	public partial class ViewContractViewModel : QrXmppViewModel
	{
		#region Fields

		private static readonly TimeSpan stateChangeTimeout = TimeSpan.FromSeconds(10);

		private readonly ViewContractNavigationArgs? args;

		// Refresh coalescing state
		private readonly object refreshLock = new();
		private bool refreshInProgress;
		private bool refreshQueued;
		private Contract? pendingContractForRefresh;
		private bool initialized;

		// The contract shown is a saved copy whose encrypted values can't be read until the current version arrives.
		private bool showingSavedCopy;

		// The next refresh was asked for by the user, so a failure is shown.
		private bool userRequestedRefresh;

		// Awaiting post-create completion flag (bindable)
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanShowSignBar))]
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

			this.XmppUriClicked = this.CreateUriCommand(UriScheme.Xmpp);
			this.IotIdUriClicked = this.CreateUriCommand(UriScheme.IotId);
			this.IotScUriClicked = this.CreateUriCommand(UriScheme.IotSc);
			this.NeuroFeatureUriClicked = this.CreateUriCommand(UriScheme.NeuroFeature);
			this.IotDiscoUriClicked = this.CreateUriCommand(UriScheme.IotDisco);
			this.EDalerUriClicked = this.CreateUriCommand(UriScheme.EDaler);
			this.HyperlinkClicked = new Command(async p => await this.ExecuteHyperlinkClicked(p));
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
				if (!await this.LoadContractAsync())
					return;

				await this.InitializeUIAsync();
				await this.GoToStateAsync(ViewContractStep.Overview);
				// If navigation provided a post-create completion, await it before enabling signing UI
				if (this.args?.PostCreateCompletion is not null)
				{
					await MainThread.InvokeOnMainThreadAsync(() => this.IsAwaitingPostCreateCompletion = true);
					try
					{
						Contract? Completed = await this.args.PostCreateCompletion.Task;
						if (Completed is not null)
						{
							// Show the completed contract (e.g., signed)
							await this.RefreshContractAsync(Completed);
						}
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
					finally
					{
						await MainThread.InvokeOnMainThreadAsync(() => this.IsAwaitingPostCreateCompletion = false);
					}
				}

				// Mark initialized: allow future refreshes, but do not force a refresh here
				this.initialized = true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
				await this.GoBack();
			}
		}

		public override async Task OnDisposeAsync()
		{
			this.UnsubscribeFromEvents();
			await base.OnDisposeAsync();
		}

		#endregion

		#region Properties

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.PropertyName == nameof(this.IsBusy))
				this.OnPropertyChanged(nameof(this.CanSign));
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSign))]
		[NotifyPropertyChangedFor(nameof(CanShowSignBar))]
		private ObservableContract? contract;

		[ObservableProperty]
		private bool isRefreshing = false;

		public BindableObject? StateObject { get; set; }

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(GoToParametersCommand))]
		[NotifyCanExecuteChangedFor(nameof(BackCommand))]
		private bool canStateChange;

		[ObservableProperty]
		private string currentState = nameof(NewContractStep.Loading);

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasHumanReadableText))]
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
		[NotifyPropertyChangedFor(nameof(SigningRolesText))]
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

		/// <summary>
		/// If Delete is offered. Follows <see cref="ContractActionRules.CanDelete"/>.
		/// </summary>
		[ObservableProperty]
		private bool canDeleteContract;

		/// <summary>
		/// If Withdraw is offered. Follows <see cref="ContractActionRules.CanWithdraw"/>.
		/// </summary>
		[ObservableProperty]
		private bool canWithdrawContract;

		/// <summary>
		/// If the contract was deleted on the server. The page shows the saved copy.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasStatusBanner))]
		[NotifyPropertyChangedFor(nameof(CanSign))]
		[NotifyPropertyChangedFor(nameof(CanShowSignBar))]
		private bool isDeletedOnServer;

		/// <summary>
		/// If the server no longer lets the user read the contract. The page shows the saved copy.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasStatusBanner))]
		[NotifyPropertyChangedFor(nameof(ShowRequestAccess))]
		[NotifyPropertyChangedFor(nameof(ShowAskSenderHint))]
		private bool isAccessDenied;

		/// <summary>
		/// If access can be asked for. Only people who signed receive the request, so the saved copy must show signatures.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowRequestAccess))]
		[NotifyPropertyChangedFor(nameof(ShowAskSenderHint))]
		private bool canRequestAccess;

		/// <summary>
		/// If the contract can be removed from the user's list of contracts.
		/// </summary>
		[ObservableProperty]
		private bool canRemoveFromList;

		/// <summary>
		/// If the open invitation to sign can be declined.
		/// </summary>
		[ObservableProperty]
		private bool canDeclineInvitation;

		/// <summary>
		/// If Invite is offered. Only people whose address is known can be invited.
		/// </summary>
		[ObservableProperty]
		private bool canInviteParts;

		/// <summary>
		/// If the parts who haven't signed can still be invited, so the Parties section shows what is needed to invite them.
		/// </summary>
		[ObservableProperty]
		private bool canManageParts;

		/// <summary>
		/// If the deleted or access-denied banner is shown.
		/// </summary>
		public bool HasStatusBanner => this.IsDeletedOnServer || this.IsAccessDenied;

		/// <summary>
		/// If "Ask for access" is offered.
		/// </summary>
		public bool ShowRequestAccess => this.IsAccessDenied && this.CanRequestAccess;

		/// <summary>
		/// If the page says to ask the sender to share the contract again.
		/// </summary>
		public bool ShowAskSenderHint => this.IsAccessDenied && !this.CanRequestAccess;

		#endregion

		#region Overview

		/// <summary>
		/// If the technical details on the overview are shown.
		/// </summary>
		[ObservableProperty]
		private bool isTechnicalDetailsExpanded;

		/// <summary>
		/// The machine-readable part of the contract, as formatted XML. Set when the technical details are shown.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasMachineReadableXml))]
		private string? machineReadableXml;

		/// <summary>
		/// If there is a machine-readable part to show.
		/// </summary>
		public bool HasMachineReadableXml => !string.IsNullOrEmpty(this.MachineReadableXml);

		/// <summary>
		/// The contract's name, which is the page title: its title, else the page's own title.
		/// </summary>
		public string ContractName =>
			string.IsNullOrEmpty(this.Contract?.Category) ? ServiceRef.Localizer[nameof(AppResources.ViewContract)] : this.Contract.Category;

		/// <summary>
		/// The localized state of the contract.
		/// </summary>
		public string StateText
		{
			get
			{
				if (this.Contract is null)
					return string.Empty;

				if (this.IsDeletedOnServer)
					return ContractStateToString.GetText(ContractState.Deleted, false);

				return ContractStateToString.GetText(this.Contract.ContractState, this.Contract.IsTemplate);
			}
		}

		/// <summary>
		/// If the state chip shows success: the contract is signed.
		/// </summary>
		public bool IsStateSuccess => !this.IsDeletedOnServer && this.Contract?.ContractState == ContractState.Signed;

		/// <summary>
		/// If the state chip shows that the contract has ended or failed.
		/// </summary>
		public bool IsStateDanger =>
			this.IsDeletedOnServer ||
			this.Contract?.ContractState is ContractState.Rejected or ContractState.Failed or ContractState.Obsoleted or ContractState.Deleted;

		/// <summary>
		/// ID of the contract when it is a signed contract that creates tokens, so the overview can list the tokens it
		/// created. Null for every other contract.
		/// </summary>
		public string? CreatedTokensContractId
		{
			get
			{
				Contract? Current = this.Contract?.Contract;

				if (Current is null || this.IsDeletedOnServer ||
					this.Contract?.ContractState is not (ContractState.Signed or ContractState.Obsoleted))
				{
					return null;
				}

				if (Current.ForMachinesNamespace != TokenTemplateService.GetNamespace(TokenAction.Create) ||
					Current.ForMachinesLocalName != TokenTemplateService.GetLocalName(TokenAction.Create))
				{
					return null;
				}

				return Current.ContractId;
			}
		}

		/// <summary>
		/// The signing window, in local time, or an empty string if signatures are accepted at any time.
		/// </summary>
		public string SigningWindowText
		{
			get
			{
				DateTime? After = this.Contract?.Contract.SignAfter;
				DateTime? Before = this.Contract?.Contract.SignBefore;

				if (After.HasValue && Before.HasValue)
					return ServiceRef.Localizer[nameof(AppResources.ContractSigningWindowBetween), FormatDate(After.Value), FormatDate(Before.Value)];

				if (After.HasValue)
					return ServiceRef.Localizer[nameof(AppResources.ContractSigningWindowFrom), FormatDate(After.Value)];

				if (Before.HasValue)
					return ServiceRef.Localizer[nameof(AppResources.ContractSigningWindowUntil), FormatDate(Before.Value)];

				return string.Empty;
			}
		}

		/// <summary>
		/// If the contract has a signing window.
		/// </summary>
		public bool HasSigningWindow => !string.IsNullOrEmpty(this.SigningWindowText);

		/// <summary>
		/// If the contract has validity dates. These are set when it becomes signed.
		/// </summary>
		private bool HasValidity => this.Contract is not null && this.Contract.Contract.From != DateTime.MinValue;

		/// <summary>
		/// When the contract is valid from, in local time.
		/// </summary>
		public string ValidFromText => this.HasValidity ? FormatDate(this.Contract!.Contract.From) : string.Empty;

		/// <summary>
		/// If the valid-from date is shown.
		/// </summary>
		public bool HasValidFrom => this.HasValidity;

		/// <summary>
		/// When the contract is valid to, in local time.
		/// </summary>
		public string ValidToText => this.HasValidTo ? FormatDate(this.Contract!.Contract.To) : string.Empty;

		/// <summary>
		/// If the valid-to date is shown.
		/// </summary>
		public bool HasValidTo => this.HasValidity && this.Contract!.Contract.To != DateTime.MaxValue;

		/// <summary>
		/// How long the contract is valid once signed. Shown until it has validity dates.
		/// </summary>
		public string DurationText => this.HasDuration ? DurationToString.ToString(this.Contract!.Contract.Duration!.Value) : string.Empty;

		/// <summary>
		/// If the duration is shown.
		/// </summary>
		public bool HasDuration => !this.HasValidity && this.Contract?.Contract.Duration is not null;

		/// <summary>
		/// When the signed contract may be removed from the server, in local time: its end, plus the archive times.
		/// </summary>
		public string ArchivedUntilText
		{
			get
			{
				if (!this.HasArchivedUntil)
					return string.Empty;

				try
				{
					Contract Current = this.Contract!.Contract;
					DateTime Until = Current.To;

					if (Current.ArchiveRequired is Waher.Content.Duration Required)
						Until += Required;

					if (Current.ArchiveOptional is Waher.Content.Duration Optional)
						Until += Optional;

					return FormatDate(Until);
				}
				catch (ArgumentOutOfRangeException)
				{
					return string.Empty;	// Beyond the last representable date.
				}
			}
		}

		/// <summary>
		/// If the archive date is shown.
		/// </summary>
		public bool HasArchivedUntil =>
			this.HasValidTo &&
			(this.Contract!.Contract.ArchiveRequired is not null || this.Contract.Contract.ArchiveOptional is not null);

		/// <summary>
		/// The required archive time. Shown until the contract has validity dates.
		/// </summary>
		public string ArchiveRequiredText => this.HasArchiveRequired ? DurationToString.ToString(this.Contract!.Contract.ArchiveRequired!.Value) : string.Empty;

		/// <summary>
		/// If the required archive time is shown.
		/// </summary>
		public bool HasArchiveRequired => !this.HasValidity && this.Contract?.Contract.ArchiveRequired is not null;

		/// <summary>
		/// The optional archive time. Shown until the contract has validity dates.
		/// </summary>
		public string ArchiveOptionalText => this.HasArchiveOptional ? DurationToString.ToString(this.Contract!.Contract.ArchiveOptional!.Value) : string.Empty;

		/// <summary>
		/// If the optional archive time is shown.
		/// </summary>
		public bool HasArchiveOptional => !this.HasValidity && this.Contract?.Contract.ArchiveOptional is not null;

		/// <summary>
		/// When the contract was created, in local time.
		/// </summary>
		public string CreatedText => this.Contract is null ? string.Empty : FormatDate(this.Contract.Contract.Created);

		/// <summary>
		/// When the contract was last updated, in local time.
		/// </summary>
		public string UpdatedText => this.HasUpdated ? FormatDate(this.Contract!.Contract.Updated) : string.Empty;

		/// <summary>
		/// If the contract has been updated since it was created.
		/// </summary>
		public bool HasUpdated => this.Contract is not null &&
			this.Contract.Contract.Updated != DateTime.MinValue &&
			this.Contract.Contract.Updated != this.Contract.Contract.Created;

		/// <summary>
		/// The template the contract was created from, if any.
		/// </summary>
		public string TemplateId => this.Contract?.TemplateId ?? string.Empty;

		/// <summary>
		/// If the contract was created from a template.
		/// </summary>
		public bool HasTemplate => !string.IsNullOrEmpty(this.TemplateId);

		/// <summary>
		/// File names of the contract's attachments.
		/// </summary>
		public string[] AttachmentNames => this.Contract?.Contract.Attachments?
			.Select(Attachment => string.IsNullOrEmpty(Attachment.FileName) ? Attachment.Id : Attachment.FileName)
			.ToArray() ?? [];

		/// <summary>
		/// If the contract has attachments.
		/// </summary>
		public bool HasAttachments => (this.Contract?.Contract.Attachments?.Length ?? 0) > 0;

		/// <summary>
		/// Shows or hides the technical details. The machine-readable part is formatted when first shown.
		/// </summary>
		[RelayCommand]
		private void ToggleTechnicalDetails()
		{
			this.IsTechnicalDetailsExpanded = !this.IsTechnicalDetailsExpanded;

			if (this.IsTechnicalDetailsExpanded && this.MachineReadableXml is null)
				this.MachineReadableXml = this.FormatMachineReadable();
		}

		/// <summary>
		/// Formats the machine-readable part of the contract as indented XML.
		/// </summary>
		/// <returns>The formatted XML, or an empty string if there is none.</returns>
		private string FormatMachineReadable()
		{
			try
			{
				if (this.Contract?.Contract.ForMachines is null)
					return string.Empty;

				return XML.PrettyXml(this.Contract.Contract.ForMachines);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return this.Contract?.Contract.ForMachines?.OuterXml ?? string.Empty;
			}
		}

		/// <summary>
		/// Formats a date and time in local time.
		/// </summary>
		/// <param name="Value">The date and time, in UTC unless marked as local.</param>
		/// <returns>The local date and time.</returns>
		private static string FormatDate(DateTime Value)
		{
			DateTime Local = Value.Kind switch
			{
				DateTimeKind.Local => Value,
				DateTimeKind.Utc => Value.ToLocalTime(),
				_ => DateTime.SpecifyKind(Value, DateTimeKind.Utc).ToLocalTime()
			};

			return Local.ToString("g", CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Follows the shown contract, so the overview updates when it changes.
		/// </summary>
		partial void OnContractChanged(ObservableContract? oldValue, ObservableContract? newValue)
		{
			if (oldValue is not null)
				oldValue.PropertyChanged -= this.Contract_PropertyChanged;

			if (newValue is not null)
				newValue.PropertyChanged += this.Contract_PropertyChanged;

			this.MachineReadableXml = this.IsTechnicalDetailsExpanded ? this.FormatMachineReadable() : null;
			this.OnOverviewChanged();
		}

		partial void OnIsDeletedOnServerChanged(bool value)
		{
			this.OnOverviewChanged();
		}

		/// <summary>
		/// The title is read when the contract is initialized, after the contract is shown.
		/// </summary>
		private void Contract_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ObservableContract.Category))
				MainThread.BeginInvokeOnMainThread(() => this.OnPropertyChanged(nameof(this.ContractName)));
		}

		/// <summary>
		/// Notifies every value the overview shows about the contract.
		/// </summary>
		private void OnOverviewChanged()
		{
			this.OnPropertyChanged(nameof(this.ContractName));
			this.OnPropertyChanged(nameof(this.StateText));
			this.OnPropertyChanged(nameof(this.IsStateSuccess));
			this.OnPropertyChanged(nameof(this.IsStateDanger));
			this.OnPropertyChanged(nameof(this.CreatedTokensContractId));
			this.OnPropertyChanged(nameof(this.SigningWindowText));
			this.OnPropertyChanged(nameof(this.HasSigningWindow));
			this.OnPropertyChanged(nameof(this.ValidFromText));
			this.OnPropertyChanged(nameof(this.HasValidFrom));
			this.OnPropertyChanged(nameof(this.ValidToText));
			this.OnPropertyChanged(nameof(this.HasValidTo));
			this.OnPropertyChanged(nameof(this.DurationText));
			this.OnPropertyChanged(nameof(this.HasDuration));
			this.OnPropertyChanged(nameof(this.ArchivedUntilText));
			this.OnPropertyChanged(nameof(this.HasArchivedUntil));
			this.OnPropertyChanged(nameof(this.ArchiveRequiredText));
			this.OnPropertyChanged(nameof(this.HasArchiveRequired));
			this.OnPropertyChanged(nameof(this.ArchiveOptionalText));
			this.OnPropertyChanged(nameof(this.HasArchiveOptional));
			this.OnPropertyChanged(nameof(this.CreatedText));
			this.OnPropertyChanged(nameof(this.UpdatedText));
			this.OnPropertyChanged(nameof(this.HasUpdated));
			this.OnPropertyChanged(nameof(this.TemplateId));
			this.OnPropertyChanged(nameof(this.HasTemplate));
			this.OnPropertyChanged(nameof(this.AttachmentNames));
			this.OnPropertyChanged(nameof(this.HasAttachments));
			this.OnPropertyChanged(nameof(this.Visibility));
		}

		#endregion

		#region Signing

		public ObservableCollection<ObservableRole> SignableRoles { get; } = new();

		/// <summary>
		/// Roles of a named-parts contract the user can sign for an earlier identity of theirs, after renewing it.
		/// </summary>
		private IReadOnlyCollection<string> rolesSignableAsEarlierIdentity = Array.Empty<string>();

		private bool HasSignableRoles => this.SignableRoles.Count > 0;
		private bool AlreadySigned => this.Contract?.Roles.Any(r => r.Parts.Any(p => p.IsMe && p.HasSigned)) == true;

		/// <summary>
		/// If signing is offered. Follows <see cref="ContractActionRules.CanSign"/>, and the user must not have signed already.
		/// </summary>
		public bool CanSign =>
			this.HasSignableRoles &&
			!this.AlreadySigned &&
			!this.IsDeletedOnServer &&
			this.Contract is not null &&
			ContractActionRules.CanSign(this.Contract.Contract, ServiceRef.TagProfile.LegalIdentity?.Id, DateTime.UtcNow,
				this.rolesSignableAsEarlierIdentity);

		// Public flag for bottom bar visibility; sign bar only when not awaiting post-create
		public bool CanShowSignBar => !this.IsAwaitingPostCreateCompletion && this.CanSign;

		public bool ReadyToSign => this.SelectedRole is not null && this.IsContractOk;

		[ObservableProperty]
		private ObservableRole? selectedRole;

		partial void OnSelectedRoleChanged(ObservableRole? oldValue, ObservableRole? newValue)
		{
			var MyLegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
			if (string.IsNullOrEmpty(MyLegalId))
				return;

			oldValue?.RemovePart(MyLegalId);
			_ = newValue?.AddPart(MyLegalId);

			OnPropertyChanged(nameof(ReadyToSign));
			SignCommand.NotifyCanExecuteChanged();
		}

		/// <summary>
		/// If the user is going through the signing steps: Summary, Review, Role, and Result.
		/// </summary>
		[ObservableProperty]
		private bool isSigningFlow;

		/// <summary>
		/// If the last signing attempt succeeded.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsSignFailure))]
		private bool isSignSuccess;

		/// <summary>
		/// If the last signing attempt failed.
		/// </summary>
		public bool IsSignFailure => !this.IsSignSuccess;

		/// <summary>
		/// The heading of the result step.
		/// </summary>
		[ObservableProperty]
		private string signResultTitle = string.Empty;

		/// <summary>
		/// What happens next, or why the signature wasn't made.
		/// </summary>
		[ObservableProperty]
		private string signResultText = string.Empty;

		/// <summary>
		/// The role the user is invited to sign as, or the roles they can choose from.
		/// </summary>
		public string SigningRolesText => !string.IsNullOrEmpty(this.ProposalRole)
			? this.ProposalRole
			: string.Join(", ", this.SignableRoles.Select(Role => Role.Name));

		/// <summary>
		/// Signs the contract in the selected role: confirms with PIN or biometrics, checks again, signs, and shows the result.
		/// A cancelled or failed confirmation stays on the Role step, and nothing is sent.
		/// </summary>
		/// <returns>A task that completes when signing has finished or was cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(ReadyToSign))]
		public Task SignAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null || this.SelectedRole is null)
					return;

				Contract ToSign = this.Contract.Contract;
				string RoleName = this.SelectedRole.Name;

				if (!await ServiceRef.AuthenticationService.AuthenticateUserAsync(AuthenticationPurpose.SignContract, true))
					return;

				// Check again just before the request, since time passes and others sign.
				IReadOnlyList<ContractSigningBlocker> Blockers = await ContractSigningGuard.CheckAsync(ToSign, RoleName);
				if (Blockers.Count > 0)
				{
					await this.ShowSignResultAsync(false, string.Join(Environment.NewLine, Blockers.Select(Blocker => Blocker.Message)));
					return;
				}

				Contract? SignedContract = null;
				string ErrorText = string.Empty;

				await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(true));
				try
				{
					SignedContract = await ServiceRef.XmppService.SignContract(ToSign, RoleName, false);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					ErrorText = ContractErrorMessages.GetMessage(Ex, ToSign);
				}
				finally
				{
					await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(false));
				}

				if (SignedContract is null)
				{
					await this.ShowSignResultAsync(false, ErrorText);
					return;
				}

				await this.ShowSignResultAsync(true, GetSignedText(SignedContract));
				await this.RefreshContractAsync(SignedContract);
			});
		}

		/// <summary>
		/// Shows the result step.
		/// </summary>
		/// <param name="Success">If the contract was signed.</param>
		/// <param name="Text">What happens next, or why it wasn't signed.</param>
		private async Task ShowSignResultAsync(bool Success, string Text)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsSignSuccess = Success;
				this.SignResultTitle = Success
					? ServiceRef.Localizer[nameof(AppResources.ContractSignResultSuccessTitle)]
					: ServiceRef.Localizer[nameof(AppResources.ContractSignResultFailedTitle)];
				this.SignResultText = string.IsNullOrWhiteSpace(Text)
					? ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]
					: Text;
			});

			await this.GoToStateAsync(ViewContractStep.Result);
		}

		/// <summary>
		/// Explains what happens after the user's signature: the contract is signed, or how many signatures are missing.
		/// </summary>
		/// <param name="Signed">The contract returned after signing.</param>
		/// <returns>The localized text.</returns>
		private static string GetSignedText(Contract Signed)
		{
			if (Signed.State == ContractState.Signed)
				return ServiceRef.Localizer[nameof(AppResources.ContractSignResultSigned)];

			int Missing = 0;

			foreach (Role Role in Signed.Roles ?? [])
			{
				int Count = Signed.ClientSignatures?.Count(Signature => Signature.Role == Role.Name) ?? 0;
				if (Count < Role.MinCount)
					Missing += Role.MinCount - Count;
			}

			if (Missing == 1)
				return ServiceRef.Localizer[nameof(AppResources.ContractSignResultWaitingOne)];

			if (Missing > 1)
				return ServiceRef.Localizer[nameof(AppResources.ContractSignResultWaitingMany), Missing];

			return ServiceRef.Localizer[nameof(AppResources.ContractSignResultAdded)];
		}

		#endregion

		#region Navigation Commands

		/// <summary>
		/// Goes back to the overview, or leaves the page when the overview is shown.
		/// </summary>
		/// <returns>A task that completes when the navigation has finished.</returns>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		public Task BackAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				ViewContractStep Step = Enum.Parse<ViewContractStep>(this.CurrentState);
				if (Step == ViewContractStep.Overview || Step == ViewContractStep.Loading)
					await base.GoBack();
				else if (this.IsSigningFlow && Step == ViewContractStep.Review)
					await this.GoToStateAsync(ViewContractStep.Summary);
				else if (this.IsSigningFlow && Step == ViewContractStep.Sign)
					await this.GoToStateAsync(ViewContractStep.Review);
				else
					await this.EndSigningFlowAsync();
			});
		}

		public override async Task GoBack()
		{
			await this.BackAsync();
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToParametersAsync() => this.RunContractActionAsync(() => this.GoToStepAsync(ViewContractStep.Parameters));

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToRolesAsync() => this.RunContractActionAsync(() => this.GoToStepAsync(ViewContractStep.Roles));

		/// <summary>
		/// Starts signing. Checks first what the server would refuse, and lists it instead of starting.
		/// </summary>
		/// <returns>A task that completes when the first step is shown, or the reasons are.</returns>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToSignAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				string? RoleName = string.IsNullOrEmpty(this.ProposalRole) ? null : this.ProposalRole;
				IReadOnlyList<ContractSigningBlocker> Blockers = await ContractSigningGuard.CheckAsync(this.Contract.Contract, RoleName);

				if (Blockers.Count > 0)
				{
					await SigningBlockersViewModel.ShowAsync(Blockers);
					return;
				}

				await MainThread.InvokeOnMainThreadAsync(() => this.IsSigningFlow = true);
				await this.GoToStateAsync(ViewContractStep.Summary);
			});
		}

		/// <summary>
		/// Goes from the summary to the full text.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToSigningReviewAsync() => this.RunContractActionAsync(this.ShowReviewAsync);

		/// <summary>
		/// Goes from the full text to choosing the role.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToSigningRoleAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (this.SelectedRole is not null)
						return;

					if (!string.IsNullOrEmpty(this.ProposalRole))
						this.SelectedRole = this.SignableRoles.FirstOrDefault(r => r.Name == this.ProposalRole);
					else if (this.SignableRoles.Count == 1)
						this.SelectedRole = this.SignableRoles[0];
				});

				await this.GoToStateAsync(ViewContractStep.Sign);
			});
		}

		/// <summary>
		/// Goes back to choosing the role after a failed attempt.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task RetrySigningAsync() => this.RunContractActionAsync(() => this.GoToStateAsync(ViewContractStep.Sign));

		/// <summary>
		/// Leaves the signing steps and shows the overview.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task FinishSigningAsync() => this.RunContractActionAsync(this.EndSigningFlowAsync);

		/// <summary>
		/// Ends the signing steps, if running, and shows the overview.
		/// </summary>
		private async Task EndSigningFlowAsync()
		{
			await MainThread.InvokeOnMainThreadAsync(() => this.IsSigningFlow = false);
			await this.GoToStateAsync(ViewContractStep.Overview);
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToReviewAsync() => this.RunContractActionAsync(this.ShowReviewAsync);

		/// <summary>
		/// Shows the human-readable text, after checking the parameter values.
		/// </summary>
		private async Task ShowReviewAsync()
		{
			if (this.Contract is null)
				return;

			await this.GoToStepAsync(ViewContractStep.Review, Prepare: async () =>
			{
				await this.ValidateParametersAsync();
				VerticalStackLayout? HumanReadableText = await this.Contract.Contract.ToMaui(this.Contract.Contract.DeviceLanguage());
				this.HumanReadableText = HumanReadableText;
			});
		}

		[RelayCommand]
		private Task OpenServerSignatureAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract?.Contract is { } ContractObj)
					await ServiceRef.NavigationService.GoToAsync(nameof(ServerSignaturePage), new ServerSignatureNavigationArgs(ContractObj), Services.UI.BackMethod.Pop);
			});
		}

		#endregion

		#region Proposals

		/// <summary>
		/// Sends a proposal to a part. The user enters the message first. Access to the contract is only granted
		/// once the user chooses to send, and Cancel neither grants access nor sends anything.
		/// </summary>
		/// <param name="Part">The part to send the proposal to.</param>
		/// <returns>A task that completes when the proposal has been sent or the user cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task SendProposalToPartAsync(ObservablePart? Part)
		{
			return this.RunContractActionAsync(async () =>
			{
				if (Part is null || this.Contract is null)
					return;

				if (!Part.CanSendProposal)
					return;

				ContactInfo? Info = await ContactInfo.FindByLegalId(Part.LegalId);
				string? BareJid = Info?.BareJid;
				if (string.IsNullOrWhiteSpace(BareJid))
					BareJid = Part.GetJid();

				if (string.IsNullOrWhiteSpace(BareJid))
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkAddressOfContactUnknown)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				string? FriendlyTarget = Info?.FriendlyName;
				if (string.IsNullOrEmpty(FriendlyTarget))
					FriendlyTarget = Part.FriendlyName ?? BareJid;

				string? Proposal = await ServiceRef.UiService.DisplayPrompt(
					ServiceRef.Localizer[nameof(AppResources.Proposal)] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.EnterProposalMessage), FriendlyTarget] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.Send)] ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.Cancel)] ?? string.Empty);

				if (Proposal is null) // Cancelled: grant no access and send nothing
					return;
				if (string.IsNullOrEmpty(Proposal)) // Use default if empty
					Proposal = ServiceRef.Localizer[nameof(AppResources.ProposalDefaultMessage)];

				await ServiceRef.XmppService.ContractsClient.AuthorizeAccessToContractAsync(
					this.Contract.ContractId,
					BareJid,
					true);

				await ServiceRef.XmppService.SendContractProposal(
					this.Contract.Contract,
					Part.Part.Role,
					BareJid,
					Proposal);
			});
		}

		/// <summary>
		/// Opens the Invite sheet, which invites everyone who hasn't signed with one message.
		/// </summary>
		/// <returns>A task that completes when the sheet has closed.</returns>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task InvitePartsAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				IReadOnlyList<InviteRecipient> Recipients = await InvitePartsViewModel.GetRecipientsAsync(this.Contract, null, true);
				if (Recipients.Count == 0)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.InviteToSign)],
						ServiceRef.Localizer[nameof(AppResources.InviteNobodyReachable)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				await InvitePartsViewModel.ShowAsync(this.Contract.Contract, Recipients);
			});
		}

		/// <summary>
		/// Declines the invitation to sign, after the user confirms. The contract and its notifications are removed.
		/// </summary>
		/// <returns>A task that completes when the invitation has been declined or the user cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task DeclineInvitationAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.DeclineInvitationConfirm)]))
					return;

				await ServiceRef.ContractOrchestratorService.DeclineInvitationAsync(this.Contract.ContractId);
				await base.GoBack();
			});
		}

		/// <summary>
		/// Removes a contract that has ended or was deleted from the user's list, after the user confirms.
		/// </summary>
		/// <returns>A task that completes when the contract has been removed or the user cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task RemoveFromListAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.RemoveFromListConfirm)]))
					return;

				await ServiceRef.ContractOrchestratorService.DeclineInvitationAsync(this.Contract.ContractId);
				await base.GoBack();
			});
		}

		/// <summary>
		/// Asks the people who signed the contract to let the user see it again.
		/// </summary>
		/// <returns>A task that completes when the request has been sent or the user cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task RequestAccessAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is not null)
					await ServiceRef.ContractOrchestratorService.RequestContractAccessAsync(this.Contract.ContractId);
			});
		}

		/// <summary>
		/// Shows a contract the user has just been given access to.
		/// </summary>
		/// <param name="Contract">The contract, as read from the server.</param>
		/// <returns>A task that completes when the refresh has been queued.</returns>
		public Task ShowAccessGrantedContractAsync(Contract Contract)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IsAccessDenied = false;
				this.CanRequestAccess = false;
			});

			this.RequestRefresh(Contract);
			return Task.CompletedTask;
		}

		#endregion

		#region Contract Management Commands

		/// <summary>
		/// Withdraws (obsoletes) the contract, which cancels it for everyone, after the user confirms.
		/// </summary>
		/// <returns>A task that completes when the contract has been withdrawn or the user cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task WithdrawContractAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				if (!await this.ConfirmAsync(nameof(AppResources.ContractWithdrawConfirm), AuthenticationPurpose.ObsoleteContract))
					return;

				Contract Withdrawn = await ServiceRef.XmppService.ObsoleteContract(this.Contract.ContractId);
				await MainThread.InvokeOnMainThreadAsync(() => this.CanWithdrawContract = false);
				await this.RefreshContractAsync(Withdrawn);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.ContractWithdrawn)]);
			});
		}

		/// <summary>
		/// Deletes the contract after the user confirms, and leaves the page.
		/// </summary>
		/// <returns>A task that completes when the contract has been deleted or the user cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task DeleteContractAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				if (!await this.ConfirmAsync(nameof(AppResources.AreYouSureYouWantToDeleteContract), AuthenticationPurpose.DeleteContract))
					return;

				await ServiceRef.XmppService.DeleteContract(this.Contract.ContractId);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.CanDeleteContract = false;
					this.CanWithdrawContract = false;
				});

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.ContractHasBeenDeleted)]);

				await base.GoBack();
			});
		}

		#endregion

		#region Clipboard and Link Commands

		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task ShareAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				this.GenerateQrCode(this.Contract.Contract.ContractIdUriString);
				await this.OpenQrPopup("");
			});
		}

		[RelayCommand]
		private Task CopyAsync(object Item)
		{
			return this.RunContractActionAsync(() => this.CopyToClipboardAsync(Item));
		}

		[RelayCommand]
		private Task OpenContractAsync(object Item)
		{
			return this.RunContractActionAsync(async () =>
			{
				if (Item is string Id)
					await App.OpenUrlAsync(Constants.UriSchemes.IotSc + ":" + Id);
			});
		}

		[RelayCommand]
		private Task OpenLinkAsync(object Item)
		{
			return this.RunContractActionAsync(async () =>
			{
				if (Item is string Url)
				{
					if (!await App.OpenUrlAsync(Url, false))
						await this.CopyToClipboardAsync(Url);
				}
				else
					await this.CopyToClipboardAsync(Item);
			});
		}

		/// <summary>
		/// Copies an item to the clipboard and tells the user. The contract's own ID is copied as a contract link.
		/// </summary>
		/// <param name="Item">The item to copy.</param>
		/// <returns>A task that completes when the item has been copied.</returns>
		private async Task CopyToClipboardAsync(object Item)
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

		#endregion

		#region State Navigation Helpers

		private Command CreateUriCommand(UriScheme Scheme)
			=> new Command(async Parameter => await this.ExecuteUriClicked(Parameter, Scheme));

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

			DateTime Deadline = DateTime.UtcNow + stateChangeTimeout;
			while (!this.CanStateChange)
			{
				if (DateTime.UtcNow >= Deadline)
				{
					// Don't wait forever if a gate was left closed. The page must stay usable.
					ServiceRef.LogService.LogWarning("Timed out waiting to change contract page state to " + NewState + ".");
					await this.SetCanStateChangeOnMainThreadAsync(true);
					break;
				}

				await Task.Delay(100);
			}

			await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await StateContainer.ChangeStateWithAnimation(this.StateObject, NewState);
				});
		}

		/// <summary>
		/// Runs a contract action. Any error is logged and shown to the user as one message, and
		/// <see cref="CanStateChange"/> is restored, so the page stays usable.
		/// </summary>
		/// <param name="Action">The action to run.</param>
		/// <returns>true if the action completed without errors, false otherwise.</returns>
		private async Task<bool> RunContractActionAsync(Func<Task> Action)
		{
			bool PreviousCanStateChange = this.CanStateChange;

			try
			{
				await Action();
				return true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				try
				{
					if (PreviousCanStateChange && !this.CanStateChange)
						await this.SetCanStateChangeOnMainThreadAsync(true);

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ContractErrorMessages.GetMessage(Ex, this.Contract?.Contract),
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
				catch (Exception Ex2)
				{
					ServiceRef.LogService.LogException(Ex2);
				}

				return false;
			}
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

		private void SubscribeToEvents()
		{
			ServiceRef.ContractStore.ContractChanged += this.ContractStore_ContractChanged;
			ServiceRef.ContractOrchestratorService.IdentityPetitionAnswered += this.OrchestratorService_IdentityPetitionAnswered;
			this.SignableRoles.CollectionChanged += this.OnSignableRolesChanged;
		}

		private void UnsubscribeFromEvents()
		{
			ServiceRef.ContractStore.ContractChanged -= this.ContractStore_ContractChanged;
			ServiceRef.ContractOrchestratorService.IdentityPetitionAnswered -= this.OrchestratorService_IdentityPetitionAnswered;
			this.SignableRoles.CollectionChanged -= this.OnSignableRolesChanged;
		}

		private void OnSignableRolesChanged(object? sender, NotifyCollectionChangedEventArgs e)
			=> MainThread.BeginInvokeOnMainThread(() =>
			{
				this.OnPropertyChanged(nameof(this.CanSign));
				this.OnPropertyChanged(nameof(this.CanShowSignBar));
				this.OnPropertyChanged(nameof(this.SigningRolesText));
			});

		/// <summary>
		/// Shows changes the store saves for this contract, whether they come from the server, a sync or the user. Contracts
		/// in store events come from decrypting calls, so they can replace a saved copy.
		/// </summary>
		private Task ContractStore_ContractChanged(object? Sender, ContractChangedEventArgs e)
		{
			if (this.Contract is null || !e.IsFor(this.Contract.ContractId))
				return Task.CompletedTask;

			switch (e.Kind)
			{
				case ContractChangeKind.Added:
				case ContractChangeKind.Updated:
					if (e.Contract is not null)
						this.RequestRefresh(e.Contract);
					break;

				case ContractChangeKind.Deleted:
					bool KeptCopy = e.Reference is not null;
					MainThread.BeginInvokeOnMainThread(() => this.ShowDeleted(KeptCopy));
					break;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Shows the answer to an identity request on the part it was sent for.
		/// </summary>
		private async Task OrchestratorService_IdentityPetitionAnswered(object? Sender, IdentityPetitionAnsweredEventArgs e)
		{
			try
			{
				if (this.Contract is null || !await this.Contract.ApplyIdentityAnswerAsync(e))
					return;

				await this.PreparePropertiesAsync();

				if (e.Accepted)
					_ = this.OfferInvitationAsync(e.LegalId);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Offers to invite a part who just showed their identity, since that is what the app needed to reach them.
		/// Nothing is sent unless the user sends it.
		/// </summary>
		/// <param name="LegalId">The legal ID of the part.</param>
		private async Task OfferInvitationAsync(string LegalId)
		{
			try
			{
				if (this.Contract is null || !this.CanManageParts || ServiceRef.NavigationService.CurrentPage is not ViewContractPage)
					return;

				IReadOnlyList<InviteRecipient> Recipients = await InvitePartsViewModel.GetRecipientsAsync(this.Contract, null, true);
				InviteRecipient[] Shown = [.. Recipients.Where(Recipient => string.Equals(Recipient.LegalId, LegalId, StringComparison.OrdinalIgnoreCase))];
				if (Shown.Length == 0)
					return;

				Contract Current = this.Contract.Contract;
				await MainThread.InvokeOnMainThreadAsync(() => InvitePartsViewModel.ShowAsync(Current, Shown));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Shows that the contract was deleted on the server, and offers only what can still be done.
		/// </summary>
		/// <param name="KeptCopy">If a read-only copy is kept, which the user can remove.</param>
		private void ShowDeleted(bool KeptCopy)
		{
			this.IsDeletedOnServer = true;
			this.IsAccessDenied = false;
			this.CanRemoveFromList = KeptCopy;
			this.CanDeleteContract = false;
			this.CanWithdrawContract = false;
			this.CanInviteParts = false;
			this.CanManageParts = false;
			this.CanDeclineInvitation = false;
		}

		/// <summary>
		/// Shows that the server no longer lets the user read the contract.
		/// </summary>
		private Task ShowAccessDeniedAsync()
		{
			bool HasSignatures = (this.Contract?.Contract.ClientSignatures?.Length ?? 0) > 0;

			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsAccessDenied = true;
				this.CanRequestAccess = HasSignatures;
				this.CanInviteParts = false;
				this.CanManageParts = false;
			});
		}

		/// <summary>
		/// Marks the contract as seen, so the list shows it as read.
		/// </summary>
		private async Task MarkViewedAsync(CaseInsensitiveString ContractId)
		{
			try
			{
				await ServiceRef.ContractStore.MarkViewedAsync(ContractId);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
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

					// Transient values are gone after creation, and encrypted values can't be checked without the key.
					if (ParamToValidate.IsTransient || (ParamToValidate.IsEncrypted && !ParamToValidate.CanReadValue))
						return (Param: ParamToValidate, IsValid: true, ValidationText: string.Empty);

					try
					{
						IsValid = await ParamToValidate.Parameter.IsParameterValid(Variables, ServiceRef.XmppService.ContractsClient).ConfigureAwait(false);
						IsValid = IsValid || ParamToValidate.Parameter.ErrorText == ContractStatus.ClientIdentityInvalid.ToString();
						ValidationText = IsValid ? string.Empty : ParamToValidate.Parameter.ErrorText ?? string.Empty;
					}
					catch (Exception Ex2)
					{
						ServiceRef.LogService.LogException(Ex2);
						IsValid = true;
					}
					return (Param: ParamToValidate, IsValid, ValidationText);
				}).ToArray();

				(ObservableParameter Param, bool IsValid, string ValidationText)[] Results = await Task.WhenAll(ValidationTasks);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach ((ObservableParameter Param, bool IsValid, string ValidationText) Result in Results)
					{
						Result.Param.IsValid = Result.IsValid;
						Result.Param.ValidationText = Result.ValidationText;
					}
				});
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

		/// <summary>
		/// Loads the contract from the navigation arguments, the saved copy or the server. A saved copy is shown at once, and
		/// the current version is read in the background.
		/// </summary>
		/// <returns>true if the contract was loaded. When false, the user has been told why and the page has already gone back.</returns>
		private async Task<bool> LoadContractAsync()
		{
			Contract? Contract = this.args!.Contract;
			CaseInsensitiveString? ContractId = this.args.ContractRef?.ContractId;

			if (Contract is null && this.args.ContractRef is not null)
			{
				try
				{
					Contract = await this.args.ContractRef.GetContract();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			if (Contract is null)
			{
				if (ContractId is null || CaseInsensitiveString.IsNullOrEmpty(ContractId))
				{
					await this.GoBack();
					return false;
				}

				ContractRefreshResult Result = await ServiceRef.ContractSyncService.RefreshAsync(ContractId, ContractOrigin.Opened, false, CancellationToken.None);
				if (Result.Contract is null)
				{
					string Message = Result.Status switch
					{
						ContractRefreshStatus.Deleted => ServiceRef.Localizer[nameof(AppResources.ContractWasDeleted)],
						ContractRefreshStatus.Offline => ServiceRef.Localizer[nameof(AppResources.AppNotConnected)],
						_ => ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeOpened)]
					};

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Message,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);

					await this.GoBack();
					return false;
				}

				Contract = Result.Contract;
			}

			this.Contract = await ObservableContract.CreateAsync(Contract);

			// A saved copy keeps encrypted values in their protected form. They show "Loading…" until the current version arrives.
			ObservableParameter[] Unreadable = [.. this.Contract.Parameters.Where(Parameter => Parameter.IsEncrypted && !Parameter.CanReadValue)];
			this.showingSavedCopy = Unreadable.Length > 0;

			if (this.args.ContractRef is not null || this.showingSavedCopy)
			{
				foreach (ObservableParameter Parameter in Unreadable)
					Parameter.IsLoadingValue = true;

				this.RequestRefresh(null);
			}

			_ = this.MarkViewedAsync(Contract.ContractId);
			return true;
		}

		private async Task InitializeUIAsync()
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				this.ProposalFriendlyName = await this.ResolveProposalFriendlyNameAsync();
				this.ProposalRole = this.args!.Role ?? string.Empty;
				this.ProposalMessage = this.args!.Proposal ?? string.Empty;
			});

			await MainThread.InvokeOnMainThreadAsync(this.PrepareDisplayableParameters);
			await MainThread.InvokeOnMainThreadAsync(this.PrepareSignableRoles);

			// Deciding which actions to offer can need the server, so the overview doesn't wait for it.
			_ = this.PreparePropertiesAsync();

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (!string.IsNullOrEmpty(this.ProposalRole))
					this.SelectedRole = this.SignableRoles.FirstOrDefault(r => r.Name == this.ProposalRole);
				else if (this.SignableRoles.Count == 1)
					this.SelectedRole = this.SignableRoles[0];
				this.OnPropertyChanged(nameof(this.CanSign));
				this.OnPropertyChanged(nameof(this.ReadyToSign));
				this.OnPropertyChanged(nameof(this.CanShowSignBar));

			});
		}

		/// <summary>
		/// Gets the name of the person who sent the invitation, or their address and that they aren't a contact.
		/// </summary>
		private async Task<string> ResolveProposalFriendlyNameAsync()
		{
			string? FromJid = this.args!.FromJID;
			if (string.IsNullOrEmpty(FromJid))
				return string.Empty;

			try
			{
				ContactInfo? Info = await ContactInfo.FindByBareJid(FromJid);
				if (Info is null)
					return ServiceRef.Localizer[nameof(AppResources.InvitationSenderNotContact), FromJid];

				return string.IsNullOrEmpty(Info.FriendlyName) ? FromJid : Info.FriendlyName;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return FromJid;
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
/*
			foreach (ObservableParameter P in this.DisplayableParameters)
			{
				P.Parameter.IsParameterValid(Vars, ServiceRef.XmppService.ContractsClient);
				ServiceRef.LogService.LogDebug($"Parameter '{P.Parameter.Name}' validation result: {P.IsValid}, Error: {P.Parameter.ErrorReason} - {P.ValidationText}");
			}
*/
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
					if (Role.Parts.Any(p => p.IsMe) || this.rolesSignableAsEarlierIdentity.Contains(Role.Name))
						this.SignableRoles.Add(Role);
			}
		}

		/// <summary>
		/// Decides which contract actions to offer, using <see cref="ContractActionRules"/> and the saved contract.
		/// When something can't be checked, the action is not offered.
		/// </summary>
		private async Task PreparePropertiesAsync()
		{
			try
			{
				if (this.Contract is null)
					return;

				Contract Current = this.Contract.Contract;
				ContractReference? Row = null;
				bool IsCreator = false;
				bool BindingNow = true;
				bool BindingIgnoringTime = true;

				try
				{
					Row = await ServiceRef.ContractStore.GetAsync(Current.ContractId);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				try
				{
					IsCreator = Row?.IsCreator == true || await ServiceRef.XmppService.IsContractCreatorAsync(Current.ContractId);
					BindingNow = await Current.IsLegallyBinding(true, ServiceRef.XmppService.ContractsClient);
					BindingIgnoringTime = BindingNow || await Current.IsLegallyBinding(false, ServiceRef.XmppService.ContractsClient);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				string? MyLegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
				bool ReadOnly = this.args?.IsReadOnly ?? false;
				DateTime UtcNow = DateTime.UtcNow;
				bool DeletedOnServer = this.IsDeletedOnServer || Row?.IsDeletedOnServer == true;
				bool Ended = DeletedOnServer || Current.State is ContractState.Obsoleted or ContractState.Deleted or ContractState.Rejected or ContractState.Failed;
				bool HasSigned = Row?.HasSigned == true || Current.ClientSignatures?.Any(Signature => Signature.LegalId == MyLegalId) == true;

				bool CanDelete = !ReadOnly && !DeletedOnServer && ContractActionRules.CanDelete(Current, IsCreator, BindingIgnoringTime, UtcNow);
				bool CanWithdraw = !ReadOnly && !DeletedOnServer && ContractActionRules.CanWithdraw(Current, IsCreator, BindingNow,
					MyLegalId, ServiceRef.XmppService.BareJid);

				// Contracts the user created or signed come back with the next sync, so only others can be removed.
				bool CanRemove = Row is not null && (DeletedOnServer || (Ended && !IsCreator && !HasSigned));
				bool CanDecline = !ReadOnly && !Ended && Row?.HasOpenInvitation == true;
				// After renewing an identity, the user can sign for the earlier one that the contract names.
				IReadOnlyCollection<string> RenewedRoles = Array.Empty<string>();
				if (!ReadOnly && !Ended && !HasSigned && !string.IsNullOrEmpty(MyLegalId) &&
					Current.PartsMode == ContractParts.ExplicitlyDefined &&
					Current.State is ContractState.Approved or ContractState.BeingSigned)
				{
					try
					{
						RenewedRoles = await ContractSigningGuard.GetRolesSignableAsEarlierIdentityAsync(Current, MyLegalId);
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				bool CanManageParts = !ReadOnly && !Ended && !this.IsAccessDenied &&
					Current.State is ContractState.Proposed or ContractState.Approved or ContractState.BeingSigned;

				// Parts whose identity the user can't see have no known address. The Parties section offers to request it instead.
				bool CanInvite = false;
				if (CanManageParts)
				{
					try
					{
						CanInvite = (await InvitePartsViewModel.GetRecipientsAsync(this.Contract, null, true)).Count > 0;
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (!ReferenceEquals(this.Contract?.Contract, Current))
						return; // A newer version of the contract is shown, and decides for itself.

					this.CanDeleteContract = CanDelete;
					this.CanWithdrawContract = CanWithdraw;
					this.CanRemoveFromList = CanRemove;
					this.CanDeclineInvitation = CanDecline;
					this.CanInviteParts = CanInvite;
					this.CanManageParts = CanManageParts;

					if (RenewedRoles.Count > 0 || this.rolesSignableAsEarlierIdentity.Count > 0)
					{
						this.rolesSignableAsEarlierIdentity = RenewedRoles;
						this.PrepareSignableRoles();
						this.OnPropertyChanged(nameof(this.CanSign));
						this.OnPropertyChanged(nameof(this.CanShowSignBar));
					}

					if (DeletedOnServer && !this.IsDeletedOnServer)
						this.ShowDeleted(Row is not null);
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
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

			// Pull to refresh passes no contract. Its failures are shown.
			if (newContract is null)
				this.userRequestedRefresh = true;

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
				while (!this.initialized || this.Contract is null)
					await Task.Delay(50);

				while (true)
				{
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

		private async Task DoRefreshAsync(Contract? NewContract)
		{
			if (this.Contract is null)
				return;

			ServiceRef.LogService.LogDebug($"RefreshContractAsync start for {this.Contract.ContractId}");
			bool PreviousStateChange = this.CanStateChange;
			ViewContractStep? StepToRestore = null;

			try
			{
				await this.SetCanStateChangeOnMainThreadAsync(false); // Gate state transitions during refresh

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (!this.IsRefreshing)
					{
						this.suppressNextRefreshCommand = true;
						this.IsRefreshing = true;
					}
				});

				bool UserRequested = this.userRequestedRefresh;
				this.userRequestedRefresh = false;

				if (NewContract is null)
				{
					// The sync service stores the contract, and decrypts it when a key is known.
					ContractRefreshResult Result = await ServiceRef.ContractSyncService.RefreshAsync(this.Contract.ContractId,
						ContractOrigin.Opened, false, CancellationToken.None);

					switch (Result.Status)
					{
						case ContractRefreshStatus.Updated:
						case ContractRefreshStatus.Unchanged:
							NewContract = Result.Contract;
							if (this.IsAccessDenied)
								await MainThread.InvokeOnMainThreadAsync(() => this.IsAccessDenied = false);
							break;

						case ContractRefreshStatus.Forbidden:
							await this.ShowAccessDeniedAsync();
							return;

						case ContractRefreshStatus.Deleted:
							bool KeptCopy = Result.Reference is not null;
							await MainThread.InvokeOnMainThreadAsync(() => this.ShowDeleted(KeptCopy));
							return;

						case ContractRefreshStatus.Offline:
							if (UserRequested)
							{
								await ServiceRef.UiService.DisplayAlert(
									ServiceRef.Localizer[nameof(AppResources.AppNotConnected)],
									ServiceRef.Localizer[nameof(AppResources.PleaseTryAgain)],
									ServiceRef.Localizer[nameof(AppResources.Ok)]);
							}
							return; // Keep showing what we have.

						default:
							if (UserRequested)
							{
								await ServiceRef.UiService.DisplayAlert(
									ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
									ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
									ServiceRef.Localizer[nameof(AppResources.Ok)]);
							}
							return;
					}
				}

				// A saved copy is always replaced, since only the current version has readable encrypted values.
				Contract Shown = this.Contract.Contract;
				if (NewContract is null ||
					(!this.showingSavedCopy &&
					NewContract.ServerSignature?.Timestamp == Shown.ServerSignature?.Timestamp &&
					NewContract.State == Shown.State &&
					NewContract.Updated == Shown.Updated))
				{
					ServiceRef.LogService.LogDebug("RefreshContractAsync skipped (no changes)");
					return;
				}

				ObservableContract Previous = this.Contract;
				ObservableContract Wrapper = new ObservableContract(NewContract);
				ViewContractStep CurrentStep = Enum.Parse<ViewContractStep>(this.CurrentState);

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					this.SelectedRole = null;
					this.Contract = Wrapper;
					await this.Contract.InitializeAsync();
				});

				this.showingSavedCopy = false;
				Previous.Dispose();

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					this.PrepareDisplayableParameters();
					this.PrepareSignableRoles();
					await this.PreparePropertiesAsync();
					this.OnPropertyChanged(nameof(this.CanSign));
					this.OnPropertyChanged(nameof(this.CanShowSignBar));
				});

				_ = this.MarkViewedAsync(NewContract.ContractId);

				StepToRestore = CurrentStep;
				ServiceRef.LogService.LogDebug($"RefreshContractAsync completed for {this.Contract.ContractId}");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				// Only reopen the gate this refresh closed. If a state change was running, the page reopens it when done.
				if (PreviousStateChange)
					await this.SetCanStateChangeOnMainThreadAsync(true);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.IsRefreshing = false;

					// When the saved copy stays, its encrypted values won't arrive now.
					if (this.Contract is not null)
					{
						foreach (ObservableParameter Parameter in this.Contract.Parameters)
							Parameter.IsLoadingValue = false;
					}
				});
			}

			if (StepToRestore.HasValue)
			{
				try
				{
					await this.GoToStateAsync(StepToRestore.Value);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}
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
		public override Task<string> Title => Task.FromResult(this.ContractName);

		#endregion

		#region Markdown Link Handlers

		public Command XmppUriClicked { get; }
		public Command IotIdUriClicked { get; }
		public Command IotScUriClicked { get; }
		public Command NeuroFeatureUriClicked { get; }
		public Command IotDiscoUriClicked { get; }
		public Command EDalerUriClicked { get; }
		public Command HyperlinkClicked { get; }

		private Task ExecuteUriClicked(object? Parameter, UriScheme Scheme)
		{
			return this.RunContractActionAsync(async () =>
			{
				if (Parameter is string Uri)
					await App.OpenUrlAsync(Uri);
			});
		}

		private Task ExecuteHyperlinkClicked(object? Parameter)
		{
			return this.RunContractActionAsync(async () =>
			{
				if (Parameter is string Url)
					await App.OpenUrlAsync(Url);
			});
		}

		#endregion
	}
}
