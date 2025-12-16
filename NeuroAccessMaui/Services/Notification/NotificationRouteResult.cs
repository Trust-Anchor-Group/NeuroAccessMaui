namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Possible outcomes when routing a notification intent.
	/// </summary>
	public enum NotificationRouteResult
	{
		/// <summary>
		/// Routing succeeded.
		/// </summary>
		Success = 0,

		/// <summary>
		/// No handler was found for the intent.
		/// </summary>
		NoHandler = 1,

		/// <summary>
		/// Routing failed.
		/// </summary>
		Failed = 2,

		/// <summary>
		/// Routing was deferred (e.g., waiting for navigation to be ready).
		/// </summary>
		Deferred = 3,

		/// <summary>
		/// Routing was intentionally ignored (e.g., user already viewing the target).
		/// </summary>
		Ignored = 4
	}
}
