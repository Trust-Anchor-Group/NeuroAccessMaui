using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using EDaler;
using Microsoft.Maui.Controls.Internals;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.AttachmentCache;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Network;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.UI.Pages;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Popups.Password;
using NeuroFeatures;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Avatar;
using Waher.Networking.XMPP.Concentrator;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Control;
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
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
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
	/// The Application class, representing an instance of the Neuro-Access app.
	/// </summary>
	public partial class App : Application, IDisposable
	{
		private static readonly TaskCompletionSource<bool> servicesSetup = new();
		private static readonly TaskCompletionSource<bool> defaultInstantiatedSource = new();
		private static bool configLoaded = false;
		private static bool defaultInstantiated = false;
		private static DateTime savedStartTime = DateTime.MinValue;
		private static bool displayedPasswordPopup = false;
		private static int startupCounter = 0;
		private readonly LoginAuditor loginAuditor;
		private Timer? autoSaveTimer;
		private readonly Task<bool> initCompleted;
		private readonly SemaphoreSlim startupWorker = new(1, 1);
		private CancellationTokenSource startupCancellation;
		private bool isDisposed;

		// The App class is not actually a singleton. Each time Android MainActivity is destroyed and then created again, a new instance
		// of the App class will be created, its OnStart method will be called and its OnResume method will not be called. This happens,
		// for example, on Android when the user presses the back button and then navigates to the app again. However, the App class
		// doesn't seem to work properly (should it?) when this happens (some chaos happens here and there), so we pretend that
		// there is only one instance (see the references to onStartResumesApplication).
		private bool onStartResumesApplication = false;

		/// <summary>
		/// Gets the last application instance.
		/// </summary>
		private static App? appInstance;

		/// <summary>
		/// Gets the current application, type casted to <see cref="App"/>.
		/// </summary>
		public static new App? Current => appInstance;

		/// <inheritdoc/>
		public App()
			: this(false)
		{
		}

		/// <inheritdoc/>
		public App(bool BackgroundStart)
		{
			App? PreviousInstance = appInstance;
			appInstance = this;

			this.onStartResumesApplication = PreviousInstance is not null;

			// If the previous instance is null, create the app state from scratch. If not, just copy the state from the previous instance.
			//!!! replace this logic with static variables
			if (PreviousInstance is null)
			{
				InitLocalizationResource();

				AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
				TaskScheduler.UnobservedTaskException += this.TaskScheduler_UnobservedTaskException;

				LoginInterval[] LoginIntervals =
					[
						new LoginInterval(Constants.Password.FirstMaxPasswordAttempts, TimeSpan.FromHours(Constants.Password.FirstBlockInHours)),
						new LoginInterval(Constants.Password.SecondMaxPasswordAttempts, TimeSpan.FromHours(Constants.Password.SecondBlockInHours)),
						new LoginInterval(Constants.Password.ThirdMaxPasswordAttempts, TimeSpan.FromHours(Constants.Password.ThirdBlockInHours))
					];

				this.loginAuditor = new LoginAuditor(Constants.Password.LogAuditorObjectID, LoginIntervals);
				this.startupCancellation = new CancellationTokenSource();
				this.initCompleted = this.Init(BackgroundStart);
			}
			else
			{
				this.loginAuditor = PreviousInstance.loginAuditor;
				this.autoSaveTimer = PreviousInstance.autoSaveTimer;
				this.initCompleted = PreviousInstance.initCompleted;
				this.startupWorker = PreviousInstance.startupWorker;
				this.startupCancellation = PreviousInstance.startupCancellation;
			}

			if (!BackgroundStart)
			{
				this.InitializeComponent();
				Current!.UserAppTheme = AppTheme.Unspecified;

				// Start page
				try
				{
					this.MainPage = ServiceHelper.GetService<AppShell>();
				}
				catch (Exception ex)
				{
					this.HandleStartupException(ex);
				}
			}
		}

		/// <summary>
		/// Gets a value indicating if the application has completed on-boarding.
		/// </summary>
		/// <remarks>
		/// This is not the same as <see cref="TagProfile.IsComplete"/>. <see cref="TagProfile.IsComplete"/> is required but not
		/// sufficient for the application to be "on-boarded". An application is on-boarded when its legal identity is on-boarded
		/// and when its internal systems are ready. For example, the loading stage of the app must complete.
		/// </remarks>
		public static bool IsOnboarded => Shell.Current is not null;

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
				new("ru", "русский"),
			];

		/// <summary>
		/// Selected language.
		/// </summary>
		public static LanguageInfo SelectedLanguage
		{
			get
			{
				string? LanguageName = Preferences.Get("user_selected_language", null);
				LanguageInfo SelectedLanguage = SupportedLanguages[0];

				if (LanguageName is not null)
				{
					SelectedLanguage = SupportedLanguages.FirstOrDefault(
						el => string.Equals(el.TwoLetterISOLanguageName, LanguageName, StringComparison.OrdinalIgnoreCase), SelectedLanguage);
				}

				if ((LanguageName is null) ||
					!string.Equals(SelectedLanguage.TwoLetterISOLanguageName, LanguageName, StringComparison.OrdinalIgnoreCase))
				{
					Preferences.Set("user_selected_language", SelectedLanguage.TwoLetterISOLanguageName);
				}

				return SelectedLanguage;
			}
		}

		private static void InitLocalizationResource()
		{
			LocalizationManager.Current.PropertyChanged += (_, _) => AppResources.Culture = LocalizationManager.Current.CurrentCulture;
			LocalizationManager.Current.CurrentCulture = SelectedLanguage;
		}

		private Task<bool> Init(bool BackgroundStart)
		{
			TaskCompletionSource<bool> Result = new();
			Task.Run(async () => await this.InitInParallel(Result, BackgroundStart));
			return Result.Task;
		}

		private async Task InitInParallel(TaskCompletionSource<bool> Result, bool BackgroundStart)
		{
			try
			{
				this.InitInstances();

				await ServiceRef.CryptoService.InitializeJwtFactory();
				await this.PerformStartup(false, BackgroundStart);

				Result.TrySetResult(true);
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				this.HandleStartupException(ex);

				servicesSetup.TrySetResult(false);
				Result.TrySetResult(false);
			}
		}

		private void InitInstances()
		{
			Assembly AppAssembly = this.GetType().Assembly;

			if (!Types.IsInitialized)
			{
				// Define the scope and reach of Runtime.Inventory (Script, Serialization, Persistence, IoC, etc.):
				Types.Initialize(
					AppAssembly,                                // Allows for objects defined in this assembly, to be instantiated and persisted.
					typeof(Database).Assembly,                  // Indexes default attributes
					typeof(ObjectSerializer).Assembly,          // Indexes general serializers
					typeof(FilesProvider).Assembly,             // Indexes special serializers
					typeof(RuntimeSettings).Assembly,           // Allows for persistence of settings in the object database
					typeof(InternetContent).Assembly,           // Common Content-Types
					typeof(ImageCodec).Assembly,                // Common Image Content-Types
					typeof(MarkdownDocument).Assembly,          // Markdown object model
					typeof(XML).Assembly,                       // XML Content-Type
					typeof(DnsResolver).Assembly,               // Serialization of DNS-related objects
					typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
					typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
					typeof(NeuroFeaturesClient).Assembly,       // Serialization of XMPP objects related to Neuro-Feature tokens
					typeof(EDalerClient).Assembly,              // Management of eDaler URIs
					typeof(SensorClient).Assembly,              // Serialization of XMPP objects related to sensors
					typeof(ControlClient).Assembly,             // Serialization of XMPP objects related to actuators
					typeof(ConcentratorClient).Assembly,        // Serialization of XMPP objects related to concentrators
					typeof(ProvisioningClient).Assembly,        // Serialization of XMPP objects related to provisioning
					typeof(PubSubClient).Assembly,              // Serialization of XMPP objects related to publish/subscribe pattern
					typeof(PepClient).Assembly,                 // Serialization of XMPP objects related to personal eventing protocol (PEP)
					typeof(AvatarClient).Assembly,              // Serialization of XMPP objects related to avatars
					typeof(PushNotificationClient).Assembly,    // Serialization of XMPP objects related to push notification
					typeof(MailClient).Assembly,                // Serialization of XMPP objects related to processing of incoming e-mail
					typeof(ThingReference).Assembly,            // IoT Abstraction library
					typeof(JwtFactory).Assembly,                // JSON Web Tokens (JWT)
					typeof(JwsAlgorithm).Assembly,              // JSON Web Signatures (JWS)
					typeof(Expression).Assembly,                // Indexes basic script functions
					typeof(Graph).Assembly,                     // Indexes graph script functions
					typeof(GraphEncoder).Assembly,              // Indexes content script functions
					typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
					typeof(HttpxClient).Assembly);              // Support for HTTP over XMPP (HTTPX) URI Scheme.
			}

			EndpointSecurity.SetCiphers(
			[
				typeof(Edwards448Endpoint)
			], false);

			// Create Services

			Types.InstantiateDefault<ITagProfile>(false);
			Types.InstantiateDefault<ILogService>(false);
			Types.InstantiateDefault<IUiService>(false);
			Types.InstantiateDefault<ICryptoService>(false);
			Types.InstantiateDefault<INetworkService>(false);
			Types.InstantiateDefault<IStorageService>(false);
			Types.InstantiateDefault<ISettingsService>(false);
			Types.InstantiateDefault<IXmppService>(false);
			Types.InstantiateDefault<IAttachmentCacheService>(false);
			Types.InstantiateDefault<IContractOrchestratorService>(false);
			Types.InstantiateDefault<INfcService>(false);
			Types.InstantiateDefault<INotificationService>(false);

			defaultInstantiatedSource.TrySetResult(true);

			// Set resolver

			DependencyResolver.ResolveUsing(type =>
			{
				if (Types.GetType(type.FullName) is null)
					return null;    // Type not managed by Runtime.Inventory. MAUI resolves this using its default mechanism.

				try
				{
					return Types.Instantiate(true, type);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					return null;
				}
			});

			servicesSetup.TrySetResult(true);
		}

		private void HandleStartupException(Exception ex)
		{
			ex = Log.UnnestException(ex);
			ServiceRef.LogService.SaveExceptionDump("StartPage", ex.ToString());
			this.DisplayBootstrapErrorPage(ex.Message, ex.StackTrace ?? string.Empty);
			return;
		}

		/// <summary>
		/// Instantiates an object of type <typeparamref name="T"/>, after assuring default instances have been created first.
		/// Assures singleton classes are only instantiated once, and that the reference to the singleton instance is returned.
		/// </summary>
		/// <typeparam name="T">Type of object to instantiate.</typeparam>
		/// <returns>Instance</returns>
		public static T Instantiate<T>()
		{
			if (!defaultInstantiated)
				defaultInstantiated = defaultInstantiatedSource.Task.Result;

			return Types.Instantiate<T>(false);
		}

		internal static async Task WaitForServiceSetup()
		{
			await servicesSetup.Task;
		}

		#region Startup/Shutdown

		/// <summary>
		/// Awaiting the services start in the background.
		/// </summary>
		public async Task OnBackgroundStart()
		{
			if (this.onStartResumesApplication)
			{
				this.onStartResumesApplication = false;
				await this.DoResume(true);
				return;
			}

			if (!this.initCompleted.Wait(60000))
				throw new Exception("Initialization did not complete in time.");
		}

		/// <inheritdoc/>
		protected override async void OnStart()
		{
			if (this.onStartResumesApplication)
			{
				this.onStartResumesApplication = false;
				this.OnResume();
				return;
			}

			if (!this.initCompleted.Wait(60000))
				throw new Exception("Initialization did not complete in time.");

			if (!await AuthenticateUser())
				await Stop();
		}

		/// <summary>
		/// Resume the services start in the background.
		/// </summary>
		public async Task DoResume(bool BackgroundStart)
		{
			appInstance = this;
			this.startupCancellation = new CancellationTokenSource();

			await this.PerformStartup(true, BackgroundStart);
		}

		/// <inheritdoc/>
		protected override async void OnResume()
		{
			await this.DoResume(false);

			if (!await AuthenticateUser())
				await Stop();
		}

		private async Task PerformStartup(bool isResuming, bool BackgroundStart)
		{
			await this.startupWorker.WaitAsync();

			try
			{
				// do nothing if the services are already started
				if (++startupCounter > 1)
					return;

				// cancel the startup if the application is closed
				CancellationToken Token = this.startupCancellation.Token;
				Token.ThrowIfCancellationRequested();

				if (!BackgroundStart)
				{
					await SendErrorReportFromPreviousRun();
					Token.ThrowIfCancellationRequested();
				}

				await ServiceRef.StorageService.Init(Token);

				if (!configLoaded)
				{
					await this.CreateOrRestoreConfiguration();
					configLoaded = true;
				}

				Token.ThrowIfCancellationRequested();

				await ServiceRef.NetworkService.Load(isResuming, Token);
				Token.ThrowIfCancellationRequested();

				await ServiceRef.XmppService.Load(isResuming, Token);
				Token.ThrowIfCancellationRequested();

				TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
				this.autoSaveTimer = new Timer(async _ => await this.AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

				await ServiceRef.UiService.Load(isResuming, Token);
				await ServiceRef.AttachmentCacheService.Load(isResuming, Token);
				await ServiceRef.ContractOrchestratorService.Load(isResuming, Token);
				await ServiceRef.ThingRegistryOrchestratorService.Load(isResuming, Token);
				await ServiceRef.NeuroWalletOrchestratorService.Load(isResuming, Token);
				await ServiceRef.NotificationService.Load(isResuming, Token);
			}
			catch (OperationCanceledException)
			{
				Log.Notice($"{(isResuming ? "OnResume " : "Initial app ")} startup was canceled.");
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				ServiceRef.LogService.SaveExceptionDump(ex.Message, ex.StackTrace ?? string.Empty);
				this.DisplayBootstrapErrorPage(ex.Message, ex.StackTrace ?? string.Empty);
			}
			finally
			{
				this.startupWorker.Release();
			}
		}

		/// <summary>
		/// Awaiting the services stop in the background.
		/// </summary>
		public async Task OnBackgroundSleep()
		{
			await this.Shutdown(false);
		}

		/// <inheritdoc/>
		protected override async void OnSleep()
		{
			// Done manually here, as the Disappearing event won't trigger when exiting the app,
			// and we want to make sure state is persisted and teardown is done correctly to avoid memory leaks.

			if (this.MainPage?.BindingContext is BaseViewModel vm)
				await vm.Shutdown();

			await this.Shutdown(false);

			SetStartInactivityTime();
		}

		internal static async Task Stop()
		{
			if (appInstance is not null)
			{
				await appInstance.Shutdown(false);
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

		private async Task Shutdown(bool inPanic)
		{
			// if the PerformStartup is not finished, cancel it first
			this.startupCancellation.Cancel();
			await this.startupWorker.WaitAsync();

			try
			{
				// do nothing if the services are already stopped
				// or if the startup counter is greater than one
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
					if (ServiceRef.UiService is not null)
						await ServiceRef.UiService.Unload();

					if (ServiceRef.ContractOrchestratorService is not null)
						await ServiceRef.ContractOrchestratorService.Unload();

					if (ServiceRef.XmppService is not null)
						await ServiceRef.XmppService.Unload();

					if (ServiceRef.NetworkService is not null)
						await ServiceRef.NetworkService.Unload();

					if (ServiceRef.AttachmentCacheService is not null)
						await ServiceRef.AttachmentCacheService.Unload();

					if (ServiceRef.UiService is not null)
						await ServiceRef.UiService.Unload();

					foreach (IEventSink Sink in Log.Sinks)
						Log.Unregister(Sink);

					if (ServiceRef.StorageService is not null)
						await ServiceRef.StorageService.Shutdown();
				}

				// Causes list of singleton instances to be cleared.
				Log.Terminate();
			}
			finally
			{
				this.startupWorker.Release();
			}
		}

		#endregion

		private void StopAutoSaveTimer()
		{
			if (this.autoSaveTimer is not null)
			{
				this.autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
				this.autoSaveTimer.Dispose();
				this.autoSaveTimer = null;
			}
		}

		private async Task AutoSave()
		{
			if (ServiceRef.TagProfile.IsDirty)
			{
				ServiceRef.TagProfile.ResetIsDirty();
				try
				{
					TagConfiguration tc = ServiceRef.TagProfile.ToConfiguration();

					try
					{
						if (string.IsNullOrEmpty(tc.ObjectId))
							await ServiceRef.StorageService.Insert(tc);
						else
							await ServiceRef.StorageService.Update(tc);
					}
					catch (KeyNotFoundException)
					{
						await ServiceRef.StorageService.Insert(tc);
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}
		}

		private async Task CreateOrRestoreConfiguration()
		{
			TagConfiguration? Configuration;

			try
			{
				Configuration = await ServiceRef.StorageService.FindFirstDeleteRest<TagConfiguration>();
			}
			catch (Exception findException)
			{
				ServiceRef.LogService.LogException(findException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				Configuration = null;
			}

			if (Configuration is null)
			{
				Configuration = new();

				try
				{
					await ServiceRef.StorageService.Insert(Configuration);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}
			}

			ServiceRef.TagProfile.FromConfiguration(Configuration);
		}

		/// <summary>
		/// Switches the application to the on-boarding experience.
		/// </summary>
		public static Task SetRegistrationPageAsync()
		{
			return SetPage(Constants.Pages.RegistrationPage);
		}

		/// <summary>
		/// Switches the application to the main experience.
		/// </summary>
		public static Task SetMainPageAsync()
		{
			return SetPage(Constants.Pages.MainPage);
		}

		/// <summary>
		/// Sets the current page, if not already shown.
		/// </summary>
		/// <param name="PagePath">Path to current page.</param>
		public static Task SetPage(string PagePath)
		{
			if (Shell.Current.CurrentState.Location?.OriginalString == PagePath)
				return Task.CompletedTask;

			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				return Shell.Current.GoToAsync(PagePath);
			});
		}

		#region Error Handling

		private async void TaskScheduler_UnobservedTaskException(object? Sender, UnobservedTaskExceptionEventArgs e)
		{
			try
			{
				Exception ex = e.Exception;
				e.SetObserved();

				ex = Log.UnnestException(ex);

				await this.Handle_UnhandledException(ex, nameof(TaskScheduler_UnobservedTaskException), false);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async void CurrentDomain_UnhandledException(object? Sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				await this.Handle_UnhandledException(e.ExceptionObject as Exception, nameof(CurrentDomain_UnhandledException), true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task Handle_UnhandledException(Exception? ex, string title, bool shutdown)
		{
			if (ex is not null)
			{
				ServiceRef.LogService.SaveExceptionDump(title, ex.ToString());
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));
			}

			if (shutdown)
				await this.Shutdown(false);

#if DEBUG
			if (!shutdown)
			{
				if (this.MainPage is not null)
				{
					if (this.Dispatcher.IsDispatchRequired)
					{
						this.Dispatcher.Dispatch(async () =>
						{
							await this.MainPage.DisplayAlert(title, ex?.ToString(),
								ServiceRef.Localizer[nameof(AppResources.Ok)]);
						});
					}
					else
					{
						await this.MainPage.DisplayAlert(title, ex?.ToString(),
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
					}
				}
			}
#endif
		}

		private void DisplayBootstrapErrorPage(string Title, string StackTrace)
		{
			this.Dispatcher.Dispatch(() =>
			{
				this.MainPage = new BootstrapErrorPage(Title, StackTrace);
			});
		}

		private static async Task SendErrorReportFromPreviousRun()
		{
			if (ServiceRef.LogService is not null)
			{
				string StackTrace = ServiceRef.LogService.LoadExceptionDump();
				if (!string.IsNullOrWhiteSpace(StackTrace))
				{
					List<KeyValuePair<string, object?>> Tags =
					[
						new KeyValuePair<string, object?>(Constants.XmppProperties.Jid, ServiceRef.XmppService.BareJid)
					];

					KeyValuePair<string, object?>[]? Tags2 = ServiceRef.TagProfile.LegalIdentity?.GetTags();

					if (Tags2 is not null)
						Tags.AddRange(Tags2);

					StringBuilder Msg = new();

					Msg.Append("Unhandled exception caused app to crash. ");
					Msg.AppendLine("Below you can find the stack trace of the corresponding exception.");
					Msg.AppendLine();
					Msg.AppendLine("```");
					Msg.AppendLine(StackTrace);
					Msg.AppendLine("```");

					Log.Alert(Msg.ToString(), Tags.ToArray());

					try
					{
						await SendAlert(StackTrace, "text/plain");
					}
					finally
					{
						ServiceRef.LogService.DeleteExceptionDump();
					}
				}
			}
		}

		internal static async Task SendAlert(string message, string contentType)
		{
			try
			{
				HttpClient client = new()
				{
					Timeout = TimeSpan.FromSeconds(30)
				};

				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				StringContent content = new(message);
				content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

				await client.PostAsync("https://lab.tagroot.io/Alert.ws", content);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Sends the contents of the database to support.
		/// </summary>
		/// <param name="FileName">Name of file used to keep track of state changes.</param>
		/// <param name="IncludeUnchanged">If unchanged material should be included.</param>
		/// <param name="SendAsAlert">If an alert is to be sent.</param>
		public static async Task EvaluateDatabaseDiff(string FileName, bool IncludeUnchanged, bool SendAsAlert)
		{
			StringBuilder Xml = new();

			using XmlDatabaseExport Output = new(Xml, true, 256);
			await Database.Export(Output);

			string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileName = Path.Combine(AppDataFolder, FileName);

			string CurrentState = Xml.ToString();
			string PrevState = File.Exists(FileName) ? File.ReadAllText(FileName) : string.Empty;

			EditScript<string> Script = Difference.AnalyzeRows(PrevState, CurrentState);
			StringBuilder Markdown = new();
			string Prefix;

			Markdown.Append("Database content changes (`");
			Markdown.Append(FileName);
			Markdown.AppendLine("`):");

			foreach (Step<string> Step in Script.Steps)
			{
				Markdown.AppendLine();

				switch (Step.Operation)
				{
					case EditOperation.Keep:
					default:
						if (!IncludeUnchanged)
						{
							continue;
						}

						Prefix = ">\t";
						break;

					case EditOperation.Insert:
						Prefix = "+>\t";
						break;

					case EditOperation.Delete:
						Prefix = "->\t";
						break;
				}

				Markdown.Append(Prefix);
				Markdown.AppendLine("```xml");

				foreach (string Row in Step.Symbols)
				{
					Markdown.Append(Prefix);
					Markdown.Append(Row);
					Markdown.AppendLine("  ");
				}

				Markdown.Append(Prefix);
				Markdown.AppendLine("```");
			}

			string DiffMsg = Markdown.ToString();

			if (SendAsAlert)
			{
				await SendAlert(DiffMsg, "text/markdown");
			}

			File.WriteAllText(FileName, CurrentState);
			File.WriteAllText(FileName + ".diff.md", DiffMsg);
		}

		#endregion

		/// <summary>
		/// Opens an URL in the application.
		/// </summary>
		/// <param name="Url">URL</param>
		/// <returns>If URL is processed or not.</returns>
		public static void OpenUrlSync(string Url)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await QrCode.OpenUrl(Url);
			});
		}

		/// <summary>
		/// Opens an URL in the application.
		/// </summary>
		/// <param name="Url">URL</param>
		/// <returns>If URL is processed or not.</returns>
		public static Task<bool> OpenUrlAsync(string Url)
		{
			return OpenUrlAsync(Url, true);
		}

		/// <summary>
		/// Opens an URL in the application.
		/// </summary>
		/// <param name="Url">URL</param>
		/// <param name="ShowErrorIfUnable">If an error message should be displayed, in case the URI could not be opened.</param>
		/// <returns>If URL is processed or not.</returns>
		public static Task<bool> OpenUrlAsync(string Url, bool ShowErrorIfUnable)
		{
			return QrCode.OpenUrl(Url, ShowErrorIfUnable);
		}

		/// <summary>
		/// Asks the user to input its password. Password is verified before being returned.
		/// </summary>
		/// <returns>Password, if the user has provided the correct password. Empty string, if password is not configured,
		/// null if operation is cancelled.</returns>
		public static async Task<string?> InputPassword()
		{
			ITagProfile Profile = ServiceRef.TagProfile;
			if (!Profile.HasLocalPassword)
				return string.Empty;

			return await InputPassword(Profile);
		}

		private static async Task<string?> InputPassword(ITagProfile Profile)
		{
			displayedPasswordPopup = true;

			try
			{
				if (!Profile.HasLocalPassword)
					return string.Empty;

				string? result = await ServiceRef.UiService.PushAsync<CheckPasswordPopup, CheckPasswordViewModel, string>();
				await CheckUserBlocking();
				return result;
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				displayedPasswordPopup = false;
			}
		}

		/// <summary>
		/// Authenticates the user using the configured authentication method.
		/// </summary>
		/// <returns>If the user has been successfully authenticated.</returns>
		public static Task<bool> AuthenticateUser(bool Force = false)
		{
			if (MainThread.IsMainThread)
				return AuthenticateUserMainThread(Force);
			else
			{
				TaskCompletionSource<bool> Result = new();

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					Result.TrySetResult(await AuthenticateUserMainThread(Force));
				});

				return Result.Task;
			}
		}

		private static async Task<bool> AuthenticateUserMainThread(bool Force = false)
		{
			bool NeedToVerifyPassword = IsInactivitySafeIntervalPassed();
			if (!Force && !NeedToVerifyPassword)
				return true;

			switch (ServiceRef.TagProfile.AuthenticationMethod)
			{
				case AuthenticationMethod.Password:
					if (!ServiceRef.TagProfile.HasLocalPassword)
						return true;

					if (displayedPasswordPopup)
						return false;

					return await InputPassword(ServiceRef.TagProfile) is not null;

				case AuthenticationMethod.Fingerprint:
					if (!ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication)
						return false;

					if (await ServiceRef.PlatformSpecific.AuthenticateUserFingerprint(
						ServiceRef.Localizer[nameof(AppResources.FingerprintTitle)],
						null,
						ServiceRef.Localizer[nameof(AppResources.FingerprintDescription)],
						ServiceRef.Localizer[nameof(AppResources.Cancel)],
						null))
					{
						await UserAuthenticationSuccessful();
						return true;
					}
					else
					{
						await UserAuthenticationFailed();
						return false;
					}

				default:
					return false;
			}
		}

		/// <summary>
		/// Verify if the user is blocked and show an alert
		/// </summary>
		public static async Task CheckUserBlocking()
		{
			DateTime? DateTimeForLogin = await appInstance!.loginAuditor.GetEarliestLoginOpportunity(Constants.Password.RemoteEndpoint, Constants.Password.Protocol);

			if (DateTimeForLogin.HasValue)
			{
				IUiService Ui = ServiceRef.UiService;
				string MessageAlert;

				if (DateTimeForLogin == DateTime.MaxValue)
					MessageAlert = ServiceRef.Localizer[nameof(AppResources.PasswordIsInvalidAplicationBlockedForever)];
				else
				{
					DateTime LocalDateTime = DateTimeForLogin.Value.ToLocalTime();
					if (DateTimeForLogin.Value.ToShortDateString() == DateTime.Today.ToShortDateString())
					{
						string DateString = LocalDateTime.ToShortTimeString();
						MessageAlert = ServiceRef.Localizer[nameof(AppResources.PasswordIsInvalidAplicationBlocked), DateString];
					}
					else if (DateTimeForLogin.Value.ToShortDateString() == DateTime.Today.AddDays(1).ToShortDateString())
					{
						string DateString = LocalDateTime.ToShortTimeString();
						MessageAlert = ServiceRef.Localizer[nameof(AppResources.PasswordIsInvalidAplicationBlockedTillTomorrow), DateString];
					}
					else
					{
						string DateString = LocalDateTime.ToString("yyyy-MM-dd, 'at' HH:mm", CultureInfo.InvariantCulture);
						MessageAlert = ServiceRef.Localizer[nameof(AppResources.PasswordIsInvalidAplicationBlocked), DateString];
					}
				}

				await Ui.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], MessageAlert);
				await Stop();
			}
		}

		/// <summary>
		/// Check the Password and reset the blocking counters if it matches
		/// </summary>
		/// <param name="Password">Password to check.</param>
		public static async Task<bool> CheckPasswordAndUnblockUser(string Password)
		{
			if (Password is null)
				return false;

			if (ServiceRef.TagProfile.ComputePasswordHash(Password) == ServiceRef.TagProfile.LocalPasswordHash)
			{
				await UserAuthenticationSuccessful();
				return true;
			}
			else
			{
				await UserAuthenticationFailed();
				return false;
			}
		}

		private static async Task UserAuthenticationSuccessful()
		{
			SetStartInactivityTime();
			SetCurrentPasswordCounter(0);
			await appInstance!.loginAuditor.UnblockAndReset(Constants.Password.RemoteEndpoint);
		}

		private static async Task UserAuthenticationFailed()
		{
			await appInstance!.loginAuditor.ProcessLoginFailure(Constants.Password.RemoteEndpoint,
				Constants.Password.Protocol, DateTime.Now, Constants.Password.Reason);

			long PasswordAttemptCounter = await GetCurrentPasswordCounter();

			PasswordAttemptCounter++;
			SetCurrentPasswordCounter(PasswordAttemptCounter);
		}

		/// <summary>
		/// Set start time of inactivity
		/// </summary>
		private static void SetStartInactivityTime()
		{
			savedStartTime = DateTime.Now;
		}

		/// <summary>
		/// Performs a check whether 5 minutes of inactivity interval has been passed
		/// </summary>
		/// <returns>True if 5 minutes has been passed and False if has not been passed</returns>
		private static bool IsInactivitySafeIntervalPassed()
		{
			return DateTime.Now.Subtract(savedStartTime).TotalMinutes > Constants.Password.PossibleInactivityInMinutes;
		}

		/// <summary>
		/// Obtains the value for CurrentPasswordCounter
		/// </summary>
		internal static async Task<long> GetCurrentPasswordCounter()
		{
			return await ServiceRef.SettingsService.RestoreLongState(Constants.Password.CurrentPasswordAttemptCounter);
		}

		/// <summary>
		/// Saves that the value for CurrentPasswordCounter
		/// </summary>
		private static async void SetCurrentPasswordCounter(long CurrentPasswordAttemptCounter)
		{
			await ServiceRef.SettingsService.SaveState(Constants.Password.CurrentPasswordAttemptCounter, CurrentPasswordAttemptCounter);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			if (disposing)
			{
				this.loginAuditor.Dispose();
				this.autoSaveTimer?.Dispose();
				this.initCompleted.Dispose();
				this.startupWorker.Dispose();
				this.startupCancellation.Dispose();
			}

			this.isDisposed = true;
		}
	}
}
