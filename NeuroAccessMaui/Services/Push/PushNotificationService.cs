using System.Threading;
using System.Threading.Tasks;
using EDaler;
using NeuroAccessMaui.Resources.Languages;
using NeuroFeatures;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;
using Waher.Content;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
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
		private readonly Dictionary<PushMessagingService, string> tokens = [];
		private DateTime lastTokenCheck = DateTime.MinValue;
		private bool isInitialized;
		private readonly object tokenVerificationSync = new();
		private Task? pendingTokenVerificationTask;

		/// <summary>
		/// Push notification service
		/// </summary>
		public PushNotificationService()
		{
			//	CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;
			//	CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
		}

		/// <summary>
		/// Loads the specified service.
		/// </summary>
		/// <param name="isResuming">Set to <c>true</c> when app is resuming.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (!this.isInitialized)
			{
				this.isInitialized = true;
				App.AppActivated += this.App_AppActivated;
				try
				{
					CrossFirebaseCloudMessaging.Current.TokenChanged += this.FirebaseCloudMessaging_TokenChanged;
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}

				this.ScheduleTokenVerification();
			}

			await base.Load(isResuming, cancellationToken);
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

		private void App_AppActivated(object? sender, EventArgs e)
		{
			this.ScheduleTokenVerification();
		}

		private async void FirebaseCloudMessaging_TokenChanged(object? sender, FCMTokenChangedEventArgs e)
		{
			try
			{
				string? token = e?.Token;
				if (string.IsNullOrEmpty(token))
					return;

				TokenInformation info = new()
				{
					Token = token,
					Service = PushMessagingService.Firebase,
					ClientType = ResolveClientType()
				};

				await this.CheckPushNotificationToken(info);
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

		private static ClientType ResolveClientType()
		{
			if (DeviceInfo.Platform == DevicePlatform.Android)
				return ClientType.Android;

			if (DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
				return ClientType.iOS;

			return ClientType.Other;
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

		private static async Task<bool> ForceTokenReport(TokenInformation TokenInformation)
		{
			string OldToken = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationToken, string.Empty);
			DateTime ReportDate = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationReportDate, DateTime.MinValue);

			return (DateTime.UtcNow.Subtract(ReportDate).TotalDays > 7) || (TokenInformation.Token != OldToken);
		}

		/// <summary>
		/// Checks if the Push Notification Token is current and registered properly.
		/// </summary>
		/// <param name="TokenInformation">Non null if we got it from the OnNewToken</param>
		public async Task CheckPushNotificationToken(TokenInformation? TokenInformation)
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

					bool ForceReport = await ForceTokenReport(TokenInformation);

					string Version = AppInfo.VersionString + "." + AppInfo.BuildString;
					string PrevVersion = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationConfigurationVersion, string.Empty);
					bool IsVersionChanged = Version != PrevVersion;

					if (IsVersionChanged || ForceReport)
					{
						string? Token = TokenInformation.Token;

						if (!string.IsNullOrEmpty(Token))
						{
							PushMessagingService Service = TokenInformation.Service;
							ClientType ClientType = TokenInformation.ClientType;
							await ServiceRef.XmppService.ReportNewPushNotificationToken(Token, Service, ClientType);

							await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationToken, TokenInformation.Token);
							await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationReportDate, DateTime.UtcNow);
						}
					}

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
