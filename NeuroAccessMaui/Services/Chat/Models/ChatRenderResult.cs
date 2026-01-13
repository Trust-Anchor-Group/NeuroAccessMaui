using System;
using System.Collections.Generic;
using System.Globalization;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Captures the outcome of rendering markdown into UI-agnostic segments.
	/// </summary>
	public class ChatRenderResult
	{
		/// <summary>
		/// Creates a new render result.
		/// </summary>
		/// <param name="messageId">Message identifier the render corresponds to.</param>
		/// <param name="fingerprint">Fingerprint representing the render inputs.</param>
		/// <param name="segments">Produced segments in display order.</param>
		/// <param name="culture">Culture used during rendering.</param>
		public ChatRenderResult(string messageId, string fingerprint, IReadOnlyList<ChatRenderSegment> segments, CultureInfo culture)
		{
			this.MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
			this.Fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
			this.Segments = segments ?? throw new ArgumentNullException(nameof(segments));
			this.Culture = culture ?? throw new ArgumentNullException(nameof(culture));
		}

		/// <summary>
		/// Message identifier.
		/// </summary>
		public string MessageId { get; }

		/// <summary>
		/// Fingerprint representing the content used for rendering.
		/// </summary>
		public string Fingerprint { get; }

		/// <summary>
		/// Rendered segments.
		/// </summary>
		public IReadOnlyList<ChatRenderSegment> Segments { get; }

		/// <summary>
		/// Culture used when rendering.
		/// </summary>
		public CultureInfo Culture { get; }
	}
}
