using Firebase.Messaging;
using global::Android.App;
using global::Android.Content;
using global::Android.Gms.Extensions;
using global::Android.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AndroidX.Core.App;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Xmpp;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;

using Application = Android.App.Application;

namespace NeuroAccessMaui.Platforms.Android
{


	[Service(Exported = false)]
	[IntentFilter([
		"com.google.firebase.MESSAGING_EVENT",
	"com.google.firebase.INSTANCE_ID_EVENT"
	])]
	public class MessagingService : FirebaseMessagingService
	{
		public override void OnMessageReceived(RemoteMessage Message)
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
		   catch (Exception ex)
		   {
   			//Log.Critical(ex);
		   }
		}




	}
}
