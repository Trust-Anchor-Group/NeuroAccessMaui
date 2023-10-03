using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.Pages.Registration;

public class RegistrationPageMessage(RegistrationStep Step)
{
	public RegistrationStep Step { get; } = Step;
}

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

	/// <inheritdoc/>
	protected override Task OnAppearingAsync()
	{
		WeakReferenceMessenger.Default.Register<RegistrationPageMessage>(this, this.HandleRegistrationPageMessage);
		return base.OnAppearingAsync();
	}

	/// <inheritdoc/>
	protected override Task OnDisappearingAsync()
	{
		WeakReferenceMessenger.Default.Unregister<RegistrationPageMessage>(this);
		return base.OnDisappearingAsync();
	}

	private async void HandleRegistrationPageMessage(object Recipient, RegistrationPageMessage Message)
	{
		switch (Message.Step)
		{
			case RegistrationStep.RequestPurpose:
				StateContainer.SetCurrentState(this.GridWithAnimation, "ChoosePurpose");
				break;
			case RegistrationStep.ValidatePhone:
				StateContainer.SetCurrentState(this.GridWithAnimation, "ValidatePhone");
				break;
			case RegistrationStep.ValidateEmail:
				StateContainer.SetCurrentState(this.GridWithAnimation, "ValidateEmail");
				break;
			case RegistrationStep.ChooseProvider:
				StateContainer.SetCurrentState(this.GridWithAnimation, "ChooseProvider");
				break;
			case RegistrationStep.CreateAccount:
				StateContainer.SetCurrentState(this.GridWithAnimation, "CreateAccount");
				break;
//			case RegistrationStep.RegisterIdentity:
//				StateContainer.SetCurrentState(this.GridWithAnimation, "RegisterIdentity");
//				break;
			case RegistrationStep.DefinePin:
				StateContainer.SetCurrentState(this.GridWithAnimation, "DefinePin");
				break;
			case RegistrationStep.Complete:
				await App.SetMainPageAsync();
				break;
		}
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
