using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Maui.Controls.Internals;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Resources.Styles;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Test;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Registration;
using Waher.Content;
using Waher.Events;
using Waher.Runtime.Inventory;
using Waher.Runtime.Text;
using Waher.Script;
using Waher.Script.Content;
using Waher.Script.Graphs;
using Waher.Security.JWS;
using Waher.Security.JWT;
using Waher.Security.LoginMonitor;
using Waher.Things;

namespace NeuroAccessMaui
{
    /// <summary>
    /// Represents an instance of the Neuro-Access app.
    /// </summary>
    public partial class App : Application, IDisposableAsync
    {
        #region Fields

        private readonly SemaphoreSlim autoSaveSemaphore = new(1, 1);
        private Timer? autoSaveTimer;
        // Initialization completion source (shared across potential recreated App instances)
        private static readonly TaskCompletionSource<bool> initCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim startupWorker = new(1, 1);
        private CancellationTokenSource startupCancellation;
        private bool isDisposed;

        private static App? appInstance;
        private static bool configLoaded;
        private static bool defaultInstantiated;
        private static int startupCounter;

        private static readonly TaskCompletionSource<bool> servicesSetup = new();
        private static readonly TaskCompletionSource<bool> defaultInstantiatedSource = new();

        /// <summary>
        /// Flag indicating if this instance is “resuming” an already-started app.
        /// 
        /// The App class is not actually a singleton. Each time Android MainActivity is destroyed and then created again, a new instance
        /// of the App class will be created, its OnStart method will be called and its OnResume method will not be called. This happens,
        /// for example, on Android when the user presses the back button and then navigates to the app again. However, the App class
        /// doesn't seem to work properly (should it?) when this happens (some chaos happens here and there), so we pretend that
        /// there is only one instance (see the references to onStartResumesApplication).
        /// </summary>
        private bool onStartResumesApplication;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current application instance.
        /// </summary>
        public static new App? Current => appInstance;

        /// <summary>
        /// Supported languages.
        /// </summary>
        public static readonly LanguageInfo[] SupportedLanguages =
        [
            new("en", "English"),
            new("sv", "svenska"),
            new("es", "español"),
            new("fr", "français"),
            new("de", "Deutsch"),
            new("da", "dansk"),
            new("no", "norsk"),
            new("fi", "suomi"),
            new("sr", "српски"),
            new("pt", "português"),
            new("ro", "română"),
            new("ru", "русский")
        ];

