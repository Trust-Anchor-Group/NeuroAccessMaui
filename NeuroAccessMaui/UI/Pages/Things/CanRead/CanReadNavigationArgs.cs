using NeuroAccessMaui.Services.Notification.Things;
using Waher.Things.SensorData;

namespace NeuroAccessMaui.UI.Pages.Things.CanRead
{
	/// <summary>
	/// Holds navigation parameters specific to displaying the can-read provisioning question.
	/// </summary>
	public class CanReadNavigationArgs : ThingNavigationArgs
	{
		private readonly CanReadNotificationEvent? @event;

		/// <summary>
		/// Creates a new instance of the <see cref="CanReadNavigationArgs"/> class.
		/// </summary>
		public CanReadNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="CanReadNavigationArgs"/> class.
		/// </summary>
		/// <param name="Event">Notification event object.</param>
		/// <param name="FriendlyName">Friendly name of device.</param>
		/// <param name="RemoteFriendlyName">Friendly name of remote entity.</param>
		public CanReadNavigationArgs(CanReadNotificationEvent Event, string FriendlyName, string RemoteFriendlyName)
			: base(Event, FriendlyName, RemoteFriendlyName)
		{
			this.@event = Event;
			this.Fields = Event.Fields;
			this.AllFields = Event.AllFields;
			this.FieldTypes = Event.FieldTypes;
		}

		/// <summary>
		/// Notification event objcet.
		/// </summary>
		public new CanReadNotificationEvent? Event => this.@event;

		/// <summary>
		/// Fields
		/// </summary>
		public string[]? Fields { get; }

		/// <summary>
		/// AllFields
		/// </summary>
		public string[]? AllFields { get; }

		/// <summary>
		/// Field Types
		/// </summary>
		public FieldType FieldTypes { get; }
	}
}
