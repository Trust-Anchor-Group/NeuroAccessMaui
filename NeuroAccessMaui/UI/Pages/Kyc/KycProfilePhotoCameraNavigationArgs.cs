using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Navigation arguments for the KYC profile-photo camera page.
	/// </summary>
	public sealed class KycProfilePhotoCameraNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Gets or sets the completion source that receives the captured JPEG bytes.
		/// </summary>
		public TaskCompletionSource<byte[]?>? CompletionSource { get; init; }
	}
}
