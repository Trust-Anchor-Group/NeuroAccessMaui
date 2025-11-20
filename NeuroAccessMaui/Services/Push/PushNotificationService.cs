using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Push notification service
	/// </summary>
	[Singleton]
	public class PushNotificationService : LoadableService, IPushNotificationService
	{
		private readonly IPushTransport pushTransport;
		private readonly IPushTokenRegistrar tokenRegistrar;
		private readonly Dictionary<PushMessagingService, string> tokens = [];
		private DateTime lastTokenCheck = DateTime.MinValue;
		private bool isInitialized;
		private readonly object tokenVerificationSync = new();
		private Task? pendingTokenVerificationTask;

		/// <summary>
		/// Push notification service
		/// </summary>
		/// <param name="PushTransport">Transport adapter.</param>
		/// <param name="TokenRegistrar">Token registrar handling broker updates.</param>
		public PushNotificationService(IPushTransport PushTransport, IPushTokenRegistrar TokenRegistrar)
		{
			this.pushTransport = PushTransport;
			this.tokenRegistrar = TokenRegistrar;
		}

		/// <summary>
		/// Loads the specified service.
		/// </summary>
		/// <param name="IsResuming">Set to <c>true</c> when app is resuming.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (!this.isInitialized)
			{
				this.isInitialized = true;
				App.AppActivated += this.App_AppActivated;
				this.pushTransport.TokenChanged += this.PushTransport_TokenChanged;
				try
				{
					await this.pushTransport.InitializeAsync(CancellationToken);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}

				this.ScheduleTokenVerification();
			}

			await base.Load(IsResuming, CancellationToken);
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

				await ServiceRef.XmppService.NewPushNotificationToken(TokenInformation);
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
				if (this.pendingTokenVerificationTask is not null && !this.pendingTokenVerificationTask.IsCompleted)
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
			try
			{
				DateTime Now = DateTime.Now;

				if (ServiceRef.XmppService.IsOnline &&
					ServiceRef.XmppService.SupportsPushNotification &&
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
					bool IsVersionChanged = Version != PrevVersion;

					await this.tokenRegistrar.ReportTokenAsync(TokenInformation, IsVersionChanged, CancellationToken);

					if (IsVersionChanged)
					{
						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationConfigurationVersion, string.Empty);
						await ServiceRef.XmppService.ClearPushNotificationRules();

						foreach (PushRuleDefinition rule in PushRuleDefinitions.All)
						{
							await ServiceRef.XmppService.AddPushNotificationRule(
								rule.MessageType,
								rule.LocalName,
								rule.Namespace,
								rule.Channel,
								rule.MessageVariable,
								rule.PatternScript,
								rule.ContentScript);
						}

						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationConfigurationVersion, Version);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
