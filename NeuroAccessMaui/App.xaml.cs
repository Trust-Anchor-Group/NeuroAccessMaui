using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Internals;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Startup;
using NeuroAccessMaui.Services.Theme;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Services.Xml;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
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
using NeuroAccessMaui.UI.Pages.Main.NfcTester;
using NeuroAccessMaui.UI.Pages.Main.QR;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Pages.Main.XmppForm;
using NeuroAccessMaui.UI.Pages.Notifications;
using NeuroAccessMaui.UI.Pages.Onboarding;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionContract;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature;
using NeuroAccessMaui.UI.Pages.Signatures.ClientSignature;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
using NeuroAccessMaui.UI.Pages.Startup;
using NeuroAccessMaui.UI.Pages.Things.CanControl;
using NeuroAccessMaui.UI.Pages.Things.CanRead;
using NeuroAccessMaui.UI.Pages.Things.IsFriend;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Things.ReadSensor;
using NeuroAccessMaui.UI.Pages.Things.ViewClaimThing;
using NeuroAccessMaui.UI.Pages.Things.ViewThing;
using NeuroAccessMaui.UI.Pages.Utility;
using NeuroAccessMaui.UI.Pages.Utility.Images;
using NeuroAccessMaui.UI.Pages.Wallet.AccountEvent;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.EDalerReceived;
using NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout;
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
using NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory;
using Waher.Content;
using Waher.Events;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.P2P.E2E;
using Waher.Persistence;
using Waher.Runtime.Text;
using Waher.Security.LoginMonitor;
using Waher.Layout;
using NeuroAccess.Nfc.TravelDocuments.PACE;
using Waher.Security;
using Waher.Security.EllipticCurves;
using Waher.Security.EllipticCurves.E2EE;
using Waher.Security.E2EE;

namespace NeuroAccessMaui
{
    /// <summary>
    /// Represents an instance of the Neuro-Access app.
    /// </summary>
    public partial class App : Application, IDisposableAsync
    {
        #region Fields

        /// <summary>Describes the currently admitted application transition.</summary>
        private enum LifecycleState { Stopped, Starting, Active, Stopping, Failed, Terminated }

        private static readonly SemaphoreSlim lifecycleSemaphore = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim autoSaveSemaphore = new SemaphoreSlim(1, 1);
        private static readonly AsyncLocal<bool> inCoreInitialization = new AsyncLocal<bool>();
        private static volatile Task? initialization;
        private static Task transition = Task.CompletedTask;
        private static Task? foregroundActivation;
        private static Task? deferredWork;
        private static CancellationTokenSource? deferredCancellation;
        private static volatile bool previousRunReported;
        private static volatile bool providerThemeRefreshed;
        private static TaskCompletionSource<bool> servicesReady = CreateServicesReadySignal();
        private static CancellationTokenSource? activationCancellation;
        private static LifecycleState lifecycleState;
        private static bool sessionAdmitted;
        private static volatile Timer? autoSaveTimer;
        private static int autoSaveScheduled;
        private static App? appInstance;
        private static Exception? startupFailure;
        private static Exception? cleanupFailure;
        private static IStorageService? storage;
        private static ICryptoService? crypto;
        private static ILogService? logging;
        private static ITagProfile? profile;
        private static ILoadableService? attachments, internetCache, notifications, push;
        private static ILoadableService? network, contracts, thingRegistry, wallet;
        private static IXmppService? xmpp;

        private static TaskCompletionSource<bool> CreateServicesReadySignal() =>
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current application instance.
        /// </summary>
        public static new App? Current => appInstance;

        /// <summary>
        /// Gets the retained process initialization outcome, independent of foreground activation.
        /// </summary>
        public static Task Initialization => initialization
            ?? Task.FromException(new InvalidOperationException("Application initialization has not started."));

        /// <summary>
        /// Supported languages.
        /// </summary>
        public static readonly LanguageInfo[] SupportedLanguages =
        [
            new("en", "English"),
            new("sv", "Svenska"),
            new("es", "Español"),
            new("fr", "Français"),
            new("de", "Deutsch"),
            new("da", "Dansk"),
            new("no", "Norsk"),
            new("fi", "Suomi"),
            new("sr", "Српски"),
            new("pt", "Português"),
            new("ro", "Română"),
            new("ru", "Русский")
        ];

        /// <summary>
        /// Gets the selected language.
        /// </summary>
        public static LanguageInfo SelectedLanguage
        {
            get
            {
				string? LanguageName = null;
				try
				{
					LanguageName = Preferences.Get("user_selected_language", null);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}

				LanguageInfo SelectedLanguage = SupportedLanguages[0];

                if (LanguageName is null)
                {
                    // Get the system's two-letter ISO language name
                    string SystemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                    // Try to find a supported language matching the system language
                    LanguageInfo? SystemLanguageInfo = SupportedLanguages
                        .FirstOrDefault(el =>
                            string.Equals(el.TwoLetterISOLanguageName, SystemLanguage, StringComparison.OrdinalIgnoreCase));

                    if (SystemLanguageInfo is not null)
                    {
                        SelectedLanguage = SystemLanguageInfo;
                    }

                    // Save the selected language for next time
                    Preferences.Set("user_selected_language", SelectedLanguage.TwoLetterISOLanguageName);
                }
                else
                {
                    SelectedLanguage = SupportedLanguages.FirstOrDefault(
                        el => string.Equals(el.TwoLetterISOLanguageName, LanguageName, StringComparison.OrdinalIgnoreCase),
                        SelectedLanguage);

                    // Ensure stored value matches a real supported language
                    if (!string.Equals(SelectedLanguage.TwoLetterISOLanguageName, LanguageName, StringComparison.OrdinalIgnoreCase))
                    {
                        Preferences.Set("user_selected_language", SelectedLanguage.TwoLetterISOLanguageName);
                    }
                }

                return SelectedLanguage;
            }
        }

        /// <summary>
        /// Indicates whether the application is onboarded.
        /// </summary>
        public static bool IsOnboarded => Shell.Current is not null;

        /// <summary>
        /// Define a static event to notify when the app enters the foreground.
        /// </summary>
        public static event EventHandler? AppActivated;



        #endregion

        #region Constructor

        /// <summary>Creates the application UI and starts process startup.</summary>
        /// <remarks>
        /// MAUI creates the application on every process start, including starts for a push message or another background
        /// component. Core initialization (local storage, profile and theme) and the session (network, XMPP, contracts) both
        /// start here, so a process started in the background keeps its session. Foreground work starts when a window is
        /// created or resumed, see <see cref="AppWindow"/>. The Waher type inventory is already being built in the background,
        /// see <see cref="RuntimeTypeInventory"/>.
        /// </remarks>
        public App()
        {
            appInstance = this;
            this.InitializeComponent();
            _ = ObserveLifecycleAsync(() => this.ResumeAsync(isBackground: true));
        }

