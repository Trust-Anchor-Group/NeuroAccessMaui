

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
            events.AddiOS(iOS => iOS.WillFinishLaunching((_,_) => {
               	CrossFirebase.Initialize();
				FirebaseCloudMessagingImplementation.Initialize();
               	return false;
            }));
#elif ANDROID
				events.AddAndroid(android => android.OnCreate((activity, _) =>
					{
						CrossFirebase.Initialize(activity);
						// Disable default notification
						FirebaseCloudMessagingImplementation.ShowLocalNotificationAction = Message =>
						{
							try
							{
								// Extract title and body from the data payload.
								Message.Data.TryGetValue("myTitle", out string? Title);
								Message.Data.TryGetValue("myBody", out string? Body);
								Message.Data.TryGetValue("channelId", out string? ChannelId);

								// Optional: Log the complete data payload for debugging.
								foreach (var key in Message.Data.Keys)
								{
									Console.WriteLine($"[Push Received] {key}: {Message.Data[key]}");
								}

								switch (ChannelId)
								{
									case Constants.PushChannels.Messages:
										ServiceRef.PlatformSpecific.ShowMessageNotification(Title, Body, Message.Data);
										break;

									case Constants.PushChannels.Petitions:
										ServiceRef.PlatformSpecific.ShowPetitionNotification(Title, Body, Message.Data);
										break;

									case Constants.PushChannels.Identities:
										ServiceRef.PlatformSpecific.ShowIdentitiesNotification(Title, Body, Message.Data);
										break;

									case Constants.PushChannels.Contracts:
										ServiceRef.PlatformSpecific.ShowContractsNotification(Title, Body, Message.Data);
										break;

									case Constants.PushChannels.EDaler:
										ServiceRef.PlatformSpecific.ShowEDalerNotification(Title, Body, Message.Data);
										break;

									case Constants.PushChannels.Tokens:
										ServiceRef.PlatformSpecific.ShowTokenNotification(Title, Body, Message.Data);
										break;

									case Constants.PushChannels.Provisioning:
										ServiceRef.PlatformSpecific.ShowProvisioningNotification(Title, Body, Message.Data);
										break;

									default:
										ServiceRef.PlatformSpecific.ShowIdentitiesNotification(Title, Body, Message.Data);
										break;
								}
							}
							catch (Exception)
							{
								// ignored
							}
						};
					}
					));
#endif
			});

			return Builder;
		}
	}
}
