using Microsoft.Maui.Controls.Shapes;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Abstract base class of notification events.
	/// </summary>
	[CollectionName("Notifications")]
	[TypeName(TypeNameSerialization.FullName)]
	[Index("Type", "Category")]
	public abstract class NotificationEvent()
	{
		/// <summary>
		/// Object ID of notification event.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// When event was received
		/// </summary>
		public DateTime Received { get; set; }

		/// <summary>
		/// Category string. Events having the same category are grouped and processed together.
		/// </summary>
		public CaseInsensitiveString? Category { get; set; }

		/// <summary>
		/// Type of notification event.
		/// </summary>
		public NotificationEventType? Type { get; set; }

		/// <summary>
		/// If notification event should be deleted when openedd.
		/// </summary>
		public virtual bool DeleteWhenOpened => true;

		/// <summary>
		/// Gets an icon for the category of event.
		/// </summary>
		/// <returns>Unicode icon.</returns>
		public abstract Task<Geometry> GetCategoryIcon();

		/// <summary>
		/// Gets a descriptive text for the event.
		/// </summary>
		public abstract Task<string> GetDescription();

		/// <summary>
		/// Opens the event.
		/// </summary>
		public abstract Task Open();

		/// <summary>
		/// Performs perparatory tasks, that will simplify opening the notification.
		/// </summary>
		public virtual Task Prepare()
		{
			return Task.CompletedTask;
		}
	}
}