        /// <summary>
        /// Override default window creation to use CustomShell as root page.
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            SyncMotionSettings();
            if (this.Windows.Any())
                return this.Windows[0];
            Page Root = startupFailure is null
                ? ServiceRef.Provider.GetRequiredService<CustomShell>()
                : new BootstrapErrorPage(startupFailure.Message, startupFailure.StackTrace ?? string.Empty);
            if (Root.Window is Window PreviousWindow)
                PreviousWindow.Page = null;
            return new AppWindow(this, Root);
        }
        #endregion

        #region Window lifecycle bridge

        /// <summary>
        /// Invoked when the MAUI window has been created, which starts or joins the first foreground activation.
        /// </summary>
        internal Task HandleWindowCreatedAsync() => ReferenceEquals(this, Current) ? this.ResumeAsync(isBackground: false) : Task.CompletedTask;

        /// <summary>
        /// Invoked when the MAUI window enters the stopped state.
        /// </summary>
        internal Task HandleWindowStoppedAsync() => ReferenceEquals(this, Current) ? this.OnBackgroundSleep() : Task.CompletedTask;

        /// <summary>
        /// Invoked when the MAUI window resumes after being stopped.
        /// </summary>
        internal Task HandleWindowResumedAsync() => ReferenceEquals(this, Current) ? this.ResumeAsync(isBackground: false) : Task.CompletedTask;

        /// <summary>
        /// Invoked when the MAUI window is being destroyed.
        /// </summary>
        internal Task HandleWindowDestroyingAsync()
        {
#if ANDROID
            return Task.CompletedTask;
#else
            return ReferenceEquals(this, Current) ? RequestStopAsync(true, false) : Task.CompletedTask;
#endif
        }

        #endregion

        #region Initialization

        private static async Task InitializeProcessAsync()
        {
            await lifecycleSemaphore.WaitAsync();
            try
            {
                // Code that initialization runs must not wait for initialization to complete, see WaitForCoreAsync.
                inCoreInitialization.Value = true;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LocalizationManager.Current.CurrentCulture = SelectedLanguage;
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                    TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                });

                // Persistence, content encoding and scripting resolve types through the inventory.
                await RuntimeTypeInventory.WaitUntilReadyAsync();
                // Register exceptions as alerts.
                Log.RegisterAlertExceptionType(true,
                    typeof(OutOfMemoryException),
                    typeof(StackOverflowException),
                    typeof(AccessViolationException),
                    typeof(InsufficientMemoryException));

				E2eEndpoint.SetCiphers([typeof(Edwards448Endpoint)], false);

