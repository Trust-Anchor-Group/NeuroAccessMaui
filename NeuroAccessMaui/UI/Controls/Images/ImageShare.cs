using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Shares images, such as charts drawn by the server, with other apps.
	/// </summary>
	public static class ImageShare
	{
		/// <summary>
		/// Gets the file an image is read from, so it can be shared.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <returns>The file, or null when the image isn't a file on the phone.</returns>
		public static string? GetFile(ImageSource? Source)
		{
			return Source is FileImageSource FileSource && File.Exists(FileSource.File) ? FileSource.File : null;
		}

		/// <summary>
		/// Shares an image with another app.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <param name="Title">What the image shows, such as the report's title.</param>
		public static async Task ShareAsync(ImageSource? Source, string Title)
		{
			string? FileName = GetFile(Source);
			if (FileName is null)
				return;

			try
			{
				byte[] Data = await File.ReadAllBytesAsync(FileName);
				string ShareName = Path.ChangeExtension(Path.GetFileName(FileName), ".png");

				ServiceRef.PlatformSpecific.ShareImage(Data, Title, Title, ShareName);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}
	}
}