        /// <summary>
        /// Gets the selected language.
        /// </summary>
        public static LanguageInfo SelectedLanguage
        {
            get
            {
                string? LanguageName = Preferences.Get("user_selected_language", null);
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

        public Task<bool> InitCompleted => initCompletedTcs.Task;


        #endregion

        #region Constructor

        public App() : this(false) { }

        public App(bool backgroundStart)
        {
            // Manage single-instance behavior.
            App? PreviousInstance = appInstance;
            appInstance = this;
            this.onStartResumesApplication = PreviousInstance is not null;

            if (PreviousInstance is null)
            {
                InitLocalizationResource();

                AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += this.TaskScheduler_UnobservedTaskException;

                this.startupCancellation = new CancellationTokenSource();
                this.StartInitialization(backgroundStart);
            }
            else
            {
                // Reuse state from the previous instance.
                this.autoSaveTimer = PreviousInstance.autoSaveTimer;
                this.startupCancellation = PreviousInstance.startupCancellation;
            }

            if (!backgroundStart)
            {
                this.InitializeComponent();

                AppTheme? CurrentTheme = ServiceRef.TagProfile.Theme;
                this.SetTheme(CurrentTheme ?? AppTheme.Light);
                ServiceRef.ThemeService.SetTheme(CurrentTheme ?? AppTheme.Light);
                // Show CustomShell directly as MainPage
                // this.MainPage = ServiceHelper.GetService<CustomShell>();
            }
        }

		void SetTheme(AppTheme Theme)
		{
			if (Theme is AppTheme.Unspecified) Theme = Application.Current!.RequestedTheme;

			ICollection<ResourceDictionary> Merged = this.Resources.MergedDictionaries;

            // Remove only our color theme dictionaries
            foreach (ResourceDictionary? Dict in Merged.Where(d => d.ContainsKey("IsLocalThemeDictionary")).ToList())
                Merged.Remove(Dict);

			// Add correct one
			if (Theme == AppTheme.Dark)
				Merged.Add(new Dark());
			else
				Merged.Add(new Light());
		}

        /// <summary>
        /// Override default window creation to use CustomShell as root page.
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            if (this.Windows.Any())
                return this.Windows[0];
            CustomShell Shell = ServiceHelper.GetService<CustomShell>();
            return new Window(Shell);
        }
        #endregion

        #region Initialization

		private static void InitLocalizationResource()
		{
			//	LocalizationManager.Current.PropertyChanged += (_, _) => AppResources.Culture = LocalizationManager.Current.CurrentCulture;
			LocalizationManager.Current.CurrentCulture = SelectedLanguage;
		}

        private void StartInitialization(bool backgroundStart)
        {
            Task.Run(async () =>
            {
                try
                {
                    this.InitializeInstances();
                    await ServiceRef.CryptoService.InitializeJwtFactory();
                    await this.PerformStartupAsync(isResuming: false, backgroundStart);
                    initCompletedTcs.TrySetResult(true);
                }
                catch (Exception Ex)
                {
                    Ex = Log.UnnestException(Ex);
                    this.HandleStartupException(Ex);
                    servicesSetup.TrySetResult(false);
                    initCompletedTcs.TrySetResult(false);
                }
            });
        }

        private void HandleStartupException(Exception Ex)
        {
            Ex = Log.UnnestException(Ex);
            ServiceRef.LogService.SaveExceptionDump("StartPage", Ex.ToString());
            this.DisplayBootstrapErrorPage(Ex.Message, Ex.StackTrace ?? string.Empty);
            return;
        }

        /// <summary>
        /// Initializes required types and registers services.
        /// </summary>
        private void InitializeInstances()
        {

            defaultInstantiatedSource.TrySetResult(true);


            // Set dependency resolver.
            DependencyResolver.ResolveUsing(type =>
            {
                if (Types.GetType(type.FullName) is null)
                    return null;

                try
                {
                    return Types.Instantiate(true, type);
                }
                catch (Exception Ex)
                {
                    ServiceRef.LogService.LogException(Ex);
                    return null;
                }
            });

            servicesSetup.TrySetResult(true);
        }

        #endregion

        #region Startup / Resume

        public async Task OnBackgroundStart()
        {
            if (this.onStartResumesApplication)
            {
                this.onStartResumesApplication = false;
                await this.ResumeAsync(isBackground: true);
                return;
            }

            // Asynchronously wait up to 60 seconds.
            if (!await this.InitCompleted.WaitAsync(TimeSpan.FromSeconds(60)))
                throw new Exception("Initialization did not complete in time.");
        }

        protected override async void OnStart()
        {
            if (this.onStartResumesApplication)
            {
                this.onStartResumesApplication = false;
                this.OnResume();
                return;
            }

            if (!await this.InitCompleted.WaitAsync(TimeSpan.FromSeconds(60)))
                throw new Exception("Initialization did not complete in time.");
        }

        public async Task ResumeAsync(bool isBackground)
        {
            appInstance = this;
            this.startupCancellation = new CancellationTokenSource();
            await this.PerformStartupAsync(isResuming: true, backgroundStart: isBackground);
        }

        protected override async void OnResume()
        {
            await this.ResumeAsync(isBackground: false);
        }

        private async Task PerformStartupAsync(bool isResuming, bool backgroundStart)
        {
            await this.startupWorker.WaitAsync();
            try
            {
                if (++startupCounter > 1)
                    return;

                CancellationToken Token = this.startupCancellation.Token;
                Token.ThrowIfCancellationRequested();

                if (!backgroundStart)
                {
                    await SendErrorReportFromPreviousRunAsync();
                    Token.ThrowIfCancellationRequested();
                }

                await ServiceRef.LogService.Load(isResuming, Token);

                await ServiceRef.StorageService.Init(Token);
                if (!configLoaded)
                {
                    await this.CreateOrRestoreConfigurationAsync();
                    configLoaded = true;
                }

                Token.ThrowIfCancellationRequested();
                await ServiceRef.NetworkService.Load(isResuming, Token);
                Token.ThrowIfCancellationRequested();
                await ServiceRef.XmppService.Load(isResuming, Token);
                Token.ThrowIfCancellationRequested();

                TimeSpan InitialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
                this.autoSaveTimer = new Timer(async _ => await this.AutoSaveAsync(), null, InitialAutoSaveDelay, Constants.Intervals.AutoSave);

                await ServiceRef.UiService.Load(isResuming, Token);
                await ServiceRef.AttachmentCacheService.Load(isResuming, Token);
                await ServiceRef.InternetCacheService.Load(isResuming, Token);
                await ServiceRef.ContractOrchestratorService.Load(isResuming, Token);
                await ServiceRef.ThingRegistryOrchestratorService.Load(isResuming, Token);
                await ServiceRef.NeuroWalletOrchestratorService.Load(isResuming, Token);
                await ServiceRef.NotificationService.Load(isResuming, Token);

                AppShell.RegisterRoutes();

                //	AppShell.AppLoaded();
            }
            catch (OperationCanceledException)
            {
                Log.Notice($"{(isResuming ? "OnResume" : "Initial app")} startup was canceled.");
            }
            catch (Exception Ex)
            {
                Ex = Log.UnnestException(Ex);
                ServiceRef.LogService.SaveExceptionDump(Ex.Message, Ex.StackTrace ?? string.Empty);
                this.DisplayBootstrapErrorPage(Ex.Message, Ex.StackTrace ?? string.Empty);
            }
            finally
            {
                this.startupWorker.Release();
            }
        }

        #endregion

        #region Sleep / Shutdown

        public async Task OnBackgroundSleep() => await this.ShutdownAsync(inPanic: false);

        protected override async void OnSleep()
        {
            Page? rootPage = this.Windows.FirstOrDefault()?.Page;
            if (rootPage?.BindingContext is BaseViewModel Vm)
                await Vm.Shutdown();

            await this.ShutdownAsync(inPanic: false);
        }

        internal static async Task StopAsync()
        {
            if (appInstance is not null)
            {
                await appInstance.ShutdownAsync(inPanic: false);
                appInstance = null;
            }

            try
            {
                await ServiceRef.PlatformSpecific.CloseApplication();
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }
        }

        private async Task ShutdownAsync(bool inPanic)
        {
            this.startupCancellation.Cancel();
            await this.startupWorker.WaitAsync();

            try
            {
                if ((startupCounter < 1) || (--startupCounter > 0))
                    return;

                this.StopAutoSaveTimer();

                if (inPanic)
                {
                    if (ServiceRef.XmppService is not null)
                        await ServiceRef.XmppService.UnloadFast();
                }
                else
                {
                    List<Task> UnloadTasks = [];

                    if (ServiceRef.UiService is not null)
                        UnloadTasks.Add(ServiceRef.UiService.Unload());

                    if (ServiceRef.ContractOrchestratorService is not null)
                        UnloadTasks.Add(ServiceRef.ContractOrchestratorService.Unload());

                    if (ServiceRef.XmppService is not null)
                        UnloadTasks.Add(ServiceRef.XmppService.Unload());

                    if (ServiceRef.NetworkService is not null)
                        UnloadTasks.Add(ServiceRef.NetworkService.Unload());

                    if (ServiceRef.AttachmentCacheService is not null)
                        UnloadTasks.Add(ServiceRef.AttachmentCacheService.Unload());

                    await Task.WhenAll(UnloadTasks);

                    foreach (IEventSink Sink in Log.Sinks)
                        Log.Unregister(Sink);

                    if (ServiceRef.StorageService is not null)
                        await ServiceRef.StorageService.Shutdown();
                }

                // Causes list of singleton instances to be cleared.
                await Log.TerminateAsync();
            }
            finally
            {
                this.startupWorker.Release();
            }
        }

        private void StopAutoSaveTimer()
        {
            if (this.autoSaveTimer is not null)
            {
                this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this.autoSaveTimer.Dispose();
                this.autoSaveTimer = null;
            }
        }

        #endregion

        #region AutoSave

        private async Task AutoSaveAsync()
        {
            await this.autoSaveSemaphore.WaitAsync();
            try
            {
                if (ServiceRef.TagProfile.IsDirty)
                {
                    ServiceRef.TagProfile.ResetIsDirty();
                    try
                    {
                        TagConfiguration Configuration = ServiceRef.TagProfile.ToConfiguration();
                        try
                        {
                            if (string.IsNullOrEmpty(Configuration.ObjectId))
                                await ServiceRef.StorageService.Insert(Configuration);
                            else
                                await ServiceRef.StorageService.Update(Configuration);
                        }
                        catch (KeyNotFoundException)
                        {
                            await ServiceRef.StorageService.Insert(Configuration);
                        }
                    }
                    catch (Exception Ex)
                    {
                        ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                    }
                }
            }
            finally
            {
                this.autoSaveSemaphore.Release();
            }
        }

        public async Task ForceSaveAsync() => await this.AutoSaveAsync();

        #endregion

        #region Configuration

        private async Task CreateOrRestoreConfigurationAsync()
        {
            TagConfiguration? Configuration;
            try
            {
                Configuration = await ServiceRef.StorageService.FindFirstDeleteRest<TagConfiguration>();
            }
            catch (Exception FindEx)
            {
                ServiceRef.LogService.LogException(FindEx, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                Configuration = null;
            }

            if (Configuration is null)
            {
                Configuration = new TagConfiguration();
                try
                {
                    await ServiceRef.StorageService.Insert(Configuration);
                }
                catch (Exception Ex)
                {
                    ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
                }
            }

            ServiceRef.TagProfile.FromConfiguration(Configuration);
        }

        #endregion

        #region Page Navigation

        /// <summary>
        /// Switch to the registration flow.
        /// </summary>
        public static Task SetRegistrationPageAsync()
            => ServiceRef.Provider.GetRequiredService<CustomShell>()
                .SetPageAsync(ServiceHelper.GetService<RegistrationPage>());

        /// <summary>
        /// Switch to the main page.
        /// </summary>
        public static Task SetMainPageAsync()
            => ServiceRef.Provider.GetRequiredService<CustomShell>()
                .SetPageAsync(ServiceHelper.GetService<MainPage>());

        public static Task SetPageAsync(string pagePath)
        {
            if (Shell.Current.CurrentState.Location?.OriginalString == pagePath)
                return Task.CompletedTask;

            return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(pagePath));
        }

        #endregion

        #region Error Handling

        private async void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Exception Ex = Log.UnnestException(e.Exception);
                e.SetObserved();
                await this.HandleUnhandledExceptionAsync(Ex, nameof(TaskScheduler_UnobservedTaskException), shutdown: false);
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }
        }

