using System;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using NeuroAccessMaui;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.AppPermissions;
using NeuroAccessMaui.Services.Cache.AttachmentCache;
using NeuroAccessMaui.Services.Cache.InternetCache;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Intents;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Network;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Push;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.Theme;
using NeuroAccessMaui.Services.ThingRegistries;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.Services.Xml;
using ZXing;
using NeuroAccessMaui.Services.Authentication;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Base class that references services in the app.
	/// </summary>
	public static class ServiceRef
	{
		/// <summary>
		/// The service provider for the app.
		/// This is set before the app is started, and will be used to resolve services
		/// </summary>
		public static IServiceProvider Provider { get; private set; } = null!;

		private static IXmppService? xmppService;
		private static IUiService? uiService;
		private static INavigationService? navigationService;
		private static ITagProfile? tagProfile;
		private static ILogService? logService;
		private static INetworkService? networkService;
		private static IContractOrchestratorService? contractOrchestratorService;
		private static IThingRegistryOrchestratorService? thingRegistryOrchestratorService;
		private static INeuroWalletOrchestratorService? neuroWalletOrchestratorService;
		private static IAttachmentCacheService? attachmentCacheService;
		private static ICryptoService? cryptoService;
		private static ISettingsService? settingsService;
		private static IStorageService? storageService;
		private static INfcService? nfcService;
		private static INotificationService? notificationService;
		private static IPushNotificationService? pushNotificationService;
		private static IReportingStringLocalizer? localizer;
		private static IPlatformSpecific? platformSpecific;
		private static IBarcodeReader? barcodeReader;
		private static IPermissionService? permissionService;
		private static IIntentService? intentService;
		private static IInternetCacheService? internetCacheService;
		private static IThemeService? themeService;
		private static IKycService? kycService;
		private static IXmlSchemaValidationService? xmlSchemaValidationService;
		private static IPopupService? popupService;
		private static IToastService? toastService;
		private static IKeyboardInsetsService? keyboardInsetsService;

		private static IAuthenticationService? authenticationService;

		/// <summary>
		/// Initializes the service reference cache with a fresh provider instance.
		/// This method must be called whenever the MAUI host builds a new DI container (e.g., Android process rehydration).
		/// </summary>
		/// <param name="provider">The service provider to use for subsequent resolutions.</param>
		public static void Initialize(IServiceProvider provider)
		{
			Provider = provider ?? throw new ArgumentNullException(nameof(provider));

			xmppService = null;
			uiService = null;
			navigationService = null;
			tagProfile = null;
			logService = null;
			networkService = null;
			contractOrchestratorService = null;
			thingRegistryOrchestratorService = null;
			neuroWalletOrchestratorService = null;
			attachmentCacheService = null;
			cryptoService = null;
			settingsService = null;
			storageService = null;
			nfcService = null;
			notificationService = null;
			pushNotificationService = null;
			localizer = null;
			platformSpecific = null;
			barcodeReader = null;
			permissionService = null;
			intentService = null;
			internetCacheService = null;
			themeService = null;
			kycService = null;
			xmlSchemaValidationService = null;
			popupService = null;
			toastService = null;
			keyboardInsetsService = null;
			authenticationService = null;
		}

		/// <summary>
		/// Gets a task that completes when all core services are initialized.
		/// </summary>
		public static Task ServicesReadyTask => App.ServicesReady;

		/// <summary>
		/// Service serializing and managing UI-related tasks.
		/// </summary>
		public static IUiService UiService
		{
			get
			{
				uiService ??= Provider.GetRequiredService<IUiService>();
				return uiService;
			}
		}

		/// <summary>
		/// Popup service for presenting application popups.
		/// </summary>
		public static IPopupService PopupService
		{
			get
			{
				popupService ??= Provider.GetRequiredService<IPopupService>();
				return popupService;
			}
		}

		/// <summary>
		/// Toast service for transient notifications.
		/// </summary>
		public static IToastService ToastService
		{
			get
			{
				toastService ??= Provider.GetRequiredService<IToastService>();
				return toastService;
			}
		}

		/// <summary>
		/// Provides the current keyboard inset state.
		/// </summary>
		public static IKeyboardInsetsService KeyboardInsetsService
		{
			get
			{
				keyboardInsetsService ??= Provider.GetRequiredService<IKeyboardInsetsService>();
				return keyboardInsetsService;
			}
		}

		/// <summary>
		/// The navigation service for navigating between pages.
		/// </summary>
		public static INavigationService NavigationService
		{
			get
			{
				navigationService ??= Provider.GetRequiredService<INavigationService>();
				return navigationService;
			}
		}

		/// <summary>
		/// The XMPP service for XMPP communication.
		/// </summary>
		public static IXmppService XmppService
		{
			get
			{
				xmppService ??= Provider.GetRequiredService<IXmppService>();
				return xmppService;
			}
		}

		/// <summary>
		/// TAG Profile service.
		/// </summary>
		public static ITagProfile TagProfile
		{
			get
			{
				tagProfile ??= Provider.GetRequiredService<ITagProfile>();
				return tagProfile;
			}
		}

		/// <summary>
		/// Log service.
		/// </summary>
		public static ILogService LogService
		{
			get
			{
				logService ??= Provider.GetRequiredService<ILogService>();
				return logService;
			}
		}

		/// <summary>
		/// Network service.
		/// </summary>
		public static INetworkService NetworkService
		{
			get
			{
				networkService ??= Provider.GetRequiredService<INetworkService>();
				return networkService;
			}
		}

		/// <summary>
		/// Contract orchestrator service.
		/// </summary>
		public static IContractOrchestratorService ContractOrchestratorService
		{
			get
			{
				contractOrchestratorService ??= Provider.GetRequiredService<IContractOrchestratorService>();
				return contractOrchestratorService;
			}
		}

		/// <summary>
		/// Thing Registry orchestrator service.
		/// </summary>
		public static IThingRegistryOrchestratorService ThingRegistryOrchestratorService
		{
			get
			{
				thingRegistryOrchestratorService ??= Provider.GetRequiredService<IThingRegistryOrchestratorService>();
				return thingRegistryOrchestratorService;
			}
		}

		/// <summary>
		/// Neuro Wallet orchestrator service.
		/// </summary>
		public static INeuroWalletOrchestratorService NeuroWalletOrchestratorService
		{
			get
			{
				neuroWalletOrchestratorService ??= Provider.GetRequiredService<INeuroWalletOrchestratorService>();
				return neuroWalletOrchestratorService;
			}
		}

		/// <summary>
		/// AttachmentCache service.
		/// </summary>
		public static IAttachmentCacheService AttachmentCacheService
		{
			get
			{
				attachmentCacheService ??= Provider.GetRequiredService<IAttachmentCacheService>();
				return attachmentCacheService;
			}
		}

		/// <summary>
		/// Crypto service.
		/// </summary>
		public static ICryptoService CryptoService
		{
			get
			{
				cryptoService ??= Provider.GetRequiredService<ICryptoService>();
				return cryptoService;
			}
		}

		/// <summary>
		/// Settings service.
		/// </summary>
		public static ISettingsService SettingsService
		{
			get
			{
				settingsService ??= Provider.GetRequiredService<ISettingsService>();
				return settingsService;
			}
		}

		/// <summary>
		/// Storage service.
		/// </summary>
		public static IStorageService StorageService
		{
			get
			{
				storageService ??= Provider.GetRequiredService<IStorageService>();
				return storageService;
			}
		}

		/// <summary>
		/// Near-Field Communication (NFC) service.
		/// </summary>
		public static INfcService NfcService
		{
			get
			{
				nfcService ??= Provider.GetRequiredService<INfcService>();
				return nfcService;
			}
		}

		/// <summary>
		/// Service for managing notifications for the user.
		/// </summary>
		public static INotificationService NotificationService
		{
			get
			{
				notificationService ??= Provider.GetRequiredService<INotificationService>();
				return notificationService;
			}
		}

		/// <summary>
		/// Service for managing push notifications from the network.
		/// </summary>
		public static IPushNotificationService PushNotificationService
		{
			get
			{
				pushNotificationService ??= Provider.GetRequiredService<IPushNotificationService>();
				return pushNotificationService;
			}
		}

		/// <summary>
		/// Permission Service
		/// </summary>
		public static IPermissionService PermissionService
		{
			get
			{
				permissionService ??= Provider.GetRequiredService<IPermissionService>();
				return permissionService;
			}
		}

		/// <summary>
		/// Localization service
		/// </summary>
		public static IReportingStringLocalizer Localizer
		{
			get
			{
				localizer ??= new ReportingStringLocalizer(LocalizationManager.GetStringLocalizer<AppResources>());
				return localizer;
			}
		}


		/// <summary>
		/// Localization service
		/// </summary>
		public static IPlatformSpecific PlatformSpecific
		{
			get
			{
				platformSpecific ??= ServiceHelper.GetService<IPlatformSpecific>();
				return platformSpecific;
			}
		}


		/// <summary>
		/// Localization service
		/// </summary>
		public static IBarcodeReader BarcodeReader
		{
			get
			{
				barcodeReader ??= ServiceHelper.GetService<IBarcodeReader>();
				return barcodeReader;
			}
		}

		public static IIntentService IntentService
		{
			get
			{
				intentService ??= Provider.GetRequiredService<IIntentService>();
				return intentService;
			}
		}

		public static IInternetCacheService InternetCacheService
		{
			get
			{
				internetCacheService ??= Provider.GetRequiredService<IInternetCacheService>();
				return internetCacheService;
			}
		}

		public static IThemeService ThemeService
		{
			get
			{
				themeService ??= Provider.GetRequiredService<IThemeService>();
				return themeService;
			}
		}

		public static IKycService KycService
		{
			get
			{
				kycService ??= Provider.GetRequiredService<IKycService>();
				return kycService;
			}
		}

		/// <summary>
		/// XML schema validation service.
		/// </summary>
		public static IXmlSchemaValidationService XmlSchemaValidationService
		{
			get
			{
				xmlSchemaValidationService ??= ServiceHelper.GetService<IXmlSchemaValidationService>();
				return xmlSchemaValidationService;
			}
		}

		/// <summary>
		/// Authentication service.
		/// </summary>
		/// <remarks>If the application does not use authentication, this service will throw exceptions when accessed.</remarks>
		public static IAuthenticationService AuthenticationService
		{

			get
			{
				authenticationService ??= Provider.GetRequiredService<IAuthenticationService>();
				return authenticationService;
			}
		}
	}
}
