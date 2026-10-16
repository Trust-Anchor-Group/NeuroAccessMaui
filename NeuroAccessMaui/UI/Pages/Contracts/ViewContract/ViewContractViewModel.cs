using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // For MainThread marshaling
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
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

		private static readonly TimeSpan stateChangeTimeout = TimeSpan.FromSeconds(10);

		private readonly ViewContractNavigationArgs? args;

		// Refresh coalescing state
		private readonly object refreshLock = new();
		private bool refreshInProgress;
		private bool refreshQueued;
		private Contract? pendingContractForRefresh;
		private bool initialized;

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

		#endregion

		#region Signing

		public ObservableCollection<ObservableRole> SignableRoles { get; } = new();

		private bool HasSignableRoles => this.SignableRoles.Count > 0;
		private bool AlreadySigned => this.Contract?.Roles.Any(r => r.Parts.Any(p => p.IsMe && p.HasSigned)) == true;

		/// <summary>
		/// If signing is offered. Follows <see cref="ContractActionRules.CanSign"/>, and the user must not have signed already.
		/// </summary>
		public bool CanSign =>
			this.HasSignableRoles &&
			!this.AlreadySigned &&
			this.Contract is not null &&
			ContractActionRules.CanSign(this.Contract.Contract, ServiceRef.TagProfile.LegalIdentity?.Id, DateTime.UtcNow);

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
		/// Signs the contract in the selected role, after the user confirms with PIN or biometrics.
		/// </summary>
		/// <returns>A task that completes when signing has finished or was cancelled.</returns>
		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(ReadyToSign))]
		public Task SignAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null || this.SelectedRole is null)
					return;

				if (!await ServiceRef.AuthenticationService.AuthenticateUserAsync(AuthenticationPurpose.SignContract, true))
					return;

				await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(true));
				try
				{
					Contract SignedContract = await ServiceRef.XmppService.SignContract(this.Contract.Contract, this.SelectedRole.Name, false);
					await this.GoToStateAsync(ViewContractStep.Overview);
					await this.RefreshContractAsync(SignedContract);
				}
				finally
				{
					await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(false));
				}
			});
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
				else
					await this.GoToStateAsync(ViewContractStep.Overview);
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

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToSignAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				await this.GoToStepAsync(ViewContractStep.Sign);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (!string.IsNullOrEmpty(this.ProposalRole))
						this.SelectedRole = this.SignableRoles.FirstOrDefault(r => r.Name == this.ProposalRole);
					else if (this.SignableRoles.Count == 1)
						this.SelectedRole = this.SignableRoles[0];
				});
			});
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToReviewAsync()
		{
			return this.RunContractActionAsync(async () =>
			{
				if (this.Contract is null)
					return;

				await this.GoToStepAsync(ViewContractStep.Review, Prepare: async () =>
				{
					await this.ValidateParametersAsync();
					VerticalStackLayout? HumanReadableText = await this.Contract.Contract.ToMaui(this.Contract.Contract.DeviceLanguage());
					this.HumanReadableText = HumanReadableText;
				});
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

		/// <summary>
		/// Loads the contract from the navigation arguments, the saved copy or the server.
		/// </summary>
		/// <returns>true if the contract was loaded. When false, the user has been told why and the page has already gone back.</returns>
		private async Task<bool> LoadContractAsync()
		{
			if (this.args!.ContractRef is null)
			{
				this.Contract = await ObservableContract.CreateAsync(this.args.Contract!);
				return true;
			}

			if (this.args.ContractRef.ContractId is not CaseInsensitiveString ContractId || CaseInsensitiveString.IsNullOrEmpty(ContractId))
			{
				await this.GoBack();
				return false;
			}

			Contract? Contract;
			try
			{
				Contract = await this.args.ContractRef.GetContract();
				Contract ??= await ServiceRef.XmppService.GetContract(ContractId);
			}
			catch (ItemNotFoundException)
			{
				if (await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeFound)],
					ServiceRef.Localizer[nameof(AppResources.Yes)],
					ServiceRef.Localizer[nameof(AppResources.No)]))
				{
					await Database.FindDelete<ContractReference>(new FilterFieldEqualTo("ContractId", ContractId));
				}

				await this.GoBack();
				return false;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ContractErrorMessages.GetMessage(Ex),
					ServiceRef.Localizer[nameof(AppResources.Ok)]);

				await this.GoBack();
				return false;
			}

			this.Contract = await ObservableContract.CreateAsync(Contract);
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
					if (Role.Parts.Any(p => p.IsMe))
						this.SignableRoles.Add(Role);
			}
		}

		/// <summary>
		/// Decides which contract actions to offer, using <see cref="ContractActionRules"/>.
		/// When something can't be checked, the action is not offered.
		/// </summary>
		private async Task PreparePropertiesAsync()
		{
			try
			{
				if (this.Contract is null)
					return;

				Contract Current = this.Contract.Contract;
				bool IsCreator = false;
				bool BindingNow = true;
				bool BindingIgnoringTime = true;

				try
				{
					IsCreator = await ServiceRef.XmppService.IsContractCreatorAsync(Current.ContractId);
					BindingNow = await Current.IsLegallyBinding(true, ServiceRef.XmppService.ContractsClient);
					BindingIgnoringTime = BindingNow || await Current.IsLegallyBinding(false, ServiceRef.XmppService.ContractsClient);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				bool ReadOnly = this.args?.IsReadOnly ?? false;
				DateTime UtcNow = DateTime.UtcNow;
				bool CanDelete = !ReadOnly && ContractActionRules.CanDelete(Current, IsCreator, BindingIgnoringTime, UtcNow);
				bool CanWithdraw = !ReadOnly && ContractActionRules.CanWithdraw(Current, IsCreator, BindingNow,
					ServiceRef.TagProfile.LegalIdentity?.Id, ServiceRef.XmppService.BareJid);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (!ReferenceEquals(this.Contract?.Contract, Current))
						return; // A newer version of the contract is shown, and decides for itself.

					this.CanDeleteContract = CanDelete;
					this.CanWithdrawContract = CanWithdraw;
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

				try
				{
					NewContract ??= await ServiceRef.XmppService.GetContract(this.Contract.ContractId);
				}
				catch (ForbiddenException)
				{
					if (await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.RefreshContract_Forbidden_Title)],
						ServiceRef.Localizer[nameof(AppResources.RefreshContract_Forbidden_Description)],
						ServiceRef.Localizer[nameof(AppResources.Yes)],
						ServiceRef.Localizer[nameof(AppResources.No)]))
					{
						await ServiceRef.NetworkService.TryRequest(
							() => ServiceRef.XmppService.PetitionContract(
								this.Contract.ContractId,
								Guid.NewGuid().ToString(),
								ServiceRef.Localizer[nameof(AppResources.RequestToAccessContract)]));
					}

					return;
				}
				catch (ItemNotFoundException)
				{
					if (await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeFound)],
						ServiceRef.Localizer[nameof(AppResources.Yes)],
						ServiceRef.Localizer[nameof(AppResources.No)]))
					{
						await Database.FindDelete<ContractReference>(new FilterFieldEqualTo("ContractId", this.Contract.ContractId));
					}

					return;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					return; // Offline or similar: keep showing what we have.
				}

				Contract Shown = this.Contract.Contract;
				if (NewContract is null ||
					(NewContract.ServerSignature?.Timestamp == Shown.ServerSignature?.Timestamp &&
					NewContract.State == Shown.State &&
					NewContract.Updated == Shown.Updated))
				{
					ServiceRef.LogService.LogDebug("RefreshContractAsync skipped (no changes)");
					return;
				}

				ObservableContract Wrapper = new ObservableContract(NewContract);
				ViewContractStep CurrentStep = Enum.Parse<ViewContractStep>(this.CurrentState);

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					this.SelectedRole = null;
					this.Contract = Wrapper;
					await this.Contract.InitializeAsync();
				});

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					this.PrepareDisplayableParameters();
					this.PrepareSignableRoles();
					await this.PreparePropertiesAsync();
					this.OnPropertyChanged(nameof(this.CanSign));
					this.OnPropertyChanged(nameof(this.CanShowSignBar));
				});

				ContractReference Ref = await Database.FindFirstDeleteRest<ContractReference>(
					new FilterFieldEqualTo("ContractId", this.Contract.ContractId));

				if (Ref is not null)
				{
					await Ref.SetContract(NewContract);
					await Database.Update(Ref);
				}

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

				await MainThread.InvokeOnMainThreadAsync(() => this.IsRefreshing = false);
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
