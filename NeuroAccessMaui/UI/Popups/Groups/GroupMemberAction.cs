namespace NeuroAccessMaui.UI.Popups.Groups
{
	/// <summary>
	/// What the user chose to do with a member of a group.
	/// </summary>
	public enum GroupMemberAction
	{
		/// <summary>
		/// Open a chat with the member.
		/// </summary>
		Message,

		/// <summary>
		/// Open the member's contact card.
		/// </summary>
		ViewContact,

		/// <summary>
		/// Make the member an admin.
		/// </summary>
		MakeAdmin,

		/// <summary>
		/// Remove the member from the group.
		/// </summary>
		Remove
	}
}
