using System;
using System.Threading;
using Microsoft.Maui.Devices;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;
using Waher.Events;
using Waher.Networking.XMPP.Push;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Firebase Cloud Messaging transport adapter.
	/// </summary>
	public sealed class FirebasePushTransport : IPushTransport
	{
		private bool isInitialized;

		/// <summary>
		/// Raised when the transport receives a refreshed token.
		/// </summary>
		public event EventHandlerAsync<TokenInformation>? TokenChanged;

		/// <summary>
		/// Initializes the transport and wires platform callbacks.
		/// </summary>
		/// <param name="CancellationToken">Cancellation token.</param>
		public Task InitializeAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			if (this.isInitialized)
				return Task.CompletedTask;

			this.isInitialized = true;

			try
			{
				CrossFirebaseCloudMessaging.Current.TokenChanged += this.OnTokenChanged;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return Task.CompletedTask;
		}

		private async void OnTokenChanged(object? Sender, FCMTokenChangedEventArgs EventArgs)
		{
			try
			{
				string? Token = EventArgs?.Token;
				if (string.IsNullOrEmpty(Token))
					return;

				TokenInformation TokenInformation = new()
				{
					Token = Token,
					Service = PushMessagingService.Firebase,
					ClientType = ResolveClientType()
				};

				EventHandlerAsync<TokenInformation>? Handler = this.TokenChanged;
				if (Handler is not null)
					await Handler.Raise(this, TokenInformation);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private static ClientType ResolveClientType()
		{
			if (DeviceInfo.Platform == DevicePlatform.Android)
				return ClientType.Android;

			if (DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
				return ClientType.iOS;

			return ClientType.Other;
		}
	}
}
