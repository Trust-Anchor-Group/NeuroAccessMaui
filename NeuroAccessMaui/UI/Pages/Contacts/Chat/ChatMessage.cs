using System.Globalization;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// Message type
	/// </summary>
	public enum MessageType
	{
		/// <summary>
		/// Message sent by the user
		/// </summary>
		Sent,

		/// <summary>
		/// Message received from remote party
		/// </summary>
		Received
	}

	/// <summary>
	/// A stored chat message.
	/// </summary>
	[CollectionName("ChatMessages")]
	[TypeName(TypeNameSerialization.None)]
	[Index("RemoteBareJid", "Created")]
	[Index("RemoteBareJid", "RemoteObjectId")]
	public class ChatMessage : IUniqueItem
	{
		/// <summary>
		/// A stored chat message.
		/// </summary>
		public ChatMessage()
		{
		}

		/// <inheritdoc/>
		public string UniqueName => this.ObjectId ?? string.Empty;

		/// <summary>
		/// Object ID. For sent messages, it is also the ID of the message stanza.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// Address of the other party.
		/// </summary>
		public CaseInsensitiveString? RemoteBareJid { get; set; }

		/// <summary>
		/// When the message was sent or received.
		/// </summary>
		public DateTime Created { get; set; } = DateTime.MinValue;

		/// <summary>
		/// When the message was last corrected, or <see cref="DateTime.MinValue"/>.
		/// </summary>
		[DefaultValueDateTimeMinValue]
		public DateTime Updated { get; set; } = DateTime.MinValue;

		/// <summary>
		/// When the other party confirmed that a sent message arrived, or <see cref="DateTime.MinValue"/>.
		/// </summary>
		[DefaultValueDateTimeMinValue]
		public DateTime Delivered { get; set; } = DateTime.MinValue;

		/// <summary>
		/// ID of the message stanza of a received message.
		/// </summary>
		public string? RemoteObjectId { get; set; }

		/// <summary>
		/// If the message was sent or received.
		/// </summary>
		public MessageType MessageType { get; set; } = MessageType.Received;

		/// <summary>
		/// Plain text of the message.
		/// </summary>
		public string PlainText { get; set; } = string.Empty;

		/// <summary>
		/// Markdown of the message, if any.
		/// </summary>
		public string Markdown { get; set; } = string.Empty;

		/// <summary>
		/// HTML of the message, if any.
		/// </summary>
		public string Html { get; set; } = string.Empty;

		/// <summary>
		/// In a group: the sender's name in the group.
		/// </summary>
		[DefaultValueNull]
		public string? SenderName { get; set; }

		/// <summary>
		/// In a group: the sender's chat address, if the group tells it.
		/// </summary>
		[DefaultValueNull]
		public string? SenderAddress { get; set; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Created.ToString(CultureInfo.CurrentCulture);
		}
	}
}
