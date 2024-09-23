using System.ComponentModel;
using System.Runtime.CompilerServices;
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
			this.ShowEntry = false;
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

			while (!this.IsAccountCreated && !this.ShowEntry)
			{

				if (!this.HasAlternativeNames)
				{
					if (this.hasGeneratedUsername)
					{
						this.ShowEntry = true;
						return;
					}

					string generatedUsername = ServiceRef.TagProfile.GenerateUsername();
					if (string.IsNullOrEmpty(generatedUsername))
					{
						// Generate a new GUID
						Guid guid = Guid.NewGuid();

						// Convert the GUID to a string without hyphens
						generatedUsername = guid.ToString("N");
					}

					this.AccountText = generatedUsername;
					this.hasGeneratedUsername = true;

				}
				else
				{
					Random rand = new();
					// Assign the randomly selected name to AccountText
					this.AccountText = this.AlternativeNames[rand.Next(0, this.AlternativeNames.Count)];
				}

				if (this.CreateAccountCommand.CanExecute(null))
					await this.CreateAccountCommand.ExecuteAsync(null);
				else
				{
					this.ShowEntry = true;
					return;
				}
			}

			if (this.CreateIdentityCommand.CanExecute(null))
				await this.CreateIdentityCommand.ExecuteAsync(null);

			await this.CheckAndHandleIdentityApplicationAsync();
		}

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			if (NewState == XmppState.Connected)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await this.DoAssignProperties();
				});
			}

			return Task.CompletedTask;
		}

		private async Task XmppContracts_LegalIdentityChanged(object _, LegalIdentityEventArgs e)
		{
			await this.CheckAndHandleIdentityApplicationAsync();
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
					this.AccountText = this.AccountText.Trim();
					this.AccountIsNotValid = false;
					this.AlternativeNames = [];
					break;
			}
		}

		/// <summary>
		/// If the manual account entry should be shown.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(this.ShowLoading))]
		private bool showEntry;

		/// <summary>
		/// If the loading indicator should be shown.
		/// </summary>
		public bool ShowLoading => !this.ShowEntry;

		/// <summary>
		/// If we already generated a username.
		/// </summary>
		private bool hasGeneratedUsername = false;

		/// <summary>
		/// If the account is not valid.
		/// </summary>
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
		public bool IsXmppConnected => ServiceRef.XmppService.State == XmppState.Connected;

		/// <summary>
		/// If App has an XMPP account defined.
		/// </summary>
		public bool IsAccountCreated => !string.IsNullOrEmpty(ServiceRef.TagProfile.Account);

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

		/// <summary>
		/// If we can create an account.
		/// </summary>
		public bool CanCreateAccount => !this.IsBusy && !this.AccountIsNotValid && (this.AccountText.Length > 0) && !this.IsAccountCreated;

		/// <summary>
		/// If we can create an identity.
		/// </summary>
		public bool CanCreateIdentity => this.IsAccountCreated && !IsLegalIdentityCreated && this.IsXmppConnected;

		/// <summary>
		/// Create an account
		/// If successful, it will also create an identity.
		/// Otherwise, it will return
		/// </summary>
		/// <returns></returns>
		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private async Task CreateAccount()
		{
			this.IsBusy = true;

			bool success = await this.TryCreateAccount();

			if (success)
			{
				if (this.CreateIdentityCommand.CanExecute(null))
					await this.CreateIdentityCommand.ExecuteAsync(null);

				await this.CheckAndHandleIdentityApplicationAsync();
			}

			this.IsBusy = false;
		}

		/// <summary>
		/// Select a name from the list of alternative names.
		/// </summary>
		[RelayCommand]
		private void SelectName(object Control)
		{
			if (Control is string AccountText)
				this.AccountText = AccountText;
		}

		/// <summary>
		/// Try to create an identity.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanCreateIdentity))]
		private async Task CreateIdentity()
		{
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
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand]
		private static async Task ValidateIdentity()
		{
			await Task.CompletedTask;
		}


		/// <summary>
		/// Try to create an account.
		/// </summary>
		/// <returns>If operation was successful or not</returns>
		private async Task<bool> TryCreateAccount()
		{

			try
			{
				string PasswordToUse = ServiceRef.CryptoService.CreateRandomPassword();

				(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(ServiceRef.TagProfile.Domain!);

				async Task OnConnected(XmppClient Client)
				{
					if (ServiceRef.TagProfile.NeedsUpdating())
						await ServiceRef.XmppService.DiscoverServices(Client);

					ServiceRef.TagProfile.SetAccount(this.AccountText, Client.PasswordHash, Client.PasswordHashMethod);

					this.OnPropertyChanged(nameof(this.IsAccountCreated));
				}

				(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
					IsIpAddress, HostName, PortNumber, this.AccountText, PasswordToUse, Constants.LanguageCodes.Default,
					ServiceRef.TagProfile.ApiKey ?? string.Empty, ServiceRef.TagProfile.ApiSecret ?? string.Empty,
					typeof(App).Assembly, OnConnected);

				if (Succeeded)
					return true;

				if (Alternatives is not null)
				{
					this.AccountIsNotValid = true;
					this.AlternativeNames = new(Alternatives);
				}
				else if (ErrorMessage is not null)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ErrorMessage,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}

			return false;
		}

		private async Task CheckAndHandleIdentityApplicationAsync()
		{
			this.IsBusy = true;
			if (ServiceRef.TagProfile.LegalIdentity is LegalIdentity LegalIdentity)
			{
				if (LegalIdentity.State == IdentityState.Approved)
				{
					if (Shell.Current.CurrentState.Location.OriginalString == Constants.Pages.RegistrationPage)
						GoToRegistrationStep(RegistrationStep.DefinePassword);
				}
				else if (LegalIdentity.IsDiscarded())
				{
					await ServiceRef.TagProfile.ClearLegalIdentity();
					/// TODO: Show error message
					GoToRegistrationStep(RegistrationStep.ValidatePhone);
				}
			}
			this.IsBusy = true;
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
