using System;
using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Represents a persisted chat message with metadata required for orchestration.
	/// </summary>
	public class ChatMessageDescriptor
	{
		/// <summary>
		/// Unique identifier for the message record within local storage.
		/// </summary>
		public string MessageId { get; set; } = string.Empty;

		/// <summary>
		/// Bare JID of the remote participant for the session.
		/// </summary>
		public string RemoteBareJid { get; set; } = string.Empty;

		/// <summary>
		/// Temporary identifier used before the server assigns an official id.
		/// </summary>
		public string? LocalTempId { get; set; }

		/// <summary>
		/// Identifier provided by the remote system, if any.
		/// </summary>
		public string? RemoteObjectId { get; set; }

		/// <summary>
		/// Message ordering direction (outgoing, incoming, or system generated).
		/// </summary>
		public ChatMessageDirection Direction { get; set; }

		/// <summary>
		/// Current delivery state.
		/// </summary>
		public ChatDeliveryStatus DeliveryStatus { get; set; }

		/// <summary>
		/// Time when the message was originally produced.
		/// </summary>
		public DateTime Created { get; set; }

		/// <summary>
		/// Time when the message was last updated.
		/// </summary>
		public DateTime Updated { get; set; }

		/// <summary>
		/// Indicates if the message was edited after initial send.
		/// </summary>
		public bool IsEdited { get; set; }

		/// <summary>
		/// Timestamp when the message was originally created before edits.
		/// </summary>
		public DateTime? OriginalCreated { get; set; }

		/// <summary>
		/// Optional reference to the message being replied to.
		/// </summary>
		public string? ReplyToId { get; set; }

		/// <summary>
		/// Markdown content as authored.
		/// </summary>
		public string Markdown { get; set; } = string.Empty;

		/// <summary>
		/// Plain text representation for search and accessibility.
		/// </summary>
		public string PlainText { get; set; } = string.Empty;

		/// <summary>
		/// HTML content when provided by the transport.
		/// </summary>
		public string Html { get; set; } = string.Empty;

		/// <summary>
		/// Hash representing the render relevant inputs for caching.
		/// </summary>
		public string ContentFingerprint { get; set; } = string.Empty;

		/// <summary>
		/// Additional metadata (e.g., media identifiers, custom payload).
		/// </summary>
		public IReadOnlyDictionary<string, string>? Metadata { get; set; }
	}
}
