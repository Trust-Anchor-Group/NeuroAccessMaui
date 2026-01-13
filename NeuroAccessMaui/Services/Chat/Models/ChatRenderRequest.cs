using System;
using System.Globalization;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Request for rendering markdown content.
	/// </summary>
	public class ChatRenderRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatRenderRequest"/> class.
		/// </summary>
		/// <param name="messageId">Message identifier that owns the content.</param>
		/// <param name="markdown">Markdown content.</param>
		/// <param name="plainText">Plain text fall-back.</param>
		/// <param name="html">Html fallback.</param>
		/// <param name="direction">Message direction.</param>
		/// <param name="culture">Culture to use during rendering.</param>
		/// <param name="fingerprint">Hash representing the payload.</param>
		public ChatRenderRequest(string messageId, string markdown, string plainText, string html, ChatMessageDirection direction, CultureInfo culture, string fingerprint)
		{
			this.MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
			this.Markdown = markdown ?? throw new ArgumentNullException(nameof(markdown));
			this.PlainText = plainText ?? throw new ArgumentNullException(nameof(plainText));
			this.Html = html ?? throw new ArgumentNullException(nameof(html));
			this.Direction = direction;
			this.Culture = culture ?? throw new ArgumentNullException(nameof(culture));
			this.Fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
		}

		/// <summary>
		/// Message identifier.
		/// </summary>
		public string MessageId { get; }

		/// <summary>
		/// Markdown source.
		/// </summary>
		public string Markdown { get; }

		/// <summary>
		/// Plain text representation.
		/// </summary>
		public string PlainText { get; }

		/// <summary>
		/// Html fallback if markdown is empty.
		/// </summary>
		public string Html { get; }

		/// <summary>
		/// Direction of message owning the content.
		/// </summary>
		public ChatMessageDirection Direction { get; }

		/// <summary>
		/// Culture to use when rendering.
		/// </summary>
		public CultureInfo Culture { get; }

		/// <summary>
		/// Fingerprint describing the relevant content.
		/// </summary>
		public string Fingerprint { get; }
	}
}
