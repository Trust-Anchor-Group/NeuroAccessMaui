using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Svg.Skia;
using Waher.Content;
using Waher.Content.Images;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Helpers
{
	/// <summary>
	/// SVG image encoder/decoder. Decodes to an <see cref="SKSvg"/>.
	/// </summary>
	public class SvgCodec : IContentDecoder, IContentEncoder
	{
		public SvgCodec() { }

		public static readonly string[] SvgContentTypes = new string[] { ImageCodec.ContentTypeSvg };
		public static readonly string[] SvgFileExtensions = new string[] { ImageCodec.FileExtensionSvg };
		public string[] ContentTypes => SvgContentTypes;
		public string[] FileExtensions => SvgFileExtensions;

		public bool Decodes(string ContentType, out Grade Grade)
		{
			if (string.Compare(ContentType, ImageCodec.ContentTypeSvg, true) == 0)
			{
				Grade = Grade.Excellent;
				return true;
			}
			Grade = Grade.NotAtAll;
			return false;
		}

		public Task<ContentResponse> DecodeAsync(string ContentType, byte[] Data, Encoding Encoding,
			KeyValuePair<string, string>[] Fields, Uri BaseUri, ICodecProgress Progress)
		{
			SKSvg Svg;
			try
			{
				Svg = new SKSvg();
				using (MemoryStream ms = new MemoryStream(Data))
				{
					Svg.Load(ms);
				}
			}
			catch (Exception ex)
			{
				return Task.FromResult(new ContentResponse(ex));
			}

			return Task.FromResult(new ContentResponse(ContentType, Svg, Data));
		}

		public bool Encodes(object Object, out Grade Grade, params string[] AcceptedContentTypes)
		{
			if (InternetContent.IsAccepted(SvgContentTypes, AcceptedContentTypes))
			{
				if (Object is string s && s.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Grade = Grade.Ok;
					return true;
				}
				else if (Object is SKSvg)
				{
					Grade = Grade.Barely; // Cannot re-encode reliably without original XML.
					return false;
				}
			}
			Grade = Grade.NotAtAll;
			return false;
		}

		public Task<ContentResponse> EncodeAsync(object Object, Encoding Encoding, ICodecProgress Progress, params string[] AcceptedContentTypes)
		{
			if (!InternetContent.IsAccepted(SvgContentTypes, out string ContentType, AcceptedContentTypes))
				return Task.FromResult(new ContentResponse(new ArgumentException("Unable to encode object, or content type not accepted.", nameof(Object))));

			if (!(Object is string Xml))
				return Task.FromResult(new ContentResponse(new ArgumentException("Encoder expects raw SVG markup string.", nameof(Object))));

			if (Xml.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) < 0)
				return Task.FromResult(new ContentResponse(new ArgumentException("Object does not appear to be valid SVG markup.", nameof(Object))));

			if (Encoding is null)
			{
				ContentType += "; charset=utf-8";
				Encoding = Encoding.UTF8;
			}
			else
				ContentType += "; charset=" + Encoding.WebName;

			return Task.FromResult(new ContentResponse(ContentType, Object, Encoding.GetBytes(Xml)));
		}

		public bool TryGetContentType(string FileExtension, out string ContentType)
		{
			switch (FileExtension.ToLower())
			{
				case "svg":
					ContentType = ImageCodec.ContentTypeSvg;
					return true;
				default:
					ContentType = string.Empty;
					return false;
			}
		}

		public bool TryGetFileExtension(string ContentType, out string FileExtension)
		{
			switch (ContentType.ToLower())
			{
				case "image/svg+xml":
					FileExtension = "svg";
					return true;
				default:
					FileExtension = string.Empty;
					return false;
			}
		}
	}
}
