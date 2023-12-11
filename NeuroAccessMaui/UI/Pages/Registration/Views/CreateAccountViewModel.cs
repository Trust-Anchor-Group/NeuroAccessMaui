using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class CreateAccountViewModel : BaseRegistrationViewModel
	{
		public CreateAccountViewModel() : base(RegistrationStep.CreateAccount)
		{
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
			ServiceRef.XmppService.LegalIdentityChanged += this.XmppContracts_LegalIdentityChanged;
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
			ServiceRef.XmppService.LegalIdentityChanged -= this.XmppContracts_LegalIdentityChanged;

			await base.OnDispose();
		}

		/// <inheritdoc />
		public override async Task DoAssignProperties()
		{
			await base.DoAssignProperties();

			if (string.IsNullOrEmpty(ServiceRef.TagProfile.Account))
			{
				return;
			}

			this.OnPropertyChanged(nameof(this.IsAccountCreated));

			if (ServiceRef.TagProfile.LegalIdentity is LegalIdentity LegalIdentity)
			{
				if (LegalIdentity.State == IdentityState.Approved)
					GoToRegistrationStep(RegistrationStep.DefinePin);
				else //!!! if (LegalIdentity.State == IdentityState.???)
				{
					//!!! We should not have any other state here.
					//!!! Assume that might happen if the legal id is obsoleted.
					//!!! What should we do in that case?
				}
			}
		}

		private async Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			if (this.CreateIdentityCommand.CanExecute(null))
			{
				await this.CreateIdentityCommand.ExecuteAsync(null);
			}
		}

		private async Task XmppContracts_LegalIdentityChanged(object _, LegalIdentityEventArgs e)
		{
			ServiceRef.TagProfile.SetLegalIdentity(e.Identity);

			await this.DoAssignProperties();
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName == nameof(this.IsBusy))
			{
				this.CreateAccountCommand.NotifyCanExecuteChanged();
				this.CreateIdentityCommand.NotifyCanExecuteChanged();
			}
			else if (e.PropertyName == nameof(this.AccountText))
			{
				this.AccountIsNotValid = false;
				this.AlternativeNames = [];
			}
		}

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		bool accountIsNotValid;

		/// <summary>
		/// Account name
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private string accountText = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasAlternativeNames))]
		private List<string> alternativeNames = [];

		public static bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;

		public static bool IsAccountCreated => !string.IsNullOrEmpty(ServiceRef.TagProfile.Account);

		public static bool IsLegalIdentityCreated => ServiceRef.TagProfile.LegalIdentity is not null;

		public bool HasAlternativeNames => this.AlternativeNames.Count > 0;

		public bool CanCreateAccount => !this.IsBusy && !this.AccountIsNotValid && (this.AccountText.Length > 0);

		public bool CanCreateIdentity => !this.IsBusy && IsAccountCreated && !IsLegalIdentityCreated && IsXmppConnected;

		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private async Task CreateAccount()
		{
			this.IsBusy = true;

			try
			{
				string PasswordToUse = ServiceRef.CryptoService.CreateRandomPassword();

				(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(ServiceRef.TagProfile.Domain!);

				async Task OnConnected(XmppClient Client)
				{
					if (ServiceRef.TagProfile.NeedsUpdating())
					{
						await ServiceRef.XmppService.DiscoverServices(Client);
					}

					ServiceRef.TagProfile.SetAccount(this.AccountText, Client.PasswordHash, Client.PasswordHashMethod);

					this.OnPropertyChanged(nameof(this.IsAccountCreated));
				}

				string? ApiKey = ServiceRef.TagProfile.ApiKey;
				string? ApiSecret = ServiceRef.TagProfile.ApiSecret;

				if (string.IsNullOrEmpty(ApiKey) && string.IsNullOrEmpty(ApiSecret))
				{
					ApiKey = ServiceRef.TagProfile.InitialApiKey;
					ApiSecret = ServiceRef.TagProfile.InitialApiSecret;
				}

				(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
					IsIpAddress, HostName, PortNumber, this.AccountText, PasswordToUse, Constants.LanguageCodes.Default,
					ApiKey ?? string.Empty, ApiSecret ?? string.Empty, typeof(App).Assembly, OnConnected);

				if (Succeeded)
				{
					if (this.CreateIdentityCommand.CanExecute(null))
						await this.CreateIdentityCommand.ExecuteAsync(null);
				}
				else if (Alternatives is not null)
				{
					this.AccountIsNotValid = true;
					this.AlternativeNames = new(Alternatives);
				}
				else if (ErrorMessage is not null)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ErrorMessage,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		[RelayCommand]
		private void SelectName(object o)
		{
			if (o is string AccountText)
			{
				this.AccountText = AccountText;
			}
		}

		[RelayCommand(CanExecute = nameof(CanCreateIdentity))]
		private async Task CreateIdentity()
		{
			this.IsBusy = true;

			try
			{
				RegisterIdentityModel IdentityModel = CreateRegisterModel();
				LegalIdentityAttachment[] Photos = []; // Photos are left empty

				(bool Succeeded, LegalIdentity AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
					ServiceRef.XmppService.AddLegalIdentity(IdentityModel, Photos));

				if (Succeeded)
				{
					ServiceRef.TagProfile.SetLegalIdentity(AddedIdentity);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		[RelayCommand]
		private async Task ValidateIdentity()
		{
			await Task.CompletedTask;
		}

		private static RegisterIdentityModel CreateRegisterModel()
		{
			RegisterIdentityModel IdentityModel = new();
			string s;

			if (!string.IsNullOrWhiteSpace(s = ServiceRef.TagProfile?.PhoneNumber?.Trim() ?? string.Empty))
			{
				if (string.IsNullOrWhiteSpace(s) && (ServiceRef.TagProfile?.LegalIdentity is LegalIdentity LegalIdentity))
					s = LegalIdentity[Constants.XmppProperties.Phone];

				IdentityModel.PhoneNr = s;
			}

			if (!string.IsNullOrWhiteSpace(s = ServiceRef.TagProfile?.EMail?.Trim() ?? string.Empty))
			{
				if (string.IsNullOrWhiteSpace(s) && (ServiceRef.TagProfile?.LegalIdentity is LegalIdentity LegalIdentity))
					s = LegalIdentity[Constants.XmppProperties.EMail];

				IdentityModel.EMail = s;
			}

			if (!string.IsNullOrWhiteSpace(s = ServiceRef.TagProfile?.SelectedCountry?.Trim() ?? string.Empty))
			{
				if (string.IsNullOrWhiteSpace(s) && (ServiceRef.TagProfile?.LegalIdentity is LegalIdentity LegalIdentity))
					s = LegalIdentity[Constants.XmppProperties.Country];

				IdentityModel.Country = s;
			}

			return IdentityModel;
		}
	}
}