                IXmlSchemaValidationService Xml = ServiceRef.Provider.GetRequiredService<IXmlSchemaValidationService>();
                Xml.RegisterSchema(Constants.Schemes.NeuroAccessBrandingV1, Constants.Schemes.BrandingDescriptorV1File);
                Xml.RegisterSchema(Constants.Schemes.NeuroAccessBrandingV2Url, Constants.Schemes.BrandingDescriptorV2File);
                Xml.RegisterSchema(Constants.Schemes.NeuroAccessBrandingV2, Constants.Schemes.BrandingDescriptorV2File);
                Xml.RegisterSchema(Constants.Schemes.NeuroAccessKycProcessUrl, Constants.Schemes.NeuroAccessKycProcessFile);
                Xml.RegisterSchema(Constants.Schemes.KYCProcess, Constants.Schemes.NeuroAccessKycProcessFile);
                await MainThread.InvokeOnMainThreadAsync(RegisterRoutes);
                await ServiceRef.PlatformSpecific.InitializeDeviceIdAsync();
                crypto = ServiceRef.CryptoService;
                await crypto.InitializeJwtFactory();
                logging = ServiceRef.LogService;
                await logging.Load(false, CancellationToken.None);
                storage = ServiceRef.StorageService;
                await storage.Init(CancellationToken.None);
                await CreateOrRestoreConfigurationAsync();
                // The theme only needs the local profile, so it is applied before the session starts. Applying it again
                // later does nothing unless it changed.
                ServiceRef.ThemeService.SetTheme(ServiceRef.TagProfile.Theme);
                attachments = ServiceRef.AttachmentCacheService;
                await attachments.Load(false, CancellationToken.None);
                internetCache = ServiceRef.InternetCacheService;
                await internetCache.Load(false, CancellationToken.None);
                notifications = ServiceRef.NotificationService;
                await notifications.Load(false, CancellationToken.None);
                push = ServiceRef.PushNotificationService;
                await push.Load(false, CancellationToken.None);
            }
            catch (Exception Ex)
            {
                try { await CleanupProcessAsync(); }
                catch (Exception CleanupError) { Ex.Data["ProcessCleanupFailure"] = CleanupError; }
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (lifecycleState is not LifecycleState.Terminated and not LifecycleState.Stopping)
                        lifecycleState = LifecycleState.Failed;
                    servicesReady.TrySetException(Ex);
                    _ = servicesReady.Task.Exception;
                    ReportLifecycleFailure(Ex);
                });
                throw;
            }
            finally { lifecycleSemaphore.Release(); }
        }

        /// <summary>Waits for the activation current when this call is admitted on the UI thread.</summary>
        /// <param name="CancellationToken">Cancels only this wait, never shared initialization or activation.</param>
        /// <returns>Completion when that same activation is ready; failure or cancellation otherwise.</returns>
        public static Task WaitForServicesAsync(CancellationToken CancellationToken = default) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Task Attempt = servicesReady.Task;
                await Attempt.WaitAsync(CancellationToken);
                CancellationToken.ThrowIfCancellationRequested();
                if (!ReferenceEquals(Attempt, servicesReady.Task) || lifecycleState != LifecycleState.Active)
                    throw new OperationCanceledException("The requested activation is no longer active.");
            });

        /// <summary>Waits until durable process initialization (local storage, profile and local services) has completed.</summary>
        /// <param name="CancellationToken">Cancels only this wait, never the initialization itself.</param>
        /// <returns>Completion when initialization has succeeded; its failure or cancellation otherwise.</returns>
        /// <remarks>
        /// Starts initialization if no application instance has started it yet. Returns at once when called from code that
        /// initialization itself runs, so that code can never wait for its own completion.
        /// </remarks>
        public static Task WaitForCoreAsync(CancellationToken CancellationToken = default)
        {
            Task? Core = initialization;
            if (Core is not null && Core.IsCompletedSuccessfully)
                return Task.CompletedTask;
            if (inCoreInitialization.Value)
                return Task.CompletedTask;
            return WaitForCoreSlowAsync(CancellationToken);
        }

        private static async Task WaitForCoreSlowAsync(CancellationToken CancellationToken)
        {
            Task? Core = null;
            await MainThread.InvokeOnMainThreadAsync(() => { Core = StartCore(); });
            await Core!.WaitAsync(CancellationToken);
        }

        /// <summary>Starts durable process initialization unless it has already started. Must run on the main thread.</summary>
        /// <returns>The retained initialization task.</returns>
        private static Task StartCore() => initialization ??= Task.Run(InitializeProcessAsync);

        /// <summary>Observes an asynchronous platform callback without allowing its exception to escape.</summary>
        /// <param name="Callback">Lifecycle callback to invoke immediately.</param>
        /// <returns>Completion after observing the callback outcome.</returns>
        /// <remarks>
        /// Startup, activation and cleanup report their own failures, which shows the bootstrap error page. Any other
        /// exception that reaches this point, such as one thrown by an <see cref="AppActivated"/> subscriber or by foreground
        /// UI work, is only logged.
        /// </remarks>
        internal static async Task ObserveLifecycleAsync(Func<Task> Callback)
        {
            try { await Callback(); }
            catch (Exception Ex) when (IsLifecycleException(Ex)) { }
            catch (Exception Ex)
            {
                try { ServiceRef.LogService.LogException(Ex); }
                catch (Exception ReportingError) { System.Diagnostics.Debug.WriteLine(ReportingError); }
            }
        }

        /// <summary>Identifies cancellation or failure already reported by the lifecycle owner.</summary>
        /// <param name="Error">The consumer's observed failure.</param>
        /// <returns>Whether duplicate diagnostics should be suppressed.</returns>
        internal static bool IsLifecycleException(Exception Error) =>
            Error is OperationCanceledException || Error.Data.Contains("ApplicationLifecycleFailure");

        private static void ReportLifecycleFailure(Exception Ex)
        {
            if (Ex.Data.Contains("ApplicationLifecycleFailure"))
                return;
            Ex.Data["ApplicationLifecycleFailure"] = true;
            startupFailure = Ex;
            try
            {
                ServiceRef.LogService.SaveExceptionDump("Startup", Ex.ToString());
                Current?.DisplayBootstrapErrorPage(Ex.Message, Ex.StackTrace ?? string.Empty);
            }
            catch (Exception ReportingError) { System.Diagnostics.Debug.WriteLine(ReportingError); }
        }

        #endregion

        #region Startup / Resume

        /// <summary>Starts or joins an activation requested by background processing.</summary>
        /// <returns>The shared activation outcome.</returns>
        public Task OnBackgroundStart() => this.ResumeAsync(true);

        /// <summary>Starts or joins the current activation, applying foreground UI when requested.</summary>
        /// <param name="isBackground">Whether foreground presentation should be deferred.</param>
        /// <returns>The activation outcome, including required foreground setup.</returns>
        /// <remarks>
        /// <see cref="Application.OnStart"/>, <see cref="Application.OnResume"/> and <see cref="Application.OnSleep"/> are not
        /// overridden: MAUI raises them for the same window transitions that <see cref="AppWindow"/> forwards, and handling
        /// both ran every transition twice. Window creation and every resume join the same activation, and its foreground
        /// work runs once.
        /// </remarks>
        public Task ResumeAsync(bool isBackground) => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!ReferenceEquals(this, Current))
                return;
            Task Activation = AdmitActivation();
            Task Attempt = servicesReady.Task;
            await Activation;
            if (!ReferenceEquals(Attempt, servicesReady.Task) || lifecycleState != LifecycleState.Active)
                throw new OperationCanceledException();
            if (isBackground || ReferenceEquals(foregroundActivation, Attempt))
                return;
            foregroundActivation = Attempt;
            this.ApplyForegroundState();
        });

        /// <summary>Applies the foreground UI state once per activation. Runs on the main thread.</summary>
        private void ApplyForegroundState()
        {
            // Does nothing unless the theme changed, for example when it follows the system theme.
            ServiceRef.ThemeService.SetTheme(ServiceRef.TagProfile.Theme);
            Window? Window = this.Windows.FirstOrDefault();
            if (Window?.Page is BootstrapErrorPage)
                Window.Page = ServiceRef.Provider.GetRequiredService<CustomShell>();
            if (Window?.Page is CustomShell Shell)
            {
                _ = Shell.InitializeLoadingPageAsync();

                // On a cold start the loading page schedules deferred work once it has chosen the first page. On a resume
                // the first page is already shown, so work that did not run to completion is scheduled again here.
                if (Shell.CurrentPage is not LoadingPage)
                    ScheduleDeferredWork();
            }
        }

        private static Task AdmitActivation()
        {
            if (lifecycleState == LifecycleState.Terminated)
                return Task.FromException(new ObjectDisposedException(nameof(App)));
            if (cleanupFailure is not null)
                return Task.FromException(cleanupFailure);
            if (initialization?.IsFaulted == true || initialization?.IsCanceled == true)
                return initialization;
            if (lifecycleState is LifecycleState.Starting or LifecycleState.Active)
                return transition;
            // Core initialization can have been started without a session, by WaitForCoreAsync, so resumption is tracked
            // separately.
            bool IsResuming = sessionAdmitted;
            if (IsResuming || servicesReady.Task.IsCompleted)
            {
                servicesReady.TrySetCanceled();
                servicesReady = CreateServicesReadySignal();
            }
            sessionAdmitted = true;
            StartCore();
            activationCancellation = new CancellationTokenSource();
            lifecycleState = LifecycleState.Starting;
            TaskCompletionSource<bool> Signal = servicesReady;
            CancellationTokenSource Cancellation = activationCancellation;
            return transition = Task.Run(() => ActivateAsync(Signal, Cancellation, IsResuming));
        }

        private static async Task ActivateAsync(TaskCompletionSource<bool> Signal, CancellationTokenSource Cancellation, bool IsResuming)
        {
            CancellationToken Token = Cancellation.Token;
            try
            {
                await Initialization.WaitAsync(Token);
                await lifecycleSemaphore.WaitAsync(Token);
                try
                {
                    Token.ThrowIfCancellationRequested();
                    await CleanupActivationAsync(false);
                    if (cleanupFailure is not null)
                        throw cleanupFailure;
                    try
                    {
                        network = ServiceRef.NetworkService;
                        await network.Load(IsResuming, Token);
                        xmpp = ServiceRef.XmppService;
                        await xmpp.Load(IsResuming, Token);
                        contracts = ServiceRef.ContractOrchestratorService;
                        await contracts.Load(IsResuming, Token);
                        thingRegistry = ServiceRef.ThingRegistryOrchestratorService;
                        await thingRegistry.Load(IsResuming, Token);
                        wallet = ServiceRef.NeuroWalletOrchestratorService;
                        await wallet.Load(IsResuming, Token);
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            Token.ThrowIfCancellationRequested();
                            if (!ReferenceEquals(Signal, servicesReady))
                                throw new OperationCanceledException();
                            lifecycleState = LifecycleState.Active;
                            startupFailure = null;
                            // Profile changes bring the next save forward (see TagProfile_Changed); the period is a safety net.
                            Interlocked.Exchange(ref autoSaveScheduled, 0);
                            autoSaveTimer = new Timer(SaveFromTimer, Signal.Task,
                                Constants.Intervals.AutoSave.Multiply(4), Constants.Intervals.AutoSaveFallback);
                            Signal.TrySetResult(true);
                        });
                    }
                    catch (Exception Ex)
                    {
                        try { await CleanupActivationAsync(false); }
                        catch (Exception CleanupError)
                        {
                            Ex.Data["ActivationCleanupFailure"] = CleanupError;
                            if (Ex is OperationCanceledException)
                                throw;
                        }
                        throw;
                    }
                }
                finally { lifecycleSemaphore.Release(); }
            }
            catch (OperationCanceledException) when (Token.IsCancellationRequested) { Signal.TrySetCanceled(); throw; }
            catch (Exception Ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Signal.TrySetException(Ex);
                    _ = Signal.Task.Exception;
                    if (ReferenceEquals(Signal, servicesReady) && lifecycleState != LifecycleState.Terminated)
                        lifecycleState = LifecycleState.Failed;
                    ReportLifecycleFailure(Ex);
                });
                throw;
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (ReferenceEquals(activationCancellation, Cancellation) && lifecycleState == LifecycleState.Active)
                        return;
                    if (ReferenceEquals(activationCancellation, Cancellation))
                        activationCancellation = null;
                    Cancellation.Dispose();
                });
            }
        }

        private static async Task ObserveOptionalAsync(Func<Task> Callback)
        {
            try { await Callback(); }
            catch (OperationCanceledException) { }
            catch (Exception Ex)
            {
                try { Log.Exception(Ex); }
                catch (Exception ReportingError) { System.Diagnostics.Debug.WriteLine(ReportingError); }
            }
        }

        /// <summary>
        /// Schedules startup work that the first page does not need, to run a few seconds after it has been shown.
        /// </summary>
        /// <remarks>
        /// Runs the crash report of the previous run and the background refresh of provider branding. Each step runs to
        /// completion once per process, except that a branding refresh that failed for a transient reason is tried again at
        /// the next foreground activation. Going to the background cancels pending work, and the next foreground activation
        /// schedules the remaining steps again. Safe to call from any thread and any number of times.
        /// </remarks>
        internal static void ScheduleDeferredWork()
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(ScheduleDeferredWork);
                return;
            }

            if (lifecycleState != LifecycleState.Active)
                return;
            if (previousRunReported && providerThemeRefreshed)
                return;

            Task? Previous = deferredWork;
            bool PreviousRunning = Previous is not null && !Previous.IsCompleted;
            if (PreviousRunning && deferredCancellation?.IsCancellationRequested != true)
                return;

            // A new source each time, so work cancelled by going to the background can be scheduled again. A cancelled run can
            // still be finishing a step that does not observe cancellation, such as a network request. The new run waits for it
            // instead of being skipped, and the source of that run is left to it rather than disposed under it.
            if (!PreviousRunning)
                deferredCancellation?.Dispose();
            CancellationTokenSource Cancellation = new CancellationTokenSource();
            deferredCancellation = Cancellation;
            deferredWork = Task.Run(async () =>
            {
                if (PreviousRunning)
                    await Task.WhenAny(Previous!);
                await RunDeferredWorkAsync(Cancellation.Token);
            });
        }

        private static async Task RunDeferredWorkAsync(CancellationToken Token)
        {
            try
            {
                // Leave the first frames and the first user interactions to the UI.
                await Task.Delay(Constants.Intervals.DeferredStartupWork, Token);

                if (!previousRunReported)
                {
                    await ObserveOptionalAsync(SendErrorReportFromPreviousRunAsync);
                    previousRunReported = true;
                }

                Token.ThrowIfCancellationRequested();

                // Provider branding exists only for onboarded profiles. Until then this step stays pending, so it runs once
                // onboarding has finished and the loading page has shown the main page.
                if (!providerThemeRefreshed && ServiceRef.TagProfile.IsComplete())
                {
                    ThemeApplyOutcome Outcome = ThemeApplyOutcome.FailedTransient;
                    await ObserveOptionalAsync(async () =>
                        Outcome = await ServiceRef.ThemeService.ApplyProviderThemeAsync(ThemeFetchPolicy.BackgroundRefresh, Token));
                    Token.ThrowIfCancellationRequested();

                    // The loading page no longer waits for branding. A transient failure, typically a start without network
                    // and without cached branding, is therefore retried at the next foreground activation.
                    if (Outcome != ThemeApplyOutcome.FailedTransient)
                        providerThemeRefreshed = true;
                }
            }
            catch (OperationCanceledException) when (Token.IsCancellationRequested)
            {
                // The app left the foreground or is shutting down; the next activation schedules the remaining work.
            }
        }

		internal static void RegisterRoutes()
		{
			Routing.RegisterRoute(nameof(LoadingPage), typeof(LoadingPage));

			Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
			Routing.RegisterRoute(nameof(OnboardingPage), typeof(OnboardingPage));

			// Applications:
			Routing.RegisterRoute(nameof(ApplicationsPage), typeof(ApplicationsPage));
			Routing.RegisterRoute(nameof(KycApplicationStatusPage), typeof(KycApplicationStatusPage));
			Routing.RegisterRoute(nameof(KycProcessPage), typeof(KycProcessPage));
			Routing.RegisterRoute(nameof(KycDocumentMrzScannerPage), typeof(KycDocumentMrzScannerPage));
			Routing.RegisterRoute(nameof(KycProfilePhotoCameraPage), typeof(KycProfilePhotoCameraPage));
			Routing.RegisterRoute(nameof(KycTravelDocumentPage), typeof(KycTravelDocumentPage));

			// Contacts
			Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
			Routing.RegisterRoute(nameof(MyContactsPage), typeof(MyContactsPage));

			// Contracts
			Routing.RegisterRoute(nameof(MyContractsPage), typeof(MyContractsPage));
			Routing.RegisterRoute(nameof(NewContractPage), typeof(NewContractPage));
			Routing.RegisterRoute(nameof(ViewContractPage), typeof(ViewContractPage));

			// Identity
			Routing.RegisterRoute(nameof(TransferIdentityPage), typeof(TransferIdentityPage));
			Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));

			// Main
			Routing.RegisterRoute(nameof(CalculatorPage), typeof(CalculatorPage));
			Routing.RegisterRoute(nameof(ChangePasswordPage), typeof(ChangePasswordPage));
			Routing.RegisterRoute(nameof(DurationPage), typeof(DurationPage));
			Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));
			Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
			Routing.RegisterRoute(nameof(VerifyCodePage), typeof(VerifyCodePage));
			Routing.RegisterRoute(nameof(XmppFormPage), typeof(XmppFormPage));
			Routing.RegisterRoute(nameof(AppsPage), typeof(AppsPage));
            Routing.RegisterRoute(nameof(NfcTesterPage), typeof(NfcTesterPage));

			//Notifications
			Routing.RegisterRoute(nameof(NotificationsPage), typeof(NotificationsPage));

			// Petitions
			Routing.RegisterRoute(nameof(PetitionContractPage), typeof(PetitionContractPage));
			Routing.RegisterRoute(nameof(PetitionIdentityPage), typeof(PetitionIdentityPage));
			Routing.RegisterRoute(nameof(PetitionPeerReviewPage), typeof(PetitionPeerReviewPage));
			Routing.RegisterRoute(nameof(PetitionSignaturePage), typeof(PetitionSignaturePage));

			// Signatures
			Routing.RegisterRoute(nameof(ClientSignaturePage), typeof(ClientSignaturePage));
			Routing.RegisterRoute(nameof(ServerSignaturePage), typeof(ServerSignaturePage));

			// Things
			Routing.RegisterRoute(nameof(CanControlPage), typeof(CanControlPage));
			Routing.RegisterRoute(nameof(CanReadPage), typeof(CanReadPage));
			Routing.RegisterRoute(nameof(IsFriendPage), typeof(IsFriendPage));
			Routing.RegisterRoute(nameof(MyThingsPage), typeof(MyThingsPage));
			Routing.RegisterRoute(nameof(ReadSensorPage), typeof(ReadSensorPage));
			Routing.RegisterRoute(nameof(ViewClaimThingPage), typeof(ViewClaimThingPage));
			Routing.RegisterRoute(nameof(ViewThingPage), typeof(ViewThingPage));

			// Wallet
			Routing.RegisterRoute(nameof(AccountEventPage), typeof(AccountEventPage));
			Routing.RegisterRoute(nameof(BuyEDalerPage), typeof(BuyEDalerPage));
			Routing.RegisterRoute(nameof(EDalerReceivedPage), typeof(EDalerReceivedPage));
			Routing.RegisterRoute(nameof(IssueEDalerPage), typeof(IssueEDalerPage));
			Routing.RegisterRoute(nameof(MachineReportPage), typeof(MachineReportPage));
			Routing.RegisterRoute(nameof(MachineVariablesPage), typeof(MachineVariablesPage));
			Routing.RegisterRoute(nameof(MyTokensPage), typeof(MyTokensPage));
			Routing.RegisterRoute(nameof(MyEDalerWalletPage), typeof(MyEDalerWalletPage));
			Routing.RegisterRoute(nameof(MyTokensPage), typeof(MyTokensPage));
			Routing.RegisterRoute(nameof(PaymentPage), typeof(PaymentPage));
			Routing.RegisterRoute(nameof(PaymentAcceptancePage), typeof(PaymentAcceptancePage));
			Routing.RegisterRoute(nameof(PendingPaymentPage), typeof(PendingPaymentPage));
			Routing.RegisterRoute(nameof(RequestPaymentPage), typeof(RequestPaymentPage));
			Routing.RegisterRoute(nameof(SellEDalerPage), typeof(SellEDalerPage));
			Routing.RegisterRoute(nameof(SendPaymentPage), typeof(SendPaymentPage));
			Routing.RegisterRoute(nameof(ServiceProvidersPage), typeof(ServiceProvidersPage));
			Routing.RegisterRoute(nameof(TokenDetailsPage), typeof(TokenDetailsPage));
			Routing.RegisterRoute(nameof(TokenEventsPage), typeof(TokenEventsPage));
			Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
			Routing.RegisterRoute(nameof(TransactionHistoryPage), typeof(TransactionHistoryPage));
			Routing.RegisterRoute(nameof(EmbeddedLayoutPage), typeof(EmbeddedLayoutPage));

			// Utility
			Routing.RegisterRoute(nameof(ImageCroppingPage), typeof(ImageCroppingPage));
		}

		#endregion

        #region Sleep / Shutdown

        /// <summary>Stops active services while retaining durable process initialization.</summary>
        /// <returns>The shared stop outcome.</returns>
        public Task OnBackgroundSleep() => RequestStopAsync(false, false);

        /// <summary>Terminates owned resources and then closes the application.</summary>
        /// <returns>Completion of process shutdown.</returns>
        internal static async Task StopAsync()
        {
            await RequestStopAsync(true, false);
            await MainThread.InvokeOnMainThreadAsync(() => ServiceRef.PlatformSpecific.CloseApplication());
        }

        private static Task RequestStopAsync(bool Terminate, bool InPanic) => MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (lifecycleState == LifecycleState.Terminated ||
                (!Terminate && lifecycleState is LifecycleState.Stopped or LifecycleState.Stopping))
                return transition;
            if (!Terminate && cleanupFailure is not null)
                return Task.FromException(cleanupFailure);
            deferredCancellation?.Cancel();
            activationCancellation?.Cancel();
            if (transition.IsCompleted)
            {
                activationCancellation?.Dispose();
                activationCancellation = null;
            }
            servicesReady.TrySetCanceled();
            servicesReady = CreateServicesReadySignal();
            servicesReady.TrySetCanceled();
            lifecycleState = Terminate ? LifecycleState.Terminated : LifecycleState.Stopping;
            TaskCompletionSource<bool> Signal = servicesReady;
            return transition = Task.Run(() => StopResourcesAsync(Signal, Terminate, InPanic));
        });

        private static async Task StopResourcesAsync(TaskCompletionSource<bool> Signal, bool Terminate, bool InPanic)
        {
            if (initialization is not null)
            {
                try { await initialization; }
                catch (Exception) { } // Durable initialization reports its own failure, even after cancellation.
            }
            await lifecycleSemaphore.WaitAsync();
            try
            {
                if (!await MainThread.InvokeOnMainThreadAsync(() => ReferenceEquals(Signal, servicesReady)))
                    return;
                List<Exception> Errors = new List<Exception>();
                await TryCleanupAsync(() => CleanupActivationAsync(InPanic), Errors);
                if (Terminate)
                    await TryCleanupAsync(CleanupProcessAsync, Errors);
                else
                    await TryCleanupAsync(() => SaveConfigurationAsync(null, false), Errors);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Errors.Count > 0)
                    {
                        cleanupFailure = new AggregateException("Application cleanup failed.", Errors);
                        ReportLifecycleFailure(cleanupFailure);
                    }
                    if (ReferenceEquals(Signal, servicesReady) && !Terminate)
                        lifecycleState = cleanupFailure is null && initialization?.IsCompletedSuccessfully == true
                            ? LifecycleState.Stopped : LifecycleState.Failed;
                });
                if (Errors.Count > 0)
                    throw cleanupFailure!;
            }
            finally { lifecycleSemaphore.Release(); }
        }

        private static async Task CleanupActivationAsync(bool InPanic)
        {
            List<Exception> Errors = new List<Exception>();
            Timer? Timer = autoSaveTimer;
            if (Timer is not null)
            {
                autoSaveTimer = null;
                await Timer.DisposeAsync();
            }
            await autoSaveSemaphore.WaitAsync();
            autoSaveSemaphore.Release();
            if (wallet is not null) await TryCleanupAsync(wallet.Unload, Errors);
            wallet = null;
            if (thingRegistry is not null) await TryCleanupAsync(thingRegistry.Unload, Errors);
            thingRegistry = null;
            if (contracts is not null) await TryCleanupAsync(contracts.Unload, Errors);
            contracts = null;
            if (xmpp is not null) await TryCleanupAsync(InPanic ? xmpp.UnloadFast : xmpp.Unload, Errors);
            xmpp = null;
            if (network is not null) await TryCleanupAsync(network.Unload, Errors);
            network = null;
            if (Errors.Count > 0)
            {
                Exception Failure = new AggregateException("Activation cleanup failed.", Errors);
                await MainThread.InvokeOnMainThreadAsync(() => cleanupFailure = Failure);
                throw Failure;
            }
        }

        private static async Task CleanupProcessAsync()
        {
            List<Exception> Errors = new List<Exception>();
            Task? Deferred = deferredWork;
            if (Deferred is not null)
            {
                await MainThread.InvokeOnMainThreadAsync(() => deferredCancellation?.Cancel());
                await TryCleanupAsync(() => Deferred, Errors);
            }
            if (push is not null) await TryCleanupAsync(push.Unload, Errors);
            push = null;
            if (notifications is not null) await TryCleanupAsync(notifications.Unload, Errors);
            notifications = null;
            if (internetCache is not null) await TryCleanupAsync(internetCache.Unload, Errors);
            internetCache = null;
            if (attachments is not null) await TryCleanupAsync(attachments.Unload, Errors);
            attachments = null;
            if (initialization?.IsCompletedSuccessfully == true)
                await TryCleanupAsync(() => SaveConfigurationAsync(null, true), Errors);
            if (storage is not null) await TryCleanupAsync(storage.Shutdown, Errors);
            storage = null;
            if (crypto is not null)
                await TryCleanupAsync(() => { crypto.Dispose(); return Task.CompletedTask; }, Errors);
            crypto = null;
            if (logging is not null)
                await TryCleanupAsync(logging.EndDebugLogSessionAsync, Errors);
            logging = null;
            await TryCleanupAsync(() => Log.TerminateAsync(), Errors);
            if (Errors.Count > 0)
                throw new AggregateException("Process cleanup failed.", Errors);
        }

        private static async Task TryCleanupAsync(Func<Task> Cleanup, List<Exception> Errors)
        {
            try { await Cleanup(); }
            catch (Exception Ex) { Errors.Add(Ex); }
        }

        #endregion

        #region AutoSave

        /// <summary>
        /// Brings the next automatic save forward when the profile changes.
        /// </summary>
        /// <remarks>
        /// Only the first change after a save schedules the next one, so a burst of changes is saved together shortly after it
        /// starts, and a steady stream of changes cannot postpone saving. Does nothing while no session is active; stopping
        /// the session saves the profile.
        /// </remarks>
        /// <param name="Sender">The profile.</param>
        /// <param name="e">The changed property.</param>
        private static void TagProfile_Changed(object? Sender, PropertyChangedEventArgs e)
        {
            Timer? Timer = autoSaveTimer;
            if (Timer is null)
                return;

            if (Interlocked.Exchange(ref autoSaveScheduled, 1) == 1)
                return;

            try
            {
                Timer.Change(Constants.Intervals.AutoSave, Constants.Intervals.AutoSaveFallback);
            }
            catch (ObjectDisposedException)
            {
                // The session stopped concurrently; stopping saves the profile.
            }
        }

        private static async void SaveFromTimer(object? State)
        {
            // Changes made from here on schedule a new save.
            Interlocked.Exchange(ref autoSaveScheduled, 0);

            // Most ticks of the safety-net timer find nothing to save; checking first avoids the main-thread round trips.
            ITagProfile? Profile = profile;
            if (Profile is null || !Profile.IsDirty)
                return;

            await ObserveOptionalAsync(() => SaveConfigurationAsync(State as Task, false));
        }

        private static async Task SaveConfigurationAsync(Task? Attempt, bool FinalSave)
        {
            await autoSaveSemaphore.WaitAsync();
            try
            {
                bool CanSave = await MainThread.InvokeOnMainThreadAsync(() => FinalSave ||
                    (lifecycleState != LifecycleState.Terminated &&
                    (Attempt is null || (ReferenceEquals(Attempt, servicesReady.Task) && lifecycleState == LifecycleState.Active))));
                if (!CanSave && Attempt is null)
                    throw new ObjectDisposedException(nameof(App));
                if (!CanSave || initialization?.IsCompletedSuccessfully != true || profile is null || storage is null)
                    return;
                TagConfiguration? Configuration = await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (!profile.IsDirty)
                        return null;
                    profile.ResetIsDirty();
                    try { return profile.ToConfiguration(); }
                    catch { profile.RestoreIsDirty(); throw; }
                });
                if (Configuration is null)
                    return;
                try
                {
                    if (string.IsNullOrEmpty(Configuration.ObjectId))
                        await storage.Insert(Configuration);
                    else
                        await storage.Update(Configuration);
                }
                catch
                {
                    await MainThread.InvokeOnMainThreadAsync(profile.RestoreIsDirty);
                    throw;
                }
            }
            finally { autoSaveSemaphore.Release(); }
        }

        /// <summary>Persists a successfully loaded profile and propagates write failures.</summary>
        /// <returns>Completion of the serialized save.</returns>
        public Task ForceSaveAsync() => SaveConfigurationAsync(null, false);

        #endregion

        #region Configuration

        private static async Task CreateOrRestoreConfigurationAsync()
        {
            IEnumerable<TagConfiguration> Configurations = await Database.Find<TagConfiguration>(0, 2);
            using IEnumerator<TagConfiguration> Records = Configurations.GetEnumerator();
            TagConfiguration? Configuration = Records.MoveNext()
                ? Records.Current ?? throw new InvalidDataException("A configuration record could not be read.")
                : null;
            if (Records.MoveNext())
                Log.Warning("Multiple configuration records exist; additional records were retained.");
            if (Configuration is null)
            {
                Configuration = new TagConfiguration();
                await ServiceRef.StorageService.Insert(Configuration);
            }

            await MainThread.InvokeOnMainThreadAsync(() => ServiceRef.TagProfile.FromConfiguration(Configuration));
            ITagProfile Profile = ServiceRef.TagProfile;
            if (!ReferenceEquals(profile, Profile))
            {
                // Subscribed once for the process. The profile keeps weak references to its handlers, so the handler is static.
                Profile.Changed += TagProfile_Changed;
                profile = Profile;
            }
        }

        #endregion

        #region Error Handling

        private static async void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Exception Ex = Log.UnnestException(e.Exception);
                e.SetObserved();
                if (IsLifecycleException(Ex))
                    return;
                await HandleUnhandledExceptionAsync(Ex, nameof(TaskScheduler_UnobservedTaskException), shutdown: false);
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(Ex);
            }
        }

        private static async void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                await HandleUnhandledExceptionAsync(e.ExceptionObject as Exception, nameof(CurrentDomain_UnhandledException), shutdown: true);
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(Ex);
            }
        }

        private static async Task HandleUnhandledExceptionAsync(Exception? ex, string title, bool shutdown)
        {
            if (ex is not null)
            {
                ServiceRef.LogService.SaveExceptionDump(title, ex.ToString());
                ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object?>("Operation", title));
            }

            if (shutdown)
                await RequestStopAsync(true, true);

