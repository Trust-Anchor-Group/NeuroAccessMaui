namespace NeuroAccessMaui.Services.Groups
{
	/// <summary>
	/// What a member may do in a group.
	/// </summary>
	public enum GroupRole
	{
		/// <summary>
		/// Reads and writes messages.
		/// </summary>
		Member,

		/// <summary>
		/// Can also add and remove people, and change the group's name and description.
		/// </summary>
		Admin,

		/// <summary>
		/// Created the group, or was made owner. Can also make admins and delete the group for everyone.
		/// </summary>
		Owner
	}
}
