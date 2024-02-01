using System.Globalization;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Things;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Provisioning;

namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// Resolves pending friendship requests
	/// </summary>
	public class FriendshipResolver(string BareJid, string RemoteJid, RuleRange Range) : IEventResolver
	{
		private readonly string bareJid = BareJid.ToLower(CultureInfo.InvariantCulture);
		private readonly string remoteJid = RemoteJid.ToLower(CultureInfo.InvariantCulture);
		private readonly RuleRange range = Range;

		/// <summary>
		/// If the resolver resolves an event.
		/// </summary>
		/// <param name="Event">Pending notification event.</param>
		/// <returns>If the resolver resolves the event.</returns>
		public bool Resolves(NotificationEvent Event)
		{
			if (Event.Type != NotificationEventType.Things || Event is not IsFriendNotificationEvent IsFriendNotificationEvent)
				return false;

			if (IsFriendNotificationEvent.BareJid != this.bareJid)
				return false;

			return this.range switch
			{
				RuleRange.All => true,
				RuleRange.Domain => string.Equals(XmppClient.GetDomain(this.remoteJid), XmppClient.GetDomain(IsFriendNotificationEvent.RemoteJid), StringComparison.OrdinalIgnoreCase),
				RuleRange.Caller => string.Equals(this.remoteJid, IsFriendNotificationEvent.RemoteJid, StringComparison.OrdinalIgnoreCase),
				_ => false,
			};
		}
	}
}
