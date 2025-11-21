using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Resilience;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.UI.MVVM.Policies;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Handles deduplication and reporting of push notification tokens to the broker.
	/// </summary>
	public sealed class PushTokenRegistrar : IPushTokenRegistrar
	{
		private readonly IXmppService xmppService;
		private const int MaxAttempts = 3;

		/// <summary>
		/// Initializes a new instance of the <see cref="PushTokenRegistrar"/> class.
		/// </summary>
		/// <param name="XmppService">XMPP service.</param>
		public PushTokenRegistrar(IXmppService XmppService)
		{
			this.xmppService = XmppService;
		}

		/// <summary>
		/// Reports a push token to the broker if needed, applying deduplication and force rules.
		/// </summary>
		/// <param name="TokenInformation">Token information.</param>
		/// <param name="ForceReport">Set to <c>true</c> to always report, regardless of cached state.</param>
		/// <param name="CancellationToken">Cancellation token.</param>
		/// <returns>True if the token was reported.</returns>
		public async Task<bool> ReportTokenAsync(TokenInformation TokenInformation, bool ForceReport, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			if (!ForceReport && !await ForceTokenReportAsync(TokenInformation))
				return false;

			string? Token = TokenInformation.Token;
			if (string.IsNullOrEmpty(Token))
				return false;

			IAsyncPolicy RetryPolicy = Policies.Retry(
				MaxAttempts,
				(Attempt, _) => TimeSpan.FromSeconds(Math.Pow(2, Attempt - 1)),
				(Attempt, ex, delay) => ServiceRef.LogService.LogWarning(
					"Push token registration retry scheduled.",
					new KeyValuePair<string, object?>("attempt", Attempt),
					new KeyValuePair<string, object?>("delayMs", delay.TotalMilliseconds),
					new KeyValuePair<string, object?>("exception", ex.Message)));

			try
			{
				await PolicyRunner.RunAsync(
					async _ =>
					{
						await this.xmppService.ReportNewPushNotificationToken(Token, TokenInformation.Service, TokenInformation.ClientType);
						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationToken, TokenInformation.Token);
						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationReportDate, DateTime.UtcNow);
					},
					CancellationToken,
					RetryPolicy);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogInformational("Push token registration failed after retries.");
				ServiceRef.LogService.LogException(
					ex,
					new KeyValuePair<string, object?>("service", TokenInformation.Service.ToString()),
					new KeyValuePair<string, object?>("clientType", TokenInformation.ClientType.ToString()));
				return false;
			}

			ServiceRef.LogService.LogInformational(
				"Push token reported.",
				new KeyValuePair<string, object?>("service", TokenInformation.Service.ToString()),
				new KeyValuePair<string, object?>("clientType", TokenInformation.ClientType.ToString()));

			return true;
		}

		private static async Task<bool> ForceTokenReportAsync(TokenInformation TokenInformation)
		{
			string OldToken = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationToken, string.Empty);
			DateTime ReportDate = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationReportDate, DateTime.MinValue);

			return (DateTime.UtcNow.Subtract(ReportDate).TotalDays > 7) || (TokenInformation.Token != OldToken);
		}
	}
}
