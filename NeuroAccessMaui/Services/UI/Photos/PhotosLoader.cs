using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.UI.Pages;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Waher.Content;
using Waher.Content.Images;
using Waher.Content.Images.Exif;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using NeuroAccessMaui.Services.Cache.AttachmentCache;

namespace NeuroAccessMaui.Services.UI.Photos
{
	/// <summary>
	/// This is a helper class for downloading photos via http requests.
	/// It loads photos in the background, typically photo attachments connected to a
	/// digital identity. When the photos are loaded, they are added to an <see cref="ObservableCollection{T}"/> on the main thread.
	/// This class also handles errors when trying to load photos, and internally it uses a <see cref="IAttachmentCacheService"/>.
	/// </summary>
	public class PhotosLoader(ObservableCollection<Photo> Photos) : BaseViewModel
	{
		// Explicit contract attachment opens materialize bytes for cache/preview.
		// Keep that path bounded so remote metadata cannot exhaust mobile memory.
		private const long maximumInMemoryAttachmentBytes = 25L * 1024L * 1024L;
		private readonly ObservableCollection<Photo> photos = Photos;
		private readonly List<string> attachmentIds = [];
		private DateTime loadPhotosTimestamp;

		/// <summary>
		/// Creates a new instance of the <see cref="PhotosLoader"/> class.
		/// Use this constructor for when you want to load a a <b>single photo</b>.
		/// </summary>
		public PhotosLoader() : this([])
		{
		}

		/// <summary>
		/// Loads photos from the specified list of attachments.
		/// </summary>
		/// <param name="Attachments">The attachments whose files to download.</param>
		/// <param name="SignWith">How the requests are signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
		/// <param name="WhenDoneAction">A callback that is called when the photo load operation is done.</param>
		/// <returns>Returns the first photo, if available, null if no photos available.</returns>
		public Task<Photo?> LoadPhotos(Attachment[] Attachments, SignWith SignWith, Action? WhenDoneAction = null)
		{
			return this.LoadPhotos(Attachments, SignWith, DateTime.UtcNow, WhenDoneAction);
		}

