using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Extensions for generic <see cref="Array"/>s.
	/// </summary>
	public static class ArrayExtensions
	{
		/// <summary>
		/// Returns all the attachments whose content type starts with "image".
		/// </summary>
		/// <param name="attachments">The attachments to iterate.</param>
		/// <returns>Attachments</returns>
		public static IEnumerable<Attachment> GetImageAttachments(this Attachment[] attachments)
		{
			if (attachments is null)
			{
				return Enumerable.Empty<Attachment>();
			}
			return attachments.Where(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Returns the first image attachment of the array, if there is one.
		/// </summary>
		/// <param name="attachments">The attachments to iterate.</param>
		/// <returns>The first image attachment, or <c>null</c>.</returns>
		public static Attachment GetFirstImageAttachment(this Attachment[] attachments)
		{
			return attachments?.FirstOrDefault(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
		}
	}
}
