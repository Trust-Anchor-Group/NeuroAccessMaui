using IdApp.Services.Notification;
using IdApp.Services.Notification.Things;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Provisioning;

namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// Resolves pending friendship requests
	/// </summary>
	public class FriendshipResolver : IEventResolver
	{
		private readonly string bareJid;
		private readonly string remoteJid;
		private readonly RuleRange range;

		/// <summary>
		/// Resolves pending friendship requests
		/// </summary>
		public FriendshipResolver(string BareJid, string RemoteJid, RuleRange Range)
		{
			this.bareJid = BareJid.ToLower();
			this.remoteJid = RemoteJid.ToLower();
			this.range = Range;
		}

		/// <summary>
		/// If the resolver resolves an event.
		/// </summary>
		/// <param name="Event">Pending notification event.</param>
		/// <returns>If the resolver resolves the event.</returns>
		public bool Resolves(NotificationEvent Event)
		{
			if (Event.Button != EventButton.Things || Event is not IsFriendNotificationEvent IsFriendNotificationEvent)
				return false;

			if (IsFriendNotificationEvent.BareJid != this.bareJid)
				return false;

			return this.range switch
			{
				RuleRange.All => true,
				RuleRange.Domain => XmppClient.GetDomain(this.remoteJid) == XmppClient.GetDomain(IsFriendNotificationEvent.RemoteJid).ToLower(),
				RuleRange.Caller => this.remoteJid == IsFriendNotificationEvent.RemoteJid.ToLower(),
				_ => false,
			};
		}
	}
}
