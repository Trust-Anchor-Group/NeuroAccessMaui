using NeuroAccessMaui.UI.Pages.Things.IsFriend;
using IdApp.Services.Notification.Things;

namespace NeuroAccessMaui.UI.Pages.Things.CanRead
{
	/// <summary>
	/// Base class for thing navigation arguments.
	/// </summary>
	public class ThingNavigationArgs : ProvisioningNavigationArgs
	{
		/// <summary>
		/// Base class for thing navigation arguments.
		/// </summary>
		public ThingNavigationArgs() { }

		/// <summary>
		/// Creates a new instance of the <see cref="ThingNavigationArgs"/> class.
		/// </summary>
		/// <param name="Event">Notification event object.</param>
		/// <param name="FriendlyName">Friendly name of device.</param>
		/// <param name="RemoteFriendlyName">Friendly name of remote entity.</param>
		public ThingNavigationArgs(ThingNotificationEvent Event, string FriendlyName, string RemoteFriendlyName)
			: base(Event, FriendlyName, RemoteFriendlyName)
		{
			this.NodeId = Event.NodeId;
			this.SourceId = Event.SourceId;
			this.PartitionId = Event.PartitionId;
			this.UserTokens = Event.UserTokens;
			this.DeviceTokens = Event.DeviceTokens;
			this.ServiceTokens = Event.ServiceTokens;
		}

		/// <summary>
		/// Node ID
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Partition ID
		/// </summary>
		public string PartitionId { get; }

		/// <summary>
		/// Source ID
		/// </summary>
		public string SourceId { get; }

		/// <summary>
		/// User Tokens
		/// </summary>
		public ProvisioningToken[] UserTokens { get; }

		/// <summary>
		/// Service Tokens
		/// </summary>
		public ProvisioningToken[] ServiceTokens { get; }

		/// <summary>
		/// Device Tokens
		/// </summary>
		public ProvisioningToken[] DeviceTokens { get; }
	}
}
