using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Script;
using Microsoft.Maui.ApplicationModel; // For MainThread marshaling

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// View model for displaying and managing a contract in the "View Contract" page.
	/// </summary>
	public partial class ViewContractViewModel : QrXmppViewModel
	{
		#region Fields

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
			this.args = ServiceRef.UiService.PopLatestArgs<ViewContractNavigationArgs>();

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

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (!this.ValidateArgs())
				return;

			this.SubscribeToEvents();

			try
			{
				// Ensure args.Contract is loaded and displayed first
				await this.LoadContractAsync();
				await this.InitializeUIAsync();
				await this.GoToStateAsync(ViewContractStep.Overview);
				// If navigation provided a post-create completion, await it before enabling signing UI
				if (this.args?.PostCreateCompletion is not null)
				{
					this.IsAwaitingPostCreateCompletion = true;
					try
					{
						Contract? Completed = await this.args.PostCreateCompletion.Task.ConfigureAwait(false);
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
						this.IsAwaitingPostCreateCompletion = false;
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

		protected override async Task OnDispose()
		{
			this.UnsubscribeFromEvents();
			await base.OnDispose();
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

		public bool CanSign => this.HasSignableRoles && this.IsInSigningState && !this.AlreadySigned;

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

		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(ReadyToSign))]
		public async Task SignAsync()
		{
			if (this.Contract is null || this.SelectedRole is null)
				return;

			await MainThread.InvokeOnMainThreadAsync(() => this.SetIsBusy(true));
			try
			{
				Contract SignedContract = await ServiceRef.XmppService.SignContract(this.Contract.Contract, this.SelectedRole.Name, false);
				await this.GoToStateAsync(ViewContractStep.Overview);
				await this.RefreshContractAsync(SignedContract);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
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

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		public async Task BackAsync()
		{
			ViewContractStep Step = Enum.Parse<ViewContractStep>(this.CurrentState);
			if (Step == ViewContractStep.Overview || Step == ViewContractStep.Loading)
				await this.GoBack();
			else
				await this.GoToStateAsync(ViewContractStep.Overview);
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToParametersAsync() => this.GoToStepAsync(ViewContractStep.Parameters);

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task GoToRolesAsync() => this.GoToStepAsync(ViewContractStep.Roles);

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToSignAsync()
		{
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

			await this.GoToStepAsync(ViewContractStep.Review, Prepare: async () =>
			{

				await this.ValidateParametersAsync();
				VerticalStackLayout? HumanReadableText = await this.Contract.Contract.ToMaui(this.Contract.Contract.DeviceLanguage());
				this.HumanReadableText = HumanReadableText;
			});
		}

		[RelayCommand]
		private async Task OpenServerSignatureAsync()
		{
			if (this.Contract?.Contract is { } ContractObj)
				await ServiceRef.UiService.GoToAsync(nameof(ServerSignaturePage), new ServerSignatureNavigationArgs(ContractObj), Services.UI.BackMethod.Pop);
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

				if (string.IsNullOrEmpty(proposal))
					return;

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

			if (!await this.ConfirmAsync(AppResources.AreYouSureYouWantToObsoleteContract, AuthenticationPurpose.ObsoleteContract))
				return;

			await ServiceRef.XmppService.ObsoleteContract(this.Contract.ContractId);
			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
				ServiceRef.Localizer[nameof(AppResources.ContractHasBeenObsoleted)]);
		}

		[RelayCommand]
		private async Task DeleteContractAsync()
		{
			if (this.Contract is null)
				return;

			if (!await this.ConfirmAsync(AppResources.AreYouSureYouWantToDeleteContract, AuthenticationPurpose.DeleteContract))
				return;

			await ServiceRef.XmppService.DeleteContract(this.Contract.ContractId);
			this.Contract.Contract.State = ContractState.Deleted;
			await this.RefreshContractAsync(this.Contract.Contract);

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
				ServiceRef.Localizer[nameof(AppResources.ContractHasBeenDeleted)]);
		}

		[RelayCommand]
		private async Task ShowDetailsAsync()
		{
			if (this.Contract is null)
				return;

			byte[] Xml = Encoding.UTF8.GetBytes(this.Contract.Contract.ForMachines.OuterXml);
			HttpFileUploadEventArgs Slot = await ServiceRef.XmppService.RequestUploadSlotAsync(
				this.Contract.ContractId + ".xml", "text/xml; charset=utf-8", Xml.Length);

			if (Slot.Ok)
			{
				await Slot.PUT(Xml, "text/xml", (int)Constants.Timeouts.UploadFile.TotalMilliseconds);
				if (!await App.OpenUrlAsync(Slot.GetUrl, false))
					await this.CopyAsync(Slot.GetUrl);
			}
			else
			{
				await ServiceRef.UiService.DisplayException(Slot.StanzaError ?? new Exception(Slot.ErrorText));
			}
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
				await this.OpenQrPopup();
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

			while (!this.CanStateChange)
				await Task.Delay(100);

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await StateContainer.ChangeStateWithAnimation(this.StateObject, NewState);
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
			return this.args is not null && this.args.Contract is not null;
		}

		private async Task LoadContractAsync()
		{
			this.Contract = await ObservableContract.CreateAsync(this.args!.Contract!);
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
			await MainThread.InvokeOnMainThreadAsync(this.PreparePropertiesAsync);

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

			Variables Vars = new Variables();
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

		private async Task PreparePropertiesAsync()
		{
			if (this.Contract is null)
				return;

			foreach (ObservableRole? Role in this.Contract.Roles.Where(R => R.Parts.Any(P => P.IsMe)))
			{
				if (Role.Role.CanRevoke)
					this.CanObsoleteContract = this.Contract.ContractState is ContractState.Approved or ContractState.BeingSigned or ContractState.Signed;
			}

			if (this.args is not null)
			{
				try
				{
					bool Binding = await this.Contract.Contract.IsLegallyBinding(true, ServiceRef.XmppService.ContractsClient);
					MainThread.BeginInvokeOnMainThread(() =>
					{
						this.CanDeleteContract = !this.args.IsReadOnly && !Binding;
					});
				}
				catch (Exception Ex)
				{
					this.CanDeleteContract = false;
					ServiceRef.LogService.LogException(Ex);
				}
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

		private async Task DoRefreshAsync(Contract? newContract)
		{
			if (this.Contract is null)
				return;

			ServiceRef.LogService.LogDebug($"RefreshContractAsync start for {this.Contract.ContractId}");
			bool previousStateChange = this.CanStateChange;
			this.CanStateChange = false; // Gate state transitions during refresh

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
				newContract ??= await ServiceRef.XmppService.GetContract(this.Contract.ContractId);
			}
			catch (ForbiddenException)
			{
				if (await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.RefreshContract_Forbidden_Title)],
					ServiceRef.Localizer[nameof(AppResources.RefreshContract_Forbidden_Description)],
					ServiceRef.Localizer[nameof(AppResources.Yes)],
					ServiceRef.Localizer[nameof(AppResources.No)]))
				{
					if (!await ServiceRef.NetworkService.TryRequest(
						() => ServiceRef.XmppService.PetitionContract(
							this.Contract.ContractId,
							Guid.NewGuid().ToString(),
							ServiceRef.Localizer[nameof(AppResources.RequestToAccessContract)])))
					{
						MainThread.BeginInvokeOnMainThread(() =>
						{
							this.IsRefreshing = false;
						});
						return;
					}
				}
				;
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
			}
			catch
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsRefreshing = false;
				});
				return; // Ignore other exceptions
			}

			if (newContract is null || newContract.ServerSignature.Timestamp == this.Contract.Contract.ServerSignature.Timestamp)
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsRefreshing = false;
				});
				this.CanStateChange = previousStateChange;
				ServiceRef.LogService.LogDebug("RefreshContractAsync skipped (no changes)");
				return;
			}

			ObservableContract Wrapper = new ObservableContract(newContract);
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
				await Ref.SetContract(newContract);
				await Database.Update(Ref);
			}

			await this.GoToStateAsync(CurrentStep);
			this.CanStateChange = previousStateChange;
			ServiceRef.LogService.LogDebug($"RefreshContractAsync completed for {this.Contract.ContractId}");

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IsRefreshing = false;
			});
		}

		private async Task<bool> ConfirmAsync(string resourceKey, AuthenticationPurpose purpose)
		{
			if (!await AreYouSure(ServiceRef.Localizer[resourceKey]))
				return false;
			return await App.AuthenticateUserAsync(purpose, true);
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

		private async Task ExecuteUriClicked(object? parameter, UriScheme scheme)
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
