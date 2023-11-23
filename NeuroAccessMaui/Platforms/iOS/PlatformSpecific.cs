using Foundation;
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
	public void ShareImage(byte[] PngFile, string Message, string Title, string FileName)
	{
		UIImage? ImageObject = UIImage.LoadFromData(NSData.FromArray(PngFile));
		UIWindow? KeyWindow = UIApplication.SharedApplication.KeyWindow;

		if ((ImageObject is null) || (KeyWindow is null))
		{
			return;
		}

		NSString MessageObject = new(Message);
		NSObject[] Items = [MessageObject, ImageObject];
		UIActivityViewController activityController = new(Items, null);

		UIViewController? topController = KeyWindow.RootViewController;

		if (topController is not null)
		{
			while (topController.PresentedViewController is not null)
			{
				topController = topController.PresentedViewController;
			}

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
