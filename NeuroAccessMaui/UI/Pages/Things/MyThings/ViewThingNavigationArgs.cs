using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;

namespace NeuroAccessMaui.UI.Pages.Things.MyThings
{
	/// <summary>
	/// Holds navigation parameters specific to viewing things.
	/// </summary>
	public class MyThingsNavigationArgs : NavigationArgs
	{
		private readonly TaskCompletionSource<ContactInfoModel?>? thingToShare;

		/// <summary>
		/// Creates a new instance of the <see cref="MyThingsNavigationArgs"/> class.
		/// </summary>
		public MyThingsNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="MyThingsNavigationArgs"/> class.
		/// </summary>
		/// <param name="ThingToShare">Task completion source where the selected thing is placed.</param>
		public MyThingsNavigationArgs(TaskCompletionSource<ContactInfoModel?> ThingToShare)
		{
			this.thingToShare = ThingToShare;
		}

		/// <summary>
		/// Task completion source where the selected thing is placed.
		/// </summary>
		public TaskCompletionSource<ContactInfoModel?>? ThingToShare => this.thingToShare;
	}
}
