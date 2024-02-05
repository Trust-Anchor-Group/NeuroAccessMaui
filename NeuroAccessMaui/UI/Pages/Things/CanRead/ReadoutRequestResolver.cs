using System.Globalization;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Things;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Provisioning;

namespace NeuroAccessMaui.UI.Pages.Things.CanRead
{
	/// <summary>
	/// Resolves pending readout requests
	/// </summary>
	public class ReadoutRequestResolver : IEventResolver
	{
		private readonly string bareJid;
		private readonly string remoteJid;
		private readonly RuleRange range;
		private readonly ProvisioningToken? token;

		/// <summary>
		/// Resolves pending readout requests
		/// </summary>
		public ReadoutRequestResolver(string BareJid, string RemoteJid, RuleRange Range)
		{
			this.bareJid = BareJid.ToLower(CultureInfo.InvariantCulture);
			this.remoteJid = RemoteJid.ToLower(CultureInfo.InvariantCulture);
			this.range = Range;
			this.token = null;
		}

		/// <summary>
		/// Resolves pending readout requests
		/// </summary>
		public ReadoutRequestResolver(string BareJid, string RemoteJid, ProvisioningToken Token)
		{
			this.bareJid = BareJid.ToLower(CultureInfo.InvariantCulture);
			this.remoteJid = RemoteJid.ToLower(CultureInfo.InvariantCulture);
			this.range = default;
			this.token = Token;
		}

		/// <summary>
		/// If the resolver resolves an event.
		/// </summary>
		/// <param name="Event">Pending notification event.</param>
		/// <returns>If the resolver resolves the event.</returns>
		public bool Resolves(NotificationEvent Event)
		{
			if (Event.Type != NotificationEventType.Things || Event is not CanReadNotificationEvent CanReadNotificationEvent)
				return false;

			if (CanReadNotificationEvent.BareJid != this.bareJid)
				return false;

			if (this.token is null)
			{
				return this.range switch
				{
					RuleRange.All => true,
					RuleRange.Domain => string.Equals(XmppClient.GetDomain(this.remoteJid), XmppClient.GetDomain(CanReadNotificationEvent.RemoteJid), StringComparison.OrdinalIgnoreCase),
					RuleRange.Caller => string.Equals(this.remoteJid, CanReadNotificationEvent.RemoteJid?.ToLower(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase),
					_ => false,
				};
			}
			else
			{
				return this.token.Type switch
				{
					TokenType.User => this.IsResolved(CanReadNotificationEvent.UserTokens),
					TokenType.Service => this.IsResolved(CanReadNotificationEvent.ServiceTokens),
					TokenType.Device => this.IsResolved(CanReadNotificationEvent.DeviceTokens),
					_ => false,
				};
			}
		}

		private bool IsResolved(ProvisioningToken[]? Tokens)
		{
			if (Tokens is null)
				return false;

			foreach (ProvisioningToken Token in Tokens)
			{
				if (Token.Token == this.token?.Token)
					return true;
			}

			return false;
		}

	}
}
