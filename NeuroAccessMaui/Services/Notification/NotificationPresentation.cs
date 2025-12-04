namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Presentation preference for a notification intent.
	/// </summary>
	public enum NotificationPresentation
	{
		/// <summary>
		/// Render an OS notification and store it.
		/// </summary>
		RenderAndStore = 0,

		/// <summary>
		/// Store the notification but do not render an OS notification.
		/// </summary>
		StoreOnly = 1,

		/// <summary>
		/// Render an OS notification but do not store it.
		/// </summary>
		RenderOnly = 2,

		/// <summary>
		/// Do not render or store; useful for ephemeral or expectation-only events.
		/// </summary>
		Transient = 3
	}
}
