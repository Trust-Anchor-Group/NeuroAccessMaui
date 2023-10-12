using CommunityToolkit.Maui.Layouts;
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
		RegistrationStep NewStep = Message.Step;

		if (NewStep == RegistrationStep.Complete)
		{
			await App.SetMainPageAsync();
			return;
		}

		string NewState = NewStep switch
		{
			RegistrationStep.RequestPurpose => "ChoosePurpose",
			RegistrationStep.ValidatePhone => "ValidatePhone",
			RegistrationStep.ValidateEmail => "ValidateEmail",
			RegistrationStep.ChooseProvider => "ChooseProvider",
			RegistrationStep.CreateAccount => "CreateAccount",
			//!!! RegistrationStep.RegisterIdentity => "RegisterIdentity",
			RegistrationStep.DefinePin => "DefinePin",
			_ => throw new NotImplementedException(),
		};

		await this.Dispatcher.DispatchAsync(async () =>
		{
			await StateContainer.ChangeStateWithAnimation(this.GridWithAnimation, NewState, CancellationToken.None);

			if (Recipient is RegistrationPage RegistrationPage)
			{
				RegistrationViewModel ViewModel = RegistrationPage.ViewModel<RegistrationViewModel>();
				await ViewModel.DoAssignProperties(NewStep);
			}
		});
	}
}
