using System.Text;
using _Microsoft.Android.Resource.Designer;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
//using Android.Gms.Extensions;
using Android.Graphics;
using Android.OS;
using Android.Renderscripts;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Biometric;
using AndroidX.Core.App;
using AndroidX.Fragment.App;
using AndroidX.Lifecycle;
using CommunityToolkit.Mvvm.Messaging;

//using Firebase.Messaging;	// TODO: Firebase
using Java.Util.Concurrent;
using NeuroAccessMaui.Services.Push;
using Plugin.Firebase.CloudMessaging;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Rect = Android.Graphics.Rect;

using Application = Android.App.Application;
using FileProvider = AndroidX.Core.Content.FileProvider;
using Resource = Android.Resource;
using System.Globalization;

namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Android implementation of platform-specific features.
	/// </summary>
	public class PlatformSpecific : IPlatformSpecific
	{
		private bool isDisposed;

		/// <summary>
		/// Android implementation of platform-specific features.
		/// </summary>
		public PlatformSpecific()
		{
			this.InitializeKeyboard();
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

			if (Disposing)
			{
				protectionTimer?.Dispose();
				protectionTimer = null;
			}

			this.isDisposed = true;
		}

		private static bool screenProtected = true; // App started with screen protected.
		private static Timer? protectionTimer = null;

		/// <summary>
		/// If screen capture prohibition is supported
		/// </summary>
		public bool CanProhibitScreenCapture => true;

		/// <summary>
		/// If screen capture is prohibited or not.
		/// </summary>
		public bool ProhibitScreenCapture
		{
			get => screenProtected;
			set
			{
				try
				{
					protectionTimer?.Dispose();
					protectionTimer = null;

					if (screenProtected != value)
					{
						this.SetScreenSecurityProtection(value);
						screenProtected = value;
					}
				}
				catch (Exception Ex)
				{
					Log.Exception(Ex);
				}
			}
		}

		private void SetScreenSecurityProtection(bool Enabled)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					Activity? Activity = Platform.CurrentActivity;

					if (Activity is not null)
					{
						if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
						{
#pragma warning disable CA1416
							Activity.SetRecentsScreenshotEnabled(!Enabled);
#pragma warning restore CA1416
						}

						if (Enabled)
						{
							Activity.Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
							protectionTimer = new Timer(this.ProtectionTimerElapsed, null,
								Constants.Security.MaxScreenRecordingTimeSeconds * 1000, Timeout.Infinite);
						}
						else
							Activity.Window?.ClearFlags(WindowManagerFlags.Secure);
					}
				}
				catch (Exception Ex)
				{
					Log.Exception(Ex);
				}
			});
		}

		private void ProtectionTimerElapsed(object? P)
		{
			MainThread.BeginInvokeOnMainThread(() => this.ProhibitScreenCapture = false);
		}

		/// <summary>
		/// Gets the ID of the device
		/// </summary>
		public string? GetDeviceId()
		{
			ContentResolver? ContentResolver = Android.App.Application.Context.ContentResolver;

			if (ContentResolver is not null)
			{
				return Android.Provider.Settings.Secure.GetString(ContentResolver, Android.Provider.Settings.Secure.AndroidId);
			}
			try
			{
				App.SendAlertAsync("Unable to get AndroidID, ContentResolver was null", "text/plain").Wait();
				this.CloseApplication().Wait();
			}
			catch (Exception)
			{
				System.Environment.Exit(0);
			}

			return null;
		}


		/// <summary>
		/// Closes the application
		/// </summary>
		public Task CloseApplication()
		{
			Activity? Activity = Platform.CurrentActivity;
			Activity?.FinishAffinity();

			Java.Lang.JavaSystem.Exit(0);

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
			Context Context = Android.App.Application.Context;
			Java.IO.File? ExternalFilesDir = Context.GetExternalFilesDir("");

			if (ExternalFilesDir is null)
				return;

			if (!Directory.Exists(ExternalFilesDir.Path))
				Directory.CreateDirectory(ExternalFilesDir.Path);

			Java.IO.File FileDir = new(ExternalFilesDir.AbsolutePath + (Java.IO.File.Separator + FileName));

			File.WriteAllBytes(FileDir.Path, PngFile);

			Intent Intent = new(Intent.ActionSend);
			Intent.PutExtra(Intent.ExtraText, Message);
			Intent.SetType(Constants.MimeTypes.Png);

			Intent.AddFlags(ActivityFlags.GrantReadUriPermission);
			Intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
			Intent.PutExtra(Intent.ExtraStream, FileProvider.GetUriForFile(Context, "com.tag.IdApp.fileprovider", FileDir));

			Intent? MyIntent = Intent.CreateChooser(Intent, Title);

			if (MyIntent is not null)
			{
				MyIntent.AddFlags(ActivityFlags.NewTask);
				Context.StartActivity(MyIntent);
			}
		}

		/*
				/// <summary>
				/// Make a blurred screenshot
				/// TODO: Just make a screen shot. Use the portable CV library to blur it.
				/// </summary>
				public Task<byte[]> CaptureScreen(int blurRadius)
				{
					blurRadius = Math.Min(25, Math.Max(blurRadius, 0));

					Activity? Activity = Platform.CurrentActivity;
					Android.Views.View? RootView = Activity?.Window?.DecorView.RootView;

					if (RootView is null)
						return Task.FromResult<byte[]>([]);

					using Bitmap Screenshot = Bitmap.CreateBitmap(RootView.Width, RootView.Height, Bitmap.Config.Argb8888!);
					Canvas Canvas = new(Screenshot);
					RootView.Draw(Canvas);

					Bitmap? Blurred = null;

					if (Activity != null && (int)Android.OS.Build.VERSION.SdkInt >= 17)
						Blurred = ToBlurred(Screenshot, Activity, blurRadius);
					else
						Blurred = ToLegacyBlurred(Screenshot, blurRadius);

					MemoryStream Stream = new();
					Blurred.Compress(Bitmap.CompressFormat.Jpeg!, 80, Stream);
					Stream.Seek(0, SeekOrigin.Begin);

					return Task.FromResult(Stream.ToArray());
				}

				private static Bitmap ToBlurred(Bitmap originalBitmap, Activity? Activity, int radius)
				{
					// Create another bitmap that will hold the results of the filter.
					Bitmap BlurredBitmap = Bitmap.CreateBitmap(originalBitmap);
					RenderScript? RenderScript = RenderScript.Create(Activity);

					// Load up an instance of the specific script that we want to use.
					// An Element is similar to a C type. The second parameter, Element.U8_4,
					// tells the Allocation is made up of 4 fields of 8 unsigned bits.
					ScriptIntrinsicBlur? Script = ScriptIntrinsicBlur.Create(RenderScript, Android.Renderscripts.Element.U8_4(RenderScript));

					// Create an Allocation for the kernel inputs.
					Allocation? Input = Allocation.CreateFromBitmap(RenderScript, originalBitmap, Allocation.MipmapControl.MipmapFull,
						AllocationUsage.Script);

					// Assign the input Allocation to the script.
					Script?.SetInput(Input);

					// Set the blur radius
					Script?.SetRadius(radius);

					// Finally we need to create an output allocation to hold the output of the Renderscript.
					Allocation? Output = Allocation.CreateTyped(RenderScript, Input?.Type);

					// Next, run the script. This will run the script over each Element in the Allocation, and copy it's
					// output to the allocation we just created for this purpose.
					Script?.ForEach(Output);

					// Copy the output to the blurred bitmap
					Output?.CopyTo(BlurredBitmap);

					// Cleanup.
					Output?.Destroy();
					Input?.Destroy();
					Script?.Destroy();
					RenderScript?.Destroy();

					return BlurredBitmap;
				}

				// Source: http://incubator.quasimondo.com/processing/superfast_blur.php
				public static Bitmap ToLegacyBlurred(Bitmap source, int radius)
				{
					Bitmap.Config? Config = source.GetConfig();
					Config ??= Bitmap.Config.Argb8888;    // This will support transparency

					Bitmap? Img = source.Copy(Config!, true);

					int w = Img!.Width;
					int h = Img.Height;
					int wm = w - 1;
					int Hm = h - 1;
					int wh = w * h;
					int Div = radius + radius + 1;
					int[] r = new int[wh];
					int[] g = new int[wh];
					int[] b = new int[wh];
					int Rsum, Gsum, Bsum, x, y, i, P, P1, P2, yp, yi, yw;
					int[] Vmin = new int[Math.Max(w, h)];
					int[] Vmax = new int[Math.Max(w, h)];
					int[] Pix = new int[w * h];

					Img.GetPixels(Pix, 0, w, 0, 0, w, h);

					int[] Dv = new int[256 * Div];
					for (i = 0; i < 256 * Div; i++)
						Dv[i] = (i / Div);

					yw = yi = 0;

					for (y = 0; y < h; y++)
					{
						Rsum = Gsum = Bsum = 0;
						for (i = -radius; i <= radius; i++)
						{
							P = Pix[yi + Math.Min(wm, Math.Max(i, 0))];
							Rsum += (P & 0xff0000) >> 16;
							Gsum += (P & 0x00ff00) >> 8;
							Bsum += P & 0x0000ff;
						}
						for (x = 0; x < w; x++)
						{

							r[yi] = Dv[Rsum];
							g[yi] = Dv[Gsum];
							b[yi] = Dv[Bsum];

							if (y == 0)
							{
								Vmin[x] = Math.Min(x + radius + 1, wm);
								Vmax[x] = Math.Max(x - radius, 0);
							}

							P1 = Pix[yw + Vmin[x]];
							P2 = Pix[yw + Vmax[x]];

							Rsum += ((P1 & 0xff0000) - (P2 & 0xff0000)) >> 16;
							Gsum += ((P1 & 0x00ff00) - (P2 & 0x00ff00)) >> 8;
							Bsum += (P1 & 0x0000ff) - (P2 & 0x0000ff);
							yi++;
						}
						yw += w;
					}

					for (x = 0; x < w; x++)
					{
						Rsum = Gsum = Bsum = 0;
						yp = -radius * w;
						for (i = -radius; i <= radius; i++)
						{
							yi = Math.Max(0, yp) + x;
							Rsum += r[yi];
							Gsum += g[yi];
							Bsum += b[yi];
							yp += w;
						}
						yi = x;
						for (y = 0; y < h; y++)
						{
							// Preserve alpha channel: ( 0xff000000 & pix[yi] )
							int rgb = (Dv[Rsum] << 16) | (Dv[Gsum] << 8) | Dv[Bsum];
							Pix[yi] = ((int)(0xff000000 & Pix[yi]) | rgb);
							if (x == 0)
							{
								Vmin[y] = Math.Min(y + radius + 1, Hm) * w;
								Vmax[y] = Math.Max(y - radius, 0) * w;
							}
							P1 = x + Vmin[y];
							P2 = x + Vmax[y];

							Rsum += r[P1] - r[P2];
							Gsum += g[P1] - g[P2];
							Bsum += b[P1] - b[P2];

							yi += w;
						}
					}

					Img.SetPixels(Pix, 0, w, 0, 0, w, h);
					return Img;
				}
		*/

		/// <summary>
		/// If the device supports authenticating the user using fingerprints.
		/// </summary>
		public bool SupportsFingerprintAuthentication
		{
			get
			{
				try
				{
					if (!OperatingSystem.IsAndroidVersionAtLeast(23))
						return false;


					Context Context = Android.App.Application.Context;

					// For Android 28 and later, check for UseBiometric; for earlier versions, check for UseFingerprint.
					if (OperatingSystem.IsAndroidVersionAtLeast(28)) // API 28+
					{
						if (Context.CheckCallingOrSelfPermission(Manifest.Permission.UseBiometric) != Permission.Granted)
							return false;
					}
					else
					{
						if (Context.CheckCallingOrSelfPermission(Manifest.Permission.UseFingerprint) != Permission.Granted)
							return false;
					}

					BiometricManager Manager = BiometricManager.From(Context);
					int Level = BiometricManager.Authenticators.BiometricWeak;

					return Manager.CanAuthenticate(Level) == BiometricManager.BiometricSuccess;

					// TODO: AndroidX package conflicts arose between Maui & Xamarin.AndroidX.Biometrics package, that the
					//       Plugin.Fingerprint seems to have resolved. Using this library while Maui fixes the problem, even
					//       though the library is not used in code.

					// TODO: Consider alternative levels:
					//
					//			public interface Authenticators {
					//			        /**
					//			         * Any biometric (e.g. fingerprint, iris, or face) on the device that meets or exceeds the
					//			         * requirements for <strong>Class 3</strong> (formerly <strong>Strong</strong>), as defined
					//			         * by the Android CDD.
					//			         */
					//			        int BIOMETRIC_STRONG = 0x000F;
					//			
					//			        /**
					//			         * Any biometric (e.g. fingerprint, iris, or face) on the device that meets or exceeds the
					//			         * requirements for <strong>Class 2</strong> (formerly <strong>Weak</strong>), as defined by
					//			         * the Android CDD.
					//			         *
					//			         * <p>Note that this is a superset of {@link #BIOMETRIC_STRONG} and is defined such that
					//			         * {@code BIOMETRIC_STRONG | BIOMETRIC_WEAK == BIOMETRIC_WEAK}.
					//			         */
					//			        int BIOMETRIC_WEAK = 0x00FF;
					//			
					//			        /**
					//			         * The non-biometric credential used to secure the device (i.e. PIN, pattern, or password).
					//			         * This should typically only be used in combination with a biometric auth type, such as
					//			         * {@link #BIOMETRIC_WEAK}.
					//			         */
					//			        int DEVICE_CREDENTIAL = 1 << 15;
					//			    }
				}
				catch (Exception)
				{
					return false;
				}
			}
		}
		/// <summary>
		/// Gets the biometric method supported by the device.
		/// Currently on android, you cannot determine if the device will use fingerprint or face recognition.
		/// Unknown is returned if the device supports biometric authentication, but the method is unknown.
		/// </summary>
		/// <returns>The BiometricMethod which is preferred/supported on this device</returns>
		public BiometricMethod GetBiometricMethod()
		{
			// Biometric authentication requires at least API level 23.
			if (OperatingSystem.IsAndroidVersionAtLeast(23))
				return BiometricMethod.None;

			Context Context = Android.App.Application.Context;

			if (OperatingSystem.IsAndroidVersionAtLeast(28)) // API 28+
			{
				// Check for the UseBiometric permission (only available on API 28+)
				if (Context.CheckCallingOrSelfPermission(Manifest.Permission.UseBiometric) != Permission.Granted)
				{
					return BiometricMethod.None;
				}
			}
			else // For API levels 23 through 27
			{
				// Check for the UseFingerprint permission, which is available on earlier versions.
				if (Context.CheckCallingOrSelfPermission(Manifest.Permission.UseFingerprint) != Permission.Granted)
				{
					return BiometricMethod.None;
				}
			}

			BiometricManager Manager = BiometricManager.From(Context);
			const int Level = BiometricManager.Authenticators.BiometricWeak;
			return Manager.CanAuthenticate(Level) == BiometricManager.BiometricSuccess
					 ? BiometricMethod.Unknown
					 : BiometricMethod.None;
		}

		/// <summary>
		/// Authenticates the user using the fingerprint sensor.
		/// </summary>
		/// <param name="Title">Title of authentication dialog.</param>
		/// <param name="Subtitle">Optional Subtitle.</param>
		/// <param name="Description">Description texst to display to user in authentication dialog.</param>
		/// <param name="Cancel">Label for Cancel button.</param>
		/// <param name="CancellationToken">Optional cancellation token, to cancel process.</param>
		/// <returns>If the user has been successfully authenticated.</returns>
		public async Task<bool> AuthenticateUserFingerprint(string Title, string? Subtitle, string Description, string Cancel,
			CancellationToken? CancellationToken)
		{
			if (!this.SupportsFingerprintAuthentication)
				return false;

			if (string.IsNullOrWhiteSpace(Title))
				throw new ArgumentException("Title cannot be empty.", nameof(Title));

			if (Platform.CurrentActivity is not FragmentActivity Activity)
				return false;

			try
			{
				BiometricPrompt.PromptInfo.Builder Builder = new();

				Builder.SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricWeak);

				Builder.SetConfirmationRequired(false);
				Builder.SetTitle(Title);
				Builder.SetDescription(Description);
				Builder.SetNegativeButtonText(Cancel);

				if (!string.IsNullOrEmpty(Subtitle))
					Builder.SetSubtitle(Subtitle);

				BiometricPrompt.PromptInfo Prompt = Builder.Build();
				IExecutorService? Executor = Executors.NewSingleThreadExecutor();
				CallbackHandler Handler = new();
				CancellationTokenRegistration? Registration = null;

				BiometricPrompt Dialog = new(Activity, Executor, Handler);
				try
				{
					Registration = CancellationToken?.Register(Dialog.CancelAuthentication);
					Dialog.Authenticate(Prompt);

					return await Handler.Result;
				}
				finally
				{
					if (Registration.HasValue)
						Registration.Value.Dispose();

					// Remove the lifecycle observer that is set by the BiometricPrompt.
					// Reference: https://stackoverflow.com/a/59637670/1489968
					// Review after referenced nugets (or Maui) has been updated.

					Java.Lang.Class Class = Java.Lang.Class.FromType(Dialog.GetType());
					Java.Lang.Reflect.Field[] Fields = Class.GetDeclaredFields();
					Java.Lang.Reflect.Field? LifecycleObserver = Fields?.FirstOrDefault(f => f.Name == "mLifecycleObserver");

					if (LifecycleObserver is not null)
					{
						LifecycleObserver.Accessible = true;
						ILifecycleObserver? LastLifecycleObserver = LifecycleObserver.Get(Dialog).JavaCast<ILifecycleObserver>();
						Lifecycle? Lifecycle = Activity.Lifecycle;

						if (LastLifecycleObserver is not null && Lifecycle is not null)
							Lifecycle.RemoveObserver(LastLifecycleObserver);
					}

					Dialog?.Dispose();
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return false;
			}
		}

		private class CallbackHandler : BiometricPrompt.AuthenticationCallback, IDialogInterfaceOnClickListener
		{
			private readonly TaskCompletionSource<bool> result = new();

			public Task<bool> Result => this.result.Task;

			public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult Result)
			{
				base.OnAuthenticationSucceeded(Result);
				this.result.TrySetResult(true);
			}

			public override void OnAuthenticationError(int ErrorCode, Java.Lang.ICharSequence ErrorString)
			{
				base.OnAuthenticationError(ErrorCode, ErrorString);
				this.result.TrySetResult(false);
			}

			public override void OnAuthenticationFailed()
			{
				base.OnAuthenticationFailed();
				this.result.TrySetResult(false);
			}

			public void OnClick(IDialogInterface? Dialog, int Which)
			{
				this.result.TrySetResult(false);
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
				ServiceRef.LogService.LogException(ex);
			}

			TokenInformation TokenInformation = new()
			{
				Token = Token,
				ClientType = ClientType.Android,
				Service = PushMessagingService.Firebase
			};

			return TokenInformation;
		}

		#region Keyboard

		/// <inheritdoc />
		public event EventHandler<KeyboardSizeMessage>? KeyboardShown;
		/// <inheritdoc />
		public event EventHandler<KeyboardSizeMessage>? KeyboardHidden;
		/// <summary>
		///	Fired when the keyboard size changes.
		/// </summary>>
		/// <remarks>
		///	On Android, the keyboard size is not constantly updated, but only when the keyboard is shown or hidden.
		/// </remarks>
		public event EventHandler<KeyboardSizeMessage>? KeyboardSizeChanged;

		private Activity? activity;
		private Android.Views.View? rootView;
		private int lastKeyboardHeight = 0;
		private Handler? initializeKeyboardHandler;

		/// <inheritdoc/>
		public void HideKeyboard()
		{
			if (this.activity is null || this.rootView is null)
				return;
			InputMethodManager? InputMethodManager = this.activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
			InputMethodManager?.HideSoftInputFromWindow(this.rootView.WindowToken, HideSoftInputFlags.None);
			this.activity.Window?.DecorView.ClearFocus();
		}

		private void InitializeKeyboard()
		{
			this.activity = Platform.CurrentActivity;
			try
			{
				this.initializeKeyboardHandler = new Handler(Looper.MainLooper!);
				this.CheckRootView();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private void CheckRootView()
		{
			this.activity = Platform.CurrentActivity;

			if (this.activity?.Window?.DecorView.RootView?.ViewTreeObserver is null)
			{
				this.initializeKeyboardHandler?.PostDelayed(this.CheckRootView, 100);
				return;
			}
			this.activity = Platform.CurrentActivity;
			this.rootView = this.activity!.Window!.DecorView.RootView;
			this.rootView!.ViewTreeObserver!.GlobalLayout += this.OnGlobalLayout;
		}



		private void OnGlobalLayout(object? sender, EventArgs e)
		{
			Rect r = new();
			this.rootView!.GetWindowVisibleDisplayFrame(r);

			int ScreenHeight = this.rootView.RootView!.Height;
			int StatusBarHeight = 0;
			int ActionBarHeight = 0;

			// if this succeeds, we can calculate an exact keyboard height
			Android.Views.View? ContentView = this.rootView.FindViewById(Android.Views.Window.IdAndroidContent);
			if (ContentView is not null)
			{
				StatusBarHeight = r.Top - ContentView.Top;
				ActionBarHeight = r.Bottom - ContentView.Bottom;
			}

			// Calculate the height of the keyboard (if the above fails, the keyboardsize will include the size of the action and status bar)
			int AvailableScreenHeight = ScreenHeight - StatusBarHeight - ActionBarHeight;
			int VisibleHeight = r.Height();
			int KeypadHeight = AvailableScreenHeight - VisibleHeight; // height of the keyboard, but is not garanteed to be the actual keyboard height it might include other things such as the action bar.

			// Assume keyboard is shown if more than 15% of the available screen height is used.
			// This is a heuristic, and may need to be adjusted.
			// I really don't like this solution, but android doesn't provide a better way to detect the keyboard at the time of writing.
			// Checking keyboardheight > 0 is not enough, because the keyboardheight is not garanteed to be accurate on all devices and circumstances

			if (KeypadHeight > AvailableScreenHeight * 0.15)
			{
				this.lastKeyboardHeight = KeypadHeight;
				this.KeyboardSizeChanged.Raise(this, new KeyboardSizeMessage(KeypadHeight));
				WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(KeypadHeight));
				this.KeyboardShown.Raise(this, new KeyboardSizeMessage(KeypadHeight));
			}
			else
			{
				if (this.lastKeyboardHeight == 0)
					return;

				this.lastKeyboardHeight = 0;
				this.KeyboardSizeChanged.Raise(this, new KeyboardSizeMessage(0));
				WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(0));
				this.KeyboardHidden.Raise(this, new KeyboardSizeMessage(0));
			}
		}
		#endregion

		#region Notifications
		public void ShowMessageNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			Context Context = Application.Context;
			Intent Intent = new Intent(Context, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);
			foreach (string Key in Data.Keys)
			{
				Intent.PutExtra(Key, Data[Key]);
			}
			PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(Context, 100, Intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
			int ResIdentifier = Context.Resources?.GetIdentifier("app_icon", "drawable", Context.PackageName) ?? 0;
			if (ResIdentifier == 0)
			{
				ServiceRef.LogService.LogWarning("App icon not found. Aborting local notification");
				return;
			}

			if (Data.TryGetValue("fromJid", out string? FromJid) && !string.IsNullOrEmpty(FromJid))
			{
				Intent.SetData(Android.Net.Uri.Parse(Constants.UriSchemes.Xmpp + ":" + FromJid));
				Intent.SetAction(Intent.ActionView);
			}

			NotificationCompat.Builder Builder = new NotificationCompat.Builder(Context, Constants.PushChannels.Messages)
				 .SetSmallIcon(ResIdentifier)
				 .SetContentTitle(Title)
				 .SetContentText(MessageBody)
				 .SetAutoCancel(true)
				 .SetContentIntent(PendingIntent);

			NotificationManagerCompat NotificationManager = NotificationManagerCompat.From(Context);
			NotificationManager.Notify(100, Builder.Build());
		}

		public void ShowIdentitiesNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			Context Context = Application.Context;
			Intent Intent = new Intent(Context, typeof(MainActivity));

			Intent.AddFlags(ActivityFlags.ClearTop);
			foreach (string Key in Data.Keys)
			{
				Intent.PutExtra(Key, Data[Key]);
			}

			// Optionally add additional details (for example, appending a legal id)
			string ContentText = MessageBody;
			if (Data.TryGetValue("legalId", out string? LegalId) && !string.IsNullOrEmpty(LegalId))
			{
				Intent.SetData(Android.Net.Uri.Parse(Constants.UriSchemes.IotId + ":" + LegalId));
				Intent.SetAction(Intent.ActionView);
				ContentText += System.Environment.NewLine + $"({LegalId})";
			}

			PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(Context, 101, Intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);

			int ResIdentifier = Context.Resources?.GetIdentifier("app_icon", "drawable", Context.PackageName) ?? 0;
			if (ResIdentifier == 0)
			{
				ServiceRef.LogService.LogWarning("App icon not found. Aborting local notification");
				return;
			}


			NotificationCompat.Builder Builder = new NotificationCompat.Builder(Context, Constants.PushChannels.Identities)
				 .SetSmallIcon(ResIdentifier)
				 .SetContentTitle(Title)
				 .SetContentText(ContentText)
				 .SetAutoCancel(true)
				 .SetContentIntent(PendingIntent);

			NotificationManagerCompat NotificationManager = NotificationManagerCompat.From(Context);
			NotificationManager.Notify(101, Builder.Build());
		}

		public void ShowPetitionNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			Context Context = Application.Context;
			Intent Intent = new Intent(Context, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);
			foreach (string Key in Data.Keys)
			{
				Intent.PutExtra(Key, Data[Key]);
			}

			// Use fromJid and rosterName to compose the notification body
			string FromJid = Data.TryGetValue("fromJid", out string? Value) ? Value : string.Empty;
			string RosterName = Data.TryGetValue("rosterName", out string? Value1) ? Value1 : string.Empty;
			string ContentText = $"{(string.IsNullOrEmpty(RosterName) ? FromJid : RosterName)}: {MessageBody}";

			PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(Context, 102, Intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
			int ResIdentifier = Context.Resources?.GetIdentifier("app_icon", "drawable", Context.PackageName) ?? 0;
			if (ResIdentifier == 0)
			{
				ServiceRef.LogService.LogWarning("App icon not found. Aborting local notification");
				return;
			}
			NotificationCompat.Builder Builder = new NotificationCompat.Builder(Context, Constants.PushChannels.Petitions)
				 .SetSmallIcon(ResIdentifier)
				 .SetContentTitle(Title)
				 .SetContentText(ContentText)
				 .SetAutoCancel(true)
				 .SetContentIntent(PendingIntent);

			NotificationManagerCompat NotificationManager = NotificationManagerCompat.From(Context);
			NotificationManager.Notify(102, Builder.Build());
		}

		public void ShowContractsNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			Context Context = Application.Context;
			Intent Intent = new Intent(Context, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
			{
				Intent.PutExtra(Key, Data[Key]);
			}

			StringBuilder ContentBuilder = new StringBuilder();
			ContentBuilder.Append(MessageBody);
			if (Data.TryGetValue("role", out string? Role) && !string.IsNullOrEmpty(Role))
			{
				ContentBuilder.AppendLine().Append(Role);
			}
			if (Data.TryGetValue("contractId", out string? ContractId) && !string.IsNullOrEmpty(ContractId))
			{
				ContentBuilder.AppendLine().Append(CultureInfo.InvariantCulture, $"({ContractId})");
			}
			if (Data.TryGetValue("legalId", out string? LegalId) && !string.IsNullOrEmpty(LegalId))
			{
				ContentBuilder.AppendLine().Append(CultureInfo.InvariantCulture, $"({LegalId})");
			}

			PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(Context, 103, Intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
			int ResIdentifier = Context.Resources?.GetIdentifier("app_icon", "drawable", Context.PackageName) ?? 0;
			if (ResIdentifier == 0)
			{
				ServiceRef.LogService.LogWarning("App icon not found. Aborting local notification");
				return;
			}
			NotificationCompat.Builder Builder = new NotificationCompat.Builder(Context, Constants.PushChannels.Contracts)
				 .SetSmallIcon(ResIdentifier)
				 .SetContentTitle(Title)
				 .SetContentText(ContentBuilder.ToString())
				 .SetAutoCancel(true)
				 .SetContentIntent(PendingIntent);

			NotificationManagerCompat NotificationManager = NotificationManagerCompat.From(Context);
			NotificationManager.Notify(103, Builder.Build());
		}

		public void ShowEDalerNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			Context Context = Application.Context;
			Intent Intent = new Intent(Context, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);
			foreach (string Key in Data.Keys)
			{
				Intent.PutExtra(Key, Data[Key]);
			}

			StringBuilder ContentBuilder = new StringBuilder();
			ContentBuilder.Append(MessageBody);
			if (Data.TryGetValue("amount", out string? Amount) && !string.IsNullOrEmpty(Amount))
			{
				ContentBuilder.AppendLine().Append(Amount);
				if (Data.TryGetValue("currency", out string? Currency) && !string.IsNullOrEmpty(Currency))
					ContentBuilder.Append(" " + Currency);
				if (Data.TryGetValue("timestamp", out string? Timestamp) && !string.IsNullOrEmpty(Timestamp))
               ContentBuilder.Append(string.Format(CultureInfo.InvariantCulture, " ({0})", Timestamp));
			}

			PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(Context, 104, Intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
			int ResIdentifier = Context.Resources?.GetIdentifier("app_icon", "drawable", Context.PackageName) ?? 0;
			if (ResIdentifier == 0)
			{
				ServiceRef.LogService.LogWarning("App icon not found. Aborting local notification");
				return;
			}
			NotificationCompat.Builder Builder = new NotificationCompat.Builder(Context, Constants.PushChannels.EDaler)
				 .SetSmallIcon(ResIdentifier)
				 .SetContentTitle(Title)
				 .SetContentText(ContentBuilder.ToString())
				 .SetAutoCancel(true)
				 .SetContentIntent(PendingIntent);

			NotificationManagerCompat NotificationManager = NotificationManagerCompat.From(Context);
			NotificationManager.Notify(104, Builder.Build());
		}

		public void ShowTokenNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			Context Context = Application.Context;
			Intent Intent = new Intent(Context, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);
			foreach (string Key in Data.Keys)
			{
				Intent.PutExtra(Key, Data[Key]);
			}

			StringBuilder ContentBuilder = new StringBuilder();
			ContentBuilder.Append(MessageBody);
			if (Data.TryGetValue("value", out string? Value) && !string.IsNullOrEmpty(Value))
			{
				ContentBuilder.AppendLine().Append(Value);
				if (Data.TryGetValue("currency", out string? Currency) && !string.IsNullOrEmpty(Currency))
					ContentBuilder.Append(" " + Currency);
			}

			PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(Context, 105, Intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
			int ResIdentifier = Context.Resources?.GetIdentifier("app_icon", "drawable", Context.PackageName) ?? 0;
			if (ResIdentifier == 0)
			{
				ServiceRef.LogService.LogWarning("App icon not found. Aborting local notification");
				return;
			}
			NotificationCompat.Builder Builder = new NotificationCompat.Builder(Context, Constants.PushChannels.Tokens)
				 .SetSmallIcon(ResIdentifier)
				 .SetContentTitle(Title)
				 .SetContentText(ContentBuilder.ToString())
				 .SetAutoCancel(true)
				 .SetContentIntent(PendingIntent);

			NotificationManagerCompat NotificationManager = NotificationManagerCompat.From(Context);
			NotificationManager.Notify(105, Builder.Build());
		}

		public void ShowProvisioningNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			Context Context = Application.Context;
			Intent Intent = new Intent(Context, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);
			foreach (string Key in Data.Keys)
			{
				Intent.PutExtra(Key, Data[Key]);
			}
			PendingIntent? PendingIntent = Android.App.PendingIntent.GetActivity(Context, 106, Intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
			int ResIdentifier = Context.Resources?.GetIdentifier("app_icon", "drawable", Context.PackageName) ?? 0;
			if (ResIdentifier == 0)
			{
				ServiceRef.LogService.LogWarning("App icon not found. Aborting local notification");
				return;
			}
			NotificationCompat.Builder Builder = new NotificationCompat.Builder(Context, Constants.PushChannels.Provisioning)
				 .SetSmallIcon(ResIdentifier)
				 .SetContentTitle(Title)
				 .SetContentText(MessageBody)
				 .SetAutoCancel(true)
				 .SetContentIntent(PendingIntent);

			NotificationManagerCompat NotificationManager = NotificationManagerCompat.From(Context);
			NotificationManager.Notify(106, Builder.Build());
		}
		#endregion
	}
}
