using System;
using NeuroAccessMaui.UI.Pages;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Persistence record for chat messages stored in Waher.
	/// </summary>
	[CollectionName("ChatMessages")]
	[TypeName(TypeNameSerialization.None)]
	[Index("RemoteBareJid", "Created")]
	[Index("RemoteBareJid", "RemoteObjectId")]
	public class ChatMessageRecord : IUniqueItem
	{
		/// <summary>
		/// Object identifier.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// Remote bare JID.
		/// </summary>
		public CaseInsensitiveString? RemoteBareJid { get; set; }

		/// <summary>
		/// Temporary local identifier.
		/// </summary>
		public string? LocalTempId { get; set; }

		/// <summary>
		/// Remote object identifier.
		/// </summary>
		public string? RemoteObjectId { get; set; }

		/// <summary>
		/// Direction of the message.
		/// </summary>
		public ChatMessageDirection Direction { get; set; }

		/// <summary>
		/// Delivery status.
		/// </summary>
		public ChatDeliveryStatus DeliveryStatus { get; set; }

		/// <summary>
		/// Creation timestamp (UTC).
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// Last update timestamp (UTC).
		/// </summary>
		[DefaultValueDateTimeMinValue]
		public DateTime Updated { get; set; }

		/// <summary>
		/// Indicates if the message has been edited.
		/// </summary>
		public bool IsEdited { get; set; }

		/// <summary>
		/// Timestamp when the original message was created (before edit).
		/// </summary>
		public DateTime? OriginalCreatedTimestamp { get; set; }

		/// <summary>
		/// Identifier of the message being replied to.
		/// </summary>
		public string? ReplyToId { get; set; }

		/// <summary>
		/// Markdown content.
		/// </summary>
		public string Markdown { get; set; } = string.Empty;

		/// <summary>
		/// Plain text representation.
		/// </summary>
		public string PlainText { get; set; } = string.Empty;

		/// <summary>
		/// Html fallback.
		/// </summary>
		public string Html { get; set; } = string.Empty;

		/// <summary>
		/// Fingerprint for the render cache.
		/// </summary>
		public string ContentFingerprint { get; set; } = string.Empty;

		/// <summary>
		/// Reactions serialized as JSON (future expansion).
		/// </summary>
		public string? ReactionsJson { get; set; }

		/// <summary>
		/// Additional metadata serialized as JSON.
		/// </summary>
		public string? MetadataJson { get; set; }

		/// <summary>
		/// Legacy message type persisted by previous schema versions.
		/// </summary>
		public ChatLegacyMessageType LegacyMessageType { get; set; }

		/// <inheritdoc/>
		public string UniqueName => this.ObjectId ?? this.LocalTempId ?? string.Empty;
	}

	public enum ChatLegacyMessageType
	{
		Sent = 0,
		Received = 1
	}
}
