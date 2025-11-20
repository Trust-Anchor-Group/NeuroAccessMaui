using System;
using System.Threading;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Settings;
using NeuroAccessMaui.Services.Xmpp;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Handles deduplication and reporting of push notification tokens to the broker.
	/// </summary>
	public sealed class PushTokenRegistrar : IPushTokenRegistrar
	{
		private readonly IXmppService xmppService;

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

			await this.xmppService.ReportNewPushNotificationToken(Token, TokenInformation.Service, TokenInformation.ClientType);
			await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationToken, TokenInformation.Token);
			await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationReportDate, DateTime.UtcNow);

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
