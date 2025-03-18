#if IOS
using Foundation;
using UIKit;
using UserNotifications;
#endif
#if ANDROID
using System.Linq;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;
#endif


namespace NeuroAccessMaui.CustomPermissions
{
	public class NotificationPermission : Permissions.BasePermission
	{
		public override async Task<PermissionStatus> CheckStatusAsync()
		{
#if IOS
            // Check the current notification settings on iOS
            UNNotificationSettings Settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
            return Settings.AuthorizationStatus switch
            {
	            // Map the iOS authorization status to a MAUI PermissionStatus
	            UNAuthorizationStatus.Authorized => PermissionStatus.Granted,
	            UNAuthorizationStatus.Denied => PermissionStatus.Denied,
	            _ => PermissionStatus.Unknown
            };
#elif ANDROID
			// For Android 13+ (API level 33), notifications are a runtime permission.
			if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu && OperatingSystem.IsAndroidVersionAtLeast(33))
			{
				Android.Content.Context Context = Android.App.Application.Context;
				// Check the POST_NOTIFICATIONS permission
				int Result = ((int)ContextCompat.CheckSelfPermission(Context, Manifest.Permission.PostNotifications));
				return Result == (int)Permission.Granted ? PermissionStatus.Granted : PermissionStatus.Denied;
			}
			// For earlier versions, notifications are granted by default.
			return PermissionStatus.Granted;
#else
            return PermissionStatus.Unknown;
#endif
		}

		public override void EnsureDeclared()
		{
#if ANDROID
			if (!OperatingSystem.IsAndroidVersionAtLeast(33))
				return;

			try
			{
				// On Android, check that the POST_NOTIFICATIONS permission is declared in the manifest.
				Android.Content.Context Context = Android.App.Application.Context;
				PackageManager PackageManager = Context.PackageManager ?? throw new Exception();
				string? PackageName = Context.PackageName;

				PackageInfo PackageInfo = PackageManager.GetPackageInfo(PackageName ?? string.Empty, PackageInfoFlags.Permissions) ?? throw new Exception() ;
				IList<string>? DeclaredPermissions = PackageInfo.RequestedPermissions;
				if (DeclaredPermissions == null || !DeclaredPermissions.Contains(Manifest.Permission.PostNotifications))
				{
					throw new PermissionException($"The Android manifest does not declare the required permission: {Manifest.Permission.PostNotifications}");
				}
			}
			catch (PackageManager.NameNotFoundException)
			{
				throw new PermissionException("Unable to verify that the POST_NOTIFICATIONS permission is declared in the manifest.");
			}
			catch (Exception)
			{
				// assume declared if there is any weird errors with the PackageManager
				return;
			}
#endif
			// For iOS and other platforms, no manifest declaration is required.
		}

		public override async Task<PermissionStatus> RequestAsync()
		{
#if IOS
            // Request notification permission on iOS using UNUserNotificationCenter
            (bool Granted, NSError Error) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound);

            // Optionally, register for remote notifications if needed
            if (Granted)
            {
                UIApplication.SharedApplication.InvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.RegisterForRemoteNotifications();
                });
            }
            return Granted ? PermissionStatus.Granted : PermissionStatus.Denied;
#elif ANDROID
			if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu && OperatingSystem.IsAndroidVersionAtLeast(33))
			{
				// For Android 13+, request the POST_NOTIFICATIONS permission.
				PermissionStatus Status = await Permissions.RequestAsync<Permissions.PostNotifications>();
				return Status;
			}
			// For earlier versions, notifications are automatically granted.
			return PermissionStatus.Granted;
#else
            return PermissionStatus.Unknown;
#endif
		}

		// Optional: Override ShouldShowRationale if you want to provide additional info to the user.
		public override bool ShouldShowRationale() => true;
	}
}
