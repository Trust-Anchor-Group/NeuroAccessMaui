using System;
using System.Collections.Generic;
using System.IO;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Represents a request to send media as part of an outbound message.
	/// </summary>
	public class ChatMediaSendRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMediaSendRequest"/> class.
		/// </summary>
		/// <param name="remoteBareJid">Remote bare JID.</param>
		/// <param name="mediaPath">Path to local media file.</param>
		/// <param name="contentType">Mime type.</param>
		/// <param name="isInline">If media should be embedded inline.</param>
		/// <param name="metadata">Additional metadata.</param>
		public ChatMediaSendRequest(string remoteBareJid, string mediaPath, string contentType, bool isInline, IReadOnlyDictionary<string, string>? metadata)
		{
			if (string.IsNullOrWhiteSpace(remoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(remoteBareJid));

			if (string.IsNullOrWhiteSpace(mediaPath))
				throw new ArgumentException("Media path is required.", nameof(mediaPath));

			if (!File.Exists(mediaPath))
				throw new FileNotFoundException("Media path not found.", mediaPath);

			this.RemoteBareJid = remoteBareJid;
			this.MediaPath = mediaPath;
			this.ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
			this.IsInline = isInline;
			this.Metadata = metadata;
		}

		/// <summary>
		/// Remote bare JID.
		/// </summary>
		public string RemoteBareJid { get; }

		/// <summary>
		/// Path to the media file on disk.
		/// </summary>
		public string MediaPath { get; }

		/// <summary>
		/// Mime type.
		/// </summary>
		public string ContentType { get; }

		/// <summary>
		/// Indicates whether the media should be embedded inline.
		/// </summary>
		public bool IsInline { get; }

		/// <summary>
		/// Extra metadata associated with the request.
		/// </summary>
		public IReadOnlyDictionary<string, string>? Metadata { get; }
	}
}
