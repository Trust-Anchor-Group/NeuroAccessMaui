using CommunityToolkit.Maui;
using NeuroAccessMaui.Pages.Identity;
using NeuroAccessMaui.Pages.Main;
using NeuroAccessMaui.Pages.Main.Security;
using NeuroAccessMaui.Pages.Petitions;
using NeuroAccessMaui.Pages.Registration;
using NeuroAccessMaui.Pages.Registration.Views;
using NeuroAccessMaui.Popups;
using NeuroAccessMaui.Popups.Pin;

namespace NeuroAccessMaui.Pages;

public static class PageAppExtension
{
	public static MauiAppBuilder RegisterPagesManager(this MauiAppBuilder Builder)
	{
		// Main pages
		Builder.Services.AddTransient<AppShell>();
		Builder.Services.AddTransient<MainPage, MainViewModel>();
		Builder.Services.AddTransient<SecurityPage, SecurityViewModel>();

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
		Builder.Services.AddTransient<TransferIdentityPage, TransferIdentityViewModel>();

		// Petitions
		Builder.Services.AddTransient<PetitionIdentityPage, PetitionIdentityViewModel>();
		Builder.Services.AddTransient<PetitionSignaturePage, PetitionSignatureViewModel>();

		// Popups

		Builder.Services.AddTransient<ChangePinPage, ChangePinViewModel>();
		Builder.Services.AddTransient<ViewImagePage, ViewImageViewModel>();
		Builder.Services.AddTransient<CheckPinPage>();
		Builder.Services.AddTransient<VerifyCodePage>();

		return Builder;
	}
}
