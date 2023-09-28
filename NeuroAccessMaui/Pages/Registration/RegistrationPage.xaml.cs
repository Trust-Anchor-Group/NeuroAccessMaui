using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.UI.Animations;
using NeuroAccessMaui.Services.UI.Extensions;

namespace NeuroAccessMaui.Pages.Registration;

public partial class RegistrationPage
{
	public RegistrationPage(RegistrationViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentPageModel = ViewModel;

		StateContainer.SetCurrentState(this.GridWithAnimation, "Loading");
	}

	[RelayCommand]
	async Task ChangeStateWithFadeAnimation()
	{
		string currentState = StateContainer.GetCurrentState(this.GridWithAnimation);
		currentState = (currentState is "Loaded") ? "Loading" : "Loaded";

		await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, currentState, CancellationToken.None);

		RegistrationViewModel ViewModel = this.ViewModel<RegistrationViewModel>();
		ViewModel.CurrentState = currentState;
	}

	private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		VisualStateManager.GoToState((VisualElement)sender, "Focused");

		RegistrationViewModel ViewModel = this.ViewModel<RegistrationViewModel>();
		await ViewModel.ChangeLanguageCommand.ExecuteAsync(null);

		//VisualStateManager.GoToState((VisualElement)sender, "Normal");
	}
}
