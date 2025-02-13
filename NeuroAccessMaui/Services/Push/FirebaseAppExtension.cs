

using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;
#if IOS
using Plugin.Firebase.Core.Platforms.iOS;
#elif ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Provides extension methods for registering Firebase services in a MAUI application.
	/// </summary>
	public static class FirebaseAppExtension
	{
		/// <summary>
		/// Registers the Firebase services with the MAUI application's lifecycle events.
		/// </summary>
		/// <param name="Builder">The <see cref="MauiAppBuilder"/> used to configure the application.</param>
		/// <returns>The updated <see cref="MauiAppBuilder"/> with Firebase services configured.</returns>
		/// <remarks>
		/// This method conditionally configures Firebase initialization based on the target platform.
		/// For iOS, it initializes Firebase and the Firebase Cloud Messaging service when the app is about to finish launching.
		/// For Android, it initializes Firebase during the OnCreate lifecycle event.
		/// </remarks>
		public static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder Builder)
		{
			Builder.ConfigureLifecycleEvents(events =>
			{
#if IOS
            events.AddiOS(iOS => iOS.WillFinishLaunching((_,__) => {
               CrossFirebase.Initialize();
					FirebaseCloudMessagingImplementation.Initialize();
               return false;
            }));
#elif ANDROID
				events.AddAndroid(android => android.OnCreate((activity, _) =>
					 CrossFirebase.Initialize(activity)));
#endif
			});

			return Builder;
		}
	}
}
