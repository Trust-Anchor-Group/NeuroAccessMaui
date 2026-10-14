using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Navigation arguments for the KYC camera page used by image fields that require the app camera.
	/// </summary>
	public sealed class KycProfilePhotoCameraNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Gets or sets the completion source that receives the captured JPEG bytes, or <c>null</c> when the user cancels.
		/// </summary>
		public TaskCompletionSource<byte[]?>? CompletionSource { get; init; }

		/// <summary>
		/// Gets or sets what the photo captures. It decides the camera, the framing guide, and the guidance text.
		/// Defaults to a photo of the user's face.
		/// </summary>
		public KycCaptureProfile CaptureProfile { get; init; } = KycCaptureProfile.Selfie;

		/// <summary>
		/// Gets or sets an optional title shown while the camera is active, such as the field label.
		/// </summary>
		public string? Title { get; init; }

		/// <summary>
		/// Gets or sets an optional hint from the field, shown under the guidance while the camera is active.
		/// </summary>
		public string? GuideHint { get; init; }
	}
}