		/// <summary>
		/// Cancels any ongoing download of photos in progress. Also
		/// clears the collection of photos from its content.
		/// </summary>
		public void CancelLoadPhotos()
		{
			try
			{
				this.loadPhotosTimestamp = DateTime.UtcNow;
				this.attachmentIds.Clear();
				this.photos.Clear();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Downloads one photo and stores in cache.
		/// </summary>
		/// <param name="Attachment">The attachment to download.</param>
		/// <param name="SignWith">How the requests are signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
		/// <returns>Binary content (or null), Content-Type, Rotation</returns>
		public async Task<(byte[]?, string, int)> LoadOnePhoto(Attachment Attachment, SignWith SignWith)
		{
			try
			{
				return await this.GetPhoto(Attachment, SignWith, DateTime.UtcNow);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return (null, string.Empty, 0);
		}

		/// <summary>
		/// Downloads one image or non-image attachment through the authenticated attachment service.
		/// </summary>
		/// <param name="Attachment">Attachment metadata containing the authenticated content URL.</param>
		/// <param name="SignWith">Identity material used to authorize the request.</param>
		/// <returns>Downloaded bytes and their verified content type, or an empty result when unavailable.</returns>
		public async Task<(byte[]? Data, string ContentType)> LoadOneAttachment(
			Attachment Attachment,
			SignWith SignWith)
		{
			try
			{
				return await this.GetAttachment(
					Attachment,
					SignWith,
					DateTime.UtcNow,
					maximumInMemoryAttachmentBytes);
			}
			catch (Exception Ex)
			{
				// Attachment URLs and filenames may be sensitive. Record only the
				// failure category and let the caller present a generic retry state.
				ServiceRef.LogService.LogWarning(
					"Attachment content could not be loaded.",
					new KeyValuePair<string, object?>(
						"FailureType",
						Ex.GetType().Name));
				return (null, string.Empty);
			}
		}

		private async Task<Photo?> LoadPhotos(Attachment[] Attachments, SignWith SignWith, DateTime Now, Action? WhenDoneAction)
		{
			if (Attachments is null || Attachments.Length <= 0)
			{
				WhenDoneAction?.Invoke();
				return null;
			}

			List<Attachment> AttachmentsList = Attachments.GetImageAttachments().ToList();
			List<string> NewAttachmentIds = AttachmentsList.Select(x => x.Id).ToList();

			if (this.attachmentIds.HasSameContentAs(NewAttachmentIds))
			{
				WhenDoneAction?.Invoke();

				foreach (Photo Photo in this.photos)
					return Photo;

				return null;
			}

			this.attachmentIds.Clear();
			this.attachmentIds.AddRange(NewAttachmentIds);

			Photo? First = null;

			foreach (Attachment Attachment in AttachmentsList)
			{
				if (!IsSupportedImageContentType(Attachment.ContentType))
					continue;

				if (this.loadPhotosTimestamp > Now)
				{
					WhenDoneAction?.Invoke();

					foreach (Photo Photo in this.photos)
						return Photo;

					return null;
				}

				try
				{
					(byte[]? Bin, string ContentType, int Rotation) = await this.GetPhoto(Attachment, SignWith, Now);

					if (Bin is null)
						continue;

					Photo Photo = new(Bin, Rotation, Attachment);
					First ??= Photo;

					if (Bin is not null)
					{
						TaskCompletionSource<bool> PhotoAddedTaskSource = new();

						MainThread.BeginInvokeOnMainThread(() =>
						{
							this.photos.Add(Photo);
							PhotoAddedTaskSource.TrySetResult(true);
						});

						await PhotoAddedTaskSource.Task;
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			}

			WhenDoneAction?.Invoke();

			return First;
		}

		private async Task<(byte[]?, string, int)> GetPhoto(Attachment Attachment, SignWith SignWith, DateTime Now)
		{
			(byte[]? Bin, string ContentType) = await this.GetAttachment(
				Attachment,
				SignWith,
				Now,
				int.MaxValue);
			return Bin is null
				? (null, string.Empty, 0)
				: (Bin, ContentType, GetImageRotation(Bin));
		}

		private async Task<(byte[]? Data, string ContentType)> GetAttachment(
			Attachment Attachment,
			SignWith SignWith,
			DateTime Now,
			long MaximumBytes)
		{
			if (Attachment is null || string.IsNullOrWhiteSpace(Attachment.Url))
				return (null, string.Empty);

			byte[]? Bin = null;
			string ContentType = string.Empty;
			try
			{
				(Bin, ContentType) =
					await ServiceRef.AttachmentCacheService.TryGet(Attachment.Url);
			}
			catch (Exception Ex)
			{
				LogAttachmentCacheFailure("Read", Ex);
			}

			if (Bin is not null)
			{
				return Bin.LongLength > 0 &&
					Bin.LongLength <= MaximumBytes
					? (Bin, NormalizeContentType(ContentType, Attachment.ContentType))
					: (null, string.Empty);
			}

			if (!ServiceRef.NetworkService.IsOnline ||
				!ServiceRef.XmppService.IsOnline)
			{
				return (null, string.Empty);
			}

			KeyValuePair<string, TemporaryFile> Download =
				await ServiceRef.XmppService.GetAttachment(
					Attachment.Url,
					SignWith,
					Constants.Timeouts.DownloadFile);
			using TemporaryFile File = Download.Value;

			if (this.loadPhotosTimestamp > Now ||
				File.Length <= 0 ||
				File.Length > MaximumBytes)
			{
				return (null, string.Empty);
			}

			File.Reset();
			Bin = new byte[File.Length];
			int Offset = 0;

			// Stream reads are not guaranteed to fill the requested buffer. Read
			// until complete so a truncated attachment is never cached or opened.
			while (Offset < Bin.Length)
			{
				int Read = File.Read(Bin, Offset, Bin.Length - Offset);
				if (Read <= 0)
					return (null, string.Empty);

				Offset += Read;
			}

			ContentType = NormalizeContentType(
				Download.Key,
				Attachment.ContentType);
			string ParentId = FirstNonEmpty(
				Attachment.LegalId,
				Attachment.Id,
				Attachment.Url);
			bool Permanent = false;
			if (!string.IsNullOrWhiteSpace(Attachment.LegalId))
			{
				try
				{
					Permanent =
						await ServiceRef.XmppService.IsContact(Attachment.LegalId);
				}
				catch (Exception)
				{
					// Contact status only controls cache lifetime and must not make
					// successfully retrieved attachment content unavailable.
				}
			}

			try
			{
				await ServiceRef.AttachmentCacheService.Add(
					Attachment.Url,
					ParentId,
					Permanent,
					Bin,
					ContentType);
			}
			catch (Exception Ex)
			{
				// A cache write is an optimization. The downloaded bytes remain
				// usable for the explicit action that requested them.
				LogAttachmentCacheFailure("Write", Ex);
			}

			return (Bin, ContentType);
		}

		private static void LogAttachmentCacheFailure(
			string Operation,
			Exception Ex)
		{
			ServiceRef.LogService.LogWarning(
				"Attachment cache operation failed.",
				new KeyValuePair<string, object?>("Operation", Operation),
				new KeyValuePair<string, object?>(
					"FailureType",
					Ex.GetType().Name));
		}

		private static string NormalizeContentType(
			string? DownloadedContentType,
			string? DeclaredContentType)
		{
			string ContentType = FirstNonEmpty(
				DownloadedContentType,
				DeclaredContentType);
			return string.IsNullOrWhiteSpace(ContentType)
				? "application/octet-stream"
				: ContentType;
		}

		private static string FirstNonEmpty(params string?[] Values)
		{
			foreach (string? Value in Values)
			{
				if (!string.IsNullOrWhiteSpace(Value))
					return Value.Trim();
			}

			return string.Empty;
		}

		/// <summary>
		/// Determines whether a content type is supported by the current image preview pipeline.
		/// </summary>
		/// <param name="ContentType">MIME content type to inspect.</param>
		/// <returns><c>true</c> when the image codecs can preview the content type.</returns>
		public static bool IsSupportedImageContentType(string? ContentType)
		{
			if (string.IsNullOrWhiteSpace(ContentType))
				return false;

			string MediaType = ContentType.Split(';', 2)[0].Trim();
			return ImageCodec.ImageContentTypes.Any(
				Supported => string.Equals(
					Supported,
					MediaType,
					StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Gets the rotation angle to use, to display the image correctly in Xamarin Forms.
		/// </summary>
		/// <param name="JpegImage">Binary representation of JPEG image.</param>
		/// <returns>Rotation angle (degrees).</returns>
		public static int GetImageRotation(byte[] JpegImage)
		{
			//!!! This rotation in Xamarin is limited to Android
			if (DeviceInfo.Platform == DevicePlatform.iOS)
				return 0;

			if (JpegImage is null)
				return 0;

			if (!EXIF.TryExtractFromJPeg(JpegImage, out ExifTag[] Tags))
				return 0;

			return GetImageRotation(Tags);
		}

		/// <summary>
		/// Gets the rotation angle to use, to display the image correctly in Xamarin Forms.
		/// </summary>
		/// <param name="Tags">EXIF Tags encoded in image.</param>
		/// <returns>Rotation angle (degrees).</returns>
		public static int GetImageRotation(ExifTag[] Tags)
		{
			foreach (ExifTag Tag in Tags)
			{
				if (Tag.Name == ExifTagName.Orientation)
				{
					if (Tag.Value is ushort Orientation)
					{
						return Orientation switch
						{
							1 => 0,// Top left. Default orientation.
							2 => 0,// Top right. Horizontally reversed.
							3 => 180,// Bottom right. Rotated by 180 degrees.
							4 => 180,// Bottom left. Rotated by 180 degrees and then horizontally reversed.
							5 => -90,// Left top. Rotated by 90 degrees counterclockwise and then horizontally reversed.
							6 => 90,// Right top. Rotated by 90 degrees clockwise.
							7 => 90,// Right bottom. Rotated by 90 degrees clockwise and then horizontally reversed.
							8 => -90,// Left bottom. Rotated by 90 degrees counterclockwise.
							_ => 0,
						};
					}
				}
			}

			return 0;
		}

		/// <summary>
		/// Loads a photo attachment.
		/// </summary>
		/// <param name="Attachment">Attachment containing photo.</param>
		/// <returns>Photo, Content-Type, Rotation</returns>
		public static async Task<(byte[]?, string, int)> LoadPhoto(Attachment Attachment)
		{
			PhotosLoader Loader = new();

			(byte[]?, string, int) Image = await Loader.LoadOnePhoto(Attachment, SignWith.LatestApprovedIdOrCurrentKeys);

			return Image;
		}

		/// <summary>
		/// Tries to load a photo from a set of attachments.
		/// </summary>
		/// <param name="Attachments">Attachments</param>
		/// <param name="MaxWith">Maximum width when displaying photo.</param>
		/// <param name="MaxHeight">Maximum height when displaying photo.</param>
		/// <returns>Filename, Width, Height, if loaded, (null,0,0) if not.</returns>
		public static Task<(string?, int, int)> LoadPhotoAsTemporaryFile(Attachment[] Attachments, int MaxWith, int MaxHeight)
		{
			Attachment? Photo = null;

			foreach (Attachment Attachment in Attachments.GetImageAttachments())
			{
				if (Attachment.ContentType == Constants.MimeTypes.Png)
				{
					Photo = Attachment;
					break;
				}
				else
					Photo ??= Attachment;
			}

			if (Photo is null)
				return Task.FromResult<(string?, int, int)>((null, 0, 0));
			else
				return LoadPhotoAsTemporaryFile(Photo, MaxWith, MaxHeight);
		}

		/// <summary>
		/// Tries to load a photo from an attachments.
		/// </summary>
		/// <param name="Attachment">Attachment</param>
		/// <param name="MaxWith">Maximum width when displaying photo.</param>
		/// <param name="MaxHeight">Maximum height when displaying photo.</param>
		/// <returns>Filename, Width, Height, if loaded, (null,0,0) if not.</returns>
		public static async Task<(string?, int, int)> LoadPhotoAsTemporaryFile(Attachment Attachment, int MaxWith, int MaxHeight)
		{
			(byte[]? Data, string _, int _) = await LoadPhoto(Attachment);

			if (Data is not null)
			{
				string FileName = await GetTemporaryFile(Data);
				int Width;
				int Height;

				using (SKBitmap Bitmap = SKBitmap.Decode(Data))
				{
					Width = Bitmap.Width;
					Height = Bitmap.Height;
				}

				double ScaleWidth = ((double)MaxWith) / Width;
				double ScaleHeight = ((double)MaxHeight) / Height;
				double Scale = Math.Min(ScaleWidth, ScaleHeight);

				if (Scale < 1)
				{
					Width = (int)(Width * Scale + 0.5);
					Height = (int)(Height * Scale + 0.5);
				}

				return (FileName, Width, Height);
			}
			else
				return (null, 0, 0);
		}

		#region From Waher.Content.Markdown.Model.Multimedia.ImageContent, with permission

		/// <summary>
		/// Stores an image in binary form as a temporary file. Files will be deleted when application closes.
		/// </summary>
		/// <param name="BinaryImage">Binary image.</param>
		/// <returns>Temporary file name.</returns>
		public static Task<string> GetTemporaryFile(byte[] BinaryImage)
		{
			return GetTemporaryFile(BinaryImage, "tmp");
		}

		/// <summary>
		/// Stores an image in binary form as a temporary file. Files will be deleted when application closes.
		/// </summary>
		/// <param name="BinaryImage">Binary image.</param>
		/// <param name="FileExtension">File extension.</param>
		/// <returns>Temporary file name.</returns>
		public static async Task<string> GetTemporaryFile(byte[] BinaryImage, string FileExtension)
		{
			byte[] Digest = SHA256.HashData(BinaryImage);
			string FileName = Path.Combine(Path.GetTempPath(), "tmp" + Base64Url.Encode(Digest) + "." + FileExtension);

			if (!File.Exists(FileName))
			{
				await Waher.Runtime.IO.Files.WriteAllBytesAsync(FileName, BinaryImage);

				lock (synchObject)
				{
					if (temporaryFiles is null)
					{
						temporaryFiles = [];
						Log.Terminating += CurrentDomain_ProcessExit;
					}

					temporaryFiles[FileName] = true;
				}
			}

			return FileName;
		}

		private static Dictionary<string, bool>? temporaryFiles = null;
		private static readonly object synchObject = new();

		private static Task CurrentDomain_ProcessExit(object? sender, EventArgs e)
		{
			lock (synchObject)
			{
				if (temporaryFiles is not null)
				{
					foreach (string FileName in temporaryFiles.Keys)
					{
						try
						{
							File.Delete(FileName);
						}
						catch (Exception)
						{
							// Ignore
						}
					}

					temporaryFiles.Clear();
				}
			}

			return Task.CompletedTask;
		}

		#endregion
	}
}
