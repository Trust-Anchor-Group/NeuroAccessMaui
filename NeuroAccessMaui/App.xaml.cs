using NeuroAccessMaui.DeviceSpecific;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.AttachmentCache;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.Crypto;
using NeuroAccessMaui.Services.EventLog;
using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.Services.Network;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.Settings;
using NeuroAccessMaui.Services.Storage;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.Services.Xmpp;
//!!! using Rg.Plugins.Popup.Services;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.DNS;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HTTPX;
using Waher.Networking.XMPP.P2P;
using Waher.Networking.XMPP.P2P.E2E;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;
using Waher.Runtime.Settings;
using Waher.Runtime.Text;
using Waher.Script;
using Waher.Script.Content;
using Waher.Script.Graphs;
using Waher.Security.LoginMonitor;
//!!! using Xamarin.CommunityToolkit.Helpers;
//!!! using Xamarin.Essentials;
//!!! using Xamarin.Forms;
//!!! using Xamarin.Forms.Internals;

namespace NeuroAccessMaui;

/// <summary>
/// The Application class, representing an instance of the IdApp.
/// </summary>
public partial class App : Application, IDisposable
{
	private static readonly TaskCompletionSource<bool> servicesSetup = new();
	private static readonly TaskCompletionSource<bool> defaultInstantiatedSource = new();
	private static bool configLoaded = false;
	private static ISecureDisplay secureDisplay;
	private static bool defaultInstantiated = false;
	private static DateTime savedStartTime = DateTime.MinValue;
	private static bool displayedPinPopup = false;
	private static int startupCounter = 0;
	private readonly LoginAuditor loginAuditor;
	private Timer autoSaveTimer;
	private IServiceReferences services;
	private Profiler startupProfiler;
	private readonly Task<bool> initCompleted;
	private readonly SemaphoreSlim startupWorker = new(1, 1);
	private CancellationTokenSource startupCancellation;

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
	public static new App? Current => (App?)Application.Current;

	///<inheritdoc/>
	public App(Assembly DeviceAssembly) : this(false, DeviceAssembly)
	{
	}

