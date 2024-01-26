using Microsoft.Extensions.Localization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.AttachmentCache;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.Services.Network;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Xmpp;
using ZXing;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Base class that references services in the app.
	/// </summary>
	public static class ServiceRef
	{
		private static IXmppService? xmppService;
		private static IUiSerializer? uiSerializer;
		private static ITagProfile? tagProfile;
		private static INavigationService? navigationService;
		private static ILogService? logService;
		private static INetworkService? networkService;
		private static IContractOrchestratorService? contractOrchestratorService;
		private static IAttachmentCacheService? attachmentCacheService;
		private static ICryptoService? cryptoService;
		private static ISettingsService? settingsService;
		private static IStorageService? storageService;
		private static INfcService? nfcService;
		private static INotificationService? notificationService;
		private static IStringLocalizer? localizer;
		private static IPlatformSpecific? platformSpecific;
		private static IBarcodeReader? barcodeReader;

		/// <summary>
		/// The dispatcher to use for alerts and accessing the main thread.
		/// </summary>
		public static IUiSerializer UiSerializer
		{
			get
			{
				uiSerializer ??= App.Instantiate<IUiSerializer>();
				return uiSerializer;
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
		/// Navigation service.
		/// </summary>
		public static INavigationService NavigationService
		{
			get
			{
				navigationService ??= App.Instantiate<INavigationService>();
				return navigationService;
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
		/// Localization service
		/// </summary>
		public static IStringLocalizer Localizer
		{
			get
			{
				localizer ??= LocalizationManager.GetStringLocalizer<AppResources>();
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
	}
}
