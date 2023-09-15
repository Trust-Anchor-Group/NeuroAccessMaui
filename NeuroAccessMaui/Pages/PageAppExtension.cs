using CommunityToolkit.Maui;

namespace NeuroAccessMaui.Pages;

public static class PageAppExtension
{
	public static MauiAppBuilder RegisterPagesManager(this MauiAppBuilder Builder)
	{
		Builder.Services.AddTransient<MainPage>();
		Builder.Services.AddTransient<MainViewModel>();

		Builder.Services.AddTransient<Registration.RegistrationPage>();
		Builder.Services.AddTransient<Registration.RegistrationViewModel>();

		Builder.Services.AddTransient<Registration.Views.ChoosePurposeView>();
		Builder.Services.AddTransient<Registration.Views.ChoosePurposeViewModel>();
		Builder.Services.AddTransient<Registration.Views.ValidatePhoneView>();
		Builder.Services.AddTransient<Registration.Views.ValidatePhoneViewModel>();
		Builder.Services.AddTransient<Registration.Views.ValidateEmailView>();
		Builder.Services.AddTransient<Registration.Views.ValidateEmailViewModel>();
		Builder.Services.AddTransient<Registration.Views.ChooseAccountView>();
		Builder.Services.AddTransient<Registration.Views.ChooseAccountViewModel>();
		Builder.Services.AddTransient<Registration.Views.DefinePinView>();
		Builder.Services.AddTransient<Registration.Views.DefinePinViewModel>();

		return Builder;
	}
}
