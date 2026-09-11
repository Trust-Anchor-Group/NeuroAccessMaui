using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui.Services.Xmpp;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Push notification service
	/// </summary>
	public class PushNotificationService : LoadableService, IPushNotificationService
	{
		private readonly IPushTransport pushTransport;
		private readonly IPushTokenRegistrar tokenRegistrar;
		private readonly IXmppService xmppService;
		private readonly Dictionary<PushMessagingService, string> tokens = [];
		private DateTime lastTokenCheck = DateTime.MinValue;
		private bool isInitialized;
		private readonly SemaphoreSlim initializationSemaphore = new SemaphoreSlim(1, 1);
		private readonly SemaphoreSlim verificationSemaphore = new SemaphoreSlim(1, 1);
		private readonly object tokenVerificationSync = new();
		private Task? pendingTokenVerificationTask;

		/// <summary>
		/// Push notification service
		/// </summary>
		/// <param name="PushTransport">Transport adapter.</param>
		/// <param name="TokenRegistrar">Token registrar handling broker updates.</param>
		/// <param name="XmppService">The process XMPP service used for optional token registration.</param>
		public PushNotificationService(IPushTransport PushTransport, IPushTokenRegistrar TokenRegistrar, IXmppService XmppService)
		{
			this.pushTransport = PushTransport;
			this.tokenRegistrar = TokenRegistrar;
			this.xmppService = XmppService;
		}

		/// <summary>
		/// Loads the specified service.
		/// </summary>
		/// <param name="IsResuming">Set to <c>true</c> when app is resuming.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			await this.initializationSemaphore.WaitAsync(CancellationToken);
			try
			{
				if (this.isInitialized)
					return;
				App.AppActivated += this.App_AppActivated;
				this.pushTransport.TokenChanged += this.PushTransport_TokenChanged;
				await this.pushTransport.InitializeAsync(CancellationToken);
				lock (this.tokenVerificationSync)
					this.isInitialized = true;
				this.IsLoaded = true;
				this.ScheduleTokenVerification();
			}
			catch (Exception Ex)
			{
				App.AppActivated -= this.App_AppActivated;
				this.pushTransport.TokenChanged -= this.PushTransport_TokenChanged;
				try { await this.pushTransport.UnloadAsync(); }
				catch (Exception CleanupError) { Ex.Data["PushCleanupFailure"] = CleanupError; }
				throw;
			}
			finally { this.initializationSemaphore.Release(); }
		}

		/// <inheritdoc/>
		public override async Task Unload()
		{
			await this.initializationSemaphore.WaitAsync();
			try
			{
				Task? Pending;
				lock (this.tokenVerificationSync)
				{
					this.isInitialized = false;
					Pending = this.pendingTokenVerificationTask;
				}
				App.AppActivated -= this.App_AppActivated;
				this.pushTransport.TokenChanged -= this.PushTransport_TokenChanged;
				if (Pending is not null)
				{
					try { await Pending; }
					catch (Exception) { } // Scheduled verification already observes remote failures.
				}
				await this.verificationSemaphore.WaitAsync();
				this.verificationSemaphore.Release();
				await this.pushTransport.UnloadAsync();
				this.IsLoaded = false;
			}
			finally { this.initializationSemaphore.Release(); }
		}

		/// <summary>
		/// New token received from push notification back-end.
		/// </summary>
		/// <param name="TokenInformation">Token information</param>
		public async Task NewToken(TokenInformation TokenInformation)
		{
			if (!string.IsNullOrEmpty(TokenInformation.Token))
			{
				lock (this.tokens)
				{
					this.tokens[TokenInformation.Service] = TokenInformation.Token;
				}

				await this.xmppService.NewPushNotificationToken(TokenInformation);
				await this.OnNewToken.Raise(this, new TokenEventArgs(TokenInformation.Service, TokenInformation.Token, TokenInformation.ClientType));
			}
		}

		private void App_AppActivated(object? Sender, EventArgs EventArgs)
		{
			this.ScheduleTokenVerification();
		}

		private async Task PushTransport_TokenChanged(object? Sender, TokenInformation TokenInformation)
		{
			try
			{
				await this.CheckPushNotificationToken(TokenInformation, CancellationToken.None);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private void ScheduleTokenVerification()
		{
			Task VerificationTask;

			lock (this.tokenVerificationSync)
			{
				if (!this.isInitialized || (this.pendingTokenVerificationTask is not null && !this.pendingTokenVerificationTask.IsCompleted))
					return;

				VerificationTask = MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await this.CheckPushNotificationToken(null);
				});

				this.pendingTokenVerificationTask = VerificationTask;
			}

			VerificationTask.ContinueWith(t =>
			{
				Exception? Exception = t.Exception?.GetBaseException() ?? t.Exception;
				if (Exception is not null)
					ServiceRef.LogService.LogException(Exception);
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		/// <summary>
		/// Event raised when a new token is made available.
		/// </summary>
		public event EventHandlerAsync<TokenEventArgs>? OnNewToken;

		/// <summary>
		/// Tries to get a token from a push notification service.
		/// </summary>
		/// <param name="Source">Source of token</param>
		/// <param name="Token">Token, if found.</param>
		/// <returns>If a token was found for the corresponding source.</returns>
		public bool TryGetToken(PushMessagingService Source, out string? Token)
		{
			lock (this.tokens)
			{
				return this.tokens.TryGetValue(Source, out Token);
			}
		}

		/// <summary>
		/// Checks if the Push Notification Token is current and registered properly.
		/// </summary>
		/// <param name="TokenInformation">Non null if we got it from the OnNewToken</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public async Task CheckPushNotificationToken(TokenInformation? TokenInformation, CancellationToken CancellationToken = default)
		{
			await this.verificationSemaphore.WaitAsync(CancellationToken);
			try
			{
				lock (this.tokenVerificationSync)
				{
					if (!this.isInitialized)
						return;
				}
				await this.xmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect);

				DateTime Now = DateTime.Now;

				if (this.xmppService.IsOnline &&
					this.xmppService.SupportsPushNotification &&
					Now.Subtract(this.lastTokenCheck).TotalHours >= 1)
				{
					this.lastTokenCheck = Now;

					if (TokenInformation is null)
					{
						TokenInformation = await ServiceRef.PlatformSpecific.GetPushNotificationToken();
						if (string.IsNullOrEmpty(TokenInformation.Token))
							return;
					}

					string Version = AppInfo.VersionString + "." + AppInfo.BuildString;
					string PrevVersion = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationConfigurationVersion, string.Empty);
					string CurrentRulesHash = PushRuleDefinitions.RuleSetHash;
					string PrevRulesHash = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationRulesHash, string.Empty);
					bool IsVersionChanged = Version != PrevVersion || CurrentRulesHash != PrevRulesHash;

					await this.tokenRegistrar.ReportTokenAsync(TokenInformation, IsVersionChanged, CancellationToken);

					if (IsVersionChanged)
					{
						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationConfigurationVersion, string.Empty);
						await this.xmppService.ClearPushNotificationRules();

						foreach (PushRuleDefinition rule in PushRuleDefinitions.All)
						{
							await this.xmppService.AddPushNotificationRule(
								rule.MessageType,
								rule.LocalName,
								rule.Namespace,
								rule.Channel,
								rule.MessageVariable,
								rule.PatternScript,
								rule.ContentScript);
						}

						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationConfigurationVersion, Version);
						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationRulesHash, CurrentRulesHash);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally { this.verificationSemaphore.Release(); }
		}
	}
}
