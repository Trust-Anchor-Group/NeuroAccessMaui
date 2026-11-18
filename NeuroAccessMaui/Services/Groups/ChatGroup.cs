using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Groups
{
	/// <summary>
	/// A group the user is in, has left, or is invited to. Its chat is a <see cref="Chat.Conversation"/> of the kind
	/// <see cref="Chat.ConversationKind.Group"/>, with the same address.
	/// </summary>
	[CollectionName("ChatGroups")]
	[TypeName(TypeNameSerialization.None)]
	[Index("Address")]
	public class ChatGroup
	{
		/// <summary>
		/// The object ID in the database.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// The group's address on the group service.
		/// </summary>
		public CaseInsensitiveString Address { get; set; } = CaseInsensitiveString.Empty;

		/// <summary>
		/// The group's name.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// What the group is for.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// The user's name in the group.
		/// </summary>
		[DefaultValueStringEmpty]
		public string MyName { get; set; } = string.Empty;

		/// <summary>
		/// What the user may do in the group.
		/// </summary>
		[DefaultValue(GroupRole.Member)]
		public GroupRole MyRole { get; set; } = GroupRole.Member;

		/// <summary>
		/// The user's place in the group.
		/// </summary>
		[DefaultValue(GroupStatus.Joined)]
		public GroupStatus Status { get; set; } = GroupStatus.Joined;

		/// <summary>
		/// Who can join the group.
		/// </summary>
		[DefaultValue(GroupJoinRule.MembersOnly)]
		public GroupJoinRule JoinRule { get; set; } = GroupJoinRule.MembersOnly;

		/// <summary>
		/// If members who are not admins can add people.
		/// </summary>
		[DefaultValue(false)]
		public bool MembersCanAdd { get; set; }

		/// <summary>
		/// When the latest message the app has came, in UTC. Messages after it are fetched when the app joins again.
		/// </summary>
		public DateTime LastSeenMessageAt { get; set; } = DateTime.MinValue;

		/// <summary>
		/// The members, as the app last knew them.
		/// </summary>
		[DefaultValueNull]
		public GroupMember[]? Members { get; set; }

		/// <summary>
		/// The chat address of who invited the user, while the invite waits for an answer.
		/// </summary>
		[DefaultValueStringEmpty]
		public string InvitedBy { get; set; } = string.Empty;

		/// <summary>
		/// When the group was added on this phone.
		/// </summary>
		public DateTime Created { get; set; } = DateTime.MinValue;

		/// <summary>
		/// The group's ID on the group service: the part of its address before the @.
		/// </summary>
		[IgnoreMember]
		public string RoomId
		{
			get
			{
				string Value = this.Address.Value;
				int i = Value.IndexOf('@');
				return i < 0 ? Value : Value[..i];
			}
		}

		/// <summary>
		/// The address of the group service: the part of the group's address after the @.
		/// </summary>
		[IgnoreMember]
		public string Domain
		{
			get
			{
				string Value = this.Address.Value;
				int i = Value.IndexOf('@');
				return i < 0 ? string.Empty : Value[(i + 1)..];
			}
		}

		/// <summary>
		/// If the user can add people: admins and owners can, and members can if the group lets them.
		/// </summary>
		[IgnoreMember]
		public bool CanAddPeople => this.Status == GroupStatus.Joined && (this.MyRole != GroupRole.Member || this.MembersCanAdd);

		/// <summary>
		/// If the user can remove people and change the group: admins and owners can.
		/// </summary>
		[IgnoreMember]
		public bool CanManage => this.Status == GroupStatus.Joined && this.MyRole != GroupRole.Member;

		/// <summary>
		/// The number of members, as the app last knew them.
		/// </summary>
		[IgnoreMember]
		public int MemberCount => this.Members?.Length ?? 0;
	}
}
