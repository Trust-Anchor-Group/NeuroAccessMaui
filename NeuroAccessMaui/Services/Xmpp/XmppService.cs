using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Pages.Registration;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Tag;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Events.XMPP;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Abuse;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.HTTPX;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Runtime.Temporary;

namespace NeuroAccessMaui.Services.Xmpp;

/// <summary>
/// XMPP Service, maintaining XMPP connections and XMPP Extensions.
///
/// Note: By duplicating event handlers on the service, event handlers continue to work, even if app
/// goes to sleep, and new clients are created when awoken again.
/// </summary>
[Singleton]
internal sealed class XmppService : LoadableService, IXmppService, IDisposable
{
	private bool isDisposed;
	private XmppClient? xmppClient;
	private ContractsClient? contractsClient;
	private HttpFileUploadClient? fileUploadClient;
	private AbuseClient? abuseClient;
	private HttpxClient? httpxClient;
	private Timer? reconnectTimer;
	private string domainName;
	private string accountName;
	private string passwordHash;
	private string passwordHashMethod;
	private bool xmppConnected = false;
	private DateTime xmppLastStateChange = DateTime.MinValue;
	private InMemorySniffer? sniffer = new(250);
	private bool isCreatingClient;
	private XmppEventSink? xmppEventSink;
	private string? token = null;
	private DateTime tokenCreated = DateTime.MinValue;

	#region Creation / Destruction

	public XmppService()
	{
	}