	///<inheritdoc/>
	public App(bool BackgroundStart, Assembly DeviceAssembly)
	{
		App? PreviousInstance = appInstance;
		appInstance = this;

		this.onStartResumesApplication = PreviousInstance is not null;

		// If the previous instance is null, create the app state from scratch. If not, just copy the state from the previous instance.
		if (PreviousInstance is null)
		{
			this.InitLocalizationResource();

			this.startupProfiler = new Profiler("App.ctor", ProfilerThreadType.Sequential);  // Comment out to remove startup profiling.
			this.startupProfiler?.Start();
			this.startupProfiler?.NewState("Init");

			AppDomain.CurrentDomain.FirstChanceException += this.CurrentDomain_FirstChanceException;
			AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += this.TaskScheduler_UnobservedTaskException;

			LoginInterval[] LoginIntervals = new[] {
				new LoginInterval(Constants.Pin.MaxPinAttempts, TimeSpan.FromHours(Constants.Pin.FirstBlockInHours)),
				new LoginInterval(Constants.Pin.MaxPinAttempts, TimeSpan.FromHours(Constants.Pin.SecondBlockInHours)),
				new LoginInterval(Constants.Pin.MaxPinAttempts, TimeSpan.FromHours(Constants.Pin.ThirdBlockInHours))};

			this.loginAuditor = new LoginAuditor(Constants.Pin.LogAuditorObjectID, LoginIntervals);
			this.startupCancellation = new CancellationTokenSource();
			this.initCompleted = this.Init(BackgroundStart, DeviceAssembly);
		}
		else
		{
			this.loginAuditor = PreviousInstance.loginAuditor;
			this.autoSaveTimer = PreviousInstance.autoSaveTimer;
			this.services = PreviousInstance.services;
			this.startupProfiler = PreviousInstance.startupProfiler;
			this.initCompleted = PreviousInstance.initCompleted;
			this.startupWorker = PreviousInstance.startupWorker;
			this.startupCancellation = PreviousInstance.startupCancellation;
		}

		if (!BackgroundStart)
		{
			this.InitializeComponent();
			Current.UserAppTheme = OSAppTheme.Unspecified;

			// Start page
			try
			{
				this.startupProfiler?.NewState("MainPage");

				this.MainPage = ServiceHelper.GetService<Pages.AppShell>();
			}
			catch (Exception ex)
			{
				this.HandleStartupException(ex);
			}
		}

		this.startupProfiler?.MainThread?.Idle();
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
	/// Selected language.
	/// </summary>
	public static string SelectedLanguage
	{
		get
		{
			string Language = Preferences.Get("user_selected_language", null);

			if (Language is null)
			{
				List<string> SupportedLanguages = new() { "en", "sv", "es", "fr", "de", "da", "no", "fi", "sr", "pt", "ro", "ru" };
				string LanguageName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
				string SupportedLanguage = SupportedLanguages.FirstOrDefault(el => el == LanguageName);
				Language = string.IsNullOrEmpty(SupportedLanguage) ? "en" : LanguageName;

				Preferences.Set("user_selected_language", Language);
			}

			return Language;
		}
	}

	private void InitLocalizationResource()
	{
		string Language = SelectedLanguage;

		CultureInfo[] Infos = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
		CultureInfo SelectedInfo = Infos.First(el => el.Name == Language);

		LocalizationResourceManager.Current.PropertyChanged += (_, _) => AppResources.Culture = LocalizationResourceManager.Current.CurrentCulture;
		LocalizationResourceManager.Current.Init(AppResources.ResourceManager, SelectedInfo);
	}

	private Task<bool> Init(bool BackgroundStart, Assembly DeviceAssembly)
	{
		ProfilerThread Thread = this.startupProfiler?.CreateThread("Init", ProfilerThreadType.Sequential);
		Thread?.Start();

		TaskCompletionSource<bool> Result = new();
		Task.Run(async () => await this.InitInParallel(Thread, Result, BackgroundStart, DeviceAssembly));
		return Result.Task;
	}

	private async Task InitInParallel(ProfilerThread Thread, TaskCompletionSource<bool> Result, bool BackgroundStart,
		Assembly DeviceAssembly)
	{
		try
		{
			this.InitInstances(Thread, DeviceAssembly);

			await this.PerformStartup(false, Thread, BackgroundStart);

			Result.TrySetResult(true);
		}
		catch (Exception ex)
		{
			ex = Waher.Events.Log.UnnestException(ex);
			Thread?.Exception(ex);
			this.HandleStartupException(ex);

			servicesSetup.TrySetResult(false);
			Result.TrySetResult(false);
		}

		Thread?.Stop();
	}

	private void InitInstances(ProfilerThread Thread, Assembly DeviceAssembly)
	{
		Thread?.NewState("Types");

		Assembly appAssembly = this.GetType().Assembly;

		if (!Types.IsInitialized)
		{
			// Define the scope and reach of Runtime.Inventory (Script, Serialization, Persistence, IoC, etc.):
			Types.Initialize(
				appAssembly,                                // Allows for objects defined in this assembly, to be instantiated and persisted.
				DeviceAssembly,                             // Device-specific assembly.
				typeof(Database).Assembly,                  // Indexes default attributes
				typeof(ObjectSerializer).Assembly,          // Indexes general serializers
				typeof(FilesProvider).Assembly,             // Indexes special serializers
				typeof(RuntimeSettings).Assembly,           // Allows for persistence of settings in the object database
				typeof(InternetContent).Assembly,           // Common Content-Types
				typeof(ImageCodec).Assembly,                // Common Image Content-Types
				typeof(XML).Assembly,                       // XML Content-Type
				typeof(DnsResolver).Assembly,               // Serialization of DNS-related objects
				typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
				typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
				typeof(Expression).Assembly,                // Indexes basic script functions
				typeof(Graph).Assembly,                     // Indexes graph script functions
				typeof(GraphEncoder).Assembly,              // Indexes content script functions
				typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
				typeof(HttpxClient).Assembly);              // Support for HTTP over XMPP (httpx) URI Schme.
		}

		EndpointSecurity.SetCiphers(new Type[]
		{
			typeof(Edwards448Endpoint)
		}, false);

		Thread?.NewState("SDK");

		// Create Services

		Types.InstantiateDefault<ITagProfile>(false);
		Types.InstantiateDefault<ILogService>(false);
		Types.InstantiateDefault<IUiSerializer>(false);
		Types.InstantiateDefault<ICryptoService>(false);
		Types.InstantiateDefault<INetworkService>(false);
		Types.InstantiateDefault<IStorageService>(false);
		Types.InstantiateDefault<ISettingsService>(false);
		Types.InstantiateDefault<INavigationService>(false);
		Types.InstantiateDefault<IXmppService>(false, appAssembly, this.startupProfiler);
		Types.InstantiateDefault<IAttachmentCacheService>(false);
		Types.InstantiateDefault<IContractOrchestratorService>(false);
		Types.InstantiateDefault<INfcService>(false);

		this.services = new ServiceReferences();

		defaultInstantiatedSource.TrySetResult(true);
		defaultInstantiated = true;

		// Set resolver

		DependencyResolver.ResolveUsing(type =>
		{
			if (Types.GetType(type.FullName) is null)
			{
				return null;    // Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.
			}

			if (type.Assembly == DeviceAssembly && Types.GetDefaultConstructor(type) is null)
			{
				return null;
			}

			try
			{
				return Types.Instantiate(true, type);
			}
			catch (Exception ex)
			{
				this.services.LogService.LogException(ex);
				return null;
			}
		});

		secureDisplay = DependencyService.Get<ISecureDisplay>();

		servicesSetup.TrySetResult(true);
	}

	private void HandleStartupException(Exception ex)
	{
		ex = Waher.Events.Log.UnnestException(ex);
		this.startupProfiler?.Exception(ex);
		this.services?.LogService?.SaveExceptionDump("StartPage", ex.ToString());
		this.DisplayBootstrapErrorPage(ex.Message, ex.StackTrace);
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
		{
			defaultInstantiated = defaultInstantiatedSource.Task.Result;
		}

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
		{
			throw new Exception("Initialization did not complete in time.");
		}

		this.StartupCompleted("StartupProfile.uml", false);
	}

	///<inheritdoc/>
	protected override async void OnStart()
	{
		if (this.onStartResumesApplication)
		{
			this.onStartResumesApplication = false;
			this.OnResume();
			return;
		}

		if (!this.initCompleted.Wait(60000))
		{
			throw new Exception("Initialization did not complete in time.");
		}

		this.StartupCompleted("StartupProfile.uml", false);

		if (!await App.VerifyPin())
		{
			await App.Stop();
		}
	}

	/// <summary>
	/// Resume the services start in the background.
	/// </summary>
	public async Task DoResume(bool BackgroundStart)
	{
		appInstance = this;
		this.startupCancellation = new CancellationTokenSource();

		await this.PerformStartup(true, null, BackgroundStart);
	}

	///<inheritdoc/>
	protected override async void OnResume()
	{
		await this.DoResume(false);

		if (!await App.VerifyPin())
		{
			await App.Stop();
		}
	}

	private async Task PerformStartup(bool isResuming, ProfilerThread Thread, bool BackgroundStart)
	{
		await this.startupWorker.WaitAsync();

		try
		{
			// do nothing if the services are already started
			if (++App.startupCounter > 1)
			{
				return;
			}

			// cancel the startup if the application is closed
			CancellationToken Token = this.startupCancellation.Token;
			Token.ThrowIfCancellationRequested();

			if (!BackgroundStart)
			{
				Thread?.NewState("Report");
				await this.SendErrorReportFromPreviousRun();

				Thread?.NewState("Startup");
				this.services.UiSerializer.IsRunningInTheBackground = false;

				Token.ThrowIfCancellationRequested();
			}

			Thread?.NewState("DB");
			ProfilerThread SubThread = Thread?.CreateSubThread("Database", ProfilerThreadType.Sequential);

			await this.services.StorageService.Init(SubThread, Token);

			if (!App.configLoaded)
			{
				Thread?.NewState("Config");
				await this.CreateOrRestoreConfiguration();

				App.configLoaded = true;
			}

			Token.ThrowIfCancellationRequested();

			Thread?.NewState("Network");
			await this.services.NetworkService.Load(isResuming, Token);

			Token.ThrowIfCancellationRequested();

			Thread?.NewState("XMPP");
			await this.services.XmppService.Load(isResuming, Token);

			Token.ThrowIfCancellationRequested();

			Thread?.NewState("Timer");
			TimeSpan initialAutoSaveDelay = Constants.Intervals.AutoSave.Multiply(4);
			this.autoSaveTimer = new Timer(async _ => await this.AutoSave(), null, initialAutoSaveDelay, Constants.Intervals.AutoSave);

			Thread?.NewState("Navigation");
			await this.services.NavigationService.Load(isResuming, Token);

			Thread?.NewState("Cache");
			await this.services.AttachmentCacheService.Load(isResuming, Token);

			Thread?.NewState("Orchestrators");
			await this.services.ContractOrchestratorService.Load(isResuming, Token);
		}
		catch (OperationCanceledException)
		{
			Waher.Events.Log.Notice($"{(isResuming ? "OnResume " : "Initial app ")} startup was canceled.");
		}
		catch (Exception ex)
		{
			ex = Waher.Events.Log.UnnestException(ex);
			Thread?.Exception(ex);
			this.services?.LogService?.SaveExceptionDump(ex.Message, ex.StackTrace);
			this.DisplayBootstrapErrorPage(ex.Message, ex.StackTrace);
		}
		finally
		{
			Thread?.Stop();
			this.startupWorker.Release();
		}
	}

	/// <summary>
	/// Awaiting the services stop in the background.
	/// </summary>
	public async Task OnBackgroundSleep()
	{
		await this.Shutdown(false, true);
	}

	///<inheritdoc/>
	protected override async void OnSleep()
	{
		// Done manually here, as the Disappearing event won't trigger when exiting the app,
		// and we want to make sure state is persisted and teardown is done correctly to avoid memory leaks.

		if (this.MainPage?.BindingContext is BaseViewModel vm)
		{
			await vm.Shutdown();
		}

		await this.Shutdown(false, false);

		this.SetStartInactivityTime();
	}

	internal static async Task Stop()
	{
		if (appInstance is not null)
		{
			await appInstance.Shutdown(false, false);
			appInstance = null;
		}

		ICloseApplication closeApp = DependencyService.Get<ICloseApplication>();

		if (closeApp is not null)
		{
			await closeApp.Close();
		}
		else
		{
			Environment.Exit(0);
		}
	}

	private async Task Shutdown(bool inPanic, bool BackgroundStart)
	{
		// if the PerformStartup is not finished, cancel it first
		this.startupCancellation.Cancel();
		await this.startupWorker.WaitAsync();

		try
		{
			// do nothing if the services are already stopped
			// or if the startup counter is greater than one
			if ((App.startupCounter < 1) || (--App.startupCounter > 0))
			{
				return;
			}

			this.StopAutoSaveTimer();

			if (this.services is not null)
			{
				if (!BackgroundStart)
				{
					if (this.services.UiSerializer is not null)
					{
						this.services.UiSerializer.IsRunningInTheBackground = !inPanic;
					}
				}

				if (inPanic)
				{
					if (this.services.XmppService is not null)
					{
						await this.services.XmppService.UnloadFast();
					}
				}
				else
				{
					if (this.services.NavigationService is not null)
					{
						await this.services.NavigationService.Unload();
					}

					if (this.services.ContractOrchestratorService is not null)
					{
						await this.services.ContractOrchestratorService.Unload();
					}

					if (this.services.XmppService is not null)
					{
						await this.services.XmppService.Unload();
					}

					if (this.services.NetworkService is not null)
					{
						await this.services.NetworkService.Unload();
					}

					if (this.services.AttachmentCacheService is not null)
					{
						await this.services.AttachmentCacheService.Unload();
					}

					if (this.services.NavigationService is not null)
					{
						await this.services.NavigationService.Unload();
					}
				}

				foreach (IEventSink Sink in Waher.Events.Log.Sinks)
				{
					Waher.Events.Log.Unregister(Sink);
				}

				if (this.services.StorageService is not null)
				{
					await this.services.StorageService.Shutdown();
				}
			}

			// Causes list of singleton instances to be cleared.
			Waher.Events.Log.Terminate();
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
		if (this.services.TagProfile.IsDirty)
		{
			this.services.TagProfile.ResetIsDirty();
			try
			{
				TagConfiguration tc = this.services.TagProfile.ToConfiguration();

				try
				{
					if (string.IsNullOrEmpty(tc.ObjectId))
					{
						await this.services.StorageService.Insert(tc);
					}
					else
					{
						await this.services.StorageService.Update(tc);
					}
				}
				catch (KeyNotFoundException)
				{
					await this.services.StorageService.Insert(tc);
				}
			}
			catch (Exception ex)
			{
				this.services.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}
		}
	}

	private async Task CreateOrRestoreConfiguration()
	{
		TagConfiguration configuration;

		try
		{
			configuration = await this.services.StorageService.FindFirstDeleteRest<TagConfiguration>();
		}
		catch (Exception findException)
		{
			this.services.LogService.LogException(findException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			configuration = null;
		}

		if (configuration is null)
		{
			configuration = new TagConfiguration();

			try
			{
				await this.services.StorageService.Insert(configuration);
			}
			catch (Exception insertException)
			{
				this.services.LogService.LogException(insertException, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}
		}

		await this.services.TagProfile.FromConfiguration(configuration);
	}

	/// <summary>
	/// Switches the application to the on-boarding experience.
	/// </summary>
	public static Task SetRegistrationPageAsync()
	{
		return Shell.Current.GoToAsync("//Registration");
	}

	/// <summary>
	/// Switches the application to the main experience.
	/// </summary>
	public static Task SetMainPageAsync()
	{
		if (CanProhibitScreenCapture)
		{
			ProhibitScreenCapture = true;
		}

		return Shell.Current.GoToAsync("//MainPage");
	}

	//!!!
	/*
	private async Task SetMainPageAsync(Page Page)
	{
		Page CurrentPage = this.MainPage is Shell Shell ? Shell.CurrentPage : this.MainPage;
		if (CurrentPage is NavigationPage NavigationPage)
			CurrentPage = NavigationPage.CurrentPage;

		if (CurrentPage is not Pages.Main.Loading.LoadingPage)
		{
			await CurrentPage.Navigation.PushModalAsync(new BetweenMainPage(), animated: false);
		}

		if (CurrentPage is ContentBasePage ContentBasePage)
		{
			await ContentBasePage.ViewModel.Disappearing();
			await ContentBasePage.ViewModel.Dispose();
		}

		this.MainPage = Page;
	}
	*/

	#region Error Handling

	private void TaskScheduler_UnobservedTaskException(object Sender, UnobservedTaskExceptionEventArgs e)
	{
		Exception ex = e.Exception;
		e.SetObserved();

		ex = Waher.Events.Log.UnnestException(ex);

		this.Handle_UnhandledException(ex, nameof(TaskScheduler_UnobservedTaskException), false).Wait();
	}

	private void CurrentDomain_UnhandledException(object Sender, UnhandledExceptionEventArgs e)
	{
		this.Handle_UnhandledException(e.ExceptionObject as Exception, nameof(CurrentDomain_UnhandledException), true).Wait();
	}

	private async Task Handle_UnhandledException(Exception ex, string title, bool shutdown)
	{
		if (ex is not null)
		{
			this.services?.LogService?.SaveExceptionDump(title, ex.ToString());
			this.services?.LogService?.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod(), title));
		}

		if (shutdown)
		{
			await this.Shutdown(false, false);
		}

#if DEBUG
		if (!shutdown)
		{
			if (Device.IsInvokeRequired && (this.MainPage is not null))
			{
				Device.BeginInvokeOnMainThread(async () => await this.MainPage.DisplayAlert(title, ex?.ToString(), LocalizationResourceManager.Current["Ok"]));
			}
			else if (this.MainPage is not null)
			{
				await this.MainPage.DisplayAlert(title, ex?.ToString(), LocalizationResourceManager.Current["Ok"]);
			}
		}
#endif
	}

	private void CurrentDomain_FirstChanceException(object Sender, FirstChanceExceptionEventArgs e)
	{
		this.startupProfiler?.Exception(e.Exception);
	}

	private void DisplayBootstrapErrorPage(string Title, string StackTrace)
	{
		this.Dispatcher.BeginInvokeOnMainThread(() =>
		{
			this.MainPage = new BootstrapErrorPage
			{
				BindingContext = new BootstrapErrorViewModel
				{
					Title = Title,
					StackTrace = StackTrace,
				},
			};
		});
	}

	private async Task SendErrorReportFromPreviousRun()
	{
		if (this.services?.LogService is not null)
		{
			string StackTrace = this.services.LogService.LoadExceptionDump();
			if (!string.IsNullOrWhiteSpace(StackTrace))
			{
				List<KeyValuePair<string, object>> Tags = new()
				{
					new KeyValuePair<string, object>(Constants.XmppProperties.Jid, this.services?.XmppService?.BareJid)
				};

				KeyValuePair<string, object>[] Tags2 = this.services?.TagProfile?.LegalIdentity?.GetTags();

				if (Tags2 is not null)
				{
					Tags.AddRange(Tags2);
				}

				StringBuilder Msg = new();

				Msg.Append("Unhandled exception caused app to crash. ");
				Msg.AppendLine("Below you can find the stack trace of the corresponding exception.");
				Msg.AppendLine();
				Msg.AppendLine("```");
				Msg.AppendLine(StackTrace);
				Msg.AppendLine("```");

				Waher.Events.Log.Alert(Msg.ToString(), Tags.ToArray());

				try
				{
					await SendAlert(StackTrace, "text/plain");
				}
				finally
				{
					this.services.LogService.DeleteExceptionDump();
				}
			}
		}
	}

	internal static async Task SendAlert(string message, string contentType)
	{
		try
		{
			HttpClient client = new();
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			StringContent content = new(message);
			content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

			await client.PostAsync("https://lab.tagroot.io/Alert.ws", content);
		}
		catch (Exception ex)
		{
			Waher.Events.Log.Critical(ex);
		}
	}

	private void StartupCompleted(string ProfileFileName, bool SendProfilingAsAlert)
	{
		AppDomain.CurrentDomain.FirstChanceException -= this.CurrentDomain_FirstChanceException;

		if (this.startupProfiler is not null)
		{
			this.startupProfiler.Stop();

			string uml = this.startupProfiler.ExportPlantUml(TimeUnit.MilliSeconds);

			try
			{
				string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

				if (!Directory.Exists(AppDataFolder))
				{
					Directory.CreateDirectory(AppDataFolder);
				}

				ProfileFileName = Path.Combine(AppDataFolder, ProfileFileName);
				File.WriteAllText(ProfileFileName, uml);
			}
			catch (Exception)
			{
				// Ignore, if not able to save file.
			}

			if (SendProfilingAsAlert)
			{
				Task.Run(async () =>
				{
					try
					{
						await SendAlert("```uml\r\n" + uml + "```", "text/markdown");
					}
					catch (Exception ex)
					{
						Waher.Events.Log.Critical(ex);
					}
				});
			}

			this.startupProfiler = null;
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

		using (XmlDatabaseExport Output = new(Xml, true, 256))
		{
			await Database.Export(Output);
		}

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
		Current.services.UiSerializer.BeginInvokeOnMainThread(async () =>
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
		return QrCode.OpenUrl(Url);
	}

	/// <summary>
	/// Asks the user to input its PIN. PIN is verified before being returned.
	/// </summary>
	/// <returns>PIN, if the user has provided the correct PIN. Empty string, if PIN is not configured, null if operation is cancelled.</returns>
	public static async Task<string> InputPin()
	{
		ITagProfile Profile = App.Instantiate<ITagProfile>();

		if (!Profile.HasPin)
		{
			return string.Empty;
		}

		return await InputPin(Profile);
	}

	/// <summary>
	/// Asks the user to verify with its PIN.
	/// </summary>
	/// <returns>If the user has provided the correct PIN</returns>
	public static async Task<bool> VerifyPin(bool Force = false)
	{
#if DEBUG
		// Skip the PIN verification during the debug. Set the IsDebug to false to work normal
		bool IsDebug = true;
#else
		bool IsDebug = false;
#endif
		if (!IsDebug)
		{
			ITagProfile Profile = Instantiate<ITagProfile>();

			if (!Profile.HasPin)
			{
				return true;
			}

			bool NeedToVerifyPin = IsInactivitySafeIntervalPassed();

			if (!displayedPinPopup && (Force || NeedToVerifyPin))
			{
				return await InputPin(Profile) is not null;
			}
		}

		return true;
	}

	/// <summary>
	/// Verify if the user is blocked and show an allert
	/// </summary>
	public static async Task CheckUserBlocking()
	{
		DateTime? DateTimeForLogin = await appInstance.loginAuditor.GetEarliestLoginOpportunity(Constants.Pin.RemoteEndpoint, Constants.Pin.Protocol);

		if (DateTimeForLogin.HasValue)
		{
			IUiSerializer Ui = Instantiate<IUiSerializer>();
			string MessageAlert;

			if (DateTimeForLogin == DateTime.MaxValue)
			{
				MessageAlert = LocalizationResourceManager.Current["PinIsInvalidAplicationBlockedForever"];
			}
			else
			{
				DateTime LocalDateTime = DateTimeForLogin.Value.ToLocalTime();
				if (DateTimeForLogin.Value.ToShortDateString() == DateTime.Today.ToShortDateString())
				{
					string DateString = LocalDateTime.ToShortTimeString();
					MessageAlert = string.Format(LocalizationResourceManager.Current["PinIsInvalidAplicationBlocked"], DateString);
				}
				else if (DateTimeForLogin.Value.ToShortDateString() == DateTime.Today.AddDays(1).ToShortDateString())
				{
					string DateString = LocalDateTime.ToShortTimeString();
					MessageAlert = string.Format(LocalizationResourceManager.Current["PinIsInvalidAplicationBlockedTillTomorrow"], DateString);
				}
				else
				{
					string DateString = LocalDateTime.ToString("yyyy-MM-dd, 'at' HH:mm");
					MessageAlert = string.Format(LocalizationResourceManager.Current["PinIsInvalidAplicationBlocked"], DateString);
				}
			}

			await Ui.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], MessageAlert);
			await Stop();
		}
	}

	/// <summary>
	/// Check the PIN and reset the blocking counters if it matches
	/// </summary>
	public static async Task<string> CheckPinAndUnblockUser(string Pin, ITagProfile Profile)
	{
		if (Pin is null)
		{
			return null;
		}

		long PinAttemptCounter = await GetCurrentPinCounter();

		if (Profile.ComputePinHash(Pin) == Profile.PinHash)
		{
			ClearStartInactivityTime();
			SetCurrentPinCounter(0);
			await appInstance.loginAuditor.UnblockAndReset(Constants.Pin.RemoteEndpoint);
			await PopupNavigation.Instance.PopAsync();
			return Pin;
		}
		else
		{
			await appInstance.loginAuditor.ProcessLoginFailure(Constants.Pin.RemoteEndpoint,
					Constants.Pin.Protocol, DateTime.Now, Constants.Pin.Reason);

			PinAttemptCounter++;
			SetCurrentPinCounter(PinAttemptCounter);
		}

		long RemainingAttempts = Constants.Pin.MaxPinAttempts - PinAttemptCounter;
		IUiSerializer Ui = Instantiate<IUiSerializer>();

		await Ui.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], string.Format(LocalizationResourceManager.Current["PinIsInvalid"], RemainingAttempts));
		await CheckUserBlocking();
		return Pin;
	}

