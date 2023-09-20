using NeuroAccessMaui.Services.AttachmentCache;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.Services.Network;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.Services;

/// <summary>
/// Abstract base class for (bindable) classes that reference services in the app.
/// </summary>
public class ServiceReferences : BindableObject, IServiceReferences
{
	/// <summary>
	/// Abstract base class for (bindable) classes that reference services in the app.
	/// </summary>
	public ServiceReferences()
	{
	}

	private IXmppService xmppService;
	private IUiSerializer uiSerializer;
	private ITagProfile tagProfile;
	private INavigationService navigationService;
	private ILogService logService;
	private INetworkService networkService;
	private IContractOrchestratorService contractOrchestratorService;
	private IAttachmentCacheService attachmentCacheService;
	private ICryptoService cryptoService;
	private ISettingsService settingsService;
	private IStorageService storageService;
	private INfcService nfcService;

	/// <summary>
	/// The dispatcher to use for alerts and accessing the main thread.
	/// </summary>
	public IUiSerializer UiSerializer
	{
		get
		{
			this.uiSerializer ??= App.Instantiate<IUiSerializer>();
			return this.uiSerializer;
		}
	}

	/// <summary>
	/// The XMPP service for XMPP communication.
	/// </summary>
	public IXmppService XmppService
	{
		get
		{
			this.xmppService ??= App.Instantiate<IXmppService>();
			return this.xmppService;
		}
	}

	/// <summary>
	/// TAG Profie service.
	/// </summary>
	public ITagProfile TagProfile
	{
		get
		{
			this.tagProfile ??= App.Instantiate<ITagProfile>();
			return this.tagProfile;
		}
	}

	/// <summary>
	/// Navigation service.
	/// </summary>
	public INavigationService NavigationService
	{
		get
		{
			this.navigationService ??= App.Instantiate<INavigationService>();
			return this.navigationService;
		}
	}

	/// <summary>
	/// Log service.
	/// </summary>
	public ILogService LogService
	{
		get
		{
			this.logService ??= App.Instantiate<ILogService>();
			return this.logService;
		}
	}

	/// <summary>
	/// Network service.
	/// </summary>
	public INetworkService NetworkService
	{
		get
		{
			this.networkService ??= App.Instantiate<INetworkService>();
			return this.networkService;
		}
	}

	/// <summary>
	/// Contract orchestrator service.
	/// </summary>
	public IContractOrchestratorService ContractOrchestratorService
	{
		get
		{
			this.contractOrchestratorService ??= App.Instantiate<IContractOrchestratorService>();
			return this.contractOrchestratorService;
		}
	}

	/// <summary>
	/// AttachmentCache service.
	/// </summary>
	public IAttachmentCacheService AttachmentCacheService
	{
		get
		{
			this.attachmentCacheService ??= App.Instantiate<IAttachmentCacheService>();
			return this.attachmentCacheService;
		}
	}

	/// <summary>
	/// Crypto service.
	/// </summary>
	public ICryptoService CryptoService
	{
		get
		{
			this.cryptoService ??= App.Instantiate<ICryptoService>();
			return this.cryptoService;
		}
	}

	/// <summary>
	/// Settings service.
	/// </summary>
	public ISettingsService SettingsService
	{
		get
		{
			this.settingsService ??= App.Instantiate<ISettingsService>();
			return this.settingsService;
		}
	}

	/// <summary>
	/// Storage service.
	/// </summary>
	public IStorageService StorageService
	{
		get
		{
			this.storageService ??= App.Instantiate<IStorageService>();
			return this.storageService;
		}
	}

	/// <summary>
	/// Near-Field Communication (NFC) service.
	/// </summary>
	public INfcService NfcService
	{
		get
		{
			this.nfcService ??= App.Instantiate<INfcService>();
			return this.nfcService;
		}
	}
}
