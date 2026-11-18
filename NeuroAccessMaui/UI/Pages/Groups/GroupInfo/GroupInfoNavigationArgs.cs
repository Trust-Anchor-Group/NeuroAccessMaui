using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Groups.GroupInfo
{
	/// <summary>
	/// Says which group the group page shows.
	/// </summary>
	public class GroupInfoNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Shows no group. Used when the page is made without arguments.
		/// </summary>
		public GroupInfoNavigationArgs()
		{
		}

		/// <summary>
		/// Shows a group.
		/// </summary>
		/// <param name="GroupAddress">The group's address.</param>
		public GroupInfoNavigationArgs(string GroupAddress)
		{
			this.GroupAddress = GroupAddress;
		}

		/// <summary>
		/// The group's address.
		/// </summary>
		public string? GroupAddress { get; }
	}
}
