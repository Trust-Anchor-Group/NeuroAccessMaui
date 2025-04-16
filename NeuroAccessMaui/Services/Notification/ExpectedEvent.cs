using Microsoft.Maui.Controls.Shapes;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Notification
{

	/// <summary>
	/// Helper class to hold expected event details
	/// </summary>
	internal class ExpectedEvent(Type eventType, DateTime before, Predicate<NotificationEvent>? predicate = null)
	{
		public Type EventType { get; } = eventType;
		public DateTime Before { get; } = before;
		public Predicate<NotificationEvent>? Predicate { get; } = predicate;
	}

}
