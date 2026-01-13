using System;

namespace NeuroAccessMaui.Services.Chat.Models
{
	/// <summary>
	/// Describes a media asset associated with a chat message.
	/// </summary>
	public class ChatMediaDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatMediaDescriptor"/> class.
		/// </summary>
		/// <param name="mediaId">Unique media identifier.</param>
		/// <param name="contentType">Mime type.</param>
		/// <param name="length">Length in bytes.</param>
		/// <param name="thumbnailPath">Thumbnail path if available.</param>
		public ChatMediaDescriptor(string mediaId, string contentType, long length, string? thumbnailPath)
		{
			this.MediaId = mediaId ?? throw new ArgumentNullException(nameof(mediaId));
			this.ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
			this.Length = length;
			this.ThumbnailPath = thumbnailPath;
		}

		/// <summary>
		/// Unique media identifier.
		/// </summary>
		public string MediaId { get; }

		/// <summary>
		/// Mime type.
		/// </summary>
		public string ContentType { get; }

		/// <summary>
		/// Length in bytes.
		/// </summary>
		public long Length { get; }

		/// <summary>
		/// Path to thumbnail asset.
		/// </summary>
		public string? ThumbnailPath { get; }
	}
}
