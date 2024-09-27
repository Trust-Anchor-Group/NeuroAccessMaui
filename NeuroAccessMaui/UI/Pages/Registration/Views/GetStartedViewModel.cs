using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Tag;
using Waher.Content.Xml;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class GetStartedViewModel() : BaseRegistrationViewModel(RegistrationStep.GetStarted)
	{
		[RelayCommand]
		private void NewAccount()
		{
			GoToRegistrationStep(RegistrationStep.ValidatePhone);
		}

		[RelayCommand]
		private void ExistingAccount()
		{
			GoToRegistrationStep(RegistrationStep.ContactSupport);
		}


	}
}
