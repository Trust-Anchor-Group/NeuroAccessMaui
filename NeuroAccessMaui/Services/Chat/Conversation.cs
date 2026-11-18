using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// A chat in the list of chats: who it is with, its last message, and how the user has set it up. The messages themselves
	/// are kept as <see cref="NeuroAccessMaui.UI.Pages.Contacts.Chat.ChatMessage"/>.
	/// </summary>
	[CollectionName("Conversations")]
	[TypeName(TypeNameSerialization.None)]
	[Index("Address")]
	public class Conversation
	{
		/// <summary>
		/// The object ID in the database.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// Who the conversation is with.
		/// </summary>
		public ConversationKind Kind { get; set; } = ConversationKind.Person;

		/// <summary>
		/// The chat address of the other person, of the user for their own notes, or of the group.
		/// </summary>
		public CaseInsensitiveString Address { get; set; } = CaseInsensitiveString.Empty;

		/// <summary>
		/// The name shown for the conversation, when it was last known. The contact's current name is preferred when shown.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// When the last message was sent or received, in UTC.
		/// </summary>
		public DateTime LastMessageAt { get; set; } = DateTime.MinValue;

		/// <summary>
		/// The last message as a short plain text. Something shared, such as a photo or a contract, is a word for what it is.
		/// </summary>
		[DefaultValueStringEmpty]
		public string LastPreview { get; set; } = string.Empty;

		/// <summary>
		/// If the user sent the last message.
		/// </summary>
		[DefaultValue(false)]
		public bool LastFromMe { get; set; }

		/// <summary>
		/// The number of received messages the user has not seen yet.
		/// </summary>
		[DefaultValue(0)]
		public int UnreadCount { get; set; }

		/// <summary>
		/// If new messages in the conversation give no notification.
		/// </summary>
		[DefaultValue(false)]
		public bool Muted { get; set; }

		/// <summary>
		/// If the conversation is kept at the top of the list.
		/// </summary>
		[DefaultValue(false)]
		public bool Pinned { get; set; }
	}
}
