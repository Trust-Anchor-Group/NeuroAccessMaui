namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Delegate for notification event handlers.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate void NotificationEventHandler(object? Sender, NotificationEventArgs e);

	/// <summary>
	/// Event argument for notification events.
	/// </summary>
	/// <param name="Event">Referenced event.</param>
	public class NotificationEventArgs(NotificationEvent Event) : EventArgs()
	{
		/// <summary>
		/// Referenced event.
		/// </summary>
		public NotificationEvent Event { get; } = Event;
	}
}
