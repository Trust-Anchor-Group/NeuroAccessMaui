using System.Reflection;
using CommunityToolkit.Maui;
using EDaler;
using NeuroAccessMaui.Services.Cache.AttachmentCache;
using NeuroAccessMaui.Services.Cache.InternetCache;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Intents;
using NeuroAccessMaui.Services.Network;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Theme;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Identity.TransferIdentity;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Main.Calculator;
using NeuroAccessMaui.UI.Pages.Main.ChangePassword;
using NeuroAccessMaui.UI.Pages.Main.Duration;
using NeuroAccessMaui.UI.Pages.Main.QR;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Pages.Main.XmppForm;
using NeuroAccessMaui.UI.Pages.Notifications;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionContract;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview.Views;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature;
using NeuroAccessMaui.UI.Pages.Registration;
using NeuroAccessMaui.UI.Pages.Registration.Views;
using NeuroAccessMaui.UI.Pages.Signatures.ClientSignature;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
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
using NeuroAccessMaui.UI.Popups.Photos.Image;
using NeuroAccessMaui.UI.Popups.Tokens.AddTextNote;
using NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription;
using NeuroAccessMaui.UI.Popups.Xmpp.ReportOrBlock;
using NeuroAccessMaui.UI.Popups.Xmpp.ReportType;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscriptionRequest;
using NeuroFeatures;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Events.Persistence;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Avatar;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
using Waher.Networking.XMPP.Geo;
using Waher.Networking.XMPP.HTTPX;
using Waher.Networking.XMPP.Mail;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.P2P.E2E;
using Waher.Networking.XMPP.PEP;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.PubSub;
using Waher.Networking.XMPP.Push;
using Waher.Networking.XMPP.Sensor;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Geo;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Script;
using Waher.Script.Content;
using Waher.Script.Graphs;
using Waher.Security.JWS;
using Waher.Security.JWT;
using Waher.Things;

namespace NeuroAccessMaui.UI
{
	public static class PageAppExtension
	{
		public static MauiAppBuilder RegisterTypes(this MauiAppBuilder Builder)
		{
			Assembly AppAssembly = typeof(App).Assembly;

			if (!Types.IsInitialized)
			{
				Types.Initialize(
					AppAssembly,
					typeof(Database).Assembly,
					typeof(ObjectSerializer).Assembly,
					typeof(FilesProvider).Assembly,
					typeof(RuntimeSettings).Assembly,
					typeof(PersistedEvent).Assembly,
					typeof(InternetContent).Assembly,
					typeof(ImageCodec).Assembly,
					typeof(MarkdownDocument).Assembly,
					typeof(XML).Assembly,
					typeof(DnsResolver).Assembly,
					typeof(XmppClient).Assembly,
					typeof(ContractsClient).Assembly,
					typeof(NeuroFeaturesClient).Assembly,
					typeof(EDalerClient).Assembly,
					typeof(SensorClient).Assembly,
					typeof(ControlClient).Assembly,
					typeof(ConcentratorClient).Assembly,
					typeof(ProvisioningClient).Assembly,
					typeof(PubSubClient).Assembly,
					typeof(PepClient).Assembly,
					typeof(AvatarClient).Assembly,
					typeof(PushNotificationClient).Assembly,
					typeof(MailClient).Assembly,
					typeof(GeoClient).Assembly,
					typeof(GeoPosition).Assembly,
					typeof(ThingReference).Assembly,
					typeof(JwtFactory).Assembly,
					typeof(JwsAlgorithm).Assembly,
					typeof(Expression).Assembly,
					typeof(Graph).Assembly,
					typeof(GraphEncoder).Assembly,
					typeof(XmppServerlessMessaging).Assembly,
					typeof(HttpxClient).Assembly);
			}

			// Register exceptions as alerts.
			Log.RegisterAlertExceptionType(true,
				typeof(OutOfMemoryException),
				typeof(StackOverflowException),
				typeof(AccessViolationException),
				typeof(InsufficientMemoryException));

			EndpointSecurity.SetCiphers([typeof(Edwards448Endpoint)], false);

			// Instantiate default services.
			Builder.Services.AddSingleton<ITagProfile>((_)=>Types.InstantiateDefault<ITagProfile>(false));
			Builder.Services.AddSingleton<ILogService>((_)=>Types.InstantiateDefault<ILogService>(false));
			Builder.Services.AddSingleton<IUiService>((_)=>Types.InstantiateDefault<IUiService>(false));
			Builder.Services.AddSingleton<ICryptoService>((_)=>Types.InstantiateDefault<ICryptoService>(false));
			Builder.Services.AddSingleton<INetworkService>((_)=>Types.InstantiateDefault<INetworkService>(false));
			Builder.Services.AddSingleton<IStorageService>((_)=>Types.InstantiateDefault<IStorageService>(false));
			Builder.Services.AddSingleton<ISettingsService>((_)=>Types.InstantiateDefault<ISettingsService>(false));
			Builder.Services.AddSingleton<IXmppService>((_)=>Types.InstantiateDefault<IXmppService>(false));
			Builder.Services.AddSingleton<IAttachmentCacheService>((_)=>Types.InstantiateDefault<IAttachmentCacheService>(false));
			Builder.Services.AddSingleton<IInternetCacheService>((_)=>Types.InstantiateDefault<IInternetCacheService>(false));
			Builder.Services.AddSingleton<IContractOrchestratorService>((_)=>Types.InstantiateDefault<IContractOrchestratorService>(false));
			Builder.Services.AddSingleton<INfcService>((_) => Types.InstantiateDefault<INfcService>(false));
			Builder.Services.AddSingleton<INotificationService>((_) => Types.InstantiateDefault<INotificationService>(false));
			Builder.Services.AddSingleton<IIntentService>((_) => Types.InstantiateDefault<IIntentService>(false));
			Builder.Services.AddSingleton<IThemeService>((_) => Types.InstantiateDefault<IThemeService>(false));

			return Builder;
		}
			
