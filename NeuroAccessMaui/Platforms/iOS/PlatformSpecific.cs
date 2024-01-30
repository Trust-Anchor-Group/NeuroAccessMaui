using System.Diagnostics.CodeAnalysis;
using Foundation;
using LocalAuthentication;
using NeuroAccessMaui.Services.Push;
using ObjCRuntime;
using UIKit;
using Waher.Events;
using Waher.Networking.XMPP.Push;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// iOS implementation of platform-specific features.
	/// </summary>
	public class PlatformSpecific : IPlatformSpecific
	{
		private LAContext? localAuthenticationContext;
		private bool isDisposed;

		/// <summary>
		/// iOS implementation of platform-specific features.
		/// </summary>
		public PlatformSpecific()
		{
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			if (Disposing)
				this.DisposeLocalAuthenticationContext();

			this.isDisposed = true;
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
			if (this.localAuthenticationContext is not null)
			{
				if (this.localAuthenticationContext.RespondsToSelector(new Selector("invalidate")))
					this.localAuthenticationContext.Invalidate();

				this.localAuthenticationContext.Dispose();
				this.localAuthenticationContext = null;
			}

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
				if (!this.HasLocalAuthenticationContext)
					return false;

				try
				{
					if (!this.localAuthenticationContext.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _))
						return false;

					return true;
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					return false;
				}
			}
		}

		[MemberNotNullWhen(true, nameof(localAuthenticationContext))]
		private bool HasLocalAuthenticationContext
		{
			get
			{
				try
				{
					if (this.localAuthenticationContext is null)
					{
						NSProcessInfo ProcessInfo = new();
						NSOperatingSystemVersion MinVersion = new(10, 12, 0);
						if (!ProcessInfo.IsOperatingSystemAtLeastVersion(MinVersion))
							return false;

						if (!UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
							return false;

						if (Class.GetHandle(typeof(LAContext)) == IntPtr.Zero)
							return false;

						this.localAuthenticationContext = new LAContext();
					}

					return true;
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					return false;
				}
			}
		}

		private void DisposeLocalAuthenticationContext()
		{
			if (this.localAuthenticationContext is not null)
			{
				if (this.localAuthenticationContext.RespondsToSelector(new Selector("invalidate")))
					this.localAuthenticationContext.Invalidate();

				this.localAuthenticationContext.Dispose();
				this.localAuthenticationContext = null;
			}
		}

		/// <summary>
		/// Authenticates the user using the fingerprint sensor.
		/// </summary>
		/// <param name="Title">Title of authentication dialog.</param>
		/// <param name="Subtitle">Optional Subtitle.</param>
		/// <param name="Description">Description texst to display to user in authentication dialog.</param>
		/// <param name="Cancel">Label for Cancel button.</param>
		/// <param name="RequireConfirmation">If user confirmation is required.</param>
		/// <param name="CancellationToken">Optional cancellation token, to cancel process.</param>
		/// <returns>If the user has been successfully authenticated.</returns>
		public async Task<bool> AuthenticateUserFingerprint(string Title, string? Subtitle, string Description, string Cancel,
			CancellationToken? CancellationToken)
		{
			if (!this.HasLocalAuthenticationContext)
				return false;

			CancellationTokenRegistration? Registration = null;

			try
			{
				if (this.localAuthenticationContext.RespondsToSelector(new Selector("localizedFallbackTitle")))
					this.localAuthenticationContext.LocalizedFallbackTitle = Title;

				if (this.localAuthenticationContext.RespondsToSelector(new Selector("localizedCancelTitle")))
					this.localAuthenticationContext.LocalizedCancelTitle = Cancel;

				Registration = CancellationToken?.Register(this.DisposeLocalAuthenticationContext);

				(bool Success, NSError _) = await this.localAuthenticationContext.EvaluatePolicyAsync(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, Description);

				this.DisposeLocalAuthenticationContext();

				return Success;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return false;
			}
			finally
			{
				if (Registration.HasValue)
					Registration.Value.Dispose();
			}
		}

		/// <summary>
		/// Gets a Push Notification token for the device.
		/// </summary>
		/// <returns>Token, Service used, and type of client.</returns>
		public async Task<TokenInformation> GetToken()
		{
			string Token = string.Empty;

			try
			{
				Token = Messaging.SharedInstance.FcmToken ?? string.Empty;
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}

			TokenInformation TokenInformation = new()
			{
				Token = Token,
				ClientType = ClientType.iOS,
				Service = PushMessagingService.Firebase
			};

			return await Task.FromResult(TokenInformation);
		}

	}
}