#if DEBUG
            if (!shutdown)
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Page? AlertPage = Current?.Windows.FirstOrDefault()?.Page;
                    if (AlertPage is not null)
                        await AlertPage.DisplayAlert(title, ex?.ToString(), ServiceRef.Localizer[nameof(AppResources.Ok)]);
                });
#endif
        }

        private void DisplayBootstrapErrorPage(string Title, string StackTrace)
        {
            Window? Window = Current?.Windows.FirstOrDefault();
            if (Window is not null && Window.Page is not BootstrapErrorPage)
                Window.Page = new BootstrapErrorPage(Title, StackTrace);
        }

        private static async Task SendErrorReportFromPreviousRunAsync()
        {
            if (ServiceRef.LogService is not null)
            {
                string StackTrace = ServiceRef.LogService.LoadExceptionDump();
                if (!string.IsNullOrWhiteSpace(StackTrace))
                {
                    List<KeyValuePair<string, object?>> Tags =
                        [new KeyValuePair<string, object?>(Constants.XmppProperties.Jid, ServiceRef.XmppService.BareJid)];

                    KeyValuePair<string, object?>[]? AdditionalTags = ServiceRef.TagProfile.LegalIdentity?.GetTags();
                    if (AdditionalTags is not null)
                        Tags.AddRange(AdditionalTags);

                    StringBuilder Msg = new();
                    Msg.Append("Unhandled exception caused app to crash. ");
                    Msg.AppendLine("Below you can find the stack trace of the corresponding exception.");
                    Msg.AppendLine("\n\n");
                    Msg.AppendLine("```");
                    Msg.AppendLine(StackTrace);
                    Msg.AppendLine("```\n");

                    Log.Alert(Msg.ToString(), [.. Tags]);

                    try
                    {
                        await SendAlertAsync(StackTrace, "text/plain");
                    }
                    finally
                    {
                        ServiceRef.LogService.DeleteExceptionDump();
                    }
                }
            }
        }

        internal static async Task SendAlertAsync(string message, string contentType)
        {
            try
            {
                using HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using StringContent Content = new(message)
                {
                    Headers = { ContentType = MediaTypeHeaderValue.Parse(contentType) }
                };

                await Client.PostAsync("https://lab.tagroot.io/Alert.ws", Content);
            }
            catch (Exception Ex)
            {
                Log.Exception(Ex);
            }
        }

        public static async Task EvaluateDatabaseDiffAsync(string fileName, bool includeUnchanged, bool sendAsAlert)
        {
            StringBuilder Xml = new();
            using XmlDatabaseExport Output = new(Xml, true, 256);
            await ServiceRef.StorageService.Export(Output);

            string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName = Path.Combine(AppDataFolder, fileName);

            string CurrentState = Xml.ToString();
            string PrevState = File.Exists(fileName) ? File.ReadAllText(fileName) : string.Empty;

            EditScript<string> Script = Difference.AnalyzeRows(PrevState, CurrentState);
            StringBuilder Markdown = new();
            Markdown.AppendLine(CultureInfo.InvariantCulture, $"Database content changes (`{fileName}`):");

            foreach (Step<string> Step in Script.Steps)
            {
                Markdown.AppendLine();

                string Prefix = Step.Operation switch
                {
                    EditOperation.Insert => "+>\t",
                    EditOperation.Delete => "->\t",
                    _ => includeUnchanged ? ">\t" : string.Empty
                };

                if (string.IsNullOrEmpty(Prefix))
                    continue;

                Markdown.AppendLine(CultureInfo.InvariantCulture, $"{Prefix}```xml");
                foreach (string Row in Step.Symbols)
                    Markdown.AppendLine(CultureInfo.InvariantCulture, $"{Prefix}{Row}  ");
                Markdown.AppendLine(CultureInfo.InvariantCulture, $"{Prefix}```");
            }

            string DiffMsg = Markdown.ToString();

            if (sendAsAlert)
            {
                await SendAlertAsync(DiffMsg, "text/markdown");
            }

            File.WriteAllText(fileName, CurrentState);
            File.WriteAllText(fileName + ".diff.md", DiffMsg);
        }

        #endregion

        #region URL Handling

        public static void OpenUrlSync(string url)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!ServiceRef.TagProfile.IsComplete() && !url.StartsWith("obinfo", StringComparison.OrdinalIgnoreCase))
                {
                    await ServiceRef.UiService.DisplayAlert(
                        ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
                        ServiceRef.Localizer[nameof(AppResources.NotCompletedOnboardingError)]);
                    return;
                }

                await QrCode.OpenUrl(url);
            });
        }

        public static Task<bool> OpenUrlAsync(string url) => OpenUrlAsync(url, showErrorIfUnable: true);

        public static Task<bool> OpenUrlAsync(string url, bool showErrorIfUnable)
        {
            return QrCode.OpenUrl(url, showErrorIfUnable);
        }

        /// <summary>
        /// Opens a link that comes from a trusted source, such as the user's own server, without asking the user before it
        /// leaves the application.
        /// </summary>
        /// <param name="url">The link to open.</param>
        /// <returns>Whether the link was opened.</returns>
        public static Task<bool> OpenTrustedUrlAsync(string url)
        {
            return QrCode.OpenUrl(url, true, IsTrustedSource: true);
        }

        #endregion



        #region IDisposable Implementation

        /// <summary>Releases this UI instance without terminating process-owned services.</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Provides an extension point for releasing resources owned by this UI instance.</summary>
        /// <param name="Disposing">Whether disposal was explicitly requested.</param>
        protected virtual void Dispose(bool Disposing)
        {
            // Process-owned resources are released by lifecycle termination, not UI disposal.
        }

        /// <summary>Releases this UI instance without disposing shared lifecycle resources.</summary>
        /// <returns>A completed task.</returns>
        public virtual Task DisposeAsync()
        {
            this.Dispose();
            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        /// <summary>Notifies subscribers that a platform foreground event occurred.</summary>
        public static void RaiseAppActivated()
        {
            _ = ObserveLifecycleAsync(() => MainThread.InvokeOnMainThreadAsync(() =>
            {
                SyncMotionSettings();
                AppActivated?.Invoke(Current, EventArgs.Empty);
            }));
        }

        /// <summary>
        /// Copies the operating system's reduce-motion preference into the app's motion settings.
        /// </summary>
        /// <remarks>
        /// Runs when the window is created and each time the app returns to the foreground, which is when the user can have
        /// changed the setting. Never throws.
        /// </remarks>
        internal static void SyncMotionSettings()
        {
            try
            {
                IMotionSettings? Settings = MotionPreferences.Settings;
                Settings?.Update(ServiceRef.PlatformSpecific.IsReduceMotionEnabled, Settings.DurationScale);
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(Ex);
            }
        }

        /// <summary>
        /// Callback for validating SSL certificates.
        /// This method is called when a remote certificate is received during HTTPS communication.
        /// Fixed an issue with incomplete revocation check in the chain on iOS devices.
        /// </summary>
        public static void ValidateCertificateCallback(object? Sender, RemoteCertificateEventArgs Args)
        {
            Args.IsValid = true; // Accept certificate

            // Check for incomplete revocation check in the chain
            if ((Args.SslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 && Args.Chain is not null)
            {
                foreach (X509ChainStatus Status in Args.Chain.ChainStatus)
                {
                    // Apple-specific error code for incomplete revocation check
                    if (Status.Status == X509ChainStatusFlags.RevocationStatusUnknown ||
                        Status.Status == X509ChainStatusFlags.OfflineRevocation)
                    {
                        continue; // Ignore this error
                    }
                    if (Status.Status != X509ChainStatusFlags.NoError)
                        Args.IsValid = false; // Reject certificate
                }
            }
            else if (Args.SslPolicyErrors != SslPolicyErrors.None)
                Args.IsValid = false; // Reject certificate

            if (Args.IsValid is not null && Args.IsValid == true)
                return; // Accept certificate

            try
            {

                StringBuilder SniffMsg = new StringBuilder();

                SniffMsg.AppendLine("Invalid certificate received (and rejected).");

                SniffMsg.AppendLine();
                SniffMsg.Append("sslPolicyErrors: ");
                SniffMsg.AppendLine(Args.SslPolicyErrors.ToString());
                SniffMsg.Append("Subject: ");
                SniffMsg.AppendLine(Args.Certificate?.Subject);
                SniffMsg.Append("Issuer: ");
                SniffMsg.AppendLine(Args.Certificate?.Issuer);
                // Log the certificate details
                byte[]? Cert = Args.Certificate?.Export(X509ContentType.Cert);    // Avoids SafeHandle exception when accessing certificate later.

                if (Cert is not null)
                {
                    StringBuilder Base64 = new StringBuilder();
                    string s;
                    int c = Cert?.Length ?? 0;
                    int i = 0;
                    int j;

                    while (i < c)
                    {
                        j = Math.Min(57, c - i);
                        s = Convert.ToBase64String(Cert!, i, j);
                        i += j;

                        Base64.Append(s);

                        if (i < c)
                            Base64.AppendLine();
                    }

                    SniffMsg.Append("BASE64(Cert): ");
                    SniffMsg.Append(Base64);
                }
                SniffMsg.AppendLine();

                SniffMsg.AppendLine("Nr of elements in chain: ");
                SniffMsg.Append(Args.Chain?.ChainElements.Count ?? 0);
                SniffMsg.AppendLine();
                SniffMsg.AppendLine("Chain status: ");
                foreach (X509ChainStatus Status in Args.Chain?.ChainStatus ?? [])
                {
                    SniffMsg.Append("Status: ");
                    SniffMsg.AppendLine(Status.Status.ToString());
                    SniffMsg.Append("StatusInformation: ");
                    SniffMsg.AppendLine(Status.StatusInformation);
                    SniffMsg.AppendLine();
                }


                SniffMsg.AppendLine("Chain elements:");
                if (Args.Chain is not null)
                {
                    foreach (X509ChainElement Element in Args.Chain.ChainElements)
                    {
                        SniffMsg.Append("Certificate: ");
                        SniffMsg.AppendLine(Element.Certificate.GetNameInfo(X509NameType.SimpleName, false));
                        SniffMsg.Append("Certificate: ");
                        SniffMsg.AppendLine(Element.Certificate.GetNameInfo(X509NameType.DnsName, false));
                        SniffMsg.Append("Certificate: ");
                        SniffMsg.AppendLine(Element.Certificate.GetNameInfo(X509NameType.EmailName, false));
                        SniffMsg.Append("Certificate: ");
                        SniffMsg.AppendLine(Element.Certificate.GetNameInfo(X509NameType.UpnName, false));
                        SniffMsg.Append("Subject: ");
                        SniffMsg.AppendLine(Element.Certificate.Subject);
                        SniffMsg.Append("Issuer: ");
                        SniffMsg.AppendLine(Element.Certificate.Issuer);
                        SniffMsg.AppendLine("Info: ");
                        SniffMsg.Append(Element.Information);
                        SniffMsg.AppendLine();
                    }
                }


                ServiceRef.LogService.LogWarning(SniffMsg.ToString());
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }
        }

        #endregion
    }
}
