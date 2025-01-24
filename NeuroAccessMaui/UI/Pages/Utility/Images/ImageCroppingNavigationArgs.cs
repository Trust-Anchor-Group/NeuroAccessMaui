using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Utility.Images
{
	/// <summary>
	/// Holds navigation parameters for opening the Image Cropping page.
	/// </summary>
	public class ImageCroppingNavigationArgs : NavigationArgs
	{
		public ImageCroppingNavigationArgs() { }

		/// <summary>
		/// Creates a new set of arguments, providing the <paramref name="source"/>
		/// to be cropped and an optional <paramref name="completionSource"/> for the result.
		/// </summary>
		/// <param name="source">The image to be cropped.</param>
		/// <param name="completionSource">A TaskCompletionSource that can be used to return the cropped image bytes.</param>
		public ImageCroppingNavigationArgs(ImageSource? source, TaskCompletionSource<byte[]?>? completionSource = null)
		{
			this.Source = source;
			this.CompletionSource = completionSource;
		}

		/// <summary>
		/// The image to be cropped.
		/// </summary>
		public ImageSource? Source { get; }

		/// <summary>
		/// Optional completion source for returning the cropped image data to the caller.
		/// </summary>
		public TaskCompletionSource<byte[]?>? CompletionSource { get; }
	}
}
