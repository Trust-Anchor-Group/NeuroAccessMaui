using System;
using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Represents an outbound chat message that should be sent to the remote party.
	/// </summary>
	public class ChatOutboundMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatOutboundMessage"/> class.
		/// </summary>
		/// <param name="remoteBareJid">Remote bare JID.</param>
		/// <param name="markdown">Markdown payload.</param>
		/// <param name="plainText">Plain text fallback.</param>
		/// <param name="html">Html fallback.</param>
		/// <param name="replyToId">Message identifier being replied to.</param>
		/// <param name="localTempId">Local temporary identifier.</param>
		/// <param name="metadata">Additional metadata.</param>
		public ChatOutboundMessage(string remoteBareJid, string markdown, string plainText, string html, string? replyToId, string? localTempId, IReadOnlyDictionary<string, string>? metadata)
		{
			this.RemoteBareJid = remoteBareJid ?? throw new ArgumentNullException(nameof(remoteBareJid));
			this.Markdown = markdown ?? throw new ArgumentNullException(nameof(markdown));
			this.PlainText = plainText ?? throw new ArgumentNullException(nameof(plainText));
			this.Html = html ?? throw new ArgumentNullException(nameof(html));
			this.ReplyToId = replyToId;
			this.LocalTempId = localTempId;
			this.Metadata = metadata;
		}

		/// <summary>
		/// Remote bare JID.
		/// </summary>
		public string RemoteBareJid { get; }

		/// <summary>
		/// Markdown payload.
		/// </summary>
		public string Markdown { get; }

		/// <summary>
		/// Plain text fallback.
		/// </summary>
		public string PlainText { get; }

		/// <summary>
		/// Html fallback.
		/// </summary>
		public string Html { get; }

		/// <summary>
		/// Identifier of message being replied to.
		/// </summary>
		public string? ReplyToId { get; }

		/// <summary>
		/// Local temporary identifier.
		/// </summary>
		public string? LocalTempId { get; }

		/// <summary>
		/// Additional metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string>? Metadata { get; }
	}
}
