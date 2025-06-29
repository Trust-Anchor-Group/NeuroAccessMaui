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
				return [];
			}
			return attachments.Where(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Returns the first image attachment of the array, if there is one.
		/// </summary>
		/// <param name="attachments">The attachments to iterate.</param>
		/// <returns>The first image attachment, or <c>null</c>.</returns>
		public static Attachment? GetFirstImageAttachment(this Attachment[] attachments)
		{
			return attachments?.FirstOrDefault(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Joins two arrays.
		/// </summary>
		/// <typeparam name="T">Array element type.</typeparam>
		/// <param name="Array1">First array.</param>
		/// <param name="Array2">Second array</param>
		/// <returns>Array containing elements from both arrays.</returns>
		public static T[] Join<T>(this T[] Array1, T[] Array2)
		{
			if (Array1 is null)
				return Array2;
			else if (Array2 is null)
				return Array1;
			else
			{
				int c = Array1.Length;
				int d = Array2.Length;
				T[] Result = new T[c + d];

				Array1.CopyTo(Result, 0);
				Array2.CopyTo(Result, c);

				return Result;
			}
		}
	}
}
