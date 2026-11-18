using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Groups;

namespace NeuroAccessMaui.UI.Pages.Chats
{
	/// <summary>
	/// An invite to a group, shown at the top of Chats with Join and Decline.
	/// </summary>
	/// <param name="Group">The group the user is invited to.</param>
	/// <param name="InviterName">The name of who invited the user.</param>
	public sealed class GroupInviteItem(ChatGroup Group, string InviterName)
	{
		/// <summary>
		/// The group the user is invited to.
		/// </summary>
		public ChatGroup Group { get; } = Group;

		/// <summary>
		/// The invite as a sentence: who invited the user to which group.
		/// </summary>
		public string Text { get; } = ServiceRef.Localizer[nameof(AppResources.GroupsInvitedYou), InviterName, Group.Name];
	}
}
