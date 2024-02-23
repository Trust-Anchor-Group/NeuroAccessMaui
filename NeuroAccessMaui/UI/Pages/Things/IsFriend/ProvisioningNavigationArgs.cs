using NeuroAccessMaui.Services.Notification.Things;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// Base class for provisioning navigation arguments.
	/// </summary>
	public class ProvisioningNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Base class for provisioning navigation arguments.
		/// </summary>
		public ProvisioningNavigationArgs()
		{
		}

		/// <summary>
		/// Base class for provisioning navigation arguments.
		/// </summary>
		/// <param name="Event">Notification event object.</param>
		/// <param name="FriendlyName">Friendly name of device.</param>
		/// <param name="RemoteFriendlyName">Friendly name of remote entity.</param>
		public ProvisioningNavigationArgs(ProvisioningNotificationEvent Event, string FriendlyName, string RemoteFriendlyName)
		{
			this.Event = Event;
			this.BareJid = Event.BareJid;
			this.FriendlyName = FriendlyName;
			this.RemoteJid = Event.RemoteJid;
			this.RemoteFriendlyName = RemoteFriendlyName;
			this.Key = Event.Key;
			this.ProvisioningService = Event.ProvisioningService;
		}

		/// <summary>
		/// Notification event object.
		/// </summary>
		public ProvisioningNotificationEvent? Event { get; }

		/// <summary>
		/// Bare JID of device.
		/// </summary>
		public string? BareJid { get; }

		/// <summary>
		/// Friendly name of device.
		/// </summary>
		public string? FriendlyName { get; }

		/// <summary>
		/// Bare JID of remote entity trying to connect to device.
		/// </summary>
		public string? RemoteJid { get; }

		/// <summary>
		/// Friendly name of remote entity.
		/// </summary>
		public string? RemoteFriendlyName { get; }

		/// <summary>
		/// Provisioning key
		/// </summary>
		public string? Key { get; }

		/// <summary>
		/// Provisioning Service
		/// </summary>
		public string? ProvisioningService { get; }
	}
}
