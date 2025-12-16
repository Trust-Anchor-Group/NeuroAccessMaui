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
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Xmpp;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

using Application = Android.App.Application;
using NeuroAccessMaui.Services.Push;

namespace NeuroAccessMaui.Platforms.Android
{


	[Service(Exported = false)]
	[IntentFilter(["com.google.firebase.MESSAGING_EVENT", "com.google.firebase.INSTANCE_ID_EVENT"])]
	public class MessagingService : FirebaseMessagingService
	{
		public override void OnMessageReceived(RemoteMessage Message)
		{
			_ = this.HandleMessageAsync(Message);
		}

		private async Task HandleMessageAsync(RemoteMessage Message)
		{
			try
			{
				Dictionary<string, string> Data = new Dictionary<string, string>(Message.Data, StringComparer.OrdinalIgnoreCase);
				string Title = Data.TryGetValue("myTitle", out string? T) ? T ?? string.Empty : string.Empty;
				string? Body = Data.TryGetValue("myBody", out string? B) ? B : null;
				string ChannelId = Data.TryGetValue("channelId", out string? C) ? C ?? Constants.PushChannels.Messages : Constants.PushChannels.Messages;

				NotificationIntent Intent = new NotificationIntent
				{
					Title = Title,
					Body = Body,
					Channel = ChannelId,
					Presentation = this.ResolvePresentation(Data, NotificationPresentation.RenderAndStore)
				};

				if (Data.TryGetValue("action", out string? ActionValue) && Enum.TryParse(ActionValue, out NotificationAction ParsedAction))
					Intent.Action = ParsedAction;

				if (Data.TryGetValue("entityId", out string? EntityId))
					Intent.EntityId = EntityId;

				if (Data.TryGetValue("correlationId", out string? CorrelationId))
					Intent.CorrelationId = CorrelationId;

				string Raw = JsonSerializer.Serialize(Data);

				IServiceProvider? Provider = ServiceRef.Provider;
				if (Provider is null)
				{
					return;
				}

				INotificationServiceV2 NotificationService = Provider.GetRequiredService<INotificationServiceV2>();
				await NotificationService.AddAsync(Intent, NotificationSource.Push, Raw, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private NotificationPresentation ResolvePresentation(Dictionary<string, string> Data, NotificationPresentation Current)
		{
			if (Data.TryGetValue("silent", out string? Silent) && this.IsTrue(Silent))
				return NotificationPresentation.StoreOnly;

			if (Data.TryGetValue("delivery.silent", out string? DeliverySilent) && this.IsTrue(DeliverySilent))
				return NotificationPresentation.StoreOnly;

			return Current;
		}

		private bool IsTrue(string? Value)
		{
			if (string.IsNullOrWhiteSpace(Value))
				return false;

			return Value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
				Value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
				Value.Equals("yes", StringComparison.OrdinalIgnoreCase);
		}



	}
}
