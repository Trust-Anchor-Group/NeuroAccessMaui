namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Delegate for notification events handlers.
	/// </summary>
	/// <param name="Sender">Sender of event.</param>
	/// <param name="e">Event arguments.</param>
	public delegate void NotificationEventsHandler(object? Sender, NotificationEventsArgs e);

	/// <summary>
	/// Event argument for notification events.
	/// </summary>
	/// <param name="Events">Referenced event.</param>
	public class NotificationEventsArgs(NotificationEvent[] Events) : EventArgs()
	{
		/// <summary>
		/// Referenced events.
		/// </summary>
		public NotificationEvent[] Events { get; } = Events;
	}
}
