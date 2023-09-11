using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.Pages.Registration;

public partial class RegistrationViewModel : ObservableObject
{
	[ObservableProperty]
	private string currentState = "Loading";

	public RegistrationViewModel()
	{

	}
}