		public static MauiAppBuilder RegisterPages(this MauiAppBuilder Builder)
		{
			// Applications
			Builder.Services.AddTransient<ApplicationsPage, ApplicationsViewModel>();
			Builder.Services.AddTransient<ApplyIdPage, ApplyIdViewModel>();

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
			Builder.Services.AddTransient<AppShell>();
			Builder.Services.AddTransient<CalculatorPage, CalculatorViewModel>();
			Builder.Services.AddTransient<ChangePasswordPage, ChangePasswordViewModel>();
			Builder.Services.AddTransient<DurationPage, DurationViewModel>();
			Builder.Services.AddTransient<MainPage, MainViewModel>();
			Builder.Services.AddTransient<ScanQrCodePage, ScanQrCodeViewModel>();
			Builder.Services.AddTransient<SettingsPage, SettingsViewModel>();
			Builder.Services.AddTransient<VerifyCodePage, VerifyCodeViewModel>();
			Builder.Services.AddTransient<XmppFormPage, XmppViewModel>();
			Builder.Services.AddTransient<AppsPage,  AppsViewModel>();

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


			// Registration
			Builder.Services.AddTransient<RegistrationPage, RegistrationViewModel>();
			Builder.Services.AddTransient<LoadingView, LoadingViewModel>();
			//Builder.Services.AddTransient<RequestPurposeView, RequestPurposeViewModel>();
			Builder.Services.AddTransient<ValidatePhoneView, ValidatePhoneViewModel>();
			Builder.Services.AddTransient<ValidateEmailView, ValidateEmailViewModel>();
			Builder.Services.AddTransient<ChooseProviderView, ChooseProviderViewModel>();
			Builder.Services.AddTransient<CreateAccountView, CreateAccountViewModel>();
			Builder.Services.AddTransient<GetStartedView, GetStartedViewModel>();
			Builder.Services.AddTransient<NameEntryView, NameEntryViewModel>();
			Builder.Services.AddTransient<DefinePasswordView, DefinePasswordViewModel>();
			Builder.Services.AddTransient<BiometricsView, BiometricsViewModel>();
			Builder.Services.AddTransient<FinalizeView, FinalizeViewModel>();
			Builder.Services.AddTransient<ContactSupportView, ContactSupportViewModel>();

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

			// Popups
			Builder.Services.AddTransient<ImageView, ImageViewModel>();
			Builder.Services.AddTransient<AddTextNotePopup, AddTextNoteViewModel>();

			// Xmpp
			Builder.Services.AddTransient<RemoveSubscriptionPopup, RemoveSubscriptionViewModel>();
			Builder.Services.AddTransient<ReportOrBlockPopup, ReportOrBlockViewModel>();
			Builder.Services.AddTransient<ReportTypePopup, ReportTypeViewModel>();
			Builder.Services.AddTransient<SubscribeToPopup, SubscribeToViewModel>();
			Builder.Services.AddTransient<SubscriptionRequestPopup, SubscriptionRequestViewModel>();

			//Utility
			Builder.Services.AddTransient<ImageCropperView, ImageCroppingViewModel>();

			return Builder;
		}
	}
}
