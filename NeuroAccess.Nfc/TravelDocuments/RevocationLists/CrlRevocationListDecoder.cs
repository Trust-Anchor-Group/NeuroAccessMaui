using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using NeuroAccess.Nfc.TravelDocuments.Security;
using Waher.Content;
using Waher.Runtime.Inventory;

namespace NeuroAccess.Nfc.TravelDocuments.RevocationLists
{
	/// <summary>
	/// Decodes Certificate Revocation Lists.
	/// </summary>
	public class CrlRevocationListDecoder : IContentDecoder
	{
		/// <summary>
		/// Internet Content-Type for Certificate Revocation Lists.
		/// </summary>
		public const string DefaultContentType = "application/pkix-crl";

		/// <summary>
		/// Default file extension for CRL files.
		/// </summary>
		public const string DefaultFileExtension = "crl";

		/// <summary>
		/// Supported content types.
		/// </summary>
		public string[] ContentTypes => [DefaultContentType];

		/// <summary>
		/// Supported file extensions.
		/// </summary>
		public string[] FileExtensions => throw new NotImplementedException();

		/// <summary>
		/// Tries to get the content type of an item, given its file extension.
		/// </summary>
		/// <param name="FileExtension">File extension.</param>
		/// <param name="ContentType">Content type.</param>
		/// <returns>If the extension was recognized.</returns>
		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			switch (FileExtension.ToLower(CultureInfo.InvariantCulture))
			{
				case DefaultFileExtension:
					ContentType = DefaultContentType;
					return true;

				default:
					ContentType = string.Empty;
					return false;
			}
		}

		/// <summary>
		/// Tries to get the file extension of an item, given its Content-Type.
		/// </summary>
		/// <param name="ContentType">Content type.</param>
		/// <param name="FileExtension">File extension.</param>
		/// <returns>If the Content-Type was recognized.</returns>
		public bool TryGetFileExtension(string ContentType, out string FileExtension)
		{
			switch (ContentType.ToLower(CultureInfo.InvariantCulture))
			{
				case DefaultContentType:
					FileExtension = DefaultFileExtension;
					return true;

				default:
					FileExtension = string.Empty;
					return false;
			}
		}

		/// <summary>
		/// If the decoder decodes an object with a given content type.
		/// </summary>
		/// <param name="ContentType">Content type to decode.</param>
		/// <param name="Grade">How well the decoder decodes the object.</param>
		/// <returns>If the decoder can decode an object with the given type.</returns>
		public bool Decodes(string ContentType, out Grade Grade)
		{
			if (string.Equals(ContentType, DefaultContentType, StringComparison.OrdinalIgnoreCase))
			{
				Grade = Grade.Ok;
				return true;
			}
			else
			{
				Grade = Grade.NotAtAll;
				return false;
			}
		}

		/// <summary>
		/// Decodes an object.
		/// </summary>
		/// <param name="ContentType">Internet Content Type.</param>
		/// <param name="Data">Encoded object.</param>
		/// <param name="Encoding">Any encoding specified. Can be null if no encoding specified.</param>
		/// <param name="Fields">Any content-type related fields and their corresponding values.</param>
		///	<param name="BaseUri">Base URI, if any. If not available, value is null.</param>
		/// <param name="Progress">Optional progress reporting of encoding/decoding. Can be null.</param>
		/// <returns>Decoded object.</returns>
		public Task<ContentResponse> DecodeAsync(string ContentType, byte[] Data, Encoding Encoding, KeyValuePair<string, string>[] Fields, Uri BaseUri, ICodecProgress Progress)
		{
			if (!TravelDocumentsClient.TryDecodeDER(Data, out object? Decoded) ||
				Decoded is not Vector Crl ||
				!CertificateList.TryParse(Crl, out CertificateList? Parsed))
			{
				return Task.FromResult(new ContentResponse(new Exception("Unable to parse and decode CRL data.")));
			}

			return Task.FromResult(new ContentResponse(DefaultContentType, Parsed, Data));
		}
	}
}
