using System.Collections.Concurrent;
using Microsoft.Maui.Storage;
using SkiaSharp;
using Svg.Skia;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Parses bundled SVG assets off the UI thread and shares the parsed pictures between views.
	/// </summary>
	/// <remarks>
	/// Only assets bundled with the app package are cached. That set is small and fixed, so parsed pictures are kept for the
	/// lifetime of the process. Pictures are owned by the cache and must not be disposed by the views that draw them. A failed
	/// parse is not cached, so it is retried the next time the asset is requested.
	/// </remarks>
	internal static class SvgPictureCache
	{
		private static readonly ConcurrentDictionary<string, Lazy<Task<SKSvg>>> cache = new ConcurrentDictionary<string, Lazy<Task<SKSvg>>>(StringComparer.Ordinal);

		/// <summary>
		/// Gets the parsed picture of a bundled SVG asset, parsing it on a background thread on first use.
		/// </summary>
		/// <param name="Source">The SVG asset file name, for example "icon.svg".</param>
		/// <returns>The parsed picture, or <c>null</c> if the asset has no drawable content.</returns>
		public static async Task<SKPicture?> GetPictureAsync(string Source)
		{
			Lazy<Task<SKSvg>> Entry = cache.GetOrAdd(Source, Key => new Lazy<Task<SKSvg>>(() => Task.Run(() => LoadAsync(Key))));

			try
			{
				SKSvg Svg = await Entry.Value.ConfigureAwait(false);
				return Svg.Picture;
			}
			catch
			{
				cache.TryRemove(new KeyValuePair<string, Lazy<Task<SKSvg>>>(Source, Entry));
				throw;
			}
		}

		private static async Task<SKSvg> LoadAsync(string Source)
		{
			using Stream Stream = await FileSystem.OpenAppPackageFileAsync(Source).ConfigureAwait(false);
			SKSvg Svg = new SKSvg();
			try
			{
				Svg.Load(Stream);
				return Svg;
			}
			catch
			{
				Svg.Dispose();
				throw;
			}
		}
	}
}
