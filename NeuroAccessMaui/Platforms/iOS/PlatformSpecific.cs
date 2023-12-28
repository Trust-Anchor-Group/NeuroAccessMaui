using Foundation;
using UIKit;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// iOS implementation of platform-specific features.
	/// </summary>
	public class PlatformSpecific : IPlatformSpecific
	{
		/// <summary>
		/// iOS implementation of platform-specific features.
		/// </summary>
		public PlatformSpecific()
		{
		}

		/// <summary>
		/// If screen capture prohibition is supported
		/// </summary>
		public bool CanProhibitScreenCapture => false;

		/// <summary>
		/// If screen capture is prohibited or not.
		/// </summary>
		public bool ProhibitScreenCapture // iOS doesn't support screen protection
		{
			get => false;
			set => _ = value; // ignore the value
		}

		/// <summary>
		/// Gets the ID of the device
		/// </summary>
		public string? GetDeviceId()
		{
			return UIDevice.CurrentDevice.IdentifierForVendor.ToString();
		}

		/// <summary>
		/// Closes the application
		/// </summary>
		public Task CloseApplication()
		{
			Environment.Exit(0);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Shares an image in PNG format.
		/// </summary>
		/// <param name="PngFile">Binary representation (PNG format) of image.</param>
		/// <param name="Message">Message to send with image.</param>
		/// <param name="Title">Title for operation.</param>
		/// <param name="FileName">Filename of image file.</param>
		public void ShareImage(byte[] PngFile, string Message, string Title, string FileName)
		{
			UIImage? ImageObject = UIImage.LoadFromData(NSData.FromArray(PngFile));
			UIWindow? KeyWindow = UIApplication.SharedApplication?.KeyWindow;

			if ((ImageObject is null) || (KeyWindow is null))
				return;

			NSString MessageObject = new(Message);
			NSObject[] Items = [MessageObject, ImageObject];
			UIActivityViewController activityController = new(Items, null);

			UIViewController? topController = KeyWindow.RootViewController;

			if (topController is not null)
			{
				while (topController.PresentedViewController is not null)
					topController = topController.PresentedViewController;

				topController.PresentViewController(activityController, true, () => { });
			}
		}

		/// <summary>
		/// Force hide the keyboard
		/// </summary>
		public void HideKeyboard()
		{
		}

		/// <summary>
		/// Make a blurred screenshot
		/// TODO: Just make a screen shot. Use the portable CV library to blur it.
		/// </summary>
		public Task<byte[]> CaptureScreen(int blurRadius = 25)
		{
			blurRadius = Math.Min(25, Math.Max(blurRadius, 0));
			UIImage? capture;

			using UIBlurEffect blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Regular);
			using UIVisualEffectView blurWindow = new(blurEffect);

			blurWindow.Frame = UIScreen.MainScreen.Bounds;
			blurWindow.Alpha = Math.Min(1.0f, (1.0f / 25.0f) * blurRadius);

			UIView? subview = UIScreen.MainScreen.SnapshotView(true);
			//capture = UIScreen.MainScreen.Capture();
			//var subview = new UIImageView(capture);
			subview?.AddSubview(blurWindow);
			capture = subview?.Capture(true);
			blurWindow.RemoveFromSuperview();
			subview?.Dispose();

			//!!! capture.AsJPEG(.8f).AsStream();
			return Task.FromResult(Array.Empty<byte>());
		}

		/// <summary>
		/// If the device supports authenticating the user using fingerprints.
		/// </summary>
		public bool SupportsFingerprintAuthentication
		{
			get
			{
				return false;	// TODO
			}
		}

	}
}
