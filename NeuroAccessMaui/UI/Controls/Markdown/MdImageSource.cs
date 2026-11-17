using SkiaSharp;
using Waher.Content.Markdown.Model.Multimedia;

namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// A loaded image: a local file or a web address, with its size in pixels when known.
	/// </summary>
	/// <param name="FilePath">Local file, if the image is stored locally.</param>
	/// <param name="Uri">Web address, if the image is downloaded when shown.</param>
	/// <param name="Width">Width in pixels, if known.</param>
	/// <param name="Height">Height in pixels, if known.</param>
	public sealed record MdImageData(string? FilePath, Uri? Uri, int? Width, int? Height)
	{
		/// <summary>
		/// Creates the MAUI image source. Call on the UI thread.
		/// </summary>
		/// <returns>Image source.</returns>
		public ImageSource ToImageSource()
		{
			if (this.FilePath is not null)
				return ImageSource.FromFile(this.FilePath);

			if (this.Uri is not null)
				return ImageSource.FromUri(this.Uri);

			throw new InvalidOperationException("The image has no source.");
		}
	}

	/// <summary>
	/// Where an image comes from. The image is loaded, or a chart drawn, the first time it is asked for, off the UI
	/// thread, and kept after that.
	/// </summary>
	public sealed class MdImageSource
	{
		private static readonly Dictionary<string, Func<string, Task<MdImageData>>> schemeLoaders = new(StringComparer.OrdinalIgnoreCase);

		private readonly Func<Task<MdImageData>> loader;
		private readonly object synch = new();
		private Task<MdImageData>? task;

		/// <summary>
		/// Creates an image source that runs <paramref name="Loader"/> the first time the image is asked for.
		/// </summary>
		/// <param name="Loader">Loads the image, or draws the chart.</param>
		/// <param name="Width">Width in pixels, if known before loading.</param>
		/// <param name="Height">Height in pixels, if known before loading.</param>
		public MdImageSource(Func<Task<MdImageData>> Loader, int? Width, int? Height)
		{
			this.loader = Loader;
			this.Width = Width;
			this.Height = Height;
		}

		/// <summary>
		/// Width in pixels, if known before loading. Keeps the placeholder the size of the image.
		/// </summary>
		public int? Width { get; }

		/// <summary>
		/// Height in pixels, if known before loading.
		/// </summary>
		public int? Height { get; }

		/// <summary>
		/// If the image has been loaded.
		/// </summary>
		public bool IsLoaded => this.task?.IsCompletedSuccessfully ?? false;

		/// <summary>
		/// Loads the image, off the UI thread, the first time it is called. Later calls return the same image. If loading
		/// fails, the next call tries again.
		/// </summary>
		/// <returns>The loaded image.</returns>
		public Task<MdImageData> LoadAsync()
		{
			lock (this.synch)
			{
				if (this.task is null || this.task.IsFaulted || this.task.IsCanceled)
					this.task = Task.Run(this.loader);

				return this.task;
			}
		}

		/// <summary>
		/// Lets the app load images of a URI scheme of its own, such as encrypted chat attachments. Such links are then
		/// shown as images.
		/// </summary>
		/// <param name="Scheme">URI scheme, without the colon.</param>
		/// <param name="Loader">Loads the image from a link. Runs off the UI thread.</param>
		public static void RegisterScheme(string Scheme, Func<string, Task<MdImageData>> Loader)
		{
			lock (schemeLoaders)
			{
				schemeLoaders[Scheme] = Loader;
			}
		}

		/// <summary>
		/// If images of the scheme of <paramref name="Url"/> are loaded by a loader registered with
		/// <see cref="RegisterScheme"/>.
		/// </summary>
		/// <param name="Url">Link.</param>
		/// <returns>If a loader handles the link.</returns>
		public static bool HasSchemeLoader(string Url)
		{
			return TryGetSchemeLoader(Url, out _);
		}

		private static bool TryGetSchemeLoader(string Url, out Func<string, Task<MdImageData>>? Loader)
		{
			int i = Url.IndexOf(':');
			if (i <= 0)
			{
				Loader = null;
				return false;
			}

			lock (schemeLoaders)
			{
				return schemeLoaders.TryGetValue(Url[..i], out Loader);
			}
		}

		/// <summary>
		/// Creates an image source for a link: a data URI, a local file, a web address, or a link of a registered scheme.
		/// </summary>
		/// <param name="Url">Link.</param>
		/// <param name="Width">Width in pixels, if given in the markdown.</param>
		/// <param name="Height">Height in pixels, if given in the markdown.</param>
		/// <returns>Image source.</returns>
		public static MdImageSource FromUrl(string Url, int? Width, int? Height)
		{
			return new MdImageSource(() => LoadUrlAsync(Url, Width, Height), Width, Height);
		}

		/// <summary>
		/// Creates an image source for PNG data that is made when the image is first shown, such as a chart.
		/// </summary>
		/// <param name="Draw">Makes the PNG image, and returns its size.</param>
		/// <returns>Image source.</returns>
		public static MdImageSource FromPng(Func<Task<(byte[] Png, int Width, int Height)>> Draw)
		{
			return new MdImageSource(async () =>
			{
				(byte[] Png, int Width, int Height) = await Draw();
				string FileName = await ImageContent.GetTemporaryFile(Png, "png");
				return new MdImageData(FileName, null, Width, Height);
			}, null, null);
		}

		private static async Task<MdImageData> LoadUrlAsync(string Url, int? Width, int? Height)
		{
			if (Url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				int i = Url.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
				if (i < 0)
					throw new NotSupportedException("Only base64 data URIs are supported.");

				byte[] Data = Convert.FromBase64String(Url[(i + 7)..]);
				return await FromBytesAsync(Data);
			}

			if (TryGetSchemeLoader(Url, out Func<string, Task<MdImageData>>? Loader) && Loader is not null)
				return await Loader(Url);

			if (Uri.TryCreate(Url, UriKind.Absolute, out Uri? Parsed))
			{
				if (Parsed.IsFile)
					return new MdImageData(Parsed.LocalPath, null, Width, Height);

				if (Parsed.Scheme == Uri.UriSchemeHttps || Parsed.Scheme == Uri.UriSchemeHttp)
					return new MdImageData(null, Parsed, Width, Height);
			}
			else if (File.Exists(Url))
				return new MdImageData(Url, null, Width, Height);

			throw new NotSupportedException("Images from this link can't be shown: " + Url);
		}

		/// <summary>
		/// Stores image data as a temporary file, and reads its size.
		/// </summary>
		/// <param name="Data">Encoded image, such as PNG or JPEG.</param>
		/// <returns>The stored image.</returns>
		public static async Task<MdImageData> FromBytesAsync(byte[] Data)
		{
			int? Width = null;
			int? Height = null;

			using (SKBitmap? Bitmap = SKBitmap.Decode(Data))
			{
				if (Bitmap is null)
					throw new NotSupportedException("The image could not be decoded.");

				Width = Bitmap.Width;
				Height = Bitmap.Height;
			}

			string FileName = await ImageContent.GetTemporaryFile(Data);
			return new MdImageData(FileName, null, Width, Height);
		}
	}
}
