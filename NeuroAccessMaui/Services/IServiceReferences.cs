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
/// Interface for classes that reference services in the app.
/// </summary>
public interface IServiceReferences
{
	/// <summary>
	/// The dispatcher to use for alerts and accessing the main thread.
	/// </summary>
	IUiSerializer UiSerializer { get; }

	/// <summary>
	/// The XMPP service for XMPP communication.
	/// </summary>
	public IXmppService XmppService { get; }

	/// <summary>
	/// TAG Profie service.
	/// </summary>
	public ITagProfile TagProfile { get; }

	/// <summary>
	/// Navigation service.
	/// </summary>
	public INavigationService NavigationService { get; }

	/// <summary>
	/// Log service.
	/// </summary>
	public ILogService LogService { get; }

	/// <summary>
	/// Network service.
	/// </summary>
	public INetworkService NetworkService { get; }

	/// <summary>
	/// Contract orchestrator service.
	/// </summary>
	public IContractOrchestratorService ContractOrchestratorService { get; }

	/// <summary>
	/// AttachmentCache service.
	/// </summary>
	public IAttachmentCacheService AttachmentCacheService { get; }

	/// <summary>
	/// Crypto service.
	/// </summary>
	public ICryptoService CryptoService { get; }

	/// <summary>
	/// Settings service.
	/// </summary>
	public ISettingsService SettingsService { get; }

	/// <summary>
	/// Storage service.
	/// </summary>
	public IStorageService StorageService { get; }

	/// <summary>
	/// Near-Field Communication (NFC) service.
	/// </summary>
	public INfcService NfcService { get; }
}
