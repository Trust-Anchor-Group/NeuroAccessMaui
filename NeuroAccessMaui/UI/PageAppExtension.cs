using CommunityToolkit.Maui;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.UI.Pages.Identity.TransferIdentity;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature;
using NeuroAccessMaui.UI.Pages.Registration;
using NeuroAccessMaui.UI.Pages.Registration.Views;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroAccessMaui.UI.Popups;
using NeuroAccessMaui.UI.Popups.Pin;

namespace NeuroAccessMaui.UI
{
	public static class PageAppExtension
	{
		public static MauiAppBuilder RegisterPagesManager(this MauiAppBuilder Builder)
		{
			// Main pages
			Builder.Services.AddTransient<AppShell>();
			Builder.Services.AddTransient<MainPage, MainViewModel>();
			Builder.Services.AddTransient<SettingsPage, SettingsViewModel>();
			Builder.Services.AddTransient<ChangePinPage, ChangePinViewModel>();

			// Registration pages & views
			Builder.Services.AddTransient<RegistrationPage, RegistrationViewModel>();
			Builder.Services.AddTransient<LoadingView, LoadingViewModel>();
			Builder.Services.AddTransient<RequestPurposeView, RequestPurposeViewModel>();
			Builder.Services.AddTransient<ValidatePhoneView, ValidatePhoneViewModel>();
			Builder.Services.AddTransient<ValidateEmailView, ValidateEmailViewModel>();
			Builder.Services.AddTransient<ChooseProviderView, ChooseProviderViewModel>();
			Builder.Services.AddTransient<CreateAccountView, CreateAccountViewModel>();
			Builder.Services.AddTransient<DefinePinView, DefinePinViewModel>();

			// Identity pages & views
			Builder.Services.AddTransient<ViewIdentityPage, ViewIdentityViewModel>();
			Builder.Services.AddTransient<TransferIdentityPage, TransferIdentityViewModel>();

			// Petitions
			Builder.Services.AddTransient<PetitionIdentityPage, PetitionIdentityViewModel>();
			Builder.Services.AddTransient<PetitionSignaturePage, PetitionSignatureViewModel>();
			Builder.Services.AddTransient<PetitionPeerReviewPage, PetitionPeerReviewViewModel>();
			Builder.Services.AddTransient<PhotoView, EmptyViewModel>();
			Builder.Services.AddTransient<NameView, EmptyViewModel>();
			Builder.Services.AddTransient<PnrView, EmptyViewModel>();
			Builder.Services.AddTransient<NationalityView, EmptyViewModel>();
			Builder.Services.AddTransient<BirthDateView, EmptyViewModel>();
			Builder.Services.AddTransient<GenderView, EmptyViewModel>();
			Builder.Services.AddTransient<PersonalAddressInfoView, EmptyViewModel>();
			Builder.Services.AddTransient<OrganizationalInfoView, EmptyViewModel>();
			Builder.Services.AddTransient<ConsentView, EmptyViewModel>();
			Builder.Services.AddTransient<AuthenticateView, EmptyViewModel>();
			Builder.Services.AddTransient<ApprovedView, EmptyViewModel>();

			// Applications
			Builder.Services.AddTransient<ApplicationsPage, ApplicationsViewModel>();
			Builder.Services.AddTransient<ApplyIdPage, ApplyIdViewModel>();

			// Wallet
			Builder.Services.AddTransient<ServiceProvidersPage, ServiceProvidersViewModel>();

			// Popups
			Builder.Services.AddTransient<ViewImagePage, ViewImageViewModel>();
			Builder.Services.AddTransient<CheckPinPage>();

			// Controls

			return Builder;
		}
	}
}
