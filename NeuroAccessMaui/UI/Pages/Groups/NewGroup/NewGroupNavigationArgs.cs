using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Groups.NewGroup
{
	/// <summary>
	/// Opens the page where a group is made, or where people are added to a group.
	/// </summary>
	public class NewGroupNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Makes a new group, with no one chosen yet.
		/// </summary>
		public NewGroupNavigationArgs()
		{
		}

		/// <summary>
		/// Makes a new group, with one person chosen already.
		/// </summary>
		/// <param name="PreselectedAddress">The chat address of the person chosen, or null for no one.</param>
		public NewGroupNavigationArgs(string? PreselectedAddress)
		{
			this.PreselectedAddress = PreselectedAddress;
		}

		/// <summary>
		/// Adds people to a group the user is in, instead of making a new one.
		/// </summary>
		/// <param name="GroupAddress">The group's address.</param>
		/// <returns>The navigation arguments.</returns>
		public static NewGroupNavigationArgs AddPeopleTo(string GroupAddress)
		{
			return new NewGroupNavigationArgs()
			{
				GroupAddress = GroupAddress
			};
		}

		/// <summary>
		/// The chat address of a person chosen from the start, if any.
		/// </summary>
		public string? PreselectedAddress { get; }

		/// <summary>
		/// The address of the group people are added to, or null when a new group is made.
		/// </summary>
		public string? GroupAddress { get; private init; }
	}
}
