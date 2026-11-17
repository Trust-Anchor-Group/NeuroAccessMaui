using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Content;
using NeuroAccessMaui.UI.Controls.Markdown;
using SkiaSharp;
using Waher.Content;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Networking.XMPP.HttpFileUpload;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Sends and shows images in chats. Images are encrypted before they are uploaded, and the key travels in the message,
	/// as an <c>aes256:</c> link.
	/// </summary>
	public static class ChatAttachments
	{
		private const int MaxImageSide = 2048;
		private const int JpegQuality = 85;
		private const int DownloadTimeoutMs = 60000;
		private const string encryptedContentType = "application/octet-stream";
		private const string encryptedFileExtension = "bin";

		private static readonly Dictionary<string, Task<MdImageData>> loading = new(StringComparer.Ordinal);
		private static int registered;

		/// <summary>
		/// Lets message texts show encrypted images. Call before the first message text is built.
		/// </summary>
		public static void Register()
		{
			if (Interlocked.Exchange(ref registered, 1) == 0)
				MdImageSource.RegisterScheme(Constants.UriSchemes.Aes256, LoadImageAsync);
		}

		/// <summary>
		/// Prepares, encrypts and uploads an image, and returns the markdown that shows it.
		/// </summary>
		/// <param name="Data">Encoded image, as taken or picked.</param>
		/// <param name="FileName">File name of the image.</param>
		/// <returns>Markdown for the message.</returns>
		/// <exception cref="InvalidOperationException">If the image is too large, or the app can't connect. The message
		/// can be shown to the user.</exception>
		public static async Task<string> UploadImageAsync(byte[] Data, string FileName)
		{
			(byte[] Image, string Name, string ContentType, int Width, int Height) = await Task.Run(() => PrepareImage(Data, FileName));

			byte[] Key = RandomNumberGenerator.GetBytes(32);
			byte[] IV = RandomNumberGenerator.GetBytes(16);
			byte[] Encrypted;

			using (Aes Aes = Aes.Create())
			{
				Aes.Mode = CipherMode.CBC;
				Aes.Padding = PaddingMode.PKCS7;

				using ICryptoTransform Transform = Aes.CreateEncryptor(Key, IV);
				Encrypted = Transform.TransformFinalBlock(Image, 0, Image.Length);
			}

			if (Encrypted.Length > ServiceRef.TagProfile.HttpFileUploadMaxSize)
				throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.PhotoIsTooLarge)]);

			// Taking or picking a photo switches to another app, so the app may have to connect again.
			if (!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
			{
				throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.UnableToConnectTo),
					ServiceRef.TagProfile.Domain ?? string.Empty]);
			}

			// The upload service rejects files whose extension does not match their content type. The encrypted bytes are
			// sent as application/octet-stream, so they are uploaded with the matching .bin extension.
			string UploadName = Path.ChangeExtension(Name, encryptedFileExtension);

			// Tells the upload service that the content is encrypted, so it can be kept in encrypted storage.

			StringBuilder Xml = new();

			Xml.Append("<prepare xmlns='http://waher.se/Schema/EncryptedStorage.xsd' filename='");
			Xml.Append(XML.Encode(UploadName));
			Xml.Append("' size='");
			Xml.Append(Encrypted.Length.ToString(CultureInfo.InvariantCulture));
			Xml.Append("' content-type='");
			Xml.Append(encryptedContentType);
			Xml.Append("'/>");

			await ServiceRef.XmppService.IqSetAsync(ServiceRef.TagProfile.HttpFileUploadJid!, Xml.ToString());

			HttpFileUploadEventArgs Slot = await ServiceRef.XmppService.RequestUploadSlotAsync(UploadName, encryptedContentType,
				Encrypted.Length);

			if (!Slot.Ok)
				throw Slot.StanzaError ?? new Exception(Slot.ErrorText);

			await Slot.PUT(Encrypted, encryptedContentType, (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

			StringBuilder Url = new();

			Url.Append(Constants.UriSchemes.Aes256);
			Url.Append(':');
			Url.Append(Convert.ToBase64String(Key));
			Url.Append(':');
			Url.Append(Convert.ToBase64String(IV));
			Url.Append(':');
			Url.Append(ContentType);
			Url.Append(':');
			Url.Append(Slot.GetUrl);

			string Link = Url.ToString();

			// The sender sees the image at once, without downloading it again.
			try
			{
				await StoreAsync(GetCacheFileName(Link, ContentType), Image);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return ImageMarkdown(Name, Link, Width, Height);
		}

		/// <summary>
		/// Prepares an image for notes to yourself, and keeps it on the phone. Nothing is uploaded.
		/// </summary>
		/// <param name="Data">Encoded image, as taken or picked.</param>
		/// <param name="FileName">File name of the image.</param>
		/// <returns>Markdown for the note.</returns>
		public static async Task<string> StoreNoteImageAsync(byte[] Data, string FileName)
		{
			(byte[] Image, string Name, string ContentType, int Width, int Height) = await Task.Run(() => PrepareImage(Data, FileName));

			string Folder = Path.Combine(FileSystem.AppDataDirectory, "ChatNotes");
			Directory.CreateDirectory(Folder);

			if (!InternetContent.TryGetFileExtension(ContentType, out string Extension) || string.IsNullOrEmpty(Extension))
				Extension = Path.GetExtension(Name).TrimStart('.');

			string FilePath = Path.Combine(Folder, Guid.NewGuid().ToString("N") + "." + Extension);
			await StoreAsync(FilePath, Image);

			return ImageMarkdown(Name, new Uri(FilePath).AbsoluteUri, Width, Height);
		}

		private static string ImageMarkdown(string Name, string Link, int Width, int Height)
		{
			StringBuilder Markdown = new();

			Markdown.Append("![");
			Markdown.Append(MarkdownDocument.Encode(Name));
			Markdown.Append("](");
			Markdown.Append(Link);

			if (Width > 0 && Height > 0)
			{
				Markdown.Append(' ');
				Markdown.Append(Width.ToString(CultureInfo.InvariantCulture));
				Markdown.Append(' ');
				Markdown.Append(Height.ToString(CultureInfo.InvariantCulture));
			}

			Markdown.Append(')');

			return Markdown.ToString();
		}

		/// <summary>
		/// Turns a photo upright, makes it at most <see cref="MaxImageSide"/> pixels on its longest side, and encodes it again
		/// as JPEG, which also removes its meta data, such as where it was taken. Images that can't be decoded, and
		/// animations, are sent as they are.
		/// </summary>
		private static (byte[] Data, string FileName, string ContentType, int Width, int Height) PrepareImage(byte[] Data, string FileName)
		{
			if (!InternetContent.TryGetContentType(Path.GetExtension(FileName), out string ContentType))
				ContentType = "application/octet-stream";

			try
			{
				using SKData Encoded = SKData.CreateCopy(Data);
				using SKCodec? Codec = SKCodec.Create(Encoded);

				if (Codec is null)
					return (Data, FileName, ContentType, 0, 0);

				if (Codec.FrameCount > 1)
				{
					(int W0, int H0) = OrientedSize(Codec.Info.Width, Codec.Info.Height, Codec.EncodedOrigin);
					return (Data, FileName, ContentType, W0, H0);
				}

				SKBitmap? Bitmap = SKBitmap.Decode(Codec);
				if (Bitmap is null)
					return (Data, FileName, ContentType, 0, 0);

				try
				{
					Bitmap = Orient(Bitmap, Codec.EncodedOrigin);

					int Width = Bitmap.Width;
					int Height = Bitmap.Height;
					int Longest = Math.Max(Width, Height);

					if (Longest > MaxImageSide)
					{
						double Scale = (double)MaxImageSide / Longest;
						SKImageInfo Info = new(Math.Max(1, (int)(Width * Scale + 0.5)), Math.Max(1, (int)(Height * Scale + 0.5)),
							Bitmap.ColorType, Bitmap.AlphaType, Bitmap.ColorSpace);

						SKBitmap? Resized = Bitmap.Resize(Info, new SKSamplingOptions(SKCubicResampler.Mitchell));
						if (Resized is not null)
						{
							Bitmap.Dispose();
							Bitmap = Resized;
						}
					}

					using SKData Jpeg = Bitmap.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
					if (Jpeg is null)
						return (Data, FileName, ContentType, Width, Height);

					return (Jpeg.ToArray(), Path.ChangeExtension(FileName, ".jpg"), "image/jpeg", Bitmap.Width, Bitmap.Height);
				}
				finally
				{
					Bitmap.Dispose();
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return (Data, FileName, ContentType, 0, 0);
			}
		}

		/// <summary>
		/// Turns a bitmap upright, as its camera orientation says. The original is disposed if a new bitmap is made.
		/// </summary>
		private static SKBitmap Orient(SKBitmap Bitmap, SKEncodedOrigin Origin)
		{
			if (Origin == SKEncodedOrigin.TopLeft || Origin == SKEncodedOrigin.Default)
				return Bitmap;

			bool Swap = SwapsSides(Origin);
			SKBitmap Result = new(Swap ? Bitmap.Height : Bitmap.Width, Swap ? Bitmap.Width : Bitmap.Height);

			using (SKCanvas Canvas = new(Result))
			{
				switch (Origin)
				{
					case SKEncodedOrigin.TopRight:
						Canvas.Translate(Result.Width, 0);
						Canvas.Scale(-1, 1);
						break;

					case SKEncodedOrigin.BottomRight:
						Canvas.Translate(Result.Width, Result.Height);
						Canvas.RotateDegrees(180);
						break;

					case SKEncodedOrigin.BottomLeft:
						Canvas.Translate(0, Result.Height);
						Canvas.Scale(1, -1);
						break;

					case SKEncodedOrigin.LeftTop:
						Canvas.RotateDegrees(90);
						Canvas.Scale(1, -1);
						break;

					case SKEncodedOrigin.RightTop:
						Canvas.Translate(Result.Width, 0);
						Canvas.RotateDegrees(90);
						break;

					case SKEncodedOrigin.RightBottom:
						Canvas.Translate(Result.Width, Result.Height);
						Canvas.RotateDegrees(90);
						Canvas.Scale(-1, 1);
						break;

					case SKEncodedOrigin.LeftBottom:
						Canvas.Translate(0, Result.Height);
						Canvas.RotateDegrees(270);
						break;
				}

				Canvas.DrawBitmap(Bitmap, 0, 0, SKSamplingOptions.Default);
			}

			Bitmap.Dispose();
			return Result;
		}

		private static bool SwapsSides(SKEncodedOrigin Origin)
		{
			return Origin == SKEncodedOrigin.LeftTop || Origin == SKEncodedOrigin.RightTop ||
				Origin == SKEncodedOrigin.RightBottom || Origin == SKEncodedOrigin.LeftBottom;
		}

		private static (int Width, int Height) OrientedSize(int Width, int Height, SKEncodedOrigin Origin)
		{
			return SwapsSides(Origin) ? (Height, Width) : (Width, Height);
		}

		/// <summary>
		/// Loads an encrypted image: from the device if it has been shown before, or else downloaded and decrypted.
		/// Runs off the UI thread.
		/// </summary>
		private static Task<MdImageData> LoadImageAsync(string Link)
		{
			lock (loading)
			{
				if (loading.TryGetValue(Link, out Task<MdImageData>? Running))
					return Running;

				Task<MdImageData> Loader = DoLoadImageAsync(Link);
				loading[Link] = Loader;

				Loader.ContinueWith(_ =>
				{
					lock (loading)
					{
						loading.Remove(Link);
					}
				}, TaskScheduler.Default);

				return Loader;
			}
		}

		private static async Task<MdImageData> DoLoadImageAsync(string Link)
		{
			if (!Uri.TryCreate(Link, UriKind.Absolute, out Uri? Parsed) ||
				!Aes256Getter.TryParse(Parsed, out byte[]? Key, out byte[]? IV, out string? ContentType, out Uri? EncryptedUri))
			{
				throw new NotSupportedException("The encrypted link is not valid.");
			}

			string FileName = GetCacheFileName(Link, ContentType);

			if (!File.Exists(FileName))
			{
				ContentResponse Response = await InternetContent.GetAsync(EncryptedUri, null, App.ValidateCertificateCallback,
					DownloadTimeoutMs, new KeyValuePair<string, string>("Accept", "application/octet-stream"));

				Response.AssertOk();

				byte[] Encrypted = Response.Encoded ?? Response.Decoded as byte[]
					?? throw new IOException("The encrypted image could not be downloaded.");

				byte[] Data;

				using (Aes Aes = Aes.Create())
				{
					Aes.Mode = CipherMode.CBC;
					Aes.Padding = PaddingMode.PKCS7;

					using ICryptoTransform Transform = Aes.CreateDecryptor(Key, IV);
					Data = Transform.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
				}

				await StoreAsync(FileName, Data);
			}

			(int Width, int Height) = ReadSize(FileName);
			return new MdImageData(FileName, null, Width > 0 ? Width : null, Height > 0 ? Height : null);
		}

		/// <summary>
		/// Reads the size of a stored image, as it is shown, upright.
		/// </summary>
		private static (int Width, int Height) ReadSize(string FileName)
		{
			using SKCodec? Codec = SKCodec.Create(FileName);
			if (Codec is null)
				throw new NotSupportedException("The image could not be decoded.");

			return OrientedSize(Codec.Info.Width, Codec.Info.Height, Codec.EncodedOrigin);
		}

		/// <summary>
		/// Decrypted images are kept in the app's cache folder, named by a hash of their link, since the link holds the key.
		/// </summary>
		private static string GetCacheFileName(string Link, string ContentType)
		{
			string Folder = Path.Combine(FileSystem.CacheDirectory, "ChatImages");
			Directory.CreateDirectory(Folder);

			string Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Link)));

			if (!InternetContent.TryGetFileExtension(ContentType, out string Extension) || string.IsNullOrEmpty(Extension))
				Extension = "bin";

			return Path.Combine(Folder, Hash + "." + Extension);
		}

		private static async Task StoreAsync(string FileName, byte[] Data)
		{
			string Temporary = FileName + ".tmp";
			await File.WriteAllBytesAsync(Temporary, Data);
			File.Move(Temporary, FileName, true);
		}
	}
}
