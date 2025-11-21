namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Notification lifecycle state.
	/// </summary>
	public enum NotificationState
	{
		/// <summary>
		/// Notification has been created but not yet delivered.
		/// </summary>
		New = 0,

		/// <summary>
		/// Notification has been delivered to the user.
		/// </summary>
		Delivered = 1,

		/// <summary>
		/// Notification has been read.
		/// </summary>
		Read = 2,

		/// <summary>
		/// Notification has been consumed and acted upon.
		/// </summary>
		Consumed = 3
	}
}
