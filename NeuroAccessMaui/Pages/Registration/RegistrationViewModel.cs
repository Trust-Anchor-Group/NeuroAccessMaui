using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.Pages.Registration;

public partial class RegistrationViewModel : BaseViewModel
{
	[ObservableProperty]
	private string currentState = "Loading";

	public RegistrationViewModel()
	{

	}
}
