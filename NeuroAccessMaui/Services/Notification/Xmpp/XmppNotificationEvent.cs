namespace NeuroAccessMaui.Services.Notification.Xmpp
{
	/// <summary>
	/// Abstract base class of XMPP notification events.
	/// </summary>
	public abstract class XmppNotificationEvent : NotificationEvent
	{
		/// <summary>
		/// Abstract base class of XMPP notification events.
		/// </summary>
		public XmppNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Bare JID of sender.
		/// </summary>
		public string BareJid { get; set; } = string.Empty;
	}
}
