using UIKit;

namespace NeuroAccessMaui.Services;

public class PlatformSpecific : IPlatformSpecific
{
	public PlatformSpecific()
	{
	}

	/// <inheritdoc/>
	public bool CanProhibitScreenCapture => false;

	/// <inheritdoc/>
	public bool ProhibitScreenCapture // iOS doesn't support screen protection
	{
		get => false;
		set => _ = value; // ignore the value
	}

	/// <inheritdoc/>
	public string? GetDeviceId()
	{
		return UIDevice.CurrentDevice.IdentifierForVendor.ToString();
	}

	/// <inheritdoc/>
	public Task CloseApplication()
	{
		Environment.Exit(0);
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public class ShareContent : IShareContent
	{
		/// <summary>
		/// Shares an image in PNG format.
		/// </summary>
		/// <param name="PngFile">Binary representation (PNG format) of image.</param>
		/// <param name="Message">Message to send with image.</param>
		/// <param name="Title">Title for operation.</param>
		/// <param name="FileName">Filename of image file.</param>
		public void ShareImage(byte[] PngFile, string Message, string Title, string FileName)
		{
			UIImage ImageObject = UIImage.LoadFromData(NSData.FromArray(PngFile));
			NSString MessageObject = new NSString(Message);
			//ImageSource Image = ImageSource.FromStream(() => new MemoryStream(PngFile));
			//NSObject ImageObject = NSObject.FromObject(Image);
			//NSObject MessageObject = NSObject.FromObject(Message);
			NSObject[] Items = new NSObject[] { MessageObject, ImageObject };
			UIActivityViewController activityController = new(Items, null);
			UIViewController topController = UIApplication.SharedApplication.KeyWindow.RootViewController;

			while (topController.PresentedViewController is not null)
				topController = topController.PresentedViewController;

			topController.PresentViewController(activityController, true, () => { });
		}
	}

	public Task<byte[]> CaptureScreen(int blurRadius = 25)
	{
		blurRadius = Math.Min(25, Math.Max(blurRadius, 0));
		UIImage capture;

		using (var blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Regular))
		{
			using (var blurWindow = new UIVisualEffectView(blurEffect))
			{
				blurWindow.Frame = UIScreen.MainScreen.Bounds;
				blurWindow.Alpha = Math.Min(1.0f, (1.0f / 25.0f) * blurRadius);

				var subview = UIScreen.MainScreen.SnapshotView(true);
				//capture = UIScreen.MainScreen.Capture();
				//var subview = new UIImageView(capture);
				subview.AddSubview(blurWindow);
				capture = subview.Capture(true);
				blurWindow.RemoveFromSuperview();
				subview.Dispose();

				//!!! capture.AsJPEG(.8f).AsStream();
				return Task.FromResult(new byte[0]);
			}
		}
	}
}
