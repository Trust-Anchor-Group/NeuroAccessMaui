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
	public partial class GetStartedViewModel() : BaseRegistrationViewModel(RegistrationStep.GetStarted)
	{
		[RelayCommand]
		private void NewAccount()
		{
			GoToRegistrationStep(RegistrationStep.NameEntry);
		}

		[RelayCommand]
		private void ExistingAccount() { }

		[RelayCommand]
		private async Task ScanQrCode()
		{
			GoToRegistrationStep(RegistrationStep.ChooseProvider);
		}
	}
}
