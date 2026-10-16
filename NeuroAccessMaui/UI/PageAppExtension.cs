using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Services;
using NeuroAccessMaui.Services.AppPermissions;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Cache.AttachmentCache;
using NeuroAccessMaui.Services.Cache.Invalidation;
using NeuroAccessMaui.Services.Cache.InternetCache;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Intents;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Actions;
using NeuroAccessMaui.Services.Network;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Push;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Theme;
using NeuroAccessMaui.Services.ThingRegistries;
using NeuroAccessMaui.Services.TravelDocuments;
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Links;
using NeuroAccessMaui.Services.Xml;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.InviteParts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Identity.TransferIdentity;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Kyc;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Main.Calculator;
using NeuroAccessMaui.UI.Pages.Main.ChangePassword;
using NeuroAccessMaui.UI.Pages.Main.Duration;
using NeuroAccessMaui.UI.Pages.Main.QR;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Pages.Main.XmppForm;
using NeuroAccessMaui.UI.Pages.Main.NfcTester;
using NeuroAccessMaui.UI.Pages.Notifications;
using NeuroAccessMaui.UI.Pages.Onboarding;
using NeuroAccessMaui.UI.Pages.Onboarding.Views;
using NeuroAccessMaui.UI.Pages.Onboarding.ViewModels;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionContract;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature;
using NeuroAccessMaui.UI.Pages.Signatures.ClientSignature;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
using NeuroAccessMaui.UI.Pages.Startup;  // LoadingPage
using NeuroAccessMaui.UI.Pages.Things.CanControl;
using NeuroAccessMaui.UI.Pages.Things.CanRead;
using NeuroAccessMaui.UI.Pages.Things.IsFriend;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Things.ReadSensor;
using NeuroAccessMaui.UI.Pages.Things.ViewClaimThing;
using NeuroAccessMaui.UI.Pages.Things.ViewThing;
using NeuroAccessMaui.UI.Pages.Utility.Images;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.AccountEvent;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.EDalerReceived;
using NeuroAccessMaui.UI.Pages.Wallet.IssueEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport;
using NeuroAccessMaui.UI.Pages.Wallet.MachineVariables;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.UI.Pages.Wallet.Payment;
using NeuroAccessMaui.UI.Pages.Wallet.PaymentAcceptance;
using NeuroAccessMaui.UI.Pages.Wallet.PendingPayment;
using NeuroAccessMaui.UI.Pages.Wallet.RequestPayment;
using NeuroAccessMaui.UI.Pages.Wallet.SellEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.SendPayment;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using NeuroAccessMaui.UI.Pages.Wallet.TokenEvents;
using NeuroAccessMaui.UI.Popups.Password;
using NeuroAccessMaui.UI.Popups.Photos.Image;
using NeuroAccessMaui.UI.Popups.Settings;
using NeuroAccessMaui.UI.Popups.Tokens.AddTextNote;
using NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription;
using NeuroAccessMaui.UI.Popups.Xmpp.ReportOrBlock;
using NeuroAccessMaui.UI.Popups.Xmpp.ReportType;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscriptionRequest;
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;
using NeuroAccessMaui.UI.Popups.OnboardingHelp;
using NeuroAccessMaui.UI.Pages.Utility;
using NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout;
#if ANDROID
using NeuroAccessMaui.AndroidPlatform.Nfc;
#elif IOS
using NeuroAccessMaui.Platforms.iOS.Nfc;
#endif

namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Extension methods used during application startup for registering services and pages.
	/// </summary>
	public static class PageAppExtension
	{
		/// <summary>
		/// Registers core application services and infrastructure singletons.
		/// </summary>
		/// <param name="Builder">MAUI application builder.</param>
		/// <returns>The same builder instance for chaining.</returns>
		public static MauiAppBuilder RegisterTypes(this MauiAppBuilder Builder)
		{
			// Services are registered with their implementation types instead of being created through the Waher type inventory,
			// so resolving them never depends on the inventory, which is built in the background during startup. Resolve them by
			// interface: HybridServiceProvider maps a request for an implementation type to the registered singleton.
			Builder.Services.AddSingleton<ITagProfile, TagProfile>();
			Builder.Services.AddSingleton<ILogService, LogService>();
			Builder.Services.AddSingleton<IUiService, UiService>();
			Builder.Services.AddSingleton<ICryptoService, CryptoService>();
			Builder.Services.AddSingleton<INetworkService, NetworkService>();
			Builder.Services.AddSingleton<IStorageService, StorageService>();
			Builder.Services.AddSingleton<ISettingsService, SettingsService>();
			Builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
			Builder.Services.AddSingleton<IXmppService, XmppService>();
			Builder.Services.AddSingleton<IPushTransport, FirebasePushTransport>();
			Builder.Services.AddSingleton<IPushTokenRegistrar, PushTokenRegistrar>();
			Builder.Services.AddSingleton<INotificationRenderer>((_) =>
			{
#if ANDROID
				return new AndroidNotificationRenderer();
#elif IOS || MACCATALYST
				return new IosNotificationRenderer();
#else
				return new DefaultNotificationRenderer();
#endif
			});
			Builder.Services.AddSingleton<INotificationFilterRegistry, NotificationFilterRegistry>();
			Builder.Services.AddSingleton<INotificationIntentRouter, NotificationIntentRouter>();
			Builder.Services.AddSingleton<INotificationServiceV2, NotificationServiceV2>();
			Builder.Services.AddSingleton<IPushNotificationService, PushNotificationService>();
			Builder.Services.AddSingleton<IAttachmentCacheService, AttachmentCacheService>();
			Builder.Services.AddSingleton<IInternetCacheService, InternetCacheService>();
			Builder.Services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();
			Builder.Services.AddSingleton<IContractStore, ContractStore>();
			Builder.Services.AddSingleton<IContractSyncService, ContractSyncService>();
			Builder.Services.AddSingleton<IContractOrchestratorService, ContractOrchestratorService>();
			Builder.Services.AddSingleton<IThingRegistryOrchestratorService, ThingRegistryOrchestratorService>();
			Builder.Services.AddSingleton<INeuroWalletOrchestratorService, NeuroWalletOrchestratorService>();
			Builder.Services.AddSingleton<IIdentityApplicationGateService, IdentityApplicationGateService>();
			Builder.Services.AddSingleton<IKycService, KycService>();
			Builder.Services.AddSingleton<KycEvidenceValidationService>();
			Builder.Services.AddSingleton<IKycPreviewPromotionService, KycPreviewPromotionService>();
			Builder.Services.AddSingleton<IKycPageAction, TravelDocumentNfcAction>();
			Builder.Services.AddSingleton<KycActionRegistry>();
			Builder.Services.AddSingleton<ITravelDocumentEvidenceService, TravelDocumentEvidenceService>();
			Builder.Services.AddSingleton<ITravelDocumentReadoutService, TravelDocumentReadoutService>();
			Builder.Services.AddSingleton<INfcService, NfcService>();
			Builder.Services.AddSingleton<INfcIsoDepSessionService>((_) =>
			{
#if ANDROID
				return new AndroidNfcIsoDepSessionService();
#elif IOS
				return new IosNfcIsoDepSessionService();
#else
				return new DefaultNfcIsoDepSessionService();
#endif
			});
			Builder.Services.AddSingleton<INotificationService, NotificationService>();
			Builder.Services.AddSingleton<IIntentService, IntentService>();
			Builder.Services.AddSingleton<IXmlSchemaValidationService, XmlSchemaValidationService>();
			Builder.Services.AddSingleton<IThemeService, ThemeService>();
			Builder.Services.AddSingleton<INavigationService, NavigationService>();
			Builder.Services.AddSingleton<IPermissionService, PermissionService>();
			Builder.Services.AddSingleton<ILinkService, LinkService>();
			Builder.Services.AddSingleton<IQrService, QrService>();
			Builder.Services.AddSingleton<IPopupService, PopupService>();
			Builder.Services.AddSingleton<IToastService, ToastService>();
			Builder.Services.AddSingleton<IMotionSettings, MotionSettings>();
			Builder.Services.AddSingleton<IAnimationRegistry>((_) =>
			{
				AnimationRegistry Registry = new AnimationRegistry();
				AnimationCatalog.RegisterDefaults(Registry);
				return Registry;
			});
			Builder.Services.AddSingleton<IAnimationContextProvider, AnimationContextProvider>();
			Builder.Services.AddSingleton<IAnimationCoordinator, AnimationCoordinator>();

			return Builder;
		}

		/// <summary>
		/// Registers all MAUI pages and their corresponding view models in the dependency injection container.
		/// </summary>
		/// <param name="Builder">MAUI application builder.</param>
		/// <returns>The same builder instance for chaining.</returns>
		public static MauiAppBuilder RegisterPages(this MauiAppBuilder Builder)
		{
			// Applications
			Builder.Services.AddTransient<ApplicationsPage, ApplicationsViewModel>();
			Builder.Services.AddTransient<KycApplicationStatusPage, KycApplicationStatusViewModel>();
			Builder.Services.AddTransient<KycProcessPage, KycProcessViewModel>();
			Builder.Services.AddTransient<KycDocumentMrzScannerPage>();
			Builder.Services.AddTransient<KycProfilePhotoCameraPage>();
			Builder.Services.AddTransient<KycTravelDocumentPage, KycTravelDocumentViewModel>();

			// Contacts
			Builder.Services.AddTransient<ChatPage, ChatViewModel>();
			Builder.Services.AddTransient<MyContactsPage, ContactListViewModel>();

			// Contracts
			Builder.Services.AddTransient<MyContractsPage, MyContractsViewModel>();
			Builder.Services.AddTransient<NewContractPage, NewContractViewModel>();
			Builder.Services.AddTransient<ViewContractPage, ViewContractViewModel>();

			// Identity
			Builder.Services.AddTransient<TransferIdentityPage, TransferIdentityViewModel>();
			Builder.Services.AddTransient<ViewIdentityPage, ViewIdentityViewModel>();

			// Main
			Builder.Services.AddSingleton<CustomShell>();
			Builder.Services.AddTransient<CalculatorPage, CalculatorViewModel>();
			Builder.Services.AddTransient<ChangePasswordPage, ChangePasswordViewModel>();
			Builder.Services.AddTransient<DurationPage, DurationViewModel>();
			Builder.Services.AddTransient<HomePage, HomeViewModel>();
			Builder.Services.AddTransient<MainPage, MainViewModel>();
			Builder.Services.AddTransient<ScanQrCodePage, ScanQrCodeViewModel>();
			Builder.Services.AddTransient<SettingsPage, SettingsViewModel>();
			Builder.Services.AddTransient<VerifyCodePage, VerifyCodeViewModel>();
			Builder.Services.AddTransient<XmppFormPage, XmppViewModel>();
			Builder.Services.AddTransient<AppsPage, AppsViewModel>();
			Builder.Services.AddTransient<NfcTesterPage, NfcTesterViewModel>();
			// Startup page
			Builder.Services.AddTransient<LoadingPage>();

			//Notification
			Builder.Services.AddTransient<NotificationsPage, NotificationsViewModel>();


			// Petitions
			Builder.Services.AddTransient<PetitionContractPage, PetitionContractViewModel>();
			Builder.Services.AddTransient<PetitionIdentityPage, PetitionIdentityViewModel>();
			Builder.Services.AddTransient<PetitionPeerReviewPage, PetitionPeerReviewViewModel>();
			Builder.Services.AddTransient<PetitionSignaturePage, PetitionSignatureViewModel>();
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

			Builder.Services.AddTransient<PetitionPeerReviewNavigationArgs>();


			// Onboarding
			Builder.Services.AddTransient<OnboardingPage, OnboardingViewModel>();
			Builder.Services.AddTransient<WelcomeStepView, WelcomeOnboardingStepViewModel>();
			Builder.Services.AddTransient<CreateAccountStepView, CreateAccountOnboardingStepViewModel>();
			Builder.Services.AddTransient<NameEntryStepView, NameEntryOnboardingStepViewModel>();
			Builder.Services.AddTransient<DefinePasswordStepView, DefinePasswordOnboardingStepViewModel>();
			Builder.Services.AddTransient<ValidateEmailStepView, ValidateEmailOnboardingStepViewModel>(); // Fixed registration
			Builder.Services.AddTransient<ValidatePhoneStepView, ValidatePhoneOnboardingStepViewModel>(); // Fixed registration
			Builder.Services.AddTransient<BiometricsStepView, BiometricsOnboardingStepViewModel>();
			Builder.Services.AddTransient<FinalizeStepView, FinalizeOnboardingStepViewModel>(); // Added missing finalize step


			// Signatures
			Builder.Services.AddTransient<ClientSignaturePage, ClientSignatureViewModel>();
			Builder.Services.AddTransient<ServerSignaturePage, ServerSignatureViewModel>();

			// Things
			Builder.Services.AddTransient<CanControlPage, CanControlViewModel>();
			Builder.Services.AddTransient<CanReadPage, CanReadViewModel>();
			Builder.Services.AddTransient<IsFriendPage, IsFriendViewModel>();
			Builder.Services.AddTransient<MyThingsPage, MyThingsViewModel>();
			Builder.Services.AddTransient<ReadSensorPage, ReadSensorViewModel>();
			Builder.Services.AddTransient<ViewClaimThingPage, ViewClaimThingViewModel>();
			Builder.Services.AddTransient<ViewThingPage, ViewThingViewModel>();

			// Wallet
			Builder.Services.AddTransient<AccountEventPage, AccountEventViewModel>();
			Builder.Services.AddTransient<BuyEDalerPage, BuyEDalerViewModel>();
			Builder.Services.AddTransient<EDalerReceivedPage, EDalerReceivedViewModel>();
			Builder.Services.AddTransient<IssueEDalerPage, EDalerUriViewModel>();
			Builder.Services.AddTransient<MachineReportPage, MachineReportViewModel>();
			Builder.Services.AddTransient<MachineVariablesPage, MachineVariablesViewModel>();
			Builder.Services.AddTransient<MyTokensPage, MyTokensViewModel>();
			Builder.Services.AddTransient<MyEDalerWalletPage, MyWalletViewModel>();
			Builder.Services.AddTransient<MyTokenWalletPage, MyWalletViewModel>();
			Builder.Services.AddTransient<PaymentPage, EDalerUriViewModel>();
			Builder.Services.AddTransient<PaymentAcceptancePage, EDalerUriViewModel>();
			Builder.Services.AddTransient<PendingPaymentPage, EDalerUriViewModel>();
			Builder.Services.AddTransient<RequestPaymentPage, RequestPaymentViewModel>();
			Builder.Services.AddTransient<SellEDalerPage, SellEDalerViewModel>();
			Builder.Services.AddTransient<SendPaymentPage, EDalerUriViewModel>();
			Builder.Services.AddTransient<ServiceProvidersPage, ServiceProvidersViewModel>();
			Builder.Services.AddTransient<TokenDetailsPage, TokenDetailsViewModel>();
			Builder.Services.AddTransient<TokenEventsPage, TokenEventsViewModel>();
			Builder.Services.AddTransient<WalletPage, WalletViewModel>();
			Builder.Services.AddTransient<TransactionHistoryPage, TransactionHistoryViewModel>();
			Builder.Services.AddTransient<EmbeddedLayoutPage, EmbeddedLayoutViewModel>();

			// Popups
			Builder.Services.AddTransient<ImageView>();
			Builder.Services.AddTransient<ImageViewModel>();
			Builder.Services.AddTransient<AddTextNotePopup>();
			Builder.Services.AddTransient<AddTextNoteViewModel>();
			Builder.Services.AddTransient<CheckPasswordPopup>();
			Builder.Services.AddTransient<CheckPasswordViewModel>();
			Builder.Services.AddTransient<OnboardingHelpPopup>();
			Builder.Services.AddTransient<OnboardingHelpViewModel>();
			Builder.Services.AddTransient<SelectLanguagePopup>();
			Builder.Services.AddTransient<SelectLanguagePopupViewModel>();
			Builder.Services.AddTransient<InvitePartsPopup>();

			// Xmpp
			Builder.Services.AddTransient<RemoveSubscriptionPopup>();
			Builder.Services.AddTransient<RemoveSubscriptionViewModel>();
			Builder.Services.AddTransient<ReportOrBlockPopup>();
			Builder.Services.AddTransient<ReportOrBlockViewModel>();
			Builder.Services.AddTransient<ReportTypePopup>();
			Builder.Services.AddTransient<ReportTypeViewModel>();
			Builder.Services.AddTransient<SubscribeToPopup>();
			Builder.Services.AddTransient<SubscribeToViewModel>();
			Builder.Services.AddTransient<SubscriptionRequestPopup>();
			Builder.Services.AddTransient<SubscriptionRequestViewModel>();

			//Utility
			Builder.Services.AddTransient<ImageCropperView, ImageCroppingViewModel>();

			return Builder;
		}
	}
}