	private async Task CreateXmppClient(bool CanCreateKeys)
	{
		if (this.isCreatingClient)
		{
			return;
		}

		try
		{
			this.isCreatingClient = true;

			if (!this.XmppParametersCurrent() || this.XmppStale())
			{
				if (this.xmppClient is not null)
				{
					await this.DestroyXmppClient();
				}

				this.domainName = ServiceRef.TagProfile.Domain;
				this.accountName = ServiceRef.TagProfile.Account;
				this.passwordHash = ServiceRef.TagProfile.PasswordHash;
				this.passwordHashMethod = ServiceRef.TagProfile.PasswordHashMethod;

				string HostName;
				int PortNumber;
				bool IsIpAddress;

				if (ServiceRef.TagProfile.DefaultXmppConnectivity)
				{
					HostName = this.domainName;
					PortNumber = XmppCredentials.DefaultPort;
					IsIpAddress = false;
				}
				else
				{
					(HostName, PortNumber, IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(this.domainName);

					if (HostName == this.domainName && PortNumber == XmppCredentials.DefaultPort)
					{
						ServiceRef.TagProfile.SetDomain(this.domainName, true, ServiceRef.TagProfile.ApiKey, ServiceRef.TagProfile.ApiSecret);
					}
				}

				this.xmppLastStateChange = DateTime.Now;
				this.xmppConnected = false;

				Assembly AppAssembly = App.Current!.GetType().Assembly;

				if (string.IsNullOrEmpty(this.passwordHashMethod))
				{
					this.xmppClient = new XmppClient(HostName, PortNumber, this.accountName, this.passwordHash,
						Constants.LanguageCodes.Default, AppAssembly, this.sniffer);
				}
				else
				{
					this.xmppClient = new XmppClient(HostName, PortNumber, this.accountName, this.passwordHash, this.passwordHashMethod,
						Constants.LanguageCodes.Default, AppAssembly, this.sniffer);
				}

				this.xmppClient.RequestRosterOnStartup = false;
				this.xmppClient.TrustServer = !IsIpAddress;
				this.xmppClient.AllowCramMD5 = false;
				this.xmppClient.AllowDigestMD5 = false;
				this.xmppClient.AllowPlain = false;
				this.xmppClient.AllowEncryption = true;
				this.xmppClient.AllowScramSHA1 = true;
				this.xmppClient.AllowScramSHA256 = true;
				this.xmppClient.AllowQuickLogin = true;

				this.xmppClient.RequestRosterOnStartup = true;
				this.xmppClient.OnStateChanged += this.XmppClient_StateChanged;
				this.xmppClient.OnConnectionError += this.XmppClient_ConnectionError;
				this.xmppClient.OnError += this.XmppClient_Error;
				this.xmppClient.OnPresenceSubscribe += this.XmppClient_OnPresenceSubscribe;

				this.xmppClient.RegisterMessageHandler("Delivered", ContractsClient.NamespaceOnboarding, this.TransferIdDelivered, true);
				this.xmppClient.RegisterMessageHandler("clientMessage", ContractsClient.NamespaceLegalIdentities, this.ClientMessage, true);

				this.xmppEventSink = new XmppEventSink("XMPP Event Sink", this.xmppClient, ServiceRef.TagProfile.LogJid, false);

				// Add extensions before connecting

				this.abuseClient = new AbuseClient(this.xmppClient);

				if (!string.IsNullOrWhiteSpace(ServiceRef.TagProfile.LegalJid))
				{
					this.contractsClient = new ContractsClient(this.xmppClient, ServiceRef.TagProfile.LegalJid);
					this.RegisterContractsEventHandlers();

					if (!await this.contractsClient.LoadKeys(false))
					{
						if (!CanCreateKeys)
						{
							Log.Alert("Regeneration of keys not permitted at this time.",
								string.Empty, string.Empty, string.Empty, EventLevel.Major, string.Empty, string.Empty, Environment.StackTrace);

							throw new Exception("Regeneration of keys not permitted at this time.");
						}

						await this.contractsClient.GenerateNewKeys();
					}
				}

				if (!string.IsNullOrWhiteSpace(ServiceRef.TagProfile.HttpFileUploadJid) && ServiceRef.TagProfile.HttpFileUploadMaxSize.HasValue)
				{
					this.fileUploadClient = new HttpFileUploadClient(this.xmppClient, ServiceRef.TagProfile.HttpFileUploadJid, ServiceRef.TagProfile.HttpFileUploadMaxSize);
				}

				this.httpxClient = new HttpxClient(this.xmppClient, 8192);
				Types.SetModuleParameter("XMPP", this.xmppClient);      // Makes the XMPP Client the default XMPP client, when resolving HTTP over XMPP requests.

				this.IsLoggedOut = false;
				this.xmppClient.Connect(IsIpAddress ? string.Empty : this.domainName);
				this.RecreateReconnectTimer();

				// Await connected state during registration or user initiated log in, but not otherwise.
				if (!ServiceRef.TagProfile.IsCompleteOrWaitingForValidation())
				{
					if (!await this.WaitForConnectedState(Constants.Timeouts.XmppConnect))
					{
						ServiceRef.LogService.LogWarning("Connection to XMPP server failed.",
							new KeyValuePair<string, object>("Domain", this.domainName),
							new KeyValuePair<string, object>("Account", this.accountName),
							new KeyValuePair<string, object>("Timeout", Constants.Timeouts.XmppConnect));
					}
				}
			}
		}
		finally
		{
			this.isCreatingClient = false;
		}
	}

	private async Task DestroyXmppClient()
	{
		this.reconnectTimer?.Dispose();
		this.reconnectTimer = null;

		await this.OnConnectionStateChanged(XmppState.Offline);

		if (this.xmppEventSink is not null)
		{
			ServiceRef.LogService.RemoveListener(this.xmppEventSink);
			this.xmppEventSink.Dispose();
			this.xmppEventSink = null;
		}

		this.contractsClient?.Dispose();
		this.contractsClient = null;

		this.fileUploadClient?.Dispose();
		this.fileUploadClient = null;

		this.abuseClient?.Dispose();
		this.abuseClient = null;

		this.xmppClient?.Dispose();
		this.xmppClient = null;
	}

	private bool XmppStale()
	{
		return this.xmppClient is null ||
			this.xmppClient.State == XmppState.Offline ||
			this.xmppClient.State == XmppState.Error ||
			(this.xmppClient.State != XmppState.Connected && (DateTime.Now - this.xmppLastStateChange).TotalSeconds >= 10);
	}

	private bool XmppParametersCurrent()
	{
		if (this.xmppClient is null)
		{
			return false;
		}

		if (this.domainName != ServiceRef.TagProfile.Domain)
		{
			return false;
		}

		if (this.accountName != ServiceRef.TagProfile.Account)
		{
			return false;
		}

		if (this.passwordHash != ServiceRef.TagProfile.PasswordHash)
		{
			return false;
		}

		if (this.passwordHashMethod != ServiceRef.TagProfile.PasswordHashMethod)
		{
			return false;
		}

		if (this.contractsClient?.ComponentAddress != ServiceRef.TagProfile.LegalJid)
		{
			return false;
		}

		if (this.fileUploadClient?.FileUploadJid != ServiceRef.TagProfile.HttpFileUploadJid)
		{
			return false;
		}

		return true;
	}

	private bool ShouldCreateClient()
	{
		return ServiceRef.TagProfile.Step > RegistrationStep.Account && !this.XmppParametersCurrent();
	}

	private void RecreateReconnectTimer()
	{
		this.reconnectTimer?.Dispose();
		this.reconnectTimer = new Timer(this.ReconnectTimer_Tick, null, Constants.Intervals.Reconnect, Constants.Intervals.Reconnect);
	}

	#endregion

	#region Lifecycle

	public async Task<bool> WaitForConnectedState(TimeSpan Timeout)
	{
		if (this.xmppClient is null)
		{
			DateTime Start = DateTime.Now;

			while (this.xmppClient is null && DateTime.Now - Start < Timeout)
			{
				await Task.Delay(1000);
			}

			if (this.xmppClient is null)
			{
				return false;
			}

			Timeout -= DateTime.Now - Start;
		}

		if (this.xmppClient.State == XmppState.Connected)
		{
			return true;
		}

		if (Timeout < TimeSpan.Zero)
		{
			return false;
		}

		int i = await this.xmppClient.WaitStateAsync((int)Timeout.TotalMilliseconds, XmppState.Connected);
		return i >= 0;
	}

	public override async Task Load(bool isResuming, CancellationToken cancellationToken)
	{
		if (this.BeginLoad(cancellationToken))
		{
			try
			{
				ServiceRef.TagProfile.StepChanged += this.TagProfile_StepChanged;

				if (this.ShouldCreateClient())
				{
					await this.CreateXmppClient(ServiceRef.TagProfile.Step <= RegistrationStep.RegisterIdentity);
				}

				if ((this.xmppClient is not null) &&
					this.xmppClient.State == XmppState.Connected &&
					ServiceRef.TagProfile.IsCompleteOrWaitingForValidation())
				{
					// Don't await this one, just fire and forget, to improve startup time.
					_ = this.xmppClient.SetPresenceAsync(Availability.Online);
				}

				this.EndLoad(true);
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				this.EndLoad(false);
			}
		}
	}

	public override Task Unload()
	{
		return this.Unload(false);
	}

	public Task UnloadFast()
	{
		return this.Unload(true);
	}

	private async Task Unload(bool fast)
	{
		if (this.BeginUnload())
		{
			try
			{
				ServiceRef.TagProfile.StepChanged -= this.TagProfile_StepChanged;

				this.reconnectTimer?.Dispose();
				this.reconnectTimer = null;

				if (this.xmppClient is not null)
				{
					this.xmppClient.CheckConnection = false;

					if (!fast)
					{
						try
						{
							await Task.WhenAny(
								this.xmppClient.SetPresenceAsync(Availability.Offline),
								Task.Delay(1000)    // Wait at most 1000 ms.
							);
						}
						catch (Exception)
						{
							// Ignore
						}
					}
				}

				await this.DestroyXmppClient();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}

			this.EndUnload();
		}
	}

	private void TagProfile_StepChanged(object? Sender, EventArgs e)
	{
		if (!this.IsLoaded)
		{
			return;
		}

		Task.Run(async () =>
		{
			try
			{
				if (this.ShouldCreateClient())
				{
					await this.CreateXmppClient(ServiceRef.TagProfile.Step <= RegistrationStep.RegisterIdentity);
				}
				else if (ServiceRef.TagProfile.Step <= RegistrationStep.Account)
				{
					await this.DestroyXmppClient();
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		});
	}

	private Task XmppClient_Error(object Sender, Exception e)
	{
		this.LatestError = e.Message;
		return Task.CompletedTask;
	}

	private Task XmppClient_ConnectionError(object Sender, Exception e)
	{
		if (e is ObjectDisposedException)
		{
			this.LatestConnectionError = ServiceRef.Localizer[nameof(AppResources.UnableToConnect)];
		}
		else
		{
			this.LatestConnectionError = e.Message;
		}

		return Task.CompletedTask;
	}

	private async Task XmppClient_StateChanged(object Sender, XmppState NewState)
	{
		this.xmppLastStateChange = DateTime.Now;

		switch (NewState)
		{
			case XmppState.Connecting:
				this.LatestError = string.Empty;
				this.LatestConnectionError = string.Empty;
				break;

			case XmppState.Connected:
				this.LatestError = string.Empty;
				this.LatestConnectionError = string.Empty;

				this.xmppConnected = true;

				this.RecreateReconnectTimer();

				if (string.IsNullOrEmpty(ServiceRef.TagProfile.PasswordHashMethod))
				{
					ServiceRef.TagProfile.SetAccount(ServiceRef.TagProfile.Account, this.xmppClient.PasswordHash, this.xmppClient.PasswordHashMethod);
				}

				if (ServiceRef.TagProfile.NeedsUpdating() && await this.DiscoverServices())
				{
					if (this.contractsClient is null && !string.IsNullOrWhiteSpace(ServiceRef.TagProfile.LegalJid))
					{
						this.contractsClient = new ContractsClient(this.xmppClient, ServiceRef.TagProfile.LegalJid);
						this.RegisterContractsEventHandlers();

						if (!await this.contractsClient.LoadKeys(false))
						{
							this.contractsClient.Dispose();
							this.contractsClient = null;
						}
					}

					if (this.fileUploadClient is null && !string.IsNullOrWhiteSpace(ServiceRef.TagProfile.HttpFileUploadJid) && ServiceRef.TagProfile.HttpFileUploadMaxSize.HasValue)
					{
						this.fileUploadClient = new HttpFileUploadClient(this.xmppClient, ServiceRef.TagProfile.HttpFileUploadJid, ServiceRef.TagProfile.HttpFileUploadMaxSize);
					}
				}

				ServiceRef.LogService.AddListener(this.xmppEventSink);
				break;

			case XmppState.Offline:
			case XmppState.Error:
				if (this.xmppConnected && !this.IsUnloading)
				{
					this.xmppConnected = false;

					if (this.xmppClient is not null && !this.xmppClient.Disposed)
					{
						this.xmppClient.Reconnect();
					}
				}
				break;
		}

		await this.OnConnectionStateChanged(NewState);
	}

	/// <summary>
	/// An event that triggers whenever the connection state to the XMPP server changes.
	/// </summary>
	public event StateChangedEventHandler? ConnectionStateChanged;

	private async Task OnConnectionStateChanged(XmppState NewState)
	{
		try
		{
			Task? T = this.ConnectionStateChanged?.Invoke(this, NewState);

			if (T is not null)
			{
				await T;
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	#endregion

	#region State

	public bool IsLoggedOut { get; private set; }
	public bool IsOnline => (this.xmppClient is not null) && this.xmppClient.State == XmppState.Connected;
	public XmppState State => this.xmppClient?.State ?? XmppState.Offline;
	public string BareJid => this.xmppClient?.BareJID ?? string.Empty;

	public string LatestError { get; private set; }
	public string LatestConnectionError { get; private set; }

	#endregion

	#region Connections

	private enum ConnectOperation
	{
		Connect,
		ConnectAndCreateAccount,
		ConnectToAccount
	}

	public Task<(bool succeeded, string errorMessage)> TryConnect(string domain, bool isIpAddress, string hostName, int portNumber,
		string languageCode, Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
	{
		return this.TryConnectInner(domain, isIpAddress, hostName, portNumber, string.Empty, string.Empty, string.Empty, languageCode,
			string.Empty, string.Empty, applicationAssembly, connectedFunc, ConnectOperation.Connect);
	}

	public Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, bool isIpAddress, string hostName,
		int portNumber, string userName, string password, string languageCode, string ApiKey, string ApiSecret,
		Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc)
	{
		return this.TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, string.Empty, languageCode,
			ApiKey, ApiSecret, applicationAssembly, connectedFunc, ConnectOperation.ConnectAndCreateAccount);
	}

	public Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, bool isIpAddress, string hostName,
		int portNumber, string userName, string password, string passwordMethod, string languageCode, Assembly applicationAssembly,
		Func<XmppClient, Task> connectedFunc)
	{
		return this.TryConnectInner(domain, isIpAddress, hostName, portNumber, userName, password, passwordMethod, languageCode,
			string.Empty, string.Empty, applicationAssembly, connectedFunc, ConnectOperation.ConnectToAccount);
	}

	private async Task<(bool succeeded, string errorMessage)> TryConnectInner(string domain, bool isIpAddress, string hostName,
		int portNumber, string userName, string password, string passwordMethod, string languageCode, string ApiKey, string ApiSecret,
		Assembly applicationAssembly, Func<XmppClient, Task> connectedFunc, ConnectOperation operation)
	{
		TaskCompletionSource<bool> connected = new();
		bool succeeded;
		string errorMessage = null;
		bool streamNegotiation = false;
		bool streamOpened = false;
		bool startingEncryption = false;
		bool authenticating = false;
		bool registering = false;
		bool timeout = false;
		string connectionError = null;

		Task OnConnectionError(object _, Exception e)
		{
			if (e is ObjectDisposedException)
			{
				connectionError = ServiceRef.Localizer[nameof(AppResources.UnableToConnect)];
			}
			else
			{
				connectionError = e.Message;
			}

			connected.TrySetResult(false);
			return Task.CompletedTask;
		}

		async Task OnStateChanged(object _, XmppState newState)
		{
			switch (newState)
			{
				case XmppState.StreamNegotiation:
					streamNegotiation = true;
					break;

				case XmppState.StreamOpened:
					streamOpened = true;
					break;

				case XmppState.StartingEncryption:
					startingEncryption = true;
					break;

				case XmppState.Authenticating:
					authenticating = true;

					if (operation == ConnectOperation.Connect)
					{
						connected.TrySetResult(true);
					}

					break;

				case XmppState.Registering:
					registering = true;
					break;

				case XmppState.Connected:
					connected.TrySetResult(true);
					break;

				case XmppState.Offline:
					connected.TrySetResult(false);
					break;

				case XmppState.Error:
					// When State = Error, wait for the OnConnectionError event to arrive also, as it holds more/direct information.
					// Just in case it never would - set state error and result.
					await Task.Delay(Constants.Timeouts.XmppConnect);
					connected.TrySetResult(false);
					break;
			}
		}

		XmppClient client = null;
		try
		{
			if (string.IsNullOrEmpty(passwordMethod))
			{
				client = new XmppClient(hostName, portNumber, userName, password, languageCode, applicationAssembly, this.sniffer);
			}
			else
			{
				client = new XmppClient(hostName, portNumber, userName, password, passwordMethod, languageCode, applicationAssembly, this.sniffer);
			}

			if (operation == ConnectOperation.ConnectAndCreateAccount)
			{
				if (!string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ApiSecret))
				{
					client.AllowRegistration(ApiKey, ApiSecret);
				}
				else
				{
					client.AllowRegistration();
				}
			}

			client.TrustServer = !isIpAddress;
			client.AllowCramMD5 = false;
			client.AllowDigestMD5 = false;
			client.AllowPlain = false;
			client.AllowEncryption = true;
			client.AllowScramSHA1 = true;
			client.AllowScramSHA256 = true;
			client.AllowQuickLogin = true;

			client.OnConnectionError += OnConnectionError;
			client.OnStateChanged += OnStateChanged;

			client.Connect(isIpAddress ? string.Empty : domain);

			void TimerCallback(object _)
			{
				timeout = true;
				connected.TrySetResult(false);
			}

			using (Timer _ = new(TimerCallback, null, (int)Constants.Timeouts.XmppConnect.TotalMilliseconds, Timeout.Infinite))
			{
				succeeded = await connected.Task;
			}

			if (succeeded && (connectedFunc is not null))
			{
				await connectedFunc(client);
			}

			client.OnStateChanged -= OnStateChanged;
			client.OnConnectionError -= OnConnectionError;
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object>(nameof(ConnectOperation), operation.ToString()));
			succeeded = false;
			errorMessage = string.Format(CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.UnableToConnectTo)], domain);
		}
		finally
		{
			client?.Dispose();
			client = null;
		}

		if (!succeeded && string.IsNullOrEmpty(errorMessage))
		{
			System.Diagnostics.Debug.WriteLine("Sniffer: ", await this.sniffer.SnifferToText());

			if (!streamNegotiation || timeout)
			{
				errorMessage = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.CantConnectTo)], domain);
			}
			else if (!streamOpened)
			{
				errorMessage = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.DomainIsNotAValidOperator)], domain);
			}
			else if (!startingEncryption)
			{
				errorMessage = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.DomainDoesNotFollowEncryptionPolicy)], domain);
			}
			else if (!authenticating)
			{
				errorMessage = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.UnableToAuthenticateWith)], domain);
			}
			else if (!registering)
			{
				if (!string.IsNullOrWhiteSpace(connectionError))
				{
					errorMessage = connectionError;
				}
				else
				{
					errorMessage = string.Format(CultureInfo.CurrentCulture,
						ServiceRef.Localizer[nameof(AppResources.OperatorDoesNotSupportRegisteringNewAccounts)], domain);
				}
			}
			else if (operation == ConnectOperation.ConnectAndCreateAccount)
			{
				errorMessage = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.AccountNameAlreadyTaken)], this.accountName);
			}
			else if (operation == ConnectOperation.ConnectToAccount)
			{
				errorMessage = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.InvalidUsernameOrPassword)], this.accountName);
			}
			else
			{
				errorMessage = string.Format(CultureInfo.CurrentCulture,
					ServiceRef.Localizer[nameof(AppResources.UnableToConnectTo)], domain);
			}
		}

		return (succeeded, errorMessage);
	}

	private void ReconnectTimer_Tick(object _)
	{
		if (this.xmppClient is null)
		{
			return;
		}

		if (!ServiceRef.NetworkService.IsOnline)
		{
			return;
		}

		if (this.XmppStale())
		{
			this.xmppLastStateChange = DateTime.Now;

			if (!this.xmppClient.Disposed)
			{
				this.xmppClient.Reconnect();
			}
		}
	}

	#endregion

	#region Password

	/// <summary>
	/// Changes the password of the account.
	/// </summary>
	/// <param name="NewPassword">New password</param>
	/// <returns>If change was successful.</returns>
	public Task<bool> ChangePassword(string NewPassword)
	{
		TaskCompletionSource<bool> PasswordChanged = new();

		this.xmppClient.ChangePassword(NewPassword, (sender, e) =>
		{
			PasswordChanged.TrySetResult(e.Ok);
			return Task.CompletedTask;
		}, null);

		return PasswordChanged.Task;
	}

	#endregion

	#region Components & Services

	/// <summary>
	/// Performs a Service Discovery on a remote entity.
	/// </summary>
	/// <param name="FullJid">Full JID of entity.</param>
	/// <returns>Service Discovery response.</returns>
	public Task<ServiceDiscoveryEventArgs> SendServiceDiscoveryRequest(string FullJid)
	{
		TaskCompletionSource<ServiceDiscoveryEventArgs> Result = new();

		this.xmppClient.SendServiceDiscoveryRequest(FullJid, (_, e) =>
		{
			Result.TrySetResult(e);
			return Task.CompletedTask;
		}, null);

		return Result.Task;
	}

	/// <summary>
	/// Run this method to discover services for any given XMPP server.
	/// </summary>
	/// <param name="Client">The client to use. Can be <c>null</c>, in which case the default is used.</param>
	/// <returns>If TAG services were found.</returns>
	public async Task<bool> DiscoverServices(XmppClient Client = null)
	{
		Client ??= this.xmppClient;

		if (Client is null)
		{
			return false;
		}

		ServiceItemsDiscoveryEventArgs response;

		try
		{
			response = await Client.ServiceItemsDiscoveryAsync(null, string.Empty, string.Empty);
		}
		catch (Exception ex)
		{
			string commsDump = await this.sniffer.SnifferToText();
			ServiceRef.LogService.LogException(ex, new KeyValuePair<string, object>("Sniffer", commsDump));
			return false;
		}

		List<Task> Tasks = [];
		object SynchObject = new();

		foreach (Item Item in response.Items)
		{
			Tasks.Add(this.CheckComponent(Client, Item, SynchObject));
		}

		await Task.WhenAll([.. Tasks]);

		if (string.IsNullOrWhiteSpace(ServiceRef.TagProfile.LegalJid))
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(ServiceRef.TagProfile.HttpFileUploadJid) || !ServiceRef.TagProfile.HttpFileUploadMaxSize.HasValue)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(ServiceRef.TagProfile.LogJid))
		{
			return false;
		}

		return true;
	}

	private async Task CheckComponent(XmppClient Client, Item Item, object SynchObject)
	{
		ServiceDiscoveryEventArgs itemResponse = await Client.ServiceDiscoveryAsync(null, Item.JID, Item.Node);

		lock (SynchObject)
		{
			if (itemResponse.HasFeature(ContractsClient.NamespaceLegalIdentities))
			{
				ServiceRef.TagProfile.SetLegalJid(Item.JID);
			}

			if (itemResponse.HasFeature(HttpFileUploadClient.Namespace))
			{
				long? maxSize = HttpFileUploadClient.FindMaxFileSize(Client, itemResponse);
				ServiceRef.TagProfile.SetFileUploadParameters(Item.JID, maxSize);
			}

			if (itemResponse.HasFeature(XmppEventSink.NamespaceEventLogging))
			{
				ServiceRef.TagProfile.SetLogJid(Item.JID);
			}
		}
	}

	#endregion

	#region Transfer

	private async Task TransferIdDelivered(object Sender, MessageEventArgs e)
	{
		if (e.From != Constants.Domains.OnboardingDomain)
		{
			return;
		}

		string Code = XML.Attribute(e.Content, "code");
		bool Deleted = XML.Attribute(e.Content, "deleted", false);

		if (!Deleted)
		{
			return;
		}

		string CodesGenerated = await RuntimeSettings.GetAsync("TransferId.CodesSent", string.Empty);
		string[] Codes = CodesGenerated.Split(CommonTypes.CRLF, StringSplitOptions.RemoveEmptyEntries);

		if (Array.IndexOf<string>(Codes, Code) < 0)
		{
			return;
		}

		await this.DestroyXmppClient();

		this.domainName = string.Empty;
		this.accountName = string.Empty;
		this.passwordHash = string.Empty;
		this.passwordHashMethod = string.Empty;
		this.xmppConnected = false;

		ServiceRef.TagProfile.ClearAll();
		await RuntimeSettings.SetAsync("TransferId.CodesSent", string.Empty);
		await Database.Provider.Flush();

		await App.SetRegistrationPageAsync();
	}

	/// <summary>
	/// Registers a Transfer ID Code
	/// </summary>
	/// <param name="Code">Transfer Code</param>
	public async Task AddTransferCode(string Code)
	{
		string CodesGenerated = await RuntimeSettings.GetAsync("TransferId.CodesSent", string.Empty);

		if (string.IsNullOrEmpty(CodesGenerated))
		{
			CodesGenerated = Code;
		}
		else
		{
			CodesGenerated += "\r\n" + Code;
		}

		await RuntimeSettings.SetAsync("TransferId.CodesSent", CodesGenerated);
		await Database.Provider.Flush();
	}

	#endregion

	#region Presence Subscriptions

	private Task XmppClient_OnPresenceSubscribe(object Sender, PresenceEventArgs e)
	{
		e.Decline();
		return Task.CompletedTask;
	}

	#endregion

	#region IQ Stanzas (Information Query)

	/// <summary>
	/// Performs an asynchronous IQ Set request/response operation.
	/// </summary>
	/// <param name="To">Destination address</param>
	/// <param name="Xml">XML to embed into the request.</param>
	/// <returns>Response XML element.</returns>
	/// <exception cref="TimeoutException">If a timeout occurred.</exception>
	/// <exception cref="XmppException">If an IQ error is returned.</exception>
	public Task<XmlElement> IqSetAsync(string To, string Xml)
	{
		return this.xmppClient.IqSetAsync(To, Xml);
	}

	#endregion

	#region Messages

	private Task ClientMessage(object Sender, MessageEventArgs e)
	{
		string Code = XML.Attribute(e.Content, "code");
		string Type = XML.Attribute(e.Content, "type");
		string Message = e.Body;

		if (!string.IsNullOrEmpty(Code))
		{
			try
			{
				//!!! can it throw an exception?
				string LocalizedMessage = ServiceRef.Localizer["ClientMessage" + Code];

				if (!string.IsNullOrEmpty(LocalizedMessage))
				{
					Message = LocalizedMessage;
				}
			}
			catch (Exception)
			{
				// Ignore
			}
		}

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			switch (Type.ToUpperInvariant())
			{
				case "NONE":
				default:
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.Information)], Message,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					break;

				case "CLIENT":
				case "SERVER":
				case "SERVICE":
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Message,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					break;

			}
		});

		return Task.CompletedTask;
	}

	#endregion

	#region Tokens

	/// <summary>
	/// Gets a token for use with APIs that are either distributed or use different
	/// protocols, when the client needs to authenticate itself using the current
	/// XMPP connection.
	/// </summary>
	/// <param name="Seconds">Number of seconds for which the token should be valid.</param>
	/// <returns>Token, if able to get a token, or null otherwise.</returns>
	public async Task<string> GetApiToken(int Seconds)
	{
		DateTime Now = DateTime.UtcNow;

		if (!string.IsNullOrEmpty(this.token) && Now.Subtract(this.tokenCreated).TotalSeconds < Seconds - 10)
		{
			return this.token;
		}

		if (!this.IsOnline)
		{
			if (!await this.WaitForConnectedState(TimeSpan.FromSeconds(20)))
			{
				return this.token;
			}
		}

		this.token = await this.httpxClient.GetJwtTokenAsync(Seconds);
		this.tokenCreated = Now;

		return this.token;
	}

	/// <summary>
	/// Performs an HTTP POST to a protected API on the server, over the current XMPP connection,
	/// authenticating the client using the credentials already provided over XMPP.
	/// </summary>
	/// <param name="LocalResource">Local Resource on the server to POST to.</param>
	/// <param name="Data">Data to post. This will be encoded using encoders in the type inventory.</param>
	/// <param name="Headers">Headers to provide in the POST.</param>
	/// <returns>Decoded response from the resource.</returns>
	/// <exception cref="Exception">Any communication error will be handle by raising the corresponding exception.</exception>
	public async Task<object> PostToProtectedApi(string LocalResource, object Data, params KeyValuePair<string, string>[] Headers)
	{
		StringBuilder Url = new();

		if (this.IsOnline)
		{
			Url.Append("httpx://");
		}
		else if (!string.IsNullOrEmpty(this.token))     // Token needs to be retrieved regularly when connected, if protected APIs are to be used when disconnected or during connection.
		{
			Url.Append("https://");

			KeyValuePair<string, string> Authorization = new("Authorization", "Bearer " + this.token);

			if (Headers is null)
			{
				Headers = [Authorization];
			}
			else
			{
				int c = Headers.Length;

				Array.Resize(ref Headers, c + 1);
				Headers[c] = Authorization;
			}
		}
		else
		{
			throw new IOException("No connection and no token available for call to protect API.");
		}

		Url.Append(ServiceRef.TagProfile.Domain);
		Url.Append(LocalResource);

		return await InternetContent.PostAsync(new Uri(Url.ToString()), Data, Headers);
	}

	#endregion

	#region HTTP File Upload

	/// <summary>
	/// Reference to HTTP File Upload client, with a check that one is created.
	/// Note: Do not make public. Reference only from inside the XmppService class.
	/// </summary>
	private HttpFileUploadClient FileUploadClient
	{
		get
		{
			if (this.fileUploadClient is null)
			{
				throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.FileUploadServiceNotFound)]);
			}

			return this.fileUploadClient;
		}
	}

	/// <summary>
	/// Returns <c>true</c> if file upload is supported, <c>false</c> otherwise.
	/// </summary>
	public bool FileUploadIsSupported
	{
		get
		{
			try
			{
				return ServiceRef.TagProfile.FileUploadIsSupported &&
					this.fileUploadClient is not null &&
					this.fileUploadClient.HasSupport;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return false;
			}
		}
	}

	/// <summary>
	/// Uploads a file to the upload component.
	/// </summary>
	/// <param name="FileName">Name of file.</param>
	/// <param name="ContentType">Internet content type.</param>
	/// <param name="ContentSize">Size of content.</param>
	public Task<HttpFileUploadEventArgs> RequestUploadSlotAsync(string FileName, string ContentType, long ContentSize)
	{
		return this.FileUploadClient.RequestUploadSlotAsync(FileName, ContentType, ContentSize);
	}


	#endregion

	#region Legal Identities

	/// <summary>
	/// Gets important attributes for a successful ID Application.
	/// </summary>
	/// <returns>ID Application attributes.</returns>
	public async Task<IdApplicationAttributesEventArgs> GetIdApplicationAttributes()
	{
		return await this.ContractsClient.GetIdApplicationAttributesAsync();
	}

	/// <summary>
	/// Adds a legal identity.
	/// </summary>
	/// <param name="Model">The model holding all the values needed.</param>
	/// <param name="Attachments">The physical attachments to upload.</param>
	/// <returns>Legal Identity</returns>
	public async Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel Model, params LegalIdentityAttachment[] Attachments)
	{
		await this.ContractsClient.GenerateNewKeys();

		LegalIdentity identity = await this.ContractsClient.ApplyAsync(Model.ToProperties(ServiceRef.XmppService));

		foreach (LegalIdentityAttachment a in Attachments)
		{
			HttpFileUploadEventArgs e2 = await ServiceRef.XmppService.RequestUploadSlotAsync(Path.GetFileName(a.Filename), a.ContentType, a.ContentLength);

			if (!e2.Ok)
			{
				throw e2.StanzaError ?? new Exception(e2.ErrorText);
			}

			await e2.PUT(a.Data, a.ContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

			byte[] signature = await this.ContractsClient.SignAsync(a.Data, SignWith.CurrentKeys);

			identity = await this.ContractsClient.AddLegalIdAttachmentAsync(identity.Id, e2.GetUrl, signature);
		}

		await this.ContractsClient.ReadyForApprovalAsync(identity.Id);

		return identity;
	}

	/// <summary>
	/// Returns a list of legal identities.
	/// </summary>
	/// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
	/// <returns>Legal Identities</returns>
	public async Task<LegalIdentity[]> GetLegalIdentities(XmppClient client = null)
	{
		if (client is null)
		{
			return await this.ContractsClient.GetLegalIdentitiesAsync();
		}
		else
		{
			using ContractsClient cc = new(client, ServiceRef.TagProfile.LegalJid);  // No need to load keys for this operation.
			return await cc.GetLegalIdentitiesAsync();
		}
	}

	/// <summary>
	/// Gets a specific legal identity.
	/// </summary>
	/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
	/// <returns>Legal identity object</returns>
	public async Task<LegalIdentity> GetLegalIdentity(CaseInsensitiveString legalIdentityId)
	{
		ContactInfo Info = await ContactInfo.FindByLegalId(legalIdentityId);

		if (Info is not null && Info.LegalIdentity is not null)
		{
			return Info.LegalIdentity;
		}

		return await this.ContractsClient.GetLegalIdentityAsync(legalIdentityId);
	}

	/// <summary>
	/// Checks if a legal identity is in the contacts list.
	/// </summary>
	/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
	/// <returns>If the legal identity is in the contacts list.</returns>
	public async Task<bool> IsContact(CaseInsensitiveString legalIdentityId)
	{
		ContactInfo Info = await ContactInfo.FindByLegalId(legalIdentityId);
		return (Info is not null && Info.LegalIdentity is not null);
	}

	/// <summary>
	/// Checks if the client has access to the private keys of the specified legal identity.
	/// </summary>
	/// <param name="legalIdentityId">The id of the legal identity.</param>
	/// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
	/// <returns>If private keys are available.</returns>
	public async Task<bool> HasPrivateKey(CaseInsensitiveString legalIdentityId, XmppClient client = null)
	{
		if (client is null)
		{
			return await this.ContractsClient.HasPrivateKey(legalIdentityId);
		}
		else
		{
			using ContractsClient cc = new(client, ServiceRef.TagProfile.LegalJid);

			if (!await cc.LoadKeys(false))
			{
				return false;
			}

			return await cc.HasPrivateKey(legalIdentityId);
		}
	}

	/// <summary>
	/// Marks the legal identity as obsolete.
	/// </summary>
	/// <param name="legalIdentityId">The id to mark as obsolete.</param>
	/// <returns>Legal Identity</returns>
	public Task<LegalIdentity> ObsoleteLegalIdentity(CaseInsensitiveString legalIdentityId)
	{
		return this.ContractsClient.ObsoleteLegalIdentityAsync(legalIdentityId);
	}

	/// <summary>
	/// Marks the legal identity as compromised.
	/// </summary>
	/// <param name="legalIdentityId">The legal id to mark as compromised.</param>
	/// <returns>Legal Identity</returns>
	public Task<LegalIdentity> CompromiseLegalIdentity(CaseInsensitiveString legalIdentityId)
	{
		return this.ContractsClient.CompromisedLegalIdentityAsync(legalIdentityId);
	}

	/// <summary>
	/// Petitions a legal identity.
	/// </summary>
	/// <param name="LegalId">The id of the legal identity.</param>
	/// <param name="PetitionId">The petition id.</param>
	/// <param name="Purpose">The purpose of the petitioning.</param>
	public async Task PetitionIdentity(CaseInsensitiveString LegalId, string PetitionId, string Purpose)
	{
		await this.ContractsClient.AuthorizeAccessToIdAsync(ServiceRef.TagProfile.LegalIdentity.Id, LegalId, true);

		this.StartPetition(PetitionId);
		await this.ContractsClient.PetitionIdentityAsync(LegalId, PetitionId, Purpose);
	}

	private void StartPetition(string PetitionId)
	{
		lock (this.currentPetitions)
		{
			this.currentPetitions[PetitionId] = true;
		}
	}

	private bool EndPetition(string PetitionId)
	{
		lock (this.currentPetitions)
		{
			return this.currentPetitions.Remove(PetitionId);
		}
	}

	/// <summary>
	/// Sends a response to a petitioning identity request.
	/// </summary>
	/// <param name="LegalId">The id of the legal identity.</param>
	/// <param name="PetitionId">The petition id.</param>
	/// <param name="RequestorFullJid">The full Jid of the requestor.</param>
	/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
	public Task SendPetitionIdentityResponse(CaseInsensitiveString LegalId, string PetitionId, string RequestorFullJid, bool Response)
	{
		return this.ContractsClient.PetitionIdentityResponseAsync(LegalId, PetitionId, RequestorFullJid, Response);
	}

	/// <summary>
	/// An event that fires when a legal identity changes.
	/// </summary>
	public event LegalIdentityEventHandler LegalIdentityChanged;

	private async Task ContractsClient_IdentityUpdated(object Sender, LegalIdentityEventArgs e)
	{
		if (ServiceRef.TagProfile.LegalIdentity is null ||
			ServiceRef.TagProfile.LegalIdentity.Id == e.Identity.Id ||
			ServiceRef.TagProfile.LegalIdentity.Created < e.Identity.Created)
		{
			try
			{
				this.LegalIdentityChanged?.Invoke(this, e);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
			}
		}
	}

	/// <summary>
	/// An event that fires when a petition for an identity is received.
	/// </summary>
	public event LegalIdentityPetitionEventHandler PetitionForIdentityReceived;

	private async Task ContractsClient_PetitionForIdentityReceived(object Sender, LegalIdentityPetitionEventArgs e)
	{
		try
		{
			this.PetitionForIdentityReceived?.Invoke(this, e);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
		}
	}

	/// <summary>
	/// An event that fires when a petitioned identity response is received.
	/// </summary>
	public event LegalIdentityPetitionResponseEventHandler PetitionedIdentityResponseReceived;

	private async Task ContractsClient_PetitionedIdentityResponseReceived(object Sender, LegalIdentityPetitionResponseEventArgs e)
	{
		try
		{
			this.EndPetition(e.PetitionId);
			this.PetitionedIdentityResponseReceived?.Invoke(this, e);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
		}
	}

	/// <summary>
	/// Exports Keys to XML.
	/// </summary>
	/// <param name="Output">XML output.</param>
	public Task ExportSigningKeys(XmlWriter Output)
	{
		return this.ContractsClient.ExportKeys(Output);
	}

	/// <summary>
	/// Imports keys
	/// </summary>
	/// <param name="Xml">XML Definition of keys.</param>
	/// <returns>If keys could be loaded into the client.</returns>
	public Task<bool> ImportSigningKeys(XmlElement Xml)
	{
		return this.ContractsClient.ImportKeys(Xml);
	}

	/// <summary>
	/// Validates a legal identity.
	/// </summary>
	/// <param name="Identity">Legal Identity</param>
	/// <returns>The validity of the identity.</returns>
	public Task<IdentityStatus> ValidateIdentity(LegalIdentity Identity)
	{
		return this.ContractsClient.ValidateAsync(Identity, true);
	}

	#endregion

	#region Smart Contracts

	/// <summary>
	/// Reference to contracts client, with a check that one is created.
	/// Note: Do not make public. Reference only from inside the XmppService class.
	/// </summary>
	private ContractsClient ContractsClient
	{
		get
		{
			if (this.contractsClient is null)
			{
				throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.LegalServiceNotFound)]);
			}

			return this.contractsClient;
		}
	}

	private void RegisterContractsEventHandlers()
	{
		this.ContractsClient.IdentityUpdated += this.ContractsClient_IdentityUpdated;
		this.ContractsClient.PetitionForIdentityReceived += this.ContractsClient_PetitionForIdentityReceived;
		this.ContractsClient.PetitionedIdentityResponseReceived += this.ContractsClient_PetitionedIdentityResponseReceived;
		this.ContractsClient.PetitionForSignatureReceived += this.ContractsClient_PetitionForSignatureReceived;
		this.ContractsClient.PetitionedSignatureResponseReceived += this.ContractsClient_PetitionedSignatureResponseReceived;
		this.ContractsClient.PetitionForPeerReviewIDReceived += this.ContractsClient_PetitionForPeerReviewIdReceived;
		this.ContractsClient.PetitionedPeerReviewIDResponseReceived += this.ContractsClient_PetitionedPeerReviewIdResponseReceived;
	}

	#endregion

	#region Attachments

	/// <summary>
	/// Gets an attachment for a contract.
	/// </summary>
	/// <param name="Url">The url of the attachment.</param>
	/// <param name="Timeout">Max timeout allowed when retrieving an attachment.</param>
	/// <param name="SignWith">How the request is signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
	/// <returns>Content-Type, and attachment file.</returns>
	public Task<KeyValuePair<string, TemporaryFile>> GetAttachment(string Url, SignWith SignWith, TimeSpan Timeout)
	{
		return this.ContractsClient.GetAttachmentAsync(Url, SignWith, (int)Timeout.TotalMilliseconds);
	}

	#endregion

	#region Peer Review

	/// <summary>
	/// Sends a petition to a third-party to review a legal identity.
	/// </summary>
	/// <param name="LegalId">The legal id to petition.</param>
	/// <param name="Identity">The legal id to peer review.</param>
	/// <param name="PetitionId">The petition id.</param>
	/// <param name="Purpose">The purpose.</param>
	public async Task PetitionPeerReviewId(CaseInsensitiveString LegalId, LegalIdentity Identity, string PetitionId, string Purpose)
	{
		await this.ContractsClient.AuthorizeAccessToIdAsync(Identity.Id, LegalId, true);

		this.StartPetition(PetitionId);
		await this.ContractsClient.PetitionPeerReviewIDAsync(LegalId, Identity, PetitionId, Purpose);
	}

	/// <summary>
	/// Adds an attachment for the peer review.
	/// </summary>
	/// <param name="Identity">The identity to which the attachment should be added.</param>
	/// <param name="ReviewerLegalIdentity">The identity of the reviewer.</param>
	/// <param name="PeerSignature">The raw signature data.</param>
	/// <returns>Legal Identity</returns>
	public Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity Identity, LegalIdentity ReviewerLegalIdentity, byte[] PeerSignature)
	{
		return this.ContractsClient.AddPeerReviewIDAttachment(Identity, ReviewerLegalIdentity, PeerSignature);
	}

	/// <summary>
	/// An event that fires when a petition for peer review is received.
	/// </summary>
	public event SignaturePetitionEventHandler PetitionForPeerReviewIdReceived;

	private async Task ContractsClient_PetitionForPeerReviewIdReceived(object Sender, SignaturePetitionEventArgs e)
	{
		try
		{
			this.PetitionForPeerReviewIdReceived?.Invoke(this, e);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
		}
	}

	/// <summary>
	/// An event that fires when a petitioned peer review response is received.
	/// </summary>
	public event SignaturePetitionResponseEventHandler PetitionedPeerReviewIdResponseReceived;

	private async Task ContractsClient_PetitionedPeerReviewIdResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
	{
		try
		{
			this.EndPetition(e.PetitionId);
			this.PetitionedPeerReviewIdResponseReceived?.Invoke(this, e);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
		}
	}

	/// <summary>
	/// Gets available service providers for buying eDaler.
	/// </summary>
	/// <returns>Available service providers for peer review of identity applications.</returns>
	public async Task<ServiceProviderWithLegalId[]> GetServiceProvidersForPeerReviewAsync()
	{
		return await this.ContractsClient.GetPeerReviewIdServiceProvidersAsync();
	}

	/// <summary>
	/// Selects a peer-review service as default, for the account, when sending a peer-review request to the
	/// Legal Identity of the Trust Provider hosting the account.
	/// </summary>
	/// <param name="ServiceId">Service ID</param>
	/// <param name="ServiceProvider">Service Provider</param>
	public async Task SelectPeerReviewService(string ServiceId, string ServiceProvider)
	{
		await this.ContractsClient.SelectPeerReviewServiceAsync(ServiceProvider, ServiceId);
	}

	private readonly Dictionary<string, bool> currentPetitions = new();

	private async Task ContractsClient_PetitionClientUrlReceived(object Sender, PetitionClientUrlEventArgs e)
	{
		lock (this.currentPetitions)
		{
			if (!this.currentPetitions.ContainsKey(e.PetitionId))
			{
				ServiceRef.LogService.LogWarning("Client URL message for a petition is ignored. Petition ID not recognized.",
					new KeyValuePair<string, object>("PetitionId", e.PetitionId),
					new KeyValuePair<string, object>("ClientUrl", e.ClientUrl));
				return;
			}
		}

		await App.OpenUrlAsync(e.ClientUrl);
	}

	#endregion

	#region Signatures

	/// <summary>
	/// Signs binary data with the corresponding private key.
	/// </summary>
	/// <param name="data">The data to sign.</param>
	/// <param name="signWith">What keys that can be used to sign the data.</param>
	/// <returns>Signature</returns>
	public Task<byte[]> Sign(byte[] data, SignWith signWith)
	{
		return this.ContractsClient.SignAsync(data, signWith);
	}

	/// <summary>Validates a signature of binary data.</summary>
	/// <param name="legalIdentity">Legal identity used to create the signature.</param>
	/// <param name="data">Binary data to sign-</param>
	/// <param name="signature">Digital signature of data</param>
	/// <returns>
	/// true = Signature is valid.
	/// false = Signature is invalid.
	/// null = Client key algorithm is unknown, and veracity of signature could not be established.
	/// </returns>
	public bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature)
	{
		return this.ContractsClient.ValidateSignature(legalIdentity, data, signature);
	}

	/// <summary>
	/// Sends a response to a petitioning signature request.
	/// </summary>
	/// <param name="LegalId">Legal Identity petitioned.</param>
	/// <param name="Content">Content to be signed.</param>
	/// <param name="Signature">Digital signature of content, made by the legal identity.</param>
	/// <param name="PetitionId">A petition identifier. This identifier will follow the petition, and can be used
	/// to identify the petition request.</param>
	/// <param name="RequestorFullJid">Full JID of requestor.</param>
	/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
	public Task SendPetitionSignatureResponse(CaseInsensitiveString LegalId, byte[] Content, byte[] Signature, string PetitionId, string RequestorFullJid, bool Response)
	{
		return this.ContractsClient.PetitionSignatureResponseAsync(LegalId, Content, Signature, PetitionId, RequestorFullJid, Response);
	}

	/// <summary>
	/// An event that fires when a petition for a signature is received.
	/// </summary>
	public event SignaturePetitionEventHandler PetitionForSignatureReceived;

	private async Task ContractsClient_PetitionForSignatureReceived(object Sender, SignaturePetitionEventArgs e)
	{
		try
		{
			this.PetitionForSignatureReceived?.Invoke(this, e);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
		}
	}

	/// <summary>
	/// Event raised when a response to a signature petition has been received.
	/// </summary>
	public event SignaturePetitionResponseEventHandler SignaturePetitionResponseReceived;

	private async Task ContractsClient_PetitionedSignatureResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
	{
		try
		{
			this.EndPetition(e.PetitionId);
			this.SignaturePetitionResponseReceived?.Invoke(this, e);
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
			await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
		}
	}

	#endregion

	#region Private XML

	/// <summary>
	/// Saves Private XML to the server. Private XML are separated by
	/// Local Name and Namespace of the root element. Only one document
	/// per fully qualified name. When saving private XML, the XML overwrites
	/// any existing XML having the same local name and namespace.
	/// </summary>
	/// <param name="Xml">XML to save.</param>
	public Task SavePrivateXml(string Xml)
	{
		return this.xmppClient.SetPrivateXmlElementAsync(Xml);
	}

	/// <summary>
	/// Saves Private XML to the server. Private XML are separated by
	/// Local Name and Namespace of the root element. Only one document
	/// per fully qualified name. When saving private XML, the XML overwrites
	/// any existing XML having the same local name and namespace.
	/// </summary>
	/// <param name="Xml">XML to save.</param>
	public Task SavePrivateXml(XmlElement Xml)
	{
		return this.xmppClient.SetPrivateXmlElementAsync(Xml);
	}

	/// <summary>
	/// Loads private XML previously stored, given the local name and
	/// namespace of the XML.
	/// </summary>
	/// <param name="LocalName">Local Name</param>
	/// <param name="Namespace">Namespace</param>
	public Task<XmlElement> LoadPrivateXml(string LocalName, string Namespace)
	{
		return this.xmppClient.GetPrivateXmlElementAsync(LocalName, Namespace);
	}

	/// <summary>
	/// Deletes private XML previously saved to the account.
	/// </summary>
	/// <param name="LocalName">Local Name</param>
	/// <param name="Namespace">Namespace</param>
	public Task DeletePrivateXml(string LocalName, string Namespace)
	{
		StringBuilder Xml = new();

		Xml.Append('<');
		Xml.Append(XML.Encode(LocalName));
		Xml.Append(" xmlns='");
		Xml.Append(XML.Encode(Namespace));
		Xml.Append("'/>");
		return this.SavePrivateXml(Xml.ToString());
	}

	#endregion

	/// <inheritdoc/>
	public void Dispose()
	{
		this.sniffer?.Dispose();
		this.abuseClient?.Dispose();
		this.contractsClient?.Dispose();
		this.fileUploadClient?.Dispose();
		this.httpxClient?.Dispose();
		this.reconnectTimer?.Dispose();
		this.xmppClient?.Dispose();
		this.xmppEventSink?.Dispose();

		this.sniffer = null;
		this.abuseClient = null;
		this.contractsClient = null;
		this.fileUploadClient = null;
		this.httpxClient = null;
		this.reconnectTimer = null;
		this.xmppClient = null;
		this.xmppEventSink = null;

		/*
		this.Dispose(true);
		GC.SuppressFinalize(this);
		*/
	}

	/*
	/// <summary>
	/// <see cref="IDisposable.Dispose"/>
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (this.isDisposed)
		{
			return;
		}

		if (disposing)
		{
			this.abuseClient.Dispose();
			this.contractsClient.Dispose();
			this.fileUploadClient.Dispose();
			this.httpxClient.Dispose();
			this.reconnectTimer.Dispose();
			this.sniffer.Dispose();
			this.xmppClient.Dispose();
			this.xmppEventSink.Dispose();
		}

		this.isDisposed = true;
	}
	*/
}
