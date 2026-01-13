using System;
using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Represents an event emitted by transport, repository, or session orchestration.
	/// </summary>
	public class ChatSessionEvent
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatSessionEvent"/> class.
		/// </summary>
		/// <param name="eventType">Event type.</param>
		/// <param name="remoteBareJid">Remote bare JID.</param>
		/// <param name="messages">Messages associated with the event.</param>
		/// <param name="additionalData">Additional data.</param>
		public ChatSessionEvent(ChatSessionEventType eventType, string remoteBareJid, IReadOnlyList<ChatMessageDescriptor>? messages, IReadOnlyDictionary<string, string>? additionalData = null)
		{
			this.EventType = eventType;
			this.RemoteBareJid = remoteBareJid ?? throw new ArgumentNullException(nameof(remoteBareJid));
			this.Messages = messages;
			this.AdditionalData = additionalData;
			this.Timestamp = DateTime.UtcNow;
		}

		/// <summary>
		/// Event type.
		/// </summary>
		public ChatSessionEventType EventType { get; }

		/// <summary>
		/// Remote bare JID impacted by the event.
		/// </summary>
		public string RemoteBareJid { get; }

		/// <summary>
		/// Messages associated with the event, if any.
		/// </summary>
		public IReadOnlyList<ChatMessageDescriptor>? Messages { get; }

		/// <summary>
		/// Additional data associated with the event.
		/// </summary>
		public IReadOnlyDictionary<string, string>? AdditionalData { get; }

		/// <summary>
		/// Time the event was created.
		/// </summary>
		public DateTime Timestamp { get; }
	}
}