        private async void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                await this.HandleUnhandledExceptionAsync(e.ExceptionObject as Exception, nameof(CurrentDomain_UnhandledException), shutdown: true);
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }
        }

        private async Task HandleUnhandledExceptionAsync(Exception? ex, string title, bool shutdown)
        {
            if (ex is not null)
            {
                ServiceRef.LogService.SaveExceptionDump(title, ex.ToString());
                ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));
            }

            if (shutdown)
                await this.ShutdownAsync(inPanic: false);

#if DEBUG
            Page? alertPage = this.Windows.FirstOrDefault()?.Page;
            if (!shutdown && alertPage is not null)
            {
                if (this.Dispatcher.IsDispatchRequired)
                {
                    this.Dispatcher.Dispatch(async () =>
                    {
                        await alertPage.DisplayAlert(title, ex?.ToString(), ServiceRef.Localizer[nameof(AppResources.Ok)]);
                    });
                }
                else
                {
                    await alertPage.DisplayAlert(title, ex?.ToString(), ServiceRef.Localizer[nameof(AppResources.Ok)]);
                }
            }
#endif
        }

        private void DisplayBootstrapErrorPage(string title, string stackTrace)
        {
            this.Dispatcher.Dispatch(() =>
            {
                Window? win = this.Windows.FirstOrDefault();
                if (win is not null)
                    win.Page = new BootstrapErrorPage(title, stackTrace);
            });
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
                    Msg.AppendLine();
                    Msg.AppendLine("```");
                    Msg.AppendLine(StackTrace);
                    Msg.AppendLine("```");

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
                if (ServiceRef.TagProfile.Step != RegistrationStep.Complete && !url.StartsWith("obinfo", StringComparison.OrdinalIgnoreCase))
                {
                    await ServiceRef.UiService.DisplayAlert(
                        ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
                        ServiceRef.Localizer[nameof(AppResources.NotCompletedOnboardingError)]);
                    return;
                }

				await QrCode.OpenUrl(url).ConfigureAwait(false);
			});
		}

        public static Task<bool> OpenUrlAsync(string url) => OpenUrlAsync(url, showErrorIfUnable: true);

        public static Task<bool> OpenUrlAsync(string url, bool showErrorIfUnable)
        {
            return QrCode.OpenUrl(url, showErrorIfUnable);
        }

        #endregion



        #region IDisposable Implementation

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <see cref="IDisposableAsync.Dispose"/>
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                this.DisposeAsync().Wait();
        }

        /// <summary>
        /// <see cref="IDisposableAsync.Dispose"/>
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            if (this.isDisposed)
                return;

            this.autoSaveTimer?.Dispose();
            this.startupWorker.Dispose();
            this.startupCancellation.Dispose();

            await Task.CompletedTask; // keep method truly async if extended later
            this.isDisposed = true;
        }

        #endregion

        #region Helpers

        public static T Instantiate<T>()
        {
            if (!defaultInstantiated)
                defaultInstantiated = defaultInstantiatedSource.Task.Result;

            return Types.Instantiate<T>(false);
        }

        public static void RaiseAppActivated()
        {
            AppActivated?.Invoke(Current, EventArgs.Empty);
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
