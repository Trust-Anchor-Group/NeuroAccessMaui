using Waher.Networking.XMPP.Contracts;

// !!! keep the namespace as is. It's important for the database
namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Represent an attachment to a <see cref="LegalIdentity"/>.
	/// </summary>
	public sealed class LegalIdentityAttachment
	{
		/// <summary>
		/// Creates an instance of a <see cref="LegalIdentityAttachment"/>.
		/// </summary>
		public LegalIdentityAttachment()
		{
		}

		/// <summary>
		/// Creates an instance of a <see cref="LegalIdentityAttachment"/>.
		/// </summary>
		/// <param name="fileName">The actual filename</param>
		/// <param name="contentType">The content type.</param>
		/// <param name="data">The raw file data.</param>
		public LegalIdentityAttachment(string fileName, string contentType, byte[] data)
		{
			this.FileName = fileName;
			this.ContentType = contentType;
			this.Data = data;
			this.ContentLength = data?.Length ?? 0;
		}

		/// <summary>
		/// The raw filename.
		/// </summary>
		public string? FileName { get; set; }

		/// <summary>
		/// Content type (mime) of the attachment.
		/// </summary>
		public string? ContentType { get; set; }

		/// <summary>
		/// The raw file data.
		/// </summary>
		public byte[]? Data { get; set; }

		/// <summary>
		/// Attachment content length.
		/// </summary>
		public long ContentLength { get; set; }
	}
}
