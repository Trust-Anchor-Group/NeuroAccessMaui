using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Authentication;
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

		private readonly IAuthenticationService authenticationService = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();
		private readonly ViewContractNavigationArgs? args;

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
				await this.LoadContractAsync();
				await this.InitializeUIAsync();
				await this.GoToStateAsync(ViewContractStep.Overview);
				if (this.RefreshContractCommand.CanExecute(null))
				{
					// Ensure command execution (and its CanExecuteChanged events) happen on UI thread
					await MainThread.InvokeOnMainThreadAsync(async () => await this.RefreshContractCommand.ExecuteAsync(null));
				}
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
				string Xaml = await this.Contract.Contract.ToMauiXaml(this.Contract.Contract.DeviceLanguage());
				VerticalStackLayout Layout = new VerticalStackLayout().LoadFromXaml(Xaml);
				this.HumanReadableText = Layout;
			});
		}

		[RelayCommand]
		private async Task OpenServerSignatureAsync()
		{
			if (this.Contract?.Contract is { } ContractObj)
				await ServiceRef.UiService.GoToAsync(nameof(ServerSignaturePage), new ServerSignatureNavigationArgs(ContractObj), Services.UI.BackMethod.Pop);
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
			=> MainThread.BeginInvokeOnMainThread(() => this.OnPropertyChanged(nameof(this.CanSign)));

		private async Task OnContractSignedAsync(object? sender, ContractSignedEventArgs e)
		{
			if (e.ContractId != this.Contract?.ContractId || e.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id)
				return;

			Contract? SignedContract = e.Contract;

			while (!this.RefreshContractCommand.CanExecute(SignedContract))
				await Task.Delay(100);

			await MainThread.InvokeOnMainThreadAsync(async () => await this.RefreshContractCommand.ExecuteAsync(SignedContract));
		}

		private async Task OnContractUpdatedAsync(object? sender, ContractReferenceEventArgs e)
		{
			if (e.ContractId != this.Contract?.ContractId)
				return;

			while (!this.RefreshContractCommand.CanExecute(null))
				await Task.Delay(100);

			await MainThread.InvokeOnMainThreadAsync(async () => await this.RefreshContractCommand.ExecuteAsync(null));
		}

		#endregion

		#region Private Helpers

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
				this.GenerateQrCode(Constants.UriSchemes.CreateSmartContractUri(this.Contract!.ContractId));
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
					or DateTimeParameter or CalcParameter or ContractReferenceParameter)
				{
					this.DisplayableParameters.Add(Parameter);
				}
			}

			foreach (ObservableParameter P in this.DisplayableParameters)
			{
				P.Parameter.IsParameterValid(Vars, ServiceRef.XmppService.ContractsClient);
				ServiceRef.LogService.LogDebug($"Parameter '{P.Parameter.Name}' validation result: {P.IsValid}, Error: {P.Parameter.ErrorReason} - {P.ValidationText}");
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
		private async Task RefreshContractAsync(Contract? newContract)
		{
			if (this.Contract is null)
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!this.IsRefreshing)
					this.IsRefreshing = true;
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
				return;
			}
			ObservableContract Wrapper = new ObservableContract(newContract);
			ViewContractStep CurrentStep = Enum.Parse<ViewContractStep>(this.CurrentState);
			//await this.GoToStateAsync(ViewContractStep.Loading);

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
			});

			ContractReference Ref = await Database.FindFirstDeleteRest<ContractReference>(
	new FilterFieldEqualTo("ContractId", this.Contract.ContractId));

			if (Ref is not null)
			{

				await Ref.SetContract(newContract);
				await Database.Update(Ref);
			}

			await this.GoToStateAsync(CurrentStep);

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IsRefreshing = false;
			});
		}

		private async Task<bool> ConfirmAsync(string resourceKey, AuthenticationPurpose purpose)
		{
			if (!await AreYouSure(ServiceRef.Localizer[resourceKey]))
				return false;
			return await authenticationService.AuthenticateUserAsync(purpose, true);
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
