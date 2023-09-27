using Android.OS;
using Android.Views;
using Waher.Events;

namespace NeuroAccessMaui.Services;

public class PlatformSpecific : IPlatformSpecific
{
	public PlatformSpecific()
	{
	}

	private static bool screenProtected = true; // App started with screen protected.
	private static Timer? protectionTimer = null;

	/// <inheritdoc/>
	public bool CanProhibitScreenCapture => true;

	/// <inheritdoc/>
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
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}
	}

	private void SetScreenSecurityProtection(bool enabled)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			try
			{
				Android.App.Activity? activity = Platform.CurrentActivity;

				if (activity is not null)
				{
					if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
					{
#pragma warning disable CA1416
						activity.SetRecentsScreenshotEnabled(!enabled);
#pragma warning restore CA1416
					}

					if (enabled)
					{
						activity.Window?.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
						protectionTimer = new Timer(this.ProtectionTimerElapsed, null, 1000 * 60 * 60, Timeout.Infinite);
					}
					else
					{
						activity.Window?.ClearFlags(WindowManagerFlags.Secure);
					}
				}
			}
			catch (Exception)
			{
			}
		});
	}

	private void ProtectionTimerElapsed(object? P)
	{
		MainThread.BeginInvokeOnMainThread(() => this.ProhibitScreenCapture = false);
	}

	/// <inheritdoc/>
	public string? GetDeviceId()
	{
		Android.Content.ContentResolver? ContentResolver = Android.App.Application.Context.ContentResolver;

		if (ContentResolver is not null)
		{
			return Android.Provider.Settings.Secure.GetString(ContentResolver, Android.Provider.Settings.Secure.AndroidId);
		}

		return null;
	}

	/// <inheritdoc/>
	public Task CloseApplication()
	{
		Android.App.Activity? Activity = Android.App.Application.Context as Android.App.Activity;    // TODO: returns null. Context points to Application instance.
		Activity?.FinishAffinity();

		Java.Lang.JavaSystem.Exit(0);

		return Task.CompletedTask;
	}
}
