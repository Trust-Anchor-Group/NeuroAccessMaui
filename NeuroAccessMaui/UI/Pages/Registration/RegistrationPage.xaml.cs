using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.Pages.Registration;

public partial class RegistrationPage
{
	public RegistrationPage(RegistrationViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentPageModel = ViewModel;

		ViewModel.SetPagesContainer([
			this.LoadingView,
			this.ChoosePurposeView,
			this.ValidatePhoneView,
			this.ValidateEmailView,
			this.ChooseProviderView,
			this.CreateAccountView,
			this.DefinePinView,
		]);

		StateContainer.SetCurrentState(this.GridWithAnimation, "Loading");
	}

	[RelayCommand]
	async Task ChangeStateWithFadeAnimation()
	{
		/*
		string currentState = StateContainer.GetCurrentState(this.GridWithAnimation);
		currentState = (currentState is "Loaded") ? "Loading" : "Loaded";

		await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, currentState, CancellationToken.None);

		RegistrationViewModel ViewModel = this.ViewModel<RegistrationViewModel>();
		ViewModel.CurrentState = currentState;
		*/
	}
}
