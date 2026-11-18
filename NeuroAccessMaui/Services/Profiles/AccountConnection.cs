using System.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Avatar;

namespace NeuroAccessMaui.Services.Profiles
{
	/// <summary>
	/// The connection the user's card, contact photos and the contacts backup use: the XMPP client, and the client that
	/// exchanges photos with other phones. Set when the connection is made, and cleared when it is closed.
	/// </summary>
	public static class AccountConnection
	{
		private static readonly object synch = new();
		private static XmppClient? client;
		private static AvatarClient? avatars;

		/// <summary>
		/// Raised when a connection has been attached and is ready to use.
		/// </summary>
		public static event EventHandler? Connected;

		/// <summary>
		/// Starts using a new connection.
		/// </summary>
		/// <param name="Client">The XMPP client.</param>
		/// <param name="Avatars">The client that exchanges photos with other phones.</param>
		public static void Attach(XmppClient Client, AvatarClient Avatars)
		{
			lock (synch)
			{
				client = Client;
				avatars = Avatars;
			}

			Client.OnStateChanged += Client_OnStateChanged;
		}

		/// <summary>
		/// Stops using the connection, when it is closed.
		/// </summary>
		public static void Detach()
		{
			XmppClient? Old;

			lock (synch)
			{
				Old = client;
				client = null;
				avatars = null;
			}

			if (Old is not null)
				Old.OnStateChanged -= Client_OnStateChanged;
		}

		/// <summary>
		/// The client that exchanges photos with other phones, if connected.
		/// </summary>
		public static AvatarClient? Avatars
		{
			get
			{
				lock (synch)
				{
					return avatars;
				}
			}
		}

		/// <summary>
		/// If the app is connected now.
		/// </summary>
		public static bool IsConnected
		{
			get
			{
				lock (synch)
				{
					return client is not null && client.State == XmppState.Connected;
				}
			}
		}

		/// <summary>
		/// The user's own chat address.
		/// </summary>
		public static string? OwnAddress
		{
			get
			{
				lock (synch)
				{
					return client?.BareJID;
				}
			}
		}

		/// <summary>
		/// Sends a request to the user's own account on the server, and waits for the answer.
		/// </summary>
		/// <param name="Set">If the request changes something, rather than reading it.</param>
		/// <param name="Xml">The request.</param>
		/// <returns>The first element of the answer, or null if the answer was empty.</returns>
		/// <exception cref="InvalidOperationException">If the app is not connected.</exception>
		/// <exception cref="Exception">The error the server answered with.</exception>
		public static async Task<XmlElement?> RequestOwnAccountAsync(bool Set, string Xml)
		{
			XmppClient Client;

			lock (synch)
			{
				if (client is null || client.State != XmppState.Connected)
					throw new InvalidOperationException("Not connected.");

				Client = client;
			}

			// The client throws the server's error, if the server answers with one.
			return Set ?
				await Client.IqSetAsync(Client.BareJID, Xml) :
				await Client.IqGetAsync(Client.BareJID, Xml);
		}

		/// <summary>
		/// Sends the user's presence again, so others see a changed photo at once.
		/// </summary>
		public static async Task ResendPresenceAsync()
		{
			XmppClient? Client;

			lock (synch)
			{
				Client = client;
			}

			if (Client is not null && Client.State == XmppState.Connected)
				await Client.SetPresenceAsync(Availability.Online);
		}

		private static Task Client_OnStateChanged(object? Sender, XmppState NewState)
		{
			if (NewState == XmppState.Connected)
			{
				try
				{
					Connected?.Invoke(null, EventArgs.Empty);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			return Task.CompletedTask;
		}
	}
}
