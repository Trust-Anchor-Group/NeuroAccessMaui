using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class CreateAccountViewModel : BaseRegistrationViewModel
{
	public CreateAccountViewModel() : base(RegistrationStep.CreateAccount)
	{
	}

	public override async Task DoAssignProperties()
	{
		await base.DoAssignProperties();

		if (string.IsNullOrEmpty(ServiceRef.TagProfile.Account))
		{
			return;
		}

		LegalIdentity? LegalIdentity = ServiceRef.TagProfile.LegalIdentity;

		if (LegalIdentity is null)
		{
			this.CreateIdentityCommand.Execute(null);
		}
		else if (LegalIdentity.State == IdentityState.Created)
		{
			this.ValidateIdentityCommand.Execute(null);
		}
		else //!!! if (LegalIdentity.State == IdentityState.???)
		{
			//!!! We should not have any other state here. Assume the legal id is obsoleted. What we should do in that case?
		}
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName == nameof(this.IsBusy))
		{
			this.CreateAccountCommand.NotifyCanExecuteChanged();
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

	public bool IsAccountCreated => !string.IsNullOrEmpty(ServiceRef.TagProfile.Account);

	public bool HasAlternativeNames => this.AlternativeNames.Count > 0;

	public bool CanCreateAccount => !this.AccountIsNotValid && !this.IsBusy && (this.AccountText.Length > 0);

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

			(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
				IsIpAddress, HostName, PortNumber, this.AccountText, PasswordToUse, Constants.LanguageCodes.Default,
				ServiceRef.TagProfile.ApiKey!, ServiceRef.TagProfile.ApiSecret!, typeof(App).Assembly, OnConnected);

			if (Succeeded)
			{
				if (this.CreateIdentityCommand.CanExecute(null))
				{
					await this.CreateIdentityCommand.ExecuteAsync(null);
				}
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

	[RelayCommand]
	private async Task CreateIdentity()
	{
		try
		{
			RegisterIdentityModel IdentityModel = this.CreateRegisterModel();
			LegalIdentityAttachment[] Photos = { /* Photos are left empty */ };

			(bool Succeeded, LegalIdentity AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
				ServiceRef.XmppService.AddLegalIdentity(IdentityModel, Photos));

			if (Succeeded)
			{
				ServiceRef.TagProfile.SetLegalIdentity(AddedIdentity);

				if (this.ValidateIdentityCommand.CanExecute(null))
				{
					await this.ValidateIdentityCommand.ExecuteAsync(null);
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayException(ex);
		}
	}


	[RelayCommand]
	private async Task ValidateIdentity()
	{
		await Task.CompletedTask;

		if (true)
		{
			//this.GoToRegistrationStep(RegistrationStep.DefinePin)
		}
	}


	private RegisterIdentityModel CreateRegisterModel()
	{
		RegisterIdentityModel IdentityModel = new();
		string s;

		if (!string.IsNullOrWhiteSpace(s = ServiceRef.TagProfile?.PhoneNumber?.Trim() ?? string.Empty))
		{
			if (string.IsNullOrWhiteSpace(s) && (ServiceRef.TagProfile?.LegalIdentity is LegalIdentity LegalIdentity))
			{
				s = LegalIdentity[Constants.XmppProperties.Phone];
			}

			IdentityModel.PhoneNr = s;
		}

		if (!string.IsNullOrWhiteSpace(s = ServiceRef.TagProfile?.EMail?.Trim() ?? string.Empty))
		{
			if (string.IsNullOrWhiteSpace(s) && (ServiceRef.TagProfile?.LegalIdentity is LegalIdentity LegalIdentity))
			{
				s = LegalIdentity[Constants.XmppProperties.EMail];
			}

			IdentityModel.EMail = s;
		}

		if (!string.IsNullOrWhiteSpace(s = ServiceRef.TagProfile?.SelectedCountry?.Trim() ?? string.Empty))
		{
			if (string.IsNullOrWhiteSpace(s) && (ServiceRef.TagProfile?.LegalIdentity is LegalIdentity LegalIdentity))
			{
				s = LegalIdentity[Constants.XmppProperties.Country];
			}

			IdentityModel.EMail = s;
		}

		// Other fields are left empty
		IdentityModel.FirstName = "N/A";
		//!!! IdentityModel.MiddleNames = "N/A";
		IdentityModel.LastNames = "N/A";
		IdentityModel.PersonalNumber = "N/A";
		IdentityModel.Address = "N/A";
		IdentityModel.Address2 = "N/A";
		IdentityModel.ZipCode = "N/A";
		//!!! IdentityModel.Area = "N/A";
		IdentityModel.City = "N/A";
		//!!! IdentityModel.Region = "N/A";
		
		return IdentityModel;
	}
}
