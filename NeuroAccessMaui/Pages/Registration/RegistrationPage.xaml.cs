using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Pages.Registration;

public partial class RegistrationPage : ContentPage
{
	private readonly RegistrationViewModel? viewModel;

	public RegistrationPage()
	{
		this.InitializeComponent();

		StateContainer.SetCurrentState(this.GridWithAnimation, "Loading");

		this.viewModel = ServiceHelper.GetService<RegistrationViewModel>();
		this.BindingContext = this.viewModel;
	}


	[RelayCommand]
	async Task ChangeStateWithFadeAnimation()
	{
		string currentState = StateContainer.GetCurrentState(this.GridWithAnimation);
		currentState = (currentState is "Loaded") ? "Loading" : "Loaded";

		await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, currentState, CancellationToken.None);

		if (this.viewModel is not null)
		{
			this.viewModel.CurrentState = currentState;
		}
	}
}
