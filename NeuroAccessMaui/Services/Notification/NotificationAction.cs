using System.Runtime.Serialization;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Actions that can be routed from notifications.
	/// </summary>
	public enum NotificationAction
	{
		/// <summary>
		/// Unknown or unspecified action.
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Opens a chat conversation.
		/// </summary>
		OpenChat = 1,

		/// <summary>
		/// Opens a profile or identity.
		/// </summary>
		OpenProfile = 2,

		/// <summary>
		/// Opens a legal identity or identity application.
		/// </summary>
		OpenIdentity = 5,

		/// <summary>
		/// Opens a presence subscription request.
		/// </summary>
		OpenPresenceRequest = 3,

		/// <summary>
		/// Opens application settings.
		/// </summary>
		OpenSettings = 4
	}
}
