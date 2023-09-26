using CommunityToolkit.Maui;
using NeuroAccessMaui.Pages.Identity;
using NeuroAccessMaui.Pages.Registration;
using NeuroAccessMaui.Pages.Registration.Views;

namespace NeuroAccessMaui.Pages;

public static class PageAppExtension
{
	public static MauiAppBuilder RegisterPagesManager(this MauiAppBuilder Builder)
	{
		Builder.Services.AddTransient<AppShell>();
		Builder.Services.AddTransient<MainPage, MainViewModel>();

		// Registration pages & views
		Builder.Services.AddTransient<RegistrationPage, RegistrationViewModel>();
		Builder.Services.AddTransient<LoadingView, LoadingViewModel>();
		Builder.Services.AddTransient<ChoosePurposeView, ChoosePurposeViewModel>();
		Builder.Services.AddTransient<ValidatePhoneView, ValidatePhoneViewModel>();
		Builder.Services.AddTransient<ValidateEmailView, ValidateEmailViewModel>();
		Builder.Services.AddTransient<ChooseAccountView, ChooseAccountViewModel>();
		Builder.Services.AddTransient<DefinePinView, DefinePinViewModel>();

		// Identity pages & views
		Builder.Services.AddTransient<ViewIdentityPage, ViewIdentityViewModel>();


		return Builder;
	}
}
