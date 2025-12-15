namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Describes the source producing a notification.
	/// </summary>
	public enum NotificationSource
	{
		/// <summary>
		/// Notification originates from XMPP events.
		/// </summary>
		Xmpp = 0,

		/// <summary>
		/// Notification originates from push delivery.
		/// </summary>
		Push = 1,

		/// <summary>
		/// Notification originates locally in the app.
		/// </summary>
		Local = 2
	}
}
