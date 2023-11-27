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
	}

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
	//!!! not implemented yet. See XmmpService.TryConnectInner comment
	bool accountIsValid = true;

	/// <summary>
	/// Email
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
	private string accountText = string.Empty;

	public bool CanCreateAccount => this.AccountIsValid && !this.IsBusy &&
		(this.AccountText.Length > 0);

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

			(bool Succeeded, string? ErrorMessage) = await ServiceRef.XmppService.TryConnectAndCreateAccount(ServiceRef.TagProfile.Domain!,
				IsIpAddress, HostName, PortNumber, this.AccountText, PasswordToUse, Constants.LanguageCodes.Default,
				ServiceRef.TagProfile.ApiKey!, ServiceRef.TagProfile.ApiSecret!, typeof(App).Assembly, OnConnected);

			if (Succeeded)
			{
				ServiceRef.TagProfile.GoToStep(RegistrationStep.DefinePin);

				WeakReferenceMessenger.Default.Send(new RegistrationPageMessage(ServiceRef.TagProfile.Step));
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
}
