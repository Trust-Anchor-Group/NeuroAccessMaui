namespace NeuroAccessMaui.Services.Groups
{
	/// <summary>
	/// Who can join a group.
	/// </summary>
	public enum GroupJoinRule
	{
		/// <summary>
		/// Only people a member adds.
		/// </summary>
		MembersOnly,

		/// <summary>
		/// Anyone with the group's invite link.
		/// </summary>
		InviteLink
	}
}
