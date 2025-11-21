using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Platform-neutral intent describing how to route a notification.
	/// </summary>
	public sealed class NotificationIntent
	{
		/// <summary>
		/// Gets or sets the title to display.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the message body to display.
		/// </summary>
		public string? Body { get; set; }

		/// <summary>
		/// Gets or sets the intended action.
		/// </summary>
		public NotificationAction Action { get; set; } = NotificationAction.Unknown;

		/// <summary>
		/// Gets or sets an entity identifier associated with the action.
		/// </summary>
		public string? EntityId { get; set; }

		/// <summary>
		/// Gets or sets the push channel identifier.
		/// </summary>
		public string? Channel { get; set; }

		/// <summary>
		/// Gets or sets an optional deep link.
		/// </summary>
		public string? DeepLink { get; set; }

		/// <summary>
		/// Gets or sets extra data used for routing.
		/// </summary>
		public Dictionary<string, string> Extras { get; set; } = new();

		/// <summary>
		/// Gets or sets the schema version of the intent payload.
		/// </summary>
		public int Version { get; set; } = 1;

		/// <summary>
		/// Gets or sets an optional correlation identifier.
		/// </summary>
		public string? CorrelationId { get; set; }
	}
}
