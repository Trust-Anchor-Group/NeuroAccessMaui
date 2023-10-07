using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class ValidatePhoneViewModel : BaseRegistrationViewModel
{
	public ValidatePhoneViewModel()
	{
	}


	public bool CanSendCode => false;

	[RelayCommand(CanExecute = nameof(CanSendCode))]
	private void SendCode()
	{

	}
}
