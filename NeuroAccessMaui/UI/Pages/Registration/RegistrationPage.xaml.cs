using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Registration;

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

		// We need to register this handlere before the LoadingView is initialised
		WeakReferenceMessenger.Default.Register<RegistrationPageMessage>(this, this.HandleRegistrationPageMessage);

		StateContainer.SetCurrentState(this.GridWithAnimation, "Loading");
	}

	~RegistrationPage()
	{
		WeakReferenceMessenger.Default.Unregister<RegistrationPageMessage>(this);
	}

	/// <inheritdoc/>
	protected override async Task OnAppearingAsync()
	{
		await base.OnAppearingAsync();

		WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, this.HandleKeyboardSizeMessage);
	}

	/// <inheritdoc/>
	protected override async Task OnDisappearingAsync()
	{
		WeakReferenceMessenger.Default.Unregister<KeyboardSizeMessage>(this);

		await base.OnDisappearingAsync();
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

	private async void HandleKeyboardSizeMessage(object Recipient, KeyboardSizeMessage Message)
	{
		await this.Dispatcher.DispatchAsync(() =>
		{
			double Bottom = 0;
			if (DeviceInfo.Platform == DevicePlatform.iOS)
			{
				Thickness SafeInsets = this.On<iOS>().SafeAreaInsets();
				Bottom = SafeInsets.Bottom;
			}

			Thickness Margin = new(0, 0, 0, Message.KeyboardSize - Bottom);
			this.TheMainGrid.Margin = Margin;
		});
	}
}
