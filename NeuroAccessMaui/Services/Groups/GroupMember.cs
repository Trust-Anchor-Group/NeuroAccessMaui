using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Groups
{
	/// <summary>
	/// A member of a group, as the app last knew them.
	/// </summary>
	[TypeName(TypeNameSerialization.None)]
	public class GroupMember
	{
		/// <summary>
		/// The member's chat address, if known.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Address { get; set; } = string.Empty;

		/// <summary>
		/// The member's name in the group.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// What the member may do in the group.
		/// </summary>
		[DefaultValue(GroupRole.Member)]
		public GroupRole Role { get; set; } = GroupRole.Member;
	}
}
