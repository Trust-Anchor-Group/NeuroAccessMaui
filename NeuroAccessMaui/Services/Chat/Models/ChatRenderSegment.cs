using System;
using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Describes a renderable segment produced by the markdown pipeline.
	/// </summary>
	public class ChatRenderSegment
	{
		/// <summary>
		/// Segment constructor.
		/// </summary>
		/// <param name="type">Segment type.</param>
		/// <param name="value">Primary textual value.</param>
		/// <param name="attributes">Optional attributes.</param>
		public ChatRenderSegment(ChatRenderSegmentType type, string value, IReadOnlyDictionary<string, string>? attributes = null)
		{
			this.Type = type;
			this.Value = value ?? throw new ArgumentNullException(nameof(value));
			this.Attributes = attributes;
		}

		/// <summary>
		/// Segment kind.
		/// </summary>
		public ChatRenderSegmentType Type { get; }

		/// <summary>
		/// Normalized value for the segment.
		/// </summary>
		public string Value { get; }

		/// <summary>
		/// Additional metadata providing context to the renderer.
		/// </summary>
		public IReadOnlyDictionary<string, string>? Attributes { get; }
	}
}
