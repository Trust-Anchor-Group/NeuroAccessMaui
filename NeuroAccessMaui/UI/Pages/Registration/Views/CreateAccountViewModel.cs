using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class CreateAccountViewModel : BaseRegistrationViewModel
	{
		public CreateAccountViewModel()
			: base(RegistrationStep.CreateAccount)
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
				return;

			this.OnPropertyChanged(nameof(IsAccountCreated));

			if (ServiceRef.TagProfile.LegalIdentity is LegalIdentity LegalIdentity)
			{
				if (LegalIdentity.State == IdentityState.Approved)
				{
					if (Shell.Current.CurrentState.Location.OriginalString == Constants.Pages.RegistrationPage)
						GoToRegistrationStep(RegistrationStep.DefinePin);
				}
				else if (LegalIdentity.IsDiscarded())
				{
					await ServiceRef.TagProfile.ClearLegalIdentity();
					GoToRegistrationStep(RegistrationStep.ValidatePhone);
				}
			}
		}

		private async Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			if (NewState == XmppState.Connected && this.CreateIdentityCommand.CanExecute(null))
				await this.CreateIdentityCommand.ExecuteAsync(null);
		}

		private async Task XmppContracts_LegalIdentityChanged(object _, LegalIdentityEventArgs e)
		{
			await this.DoAssignProperties();
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsBusy):
					this.CreateAccountCommand.NotifyCanExecuteChanged();
					this.CreateIdentityCommand.NotifyCanExecuteChanged();
					break;

				case nameof(this.AccountText):
					this.AccountIsNotValid = false;
					this.AlternativeNames = [];
					break;
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

		/// <summary>
		/// List of alternative account names.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasAlternativeNames))]
		private List<string> alternativeNames = [];

		/// <summary>
		/// If App is connected to the XMPP network.
		/// </summary>
		public static bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;

		/// <summary>
		/// If App has an XMPP account defined.
		/// </summary>
		public static bool IsAccountCreated => !string.IsNullOrEmpty(ServiceRef.TagProfile.Account);

		/// <summary>
		/// If Legal ID has been created.
		/// </summary>
		public static bool IsLegalIdentityCreated
		{
			get
			{
				return ServiceRef.TagProfile.LegalIdentity is not null &&
					(ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved ||
					ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Created);
			}
		}

		/// <summary>
		/// If alternative names are available.
		/// </summary>
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
						await ServiceRef.XmppService.DiscoverServices(Client);

					ServiceRef.TagProfile.SetAccount(this.AccountText, Client.PasswordHash, Client.PasswordHashMethod);

					this.OnPropertyChanged(nameof(IsAccountCreated));
				}

				(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
					IsIpAddress, HostName, PortNumber, this.AccountText, PasswordToUse, Constants.LanguageCodes.Default,
					ServiceRef.TagProfile.ApiKey ?? string.Empty, ServiceRef.TagProfile.ApiSecret ?? string.Empty,
					typeof(App).Assembly, OnConnected);

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
		private void SelectName(object Control)
		{
			if (Control is string AccountText)
				this.AccountText = AccountText;
		}

		[RelayCommand(CanExecute = nameof(CanCreateIdentity))]
		private async Task CreateIdentity()
		{
			this.IsBusy = true;

			try
			{
				RegisterIdentityModel IdentityModel = CreateRegisterModel();
				LegalIdentityAttachment[] Photos = []; // Photos are left empty

				(bool Succeeded, LegalIdentity? AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
					ServiceRef.XmppService.AddLegalIdentity(IdentityModel, true, Photos));

				if (Succeeded && AddedIdentity is not null)
					await ServiceRef.TagProfile.SetLegalIdentity(AddedIdentity, true);
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
		private static async Task ValidateIdentity()
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

				IdentityModel.CountryCode = s;
			}

			return IdentityModel;
		}
	}
}
