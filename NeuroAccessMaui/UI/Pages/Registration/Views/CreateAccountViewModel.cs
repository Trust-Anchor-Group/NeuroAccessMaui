using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class CreateAccountViewModel : BaseRegistrationViewModel
{
	public CreateAccountViewModel() : base(RegistrationStep.CreateAccount)
	{
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
			}

			(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
				IsIpAddress, HostName, PortNumber, this.AccountText, PasswordToUse, Constants.LanguageCodes.Default,
				ServiceRef.TagProfile.ApiKey!, ServiceRef.TagProfile.ApiSecret!, typeof(App).Assembly, OnConnected);

			if (Succeeded)
			{
				ServiceRef.TagProfile.GoToStep(RegistrationStep.DefinePin);

				WeakReferenceMessenger.Default.Send(new RegistrationPageMessage(ServiceRef.TagProfile.Step));
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
}
