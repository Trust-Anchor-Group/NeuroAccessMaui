using Foundation;
using LocalAuthentication;
using NeuroAccessMaui.Services.Push;
using ObjCRuntime;
using System.Diagnostics.CodeAnalysis;
using UIKit;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Security;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using Plugin.Firebase.CloudMessaging;
using UserNotifications;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// iOS implementation of platform-specific features.
	/// </summary>
	public class PlatformSpecific : IPlatformSpecific
	{
		private LAContext? localAuthenticationContext;
		private bool isDisposed;
		private readonly SemaphoreSlim deviceIdSemaphore = new SemaphoreSlim(1, 1);
		private string? deviceId;

		/// <summary>
		/// iOS implementation of platform-specific features.
		/// </summary>
		public PlatformSpecific()
		{
			NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, this.OnKeyboardWillShow);
			NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, this.OnKeyboardWillHide);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
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

			NSNotificationCenter.DefaultCenter.RemoveObserver(UIKeyboard.WillShowNotification);
			NSNotificationCenter.DefaultCenter.RemoveObserver(UIKeyboard.WillHideNotification);

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

		/// <inheritdoc/>
		public string? GetDeviceId() => this.deviceId
			?? throw new InvalidOperationException("The device identifier has not been initialized.");

		/// <inheritdoc/>
		public async Task InitializeDeviceIdAsync()
		{
			await this.deviceIdSemaphore.WaitAsync();
			try
			{
				if (this.deviceId is not null)
					return;
				using SecRecord SearchRecord = new SecRecord(SecKind.GenericPassword)
				{
					Service = AppInfo.PackageName,
					Account = "DeviceIdentifier"
				};
				using SecRecord? ExistingRecord = SecKeyChain.QueryAsRecord(SearchRecord, out SecStatusCode ResultCode);
				if (ResultCode == SecStatusCode.Success)
				{
					string? DeviceId = ExistingRecord?.ValueData?.ToString(NSStringEncoding.UTF8);
					if (string.IsNullOrEmpty(DeviceId))
						throw new InvalidOperationException("The persisted device identifier is empty.");
					this.deviceId = DeviceId;
					return;
				}
				if (ResultCode != SecStatusCode.ItemNotFound)
					throw new InvalidOperationException($"Unable to retrieve the device identifier: {ResultCode}.");
				if (ServiceRef.StorageService.HasExistingData())
					throw new InvalidOperationException("The device identifier is missing while local database files exist.");
				string Identifier = UIDevice.CurrentDevice.IdentifierForVendor?.ToString()
					?? throw new InvalidOperationException("The vendor identifier is unavailable.");
				using SecRecord NewRecord = new SecRecord(SecKind.GenericPassword)
				{
					Service = AppInfo.PackageName,
					Account = "DeviceIdentifier",
					Label = "Persistent Device Identifier for Vendor",
					ValueData = NSData.FromString(Identifier),
					Accessible = SecAccessible.WhenUnlockedThisDeviceOnly,
					Synchronizable = false
				};
				SecStatusCode AddResult = SecKeyChain.Add(NewRecord);
				if (AddResult != SecStatusCode.Success)
					throw new InvalidOperationException($"Unable to persist the device identifier: {AddResult}.");
				this.deviceId = Identifier;
			}
			finally { this.deviceIdSemaphore.Release(); }
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

		/*
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
		*/
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

		/// <summary>
		/// Gets the biometric method supported by the device.
		/// Can return Face, Fingerprint, Unknown, or None.
		/// </summary>
		/// <returns>The BiometricMethod which is preferred/supported on this device</returns>
		public BiometricMethod GetBiometricMethod()
		{
			if (!this.HasLocalAuthenticationContext)
				return BiometricMethod.None;

			try
			{
				if (!this.localAuthenticationContext.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _))
					return BiometricMethod.None;

				return this.localAuthenticationContext.BiometryType switch
				{
					LABiometryType.FaceId => BiometricMethod.FaceId,
					LABiometryType.TouchId => BiometricMethod.TouchId,
					_ => BiometricMethod.Unknown
				};
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return BiometricMethod.Unknown;
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
		public async Task<TokenInformation> GetPushNotificationToken()
		{
			string Token = string.Empty;

			try
			{
				await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
				Token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
			}

			TokenInformation TokenInformation = new()
			{
				Token = Token,
				ClientType = ClientType.iOS,
				Service = PushMessagingService.Firebase
			};

			return TokenInformation;
		}

		#region Keyboard
		public event EventHandler<KeyboardSizeMessage>? KeyboardShown;
		public event EventHandler<KeyboardSizeMessage>? KeyboardHidden;
		public event EventHandler<KeyboardSizeMessage>? KeyboardSizeChanged;


		/// <summary>
		/// Force hide the keyboard
		/// </summary>
		public void HideKeyboard()
		{
			AppDelegate.GetKeyWindow()?.EndEditing(true);
		}

		private void OnKeyboardWillShow(NSNotification notification)
		{
			CoreGraphics.CGRect keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);
			float keyboardHeight = (float)keyboardFrame.Height;
			KeyboardShown.Raise(this, new KeyboardSizeMessage(keyboardHeight));
			KeyboardSizeChanged.Raise(this, new KeyboardSizeMessage(keyboardHeight));
			WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(keyboardHeight));
		}

		private void OnKeyboardWillHide(NSNotification notification)
		{
			float keyboardHeight = 0;
			KeyboardHidden.Raise(this, new KeyboardSizeMessage(keyboardHeight));
			KeyboardSizeChanged.Raise(this, new KeyboardSizeMessage(keyboardHeight));
			WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(keyboardHeight));
		}


		#endregion

		#region Notifications
        /// <summary>
        /// Helper method to schedule a local notification after checking the authorization status.
        /// </summary>
        /// <param name="title">Notification title.</param>
        /// <param name="body">Notification body.</param>
        /// <param name="data">Additional data as key-value pairs.</param>
        private async void ShowLocalNotification(string title, string body, IDictionary<string, string> data)
        {
			try
			{
				// Check current notification settings without prompting the user
				UNNotificationSettings Settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
				if (Settings.AuthorizationStatus != UNAuthorizationStatus.Authorized)
					return;

				// Create the notification content
				UNMutableNotificationContent Content = new UNMutableNotificationContent
				{
					Title = title,
					Body = body,
					Sound = UNNotificationSound.Default
				};

				// Add any additional data as UserInfo
				if (data is not null)
				{
					NSMutableDictionary UserInfo = new();
					foreach (KeyValuePair<string, string> Pair in data)
					{
						UserInfo.SetValueForKey(new NSString(Pair.Value), new NSString(Pair.Key));
					}
					Content.UserInfo = UserInfo;
				}

				// Schedule the notification after a short delay
				UNTimeIntervalNotificationTrigger Trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
				UNNotificationRequest Request = UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), Content, Trigger);
				UNUserNotificationCenter.Current.AddNotificationRequest(Request, error =>
				{
					if (error is not null)
					{
						ServiceRef.LogService.LogWarning($"Error scheduling notification: {error.LocalizedDescription}");
					}
				});

			}
			catch (Exception ex)
			{
				return;
			}
        }

        // The following methods use the helper above.
        public void ShowMessageNotification(string Title, string MessageBody, IDictionary<string, string> Data)
        {
			this.ShowLocalNotification(Title, MessageBody, Data);
        }

        public void ShowIdentitiesNotification(string Title, string MessageBody, IDictionary<string, string> Data)
        {
			this.ShowLocalNotification(Title, MessageBody, Data);
        }

        public void ShowPetitionNotification(string Title, string MessageBody, IDictionary<string, string> Data)
        {
			this.ShowLocalNotification(Title, MessageBody, Data);
        }

        public void ShowContractsNotification(string Title, string MessageBody, IDictionary<string, string> Data)
        {
			this.ShowLocalNotification(Title, MessageBody, Data);
        }

        public void ShowEDalerNotification(string Title, string MessageBody, IDictionary<string, string> Data)
        {
			this.ShowLocalNotification(Title, MessageBody, Data);
        }

        public void ShowTokenNotification(string Title, string MessageBody, IDictionary<string, string> Data)
        {
			this.ShowLocalNotification(Title, MessageBody, Data);
        }

        public void ShowProvisioningNotification(string Title, string MessageBody, IDictionary<string, string> Data)
        {
			this.ShowLocalNotification(Title, MessageBody, Data);
        }
		#endregion

		public Thickness GetInsets()
		{
			/// <summary>
			/// Gets the current safe-area insets for the active window using the root view's
			/// <see cref="UIView.SafeAreaLayoutGuide"/> when available, falling back to the window's
			/// <see cref="UIWindow.SafeAreaInsets"/> if necessary.
			/// </summary>
			/// <returns>Safe-area insets as a <see cref="Thickness"/>.</returns>
			// Try to get the current UIWindow.
			UIWindow? Window = AppDelegate.GetKeyWindow();
			if (Window is null)
				return new Thickness(0);

			// Prefer using the root view's SafeAreaLayoutGuide to compute insets.
			UIView? RootView = Window.RootViewController?.View;
			if (RootView is not null)
			{
				CoreGraphics.CGRect Bounds = RootView.Bounds;
				CoreGraphics.CGRect LayoutFrame = RootView.SafeAreaLayoutGuide.LayoutFrame;

				double Left = LayoutFrame.Left - Bounds.Left;
				double Top = LayoutFrame.Top - Bounds.Top;
				double Right = Bounds.Right - LayoutFrame.Right;
				double Bottom = Bounds.Bottom - LayoutFrame.Bottom;

				// Validate non-negative before returning.
				if (Left >= 0 && Top >= 0 && Right >= 0 && Bottom >= 0)
					return new Thickness(Left, Top, Right, Bottom);
			}

			// Fallback to window safe area insets.
			UIEdgeInsets Insets = Window.SafeAreaInsets;
			return new Thickness(
				(double)Insets.Left,
				(double)Insets.Top,
				(double)Insets.Right,
				(double)Insets.Bottom);
		}
	}
}
