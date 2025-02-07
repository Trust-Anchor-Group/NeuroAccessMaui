using NeuroAccessMaui.Services.UI;
using Microsoft.Maui; // for Size

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
		/// to be cropped, an optional <paramref name="completionSource"/>, and optional properties for the cropper.
		/// </summary>
		/// <param name="source">The image to be cropped.</param>
		/// <param name="completionSource">A TaskCompletionSource that can be used to return the cropped image bytes.</param>
		/// <param name="outputResolution">Optional output resolution for the cropped image.</param>
		public ImageCroppingNavigationArgs(ImageSource? source, TaskCompletionSource<byte[]?>? completionSource = null, Size? outputResolution = null)
		{
			this.Source = source;
			this.CompletionSource = completionSource;
			this.OutputResolution = outputResolution;
		}

		/// <summary>
		/// The image to be cropped.
		/// </summary>
		public ImageSource? Source { get; }

		/// <summary>
		/// Optional completion source for returning the cropped image data to the caller.
		/// </summary>
		public TaskCompletionSource<byte[]?>? CompletionSource { get; }
		
		/// <summary>
		/// Optional output resolution for the cropped image.
		/// </summary>
		public Size? OutputResolution { get; }
	}
}
