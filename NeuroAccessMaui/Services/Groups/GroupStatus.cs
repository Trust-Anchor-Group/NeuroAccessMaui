namespace NeuroAccessMaui.Services.Groups
{
	/// <summary>
	/// The user's place in a group.
	/// </summary>
	public enum GroupStatus
	{
		/// <summary>
		/// The user has been invited, and has not answered yet.
		/// </summary>
		Invited,

		/// <summary>
		/// The user is in the group.
		/// </summary>
		Joined,

		/// <summary>
		/// The user has left the group, was removed, or the group was deleted. Its messages stay on the phone.
		/// </summary>
		Left
	}
}