	private static async Task<string> InputPin(ITagProfile Profile)
	{
		displayedPinPopup = true;

		try
		{
			if (!Profile.HasPin)
			{
				return string.Empty;
			}

			Popups.Pin.PinPopup.PinPopupPage Page = new();
			await PopupNavigation.Instance.PushAsync(Page);
			await CheckUserBlocking();
			return await Page.Result;
		}
		catch (Exception)
		{
			return null;
		}
		finally
		{
			displayedPinPopup = false;
		}
	}

	/// <summary>
	/// Set start time of inactivity
	/// </summary>
	private void SetStartInactivityTime()
	{
		savedStartTime = DateTime.Now;
	}

	/// <summary>
	/// Clears the conditions of checking inactivity
	/// </summary>
	private static void ClearStartInactivityTime()
	{
		savedStartTime = DateTime.MaxValue;
	}

	/// <summary>
	/// Performs a check whether 5 minutes of inactivity interval has been passed
	/// </summary>
	/// <returns>True if 5 minutes has been passed and False if has not been passed</returns>
	private static bool IsInactivitySafeIntervalPassed()
	{
		return DateTime.Now.Subtract(savedStartTime).TotalMinutes
			> Constants.Pin.PossibleInactivityInMinutes;
	}

	/// <summary>
	/// Obtains the value for CurrentPinCounter
	/// </summary>
	private static async Task<long> GetCurrentPinCounter()
	{
		return await appInstance.services.SettingsService.RestoreLongState(Constants.Pin.CurrentPinAttemptCounter);
	}

	/// <summary>
	/// Saves that the value for CurrentPinCounter
	/// </summary>
	private static async void SetCurrentPinCounter(long CurrentPinAttemptCounter)
	{
		await appInstance.services.SettingsService.SaveState(Constants.Pin.CurrentPinAttemptCounter, CurrentPinAttemptCounter);
	}

	/// <summary>
	/// If Screen Capture can be prohibited.
	/// </summary>
	public static bool CanProhibitScreenCapture => secureDisplay is not null;

	/// <summary>
	/// Controls of screen-capture is prohibited.
	/// </summary>
	public static bool ProhibitScreenCapture
	{
		get => secureDisplay?.ProhibitScreenCapture ?? false;
		set
		{
			if (secureDisplay is not null)
			{
				secureDisplay.ProhibitScreenCapture = value;
			}
		}
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}
}
