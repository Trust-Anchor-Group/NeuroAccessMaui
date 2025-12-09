using System;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Persisted notification record.
	/// </summary>
	[CollectionName("NotificationsV2")]
	[TypeName(TypeNameSerialization.FullName)]
	[Index(nameof(Id))]
	[Index(nameof(Channel), nameof(TimestampCreated))]
	public sealed class NotificationRecord
	{
		/// <summary>
		/// Gets or sets the object identifier.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// Gets or sets the stable notification identifier.
		/// </summary>
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the channel identifier.
		/// </summary>
		public string Channel { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the notification title.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the notification body.
		/// </summary>
		public string? Body { get; set; }

		/// <summary>
		/// Gets or sets the correlation identifier used for deduplication.
		/// </summary>
		public string? CorrelationId { get; set; }

		/// <summary>
		/// Gets or sets the action to execute when consumed.
		/// </summary>
		public string Action { get; set; } = NotificationAction.Unknown.ToString();

		/// <summary>
		/// Gets or sets the entity identifier associated with the notification.
		/// </summary>
		public string? EntityId { get; set; }

		/// <summary>
		/// Gets or sets serialized extras payload (JSON).
		/// </summary>
		public string ExtrasJson { get; set; } = "{}";

		/// <summary>
		/// Gets or sets the raw payload received from the transport.
		/// </summary>
		public string? RawPayload { get; set; }

		/// <summary>
		/// Gets or sets the schema version used when parsing the payload.
		/// </summary>
		public int SchemaVersion { get; set; } = 1;

		/// <summary>
		/// Gets or sets the timestamp when the notification was created.
		/// </summary>
		public DateTime TimestampCreated { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Gets or sets when the notification was delivered.
		/// </summary>
		public DateTime? DeliveredAt { get; set; }

		/// <summary>
		/// Gets or sets when the notification was read.
		/// </summary>
		public DateTime? ReadAt { get; set; }

		/// <summary>
		/// Gets or sets when the notification was consumed.
		/// </summary>
		public DateTime? ConsumedAt { get; set; }

		/// <summary>
		/// Gets or sets the notification state.
		/// </summary>
		public NotificationState State { get; set; }

		/// <summary>
		/// Gets or sets the notification source.
		/// </summary>
		public NotificationSource Source { get; set; }

		/// <summary>
		/// Gets or sets the presentation preference.
		/// </summary>
		public NotificationPresentation Presentation { get; set; } = NotificationPresentation.RenderAndStore;

		/// <summary>
		/// Gets or sets how many times this notification intent has been observed.
		/// </summary>
		public int OccurrenceCount { get; set; } = 1;

		/// <summary>
		/// Gets or sets the legacy notification type for migration traceability.
		/// </summary>
		public string? LegacyType { get; set; }

		/// <summary>
		/// Gets or sets the legacy notification category for migration traceability.
		/// </summary>
		public string? LegacyCategory { get; set; }
	}
}
