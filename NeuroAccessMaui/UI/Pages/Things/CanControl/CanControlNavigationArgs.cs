using NeuroAccessMaui.UI.Pages.Things.CanRead;
using IdApp.Services.Notification.Things;

namespace NeuroAccessMaui.UI.Pages.Things.CanControl
{
	/// <summary>
	/// Holds navigation parameters specific to displaying the can-control provisioning question.
	/// </summary>
	public class CanControlNavigationArgs : ThingNavigationArgs
	{
		private readonly CanControlNotificationEvent @event;

		/// <summary>
		/// Creates a new instance of the <see cref="CanControlNavigationArgs"/> class.
		/// </summary>
		public CanControlNavigationArgs() { }

		/// <summary>
		/// Creates a new instance of the <see cref="CanControlNavigationArgs"/> class.
		/// </summary>
		/// <param name="Event">Notification event object.</param>
		/// <param name="FriendlyName">Friendly name of device.</param>
		/// <param name="RemoteFriendlyName">Friendly name of remote entity.</param>
		public CanControlNavigationArgs(CanControlNotificationEvent Event, string FriendlyName, string RemoteFriendlyName)
			: base(Event, FriendlyName, RemoteFriendlyName)
		{
			this.@event = Event;
			this.Parameters = Event.Parameters;
			this.AllParameters = Event.AllParameters;
		}

		/// <summary>
		/// Notification event objcet.
		/// </summary>
		public new CanControlNotificationEvent Event => this.@event;

		/// <summary>
		/// Parameters
		/// </summary>
		public string[] Parameters { get; }

		/// <summary>
		/// AllParameters
		/// </summary>
		public string[] AllParameters { get; }
	}
}
