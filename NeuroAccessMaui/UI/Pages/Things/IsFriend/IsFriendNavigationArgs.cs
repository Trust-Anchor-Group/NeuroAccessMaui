using IdApp.Services.Notification.Things;

namespace NeuroAccessMaui.UI.Pages.Things.IsFriend
{
	/// <summary>
	/// Holds navigation parameters specific to displaying the is-friend provisioning question.
	/// </summary>
	public class IsFriendNavigationArgs : ProvisioningNavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="IsFriendNavigationArgs"/> class.
		/// </summary>
		public IsFriendNavigationArgs() { }

		/// <summary>
		/// Creates a new instance of the <see cref="IsFriendNavigationArgs"/> class.
		/// </summary>
		/// <param name="Event">Notification event object.</param>
		/// <param name="FriendlyName">Friendly name of device.</param>
		/// <param name="RemoteFriendlyName">Friendly name of remote entity.</param>
		public IsFriendNavigationArgs(IsFriendNotificationEvent Event, string FriendlyName, string RemoteFriendlyName)
			: base(Event, FriendlyName, RemoteFriendlyName)
		{
		}
	}
}
