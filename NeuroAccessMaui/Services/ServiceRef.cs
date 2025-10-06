using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.AppPermissions;
using NeuroAccessMaui.Services.Cache.AttachmentCache;
using NeuroAccessMaui.Services.Cache.InternetCache;
using NeuroAccessMaui.Services.Contracts;
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
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.Services.Xml;
using Waher.Content;
using ZXing;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Base class that references services in the app.
	/// </summary>
	public static class ServiceRef
	{
		private static IXmppService? xmppService;
		private static IUiService? uiService;
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

		/// <summary>
		/// Service serializing and managing UI-related tasks.
		/// </summary>
		public static IUiService UiService
		{
			get
			{
				uiService ??= App.Instantiate<IUiService>();
				return uiService;
			}
		}

		/// <summary>
		/// The XMPP service for XMPP communication.
		/// </summary>
		public static IXmppService XmppService
		{
			get
			{
				xmppService ??= App.Instantiate<IXmppService>();
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
				tagProfile ??= App.Instantiate<ITagProfile>();
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
				logService ??= App.Instantiate<ILogService>();
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
				networkService ??= App.Instantiate<INetworkService>();
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
				contractOrchestratorService ??= App.Instantiate<IContractOrchestratorService>();
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
				thingRegistryOrchestratorService ??= App.Instantiate<IThingRegistryOrchestratorService>();
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
				neuroWalletOrchestratorService ??= App.Instantiate<INeuroWalletOrchestratorService>();
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
				attachmentCacheService ??= App.Instantiate<IAttachmentCacheService>();
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
				cryptoService ??= App.Instantiate<ICryptoService>();
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
				settingsService ??= App.Instantiate<ISettingsService>();
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
				storageService ??= App.Instantiate<IStorageService>();
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
				nfcService ??= App.Instantiate<INfcService>();
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
				notificationService ??= App.Instantiate<INotificationService>();
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
				pushNotificationService ??= App.Instantiate<IPushNotificationService>();
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
				permissionService ??= App.Instantiate<IPermissionService>();
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
				intentService ??= App.Instantiate<IIntentService>();
				return intentService;
			}
		}

		public static IInternetCacheService InternetCacheService
		{
			get
			{
				internetCacheService ??= App.Instantiate<IInternetCacheService>();
				return internetCacheService;
			}
		}

		public static IThemeService ThemeService
		{
			get
			{
				themeService ??= App.Instantiate<IThemeService>();
				return themeService;
			}
		}

		public static IKycService KycService
		{
			get
			{
				kycService ??= App.Instantiate<IKycService>();
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
	}
}
