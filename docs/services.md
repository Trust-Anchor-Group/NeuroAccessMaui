# Services

This guide lists the app services you reach through `ServiceRef`, explains when services are loaded and unloaded, describes the user profile in `TagProfile`, and walks through adding a new service. Read it when you need something from a service, or before you write one.

- [Reaching a service](#reaching-a-service)
- [Service catalog](#service-catalog)
- [Other registered services](#other-registered-services)
- [Loadable services](#loadable-services)
- [TagProfile](#tagprofile)
- [Adding a service](#adding-a-service)

How the container, `HybridServiceProvider` and the Waher type inventory fit together is explained in [architecture.md](architecture.md#dependency-injection-and-service-resolution).

## Reaching a service

Services live in [`NeuroAccessMaui/Services/`](../NeuroAccessMaui/Services), one folder per area. Each one is registered as a singleton in `PageAppExtension.RegisterTypes` and lives as long as the process.

There are three ways to get one:

- Through a [`ServiceRef`](../NeuroAccessMaui/Services/ServiceRef.cs) property, such as `ServiceRef.XmppService`. Each property resolves its service on first use and caches it. This is what most code does.
- Through a constructor parameter, for anything the container creates: pages, view models, popups and other services. [`LoadingPage`](../NeuroAccessMaui/UI/Pages/Startup/LoadingPage.xaml.cs) and [`NotificationsViewModel`](../NeuroAccessMaui/UI/Pages/Notifications/NotificationsViewModel.cs) do this.
- Through `ServiceRef.Provider.GetRequiredService<T>()`, for registered services that have no `ServiceRef` property.

```csharp
INotificationServiceV2 Notifications = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
await Notifications.ProcessPendingAsync(CancellationToken.None);
```

Do not create services with `new`, and do not dispose them. The same instances serve every session while the process lives.

## Service catalog

These are the services with a property on `ServiceRef`.

### Profile, storage and security

| `ServiceRef` property | Interface | What it does |
| --- | --- | --- |
| `TagProfile` | [`ITagProfile`](../NeuroAccessMaui/Services/Tag/ITagProfile.cs) | The user's profile: onboarding progress, account, legal identity, server components and app preferences. See [TagProfile](#tagprofile). |
| `StorageService` | [`IStorageService`](../NeuroAccessMaui/Services/Storage/IStorageService.cs) | Starts and stops the encrypted object database, inserts and updates objects (with a flush), exports the database and flags it for repair. See [architecture.md](architecture.md#local-persistence). |
| `SettingsService` | [`ISettingsService`](../NeuroAccessMaui/Services/Settings/ISettingsService.cs) | Saves and restores key and value pairs (`SaveState`, `RestoreStringState`, `RestoreBoolState`, `RestoreState<T>` and so on) through Waher `RuntimeSettings`. |
| `CryptoService` | [`ICryptoService`](../NeuroAccessMaui/Services/Crypto/ICryptoService.cs) | The device ID, the per-file database keys kept in secure storage, random passwords, and creating and validating JWT tokens. |
| `AuthenticationService` | [`IAuthenticationService`](../NeuroAccessMaui/Services/Authentication/IAuthenticationService.cs) | Asks the user for the app password or biometrics before sensitive actions, and blocks the user after too many wrong passwords. |
| `PermissionService` | [`IPermissionService`](../NeuroAccessMaui/Services/AppPermissions/IPermissionService.cs) | Checks and requests the camera and notification permissions. |

### Platform and diagnostics

| `ServiceRef` property | Interface | What it does |
| --- | --- | --- |
| `LogService` | [`ILogService`](../NeuroAccessMaui/Services/EventLog/ILogService.cs) | Logging (`LogDebug`, `LogInformational`, `LogWarning`, `LogException`, `LogAlert`, `LogTelemetryEvent`) through the Waher event log, crash dumps for the next run's crash report, and debug log sessions. |
| `PlatformSpecific` | [`IPlatformSpecific`](../NeuroAccessMaui/Services/IPlatformSpecific.cs) | Platform functions: device ID, closing the app, blocking screen capture, screenshots, sharing images, biometrics, the push token, hiding the keyboard, insets and the reduce-motion setting. Implemented in `Platforms/<Platform>/PlatformSpecific.cs`. |
| `KeyboardInsetsService` | [`IKeyboardInsetsService`](../NeuroAccessMaui/Services/UI/IKeyboardInsetsService.cs) | The keyboard's height and visibility, with a change event. See [navigation.md](navigation.md#keyboard-insets). |
| `Localizer` | [`IReportingStringLocalizer`](../NeuroAccessMaui/Services/Localization/ReportingStringLocalizer.cs) | Looks up strings in `AppResources`, for example `ServiceRef.Localizer[nameof(AppResources.Ok)]`, and logs keys that are missing. It is created over `LocalizationManager`, not taken from the container. |

### Network and XMPP

| `ServiceRef` property | Interface | What it does |
| --- | --- | --- |
| `NetworkService` | [`INetworkService`](../NeuroAccessMaui/Services/Network/INetworkService.cs) | Connectivity state and its change event, DNS lookup of a domain's XMPP host and port, the Neuron HTTP proxy, and `TryRequest` helpers that catch, log and optionally show errors from a network call. |
| `XmppService` | [`IXmppService`](../NeuroAccessMaui/Services/Xmpp/IXmppService.cs) | The XMPP connection to the user's Neuron and everything carried over it: connection state, roster and presence, chat, legal identities and petitions, smart contracts, things, eDaler and tokens. Most of its events are `EventHandlerAsync<T>` and arrive on background threads. |
| `PushNotificationService` | [`IPushNotificationService`](../NeuroAccessMaui/Services/Push/IPushNotificationService.cs) | Receives push tokens from the platform transport and checks that the current token is registered with the Neuron. See [notifications.md](notifications.md). |

### UI

| `ServiceRef` property | Interface | What it does |
| --- | --- | --- |
| `NavigationService` | [`INavigationService`](../NeuroAccessMaui/Services/UI/INavigationService.cs) | Route-based navigation on top of `CustomShell`: `GoToAsync`, `GoBackAsync`, `SetRootAsync`, `PopToRootAsync` and navigation arguments. See [navigation.md](navigation.md). |
| `PopupService` | [`IPopupService`](../NeuroAccessMaui/Services/UI/Popups/IPopupService.cs) | Shows and closes popups, one stack, with `PopupOptions` for placement and dismissal. |
| `ToastService` | [`IToastService`](../NeuroAccessMaui/Services/UI/Toasts/IToastService.cs) | Shows one toast at a time. The `ShowTextAsync` extension in [`ToastServiceExtensions`](../NeuroAccessMaui/Services/UI/Toasts/ToastServiceExtensions.cs) shows a plain text toast. |
| `UiService` | [`IUiService`](../NeuroAccessMaui/Services/UI/IUiService.cs) | Alerts (`DisplayAlert`), prompts (`DisplayPrompt`), error messages (`DisplayException`), screenshots and SVG conversion. Its `GoToAsync`, `GoBackAsync` and `CurrentPage` come from the earlier MAUI Shell navigation and do not work with `CustomShell`. Use `NavigationService` instead. |
| `ThemeService` | [`IThemeService`](../NeuroAccessMaui/Services/Theme/IThemeService.cs) | The light or dark theme, and the provider's branding: fetching, caching and applying colors, and the URIs of branded images. See [branding.md](branding.md). |

### Identity, KYC and contracts

| `ServiceRef` property | Interface | What it does |
| --- | --- | --- |
| `KycService` | [`IKycService`](../NeuroAccessMaui/Services/Kyc/IKycService.cs) | Loads KYC processes, validates fields, builds identity applications and keeps snapshots of applications in progress. See [kyc.md](kyc.md). |
| `IdentityApplicationGateService` | [`IIdentityApplicationGateService`](../NeuroAccessMaui/Services/Identity/IdentityApplicationGateService.cs) | Decides where the user goes next in the identity application flow. |
| `ContractOrchestratorService` | [`IContractOrchestratorService`](../NeuroAccessMaui/Services/Contracts/IContractOrchestratorService.cs) | Reacts to contract and petition events from XMPP (proposals, signatures, peer review and identity petitions) and starts contract synchronization when the connection comes up. |
| `ContractStore` | [`IContractStore`](../NeuroAccessMaui/Services/Contracts/IContractStore.cs) | The only writer of stored contract references. Writes for one contract are serialized, and each write raises `ContractChanged`. |
| `ContractSyncService` | [`IContractSyncService`](../NeuroAccessMaui/Services/Contracts/IContractSyncService.cs) | Reads the user's contracts from the server and stores them through `ContractStore`. |

### Things, wallet and tokens

| `ServiceRef` property | Interface | What it does |
| --- | --- | --- |
| `ThingRegistryOrchestratorService` | [`IThingRegistryOrchestratorService`](../NeuroAccessMaui/Services/ThingRegistries/IThingRegistryOrchestratorService.cs) | Opens claim and search links for devices in thing registries. |
| `NeuroWalletOrchestratorService` | [`INeuroWalletOrchestratorService`](../NeuroAccessMaui/Services/Wallet/INeuroWalletOrchestratorService.cs) | Reacts to eDaler and Neuro-Feature token events and turns them into notifications. |
| `TokenUnreadTracker` | [`ITokenUnreadTracker`](../NeuroAccessMaui/Services/Wallet/Tokens/ITokenUnreadTracker.cs) | Counts unread notifications per token, so token lists can mark tokens with news. |
| `TokenNameCache` | [`TokenNameCache`](../NeuroAccessMaui/Services/Wallet/Tokens/TokenNameCache.cs) | Remembers token names, so notifications about a token can show its name. It has no interface. |
| `TokenTemplateService` | [`ITokenTemplateService`](../NeuroAccessMaui/Services/Wallet/Tokens/ITokenTemplateService.cs) | Finds the contract templates for token actions on the user's Neuron and prepares contracts from them. |

### Notifications, links, caches and devices

| `ServiceRef` property | Interface | What it does |
| --- | --- | --- |
| `NotificationService` | [`INotificationService`](../NeuroAccessMaui/Services/Notification/INotificationService.cs) | The older notification store: events per type (contacts, things, contracts, identities, wallet) and category, used for badges and "new" markers. The current notification pipeline is `INotificationServiceV2`, described in [notifications.md](notifications.md). |
| `IntentService` | [`IIntentService`](../NeuroAccessMaui/Services/Intents/IIntentService.cs) | Queues intents from platform code (deep links, notification taps) until the app is ready, processes them, and holds an onboarding link for the onboarding flow. |
| `AttachmentCacheService` | [`IAttachmentCacheService`](../NeuroAccessMaui/Services/Cache/AttachmentCache/IAttachmentCacheService.cs) | Caches attachment content such as identity photos by URL, as temporary or permanent entries grouped by a parent ID. |
| `InternetCacheService` | [`IInternetCacheService`](../NeuroAccessMaui/Services/Cache/InternetCache/IInternetCacheService.cs) | Caches content fetched from the internet, for example branding files. |
| `XmlSchemaValidationService` | [`IXmlSchemaValidationService`](../NeuroAccessMaui/Services/Xml/IXmlSchemaValidationService.cs) | Validates XML against registered schemas. Core initialization registers the branding and KYC process schemas from `Resources/Raw/Schemas`. |
| `NfcService` | [`INfcService`](../NeuroAccessMaui/Services/Nfc/INfcService.cs) | Handles an NFC tag found by the platform code: hands it to an active ISO-DEP session, reads a travel document, or reads or programs NDEF content. |
| `BarcodeReader` | ZXing `IBarcodeReader` | Not registered, so reading this property throws. Scan codes with `IQrService` or the scanner pages instead. |

## Other registered services

`RegisterTypes` and `RegisterOcrServices` register more services than `ServiceRef` exposes. Inject them or resolve them with `ServiceRef.Provider.GetRequiredService<T>()`.

| Service | What it does |
| --- | --- |
| [`INotificationServiceV2`](../NeuroAccessMaui/Services/Notification/INotificationServiceV2.cs) | The notification pipeline: stores, merges, renders and routes notifications from XMPP, push and taps. See [notifications.md](notifications.md). |
| [`INotificationIntentRouter`](../NeuroAccessMaui/Services/Notification/INotificationIntentRouter.cs), [`INotificationFilterRegistry`](../NeuroAccessMaui/Services/Notification/INotificationFilterRegistry.cs), [`INotificationRenderer`](../NeuroAccessMaui/Services/Push/INotificationRenderer.cs) | Routing of notification taps to pages, runtime ignore rules, and the platform renderer for local notifications. |
| [`IPushTransport`](../NeuroAccessMaui/Services/Push/IPushTransport.cs), [`IPushTokenRegistrar`](../NeuroAccessMaui/Services/Push/IPushTokenRegistrar.cs) | The Firebase push transport, and storing and reporting push tokens. |
| [`ILinkService`](../NeuroAccessMaui/Links/ILinkService.cs) | Routes a link to the `ILinkOpener` that handles it, prepares it and opens it. |
| [`IQrService`](../NeuroAccessMaui/Services/UI/QR/IQrService.cs) | Scans codes, preparing their content before the scanner closes, and generates QR code images. |
| [`ICacheInvalidationService`](../NeuroAccessMaui/Services/Cache/Invalidation/ICacheInvalidationService.cs) | Removes cache entries by parent ID or by key. |
| [`IKycPreviewPromotionService`](../NeuroAccessMaui/Services/Kyc/KycPreviewPromotionService.cs), [`KycEvidenceValidationService`](../NeuroAccessMaui/Services/Kyc/KycEvidenceValidationService.cs), [`KycActionRegistry`](../NeuroAccessMaui/Services/Kyc/Actions/KycActionRegistry.cs), [`IKycPageAction`](../NeuroAccessMaui/Services/Kyc/Actions/IKycPageAction.cs) | KYC helpers: promoting a preview identity to a real application, checking evidence requirements before submission, and the built-in page actions such as reading a travel document chip. |
| [`ITravelDocumentReadoutService`](../NeuroAccessMaui/Services/TravelDocuments/TravelDocumentReadoutService.cs), [`ITravelDocumentEvidenceService`](../NeuroAccessMaui/Services/TravelDocuments/TravelDocumentEvidenceService.cs), [`INfcIsoDepSessionService`](../NeuroAccessMaui/Services/Nfc/NfcIsoDepSessionService.cs) | Reading ICAO travel document chips over ISO-DEP, storing the evidence for the KYC flow, and the platform NFC session. |
| [`IOcrScanService`](../NeuroAccessMaui.OCR/Services/IOcrScanService.cs), [`IMrzPreviewAnalysisService`](../NeuroAccessMaui.OCR/Services/IMrzPreviewAnalysisService.cs), [`IOcrArtifactExportService`](../NeuroAccessMaui.OCR/Services/IOcrArtifactExportService.cs) | OCR from `NeuroAccessMaui.OCR`: scanning, reading the MRZ in live camera frames, and sharing debug artifacts. |
| [`IAnimationCoordinator`](../NeuroAccessMaui/Animations/IAnimationCoordinator.cs), [`IMotionSettings`](../NeuroAccessMaui/Animations/IMotionSettings.cs), `IAnimationRegistry`, `IAnimationContextProvider` | The animation system. See [animations.md](animations.md). |

## Loadable services

Services that must start and stop with the app implement [`ILoadableService`](../NeuroAccessMaui/Services/ILoadableService.cs):

| Member | Purpose |
| --- | --- |
| `Load(bool isResuming, CancellationToken cancellationToken)` | Starts the service. `isResuming` is `false` the first time in a process and `true` when a session starts again after the app was in the background. |
| `Unload()` | Stops the service. It must undo `Load`, so that `Load` can run again later. |
| `Loaded` event | Raised when the loaded state changes. A new subscriber is called at once with the current state. |

### The LoadableService base class

Most loadable services derive from [`LoadableService`](../NeuroAccessMaui/Services/LoadableService.cs). It keeps `IsLoaded`, `IsLoading`, `IsUnloading` and `IsResuming`, and gives you four helpers to call from your overrides:

| Helper | What it does |
| --- | --- |
| `BeginLoad(IsResuming, CancellationToken)` | Returns `false` if the service is already loaded. Otherwise it sets `IsLoading` and `IsResuming`, throws if cancellation was requested, and returns `true`. |
| `EndLoad(bool IsLoaded)` | Sets `IsLoaded`, clears `IsLoading` and raises `Loaded`. Pass `false` when loading failed. |
| `BeginUnload()` | Returns `false` if the service is not loaded. Otherwise it sets `IsUnloading` and returns `true`. |
| `EndUnload()` | Clears `IsLoaded` and `IsUnloading` and raises `Loaded`. |

The default `Load` and `Unload` do nothing, not even update the flags. `LoadableService` has no locking either. `App` never runs two lifecycle transitions at once, but a service that other code can load or call concurrently needs its own guard. `XmppService`, `NotificationService` and `PushNotificationService` use a `SemaphoreSlim` for this.

### When services load

[`App`](../NeuroAccessMaui/App.xaml.cs) loads services in two phases, described in [architecture.md](architecture.md#startup-and-lifecycle):

| Phase | Services, in load order | Loaded | Unloaded |
| --- | --- | --- | --- |
| Core | `LogService`, then `StorageService.Init` (its own lifecycle, not `ILoadableService`), then `AttachmentCacheService`, `InternetCacheService`, `NotificationService`, `PushNotificationService` | Once per process, always with `isResuming` set to `false` | At termination, in reverse order, followed by the final profile save, `StorageService.Shutdown` and disposing `CryptoService` |
| Session | `NetworkService`, `XmppService`, `ContractOrchestratorService`, `ThingRegistryOrchestratorService`, `NeuroWalletOrchestratorService` | At each activation | In reverse order when the app goes to the background, and at termination. A crash uses `XmppService.UnloadFast` |

Some things follow from this:

- `App.WaitForServicesAsync()`, and with it every page's lifecycle, waits for all session services to load. Keep `Load` short and never wait for the network in it. `XmppService` starts connecting in `Load` but does not wait for the connection.
- Core services must not depend on session services, which load later.
- The activation's cancellation token is cancelled when the app goes to the background before startup has finished. Pass it on to anything slow in `Load`.
- `UiService` and `INotificationServiceV2` implement `ILoadableService`, but `App` does not load them, so their `Load` overrides never run.
- `TagProfile` is not loadable. Core initialization restores it from the database right after storage starts.

`ContractOrchestratorService.Load` shows the usual shape of a session service: it subscribes to `XmppService` events, starts work if the connection is already up, and `Unload` removes every subscription again.

## TagProfile

[`TagProfile`](../NeuroAccessMaui/Services/Tag/TagProfile.cs), reached as `ServiceRef.TagProfile`, is the in-memory profile of the user on this device. It is persisted as a single [`TagConfiguration`](../NeuroAccessMaui/Services/Tag/TagConfiguration.cs) record in the `Configuration` collection.

### What it holds

| Area | Properties |
| --- | --- |
| Onboarding | `Step` (a [`RegistrationStep`](../NeuroAccessMaui/Services/Tag/RegistrationStep.cs)), `InitialDomain`, `InitialApiKey`, `InitialApiSecret`, `InitialDefaultXmppConnectivity`, `SelectedCountry`, `PhoneNumber`, `EMail`, `FirstName`, `LastName`, `FriendlyName`, `Purpose`, `IsTest`, `TestOtpTimestamp` |
| Account | `Domain`, `ApiKey`, `ApiSecret`, `DefaultXmppConnectivity`, `Account`, `XmppPasswordHash`, `XmppPasswordHashMethod` |
| Identity | `LegalIdentity`, `IdentityApplication`, `NrReviews`, `LastIdentityUpdate`, `TrustProviderId` |
| Server components | `LegalJid`, `RegistryJid`, `ProvisioningJid`, `HttpFileUploadJid` and `HttpFileUploadMaxSize`, `LogJid`, `EDalerJid`, `NeuroFeaturesJid`, `PubSubJid`, `SupportsPushNotification` |
| Local security | `LocalPasswordHash`, `HasLocalPassword`, `IsNumericPassword`, `AuthenticationMethod` |
| Preferences and flags | `Theme`, `HasWallet`, `HasThing`, `HasBetaFeatures`, `HasContractReferences`, `HasContractTemplateReferences`, `HasContractTokenCreationTemplatesReferences`, `LastEDalerBalanceDecimal`, `LastEDalerBalanceUpdate`, `IsMerchant` |

Most setters are private. Change the profile through its methods, such as `SetDomain`, `SetAccount`, `SetLegalIdentity`, `SetIdentityApplication`, `SetPurpose`, `GoToStep` or `ClearAll`, so that related fields change together.

### Questions it answers

| Method | Returns `true` when |
| --- | --- |
| `IsComplete()` | The profile has a legal identity and a local password. The loading page uses this to choose between onboarding and the main page. |
| `IsCompleteOrWaitingForValidation()` | The step is at least `CreateAccount`, or the legal identity has been created and waits for approval. |
| `ShouldCreateClient()` | There is enough account information for `XmppService` to create an XMPP client. |
| `NeedsUpdating()`, `LegalIdentityNeedsUpdating()`, `LegalIdentityNeedsRefreshing()` | Server components or the legal identity should be fetched again. |

It also hashes and scores passwords (`ComputePasswordHash`, `ValidatePasswordStrength`, `CalculatePasswordScore`).

### Events

| Event | Raised |
| --- | --- |
| `StepChanged` | When `Step` changes, unless the change suppresses it. |
| `Changed` | For each property that changes, with the property name, on the thread that changed it. |
| `OnPropertiesChanged` | After the profile has been saved, when `IsDirty` is reset. Not on each change. |

`StepChanged` and `Changed` keep only weak references to their handlers. Subscribe with a method on an object that stays alive, not with a lambda that nothing else references, or the handler can be collected and stop firing. Marshal to the main thread before touching bound properties.

### Saving

Every change sets `IsDirty`. While a session is active, `App` saves the profile shortly after the first change and on a slow timer as a safety net, and it always saves when the session stops. To save at once, call `ForceSaveAsync()` on `App.Current`. Details are in [architecture.md](architecture.md#the-profile).

## Adding a service

The steps below add a small loadable service. A service that needs no start or stop skips step 2's `Load` and `Unload` and step 5.

1. Create a folder under `NeuroAccessMaui/Services/` and define the interface there. Derive it from `ILoadableService` if the service must start and stop with the app. `[DefaultImplementation]` is optional, but it lets a request for the implementation type return the registered singleton.

	```csharp
	using Waher.Runtime.Inventory;

	namespace NeuroAccessMaui.Services.Bookmarks
	{
		/// <summary>
		/// Keeps the links the user has saved for later.
		/// </summary>
		[DefaultImplementation(typeof(BookmarkService))]
		public interface IBookmarkService : ILoadableService
		{
			/// <summary>
			/// Checks whether a link has been saved.
			/// </summary>
			/// <param name="Url">The link.</param>
			/// <returns><c>true</c> if the link has been saved.</returns>
			bool Contains(string Url);

			/// <summary>
			/// Saves a link.
			/// </summary>
			/// <param name="Url">The link to save.</param>
			Task AddAsync(string Url);
		}
	}
	```

2. Implement it. Derive from `LoadableService` and use the `BeginLoad`, `EndLoad`, `BeginUnload` and `EndUnload` helpers. The `Bookmark` class is the persisted example from [architecture.md](architecture.md#the-object-database).

	```csharp
	using Waher.Persistence;
	using Waher.Runtime.Inventory;

	namespace NeuroAccessMaui.Services.Bookmarks
	{
		/// <summary>
		/// Default implementation of <see cref="IBookmarkService"/>.
		/// </summary>
		[Singleton]
		internal sealed class BookmarkService : LoadableService, IBookmarkService
		{
			private readonly HashSet<string> urls = new();
			private readonly object syncObject = new();

			/// <inheritdoc/>
			public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
			{
				if (!this.BeginLoad(IsResuming, CancellationToken))
					return;

				try
				{
					IEnumerable<Bookmark> Stored = await Database.Find<Bookmark>();

					lock (this.syncObject)
					{
						foreach (Bookmark Item in Stored)
							this.urls.Add(Item.Url);
					}

					this.EndLoad(true);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					this.EndLoad(false);
				}
			}

			/// <inheritdoc/>
			public override Task Unload()
			{
				if (this.BeginUnload())
				{
					lock (this.syncObject)
						this.urls.Clear();

					this.EndUnload();
				}

				return Task.CompletedTask;
			}

			/// <inheritdoc/>
			public bool Contains(string Url)
			{
				lock (this.syncObject)
					return this.urls.Contains(Url);
			}

			/// <inheritdoc/>
			public async Task AddAsync(string Url)
			{
				lock (this.syncObject)
				{
					if (!this.urls.Add(Url))
						return;
				}

				await ServiceRef.StorageService.Insert(new Bookmark() { Url = Url });
			}
		}
	}
	```

3. Register it in `RegisterTypes` in [`PageAppExtension`](../NeuroAccessMaui/UI/PageAppExtension.cs). Without this line the service only resolves through the type inventory fallback, which logs a warning and returns `null` while the inventory is still being built.

	```csharp
	Builder.Services.AddSingleton<IBookmarkService, BookmarkService>();
	```

4. If the service will be used in many places, add a property to `ServiceRef` that follows the existing pattern:

	```csharp
	private static IBookmarkService? bookmarkService;

	/// <summary>
	/// Keeps the links the user has saved for later.
	/// </summary>
	public static IBookmarkService BookmarkService
	{
		get
		{
			bookmarkService ??= Provider.GetRequiredService<IBookmarkService>();
			return bookmarkService;
		}
	}
	```

	Otherwise inject `IBookmarkService` through constructors.

5. If the service is loadable, have `App` load and unload it. Choose the phase:

	- Core, in `InitializeProcessAsync` and `CleanupProcessAsync`, for services that only need local state. The example belongs here, since it only reads the database.
	- Session, in `ActivateAsync` and `CleanupActivationAsync`, for services that need the network or `XmppService`.

	Keep the service in a static field like the others, load it after the services it depends on, and unload it in reverse order:

	```csharp
	bookmarks = ServiceRef.BookmarkService;
	await bookmarks.Load(false, CancellationToken.None);
	```

	```csharp
	if (bookmarks is not null) await TryCleanupAsync(bookmarks.Unload, Errors);
	bookmarks = null;
	```

6. Add the service to the catalog in this guide.

When a service calls another service from `Load`, make sure the other service is loaded first. If the logic can be written without MAUI, put it in a class with no MAUI dependencies so it can be linked into a test project, as described in [AGENTS.md](../AGENTS.md).

## Related docs

- [Architecture](architecture.md)
- [Navigation](navigation.md)
- [Notifications](notifications.md)
- [Onboarding](onboarding.md)
- [KYC](kyc.md)
- [Branding](branding.md)
- [Animations](animations.md)
- [Getting started](getting-started.md)
- [Contributing](contributing.md)
- [AGENTS.md](../AGENTS.md), the code style and app patterns
