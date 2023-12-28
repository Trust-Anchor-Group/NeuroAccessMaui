namespace NeuroAccessMaui.Services
{
	public interface IPlatformSpecific
	{
		/// <summary>
		/// If screen capture prohibition is supported
		/// </summary>
		bool CanProhibitScreenCapture { get; }

		/// <summary>
		/// If screen capture is prohibited or not.
		/// </summary>
		bool ProhibitScreenCapture { get; set; }

		/// <summary>
		/// Gets the ID of the device
		/// </summary>
		string? GetDeviceId();

		/// <summary>
		/// Closes the application
		/// </summary>
		Task CloseApplication();

		/// <summary>
		/// Shares an image in PNG format.
		/// </summary>
		/// <param name="PngFile">Binary representation (PNG format) of image.</param>
		/// <param name="Message">Message to send with image.</param>
		/// <param name="Title">Title for operation.</param>
		/// <param name="FileName">Filename of image file.</param>
		void ShareImage(byte[] PngFile, string Message, string Title, string FileName);

		/// <summary>
		/// Force hide the keyboard
		/// </summary>
		void HideKeyboard();

		/// <summary>
		/// Make a blurred screenshot
		/// TODO: Just make a screen shot. Use the portable CV library to blur it.
		/// </summary>
		Task<byte[]> CaptureScreen(int blurRadius);

		/// <summary>
		/// If the device supports authenticating the user using fingerprints.
		/// </summary>
		bool SupportsFingerprintAuthentication { get; }
	}
}
