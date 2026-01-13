using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Events;
using NeuroAccessMaui.Services.Chat.Models;
using NeuroAccessMaui.Services;
using NeuroAccessMaui;
using Waher.Networking.XMPP.Chat;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Transport adapter that wraps XMPP chat operations.
	/// </summary>
	internal class ChatTransportService : LoadableService, IChatTransportService
	{
		private event EventHandler<ChatMessageEventArgs>? messageReceived;
		private event EventHandler<ChatMessageEventArgs>? messageUpdated;
		private event EventHandler<ChatDeliveryReceiptEventArgs>? deliveryReceiptReceived;

		public event EventHandler<ChatMessageEventArgs> MessageReceived
		{
			add { this.messageReceived += value; }
			remove { this.messageReceived -= value; }
		}

		public event EventHandler<ChatMessageEventArgs> MessageUpdated
		{
			add { this.messageUpdated += value; }
			remove { this.messageUpdated -= value; }
		}

		public event EventHandler<ChatDeliveryReceiptEventArgs> DeliveryReceiptReceived
		{
			add { this.deliveryReceiptReceived += value; }
			remove { this.deliveryReceiptReceived -= value; }
		}

		public override Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		public override Task Unload()
		{
			if (this.BeginUnload())
			{
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		public async Task<string> SendAsync(ChatOutboundMessage Message, CancellationToken CancellationToken)
		{
			await this.EnsureSessionAsync(Message.RemoteBareJid, CancellationToken).ConfigureAwait(false);

			string Identifier = Message.LocalTempId ?? string.Empty;
			if (string.IsNullOrEmpty(Identifier))
				Identifier = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);

			string Body = Message.PlainText ?? string.Empty;
			ChatClient? ChatClientInstance = ServiceRef.XmppService.ChatClient;

			if (ChatClientInstance is not null)
			{
				await ChatClientInstance.SendChatContentMessage(Message.RemoteBareJid, Body, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, Identifier).ConfigureAwait(false);
				await ChatClientInstance.SendReceiptRequest(Message.RemoteBareJid, Identifier, MessageType.Chat).ConfigureAwait(false);
				await ChatClientInstance.SendMarkable(Message.RemoteBareJid, Identifier, MessageType.Chat).ConfigureAwait(false);
			}
			else
			{
				ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, Identifier,
					Message.RemoteBareJid, string.Empty, Body, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
			}

			return Identifier;
		}

		public async Task SendCorrectionAsync(string RemoteBareJid, string RemoteObjectId, ChatOutboundMessage Message, CancellationToken CancellationToken)
		{
			await this.EnsureSessionAsync(RemoteBareJid, CancellationToken).ConfigureAwait(false);

			string Body = Message.PlainText ?? string.Empty;
			ChatClient? ChatClientInstance = ServiceRef.XmppService.ChatClient;

			if (ChatClientInstance is not null)
			{
				await ChatClientInstance.SendMessageCorrection(RemoteBareJid, RemoteObjectId, Body, string.Empty, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);
				return;
			}

			string Identifier = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
			ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged, MessageType.Chat, Identifier,
				RemoteBareJid, string.Empty, Body, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
		}

		public Task AcknowledgeAsync(string RemoteBareJid, string RemoteObjectId, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (string.IsNullOrEmpty(RemoteObjectId))
				throw new ArgumentException("Remote object id is required.", nameof(RemoteObjectId));

			return this.SendReceiptAsync(RemoteBareJid, RemoteObjectId, CancellationToken);
		}

		/// <inheritdoc/>
		public async Task SendDisplayedMarkerAsync(string RemoteBareJid, string RemoteObjectId, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (string.IsNullOrEmpty(RemoteObjectId))
				throw new ArgumentException("Remote object id is required.", nameof(RemoteObjectId));

			CancellationToken.ThrowIfCancellationRequested();

			await this.EnsureSessionAsync(RemoteBareJid, CancellationToken).ConfigureAwait(false);

			ChatClient? ChatClientInstance = ServiceRef.XmppService.ChatClient;
			if (ChatClientInstance is not null)
			{
				await ChatClientInstance.SendDisplayedMarker(RemoteBareJid, RemoteObjectId, MessageType.Chat).ConfigureAwait(false);
				return;
			}
		}

		private async Task SendReceiptAsync(string RemoteBareJid, string RemoteObjectId, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			await this.EnsureSessionAsync(RemoteBareJid, CancellationToken).ConfigureAwait(false);

			ChatClient? ChatClientInstance = ServiceRef.XmppService.ChatClient;
			if (ChatClientInstance is not null)
			{
				await ChatClientInstance.SendReceiptReceived(RemoteBareJid, RemoteObjectId, MessageType.Chat).ConfigureAwait(false);
				return;
			}
		}

		public async Task EnsureSessionAsync(string RemoteBareJid, CancellationToken CancellationToken)
		{
			await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect).ConfigureAwait(false);
		}

		public async Task SendChatStateAsync(string RemoteBareJid, ChatState State, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			CancellationToken.ThrowIfCancellationRequested();

			await this.EnsureSessionAsync(RemoteBareJid, CancellationToken).ConfigureAwait(false);
			ChatClient? ChatClientInstance = ServiceRef.XmppService.ChatClient;
			if(ChatClientInstance is not null)
				await ChatClientInstance.SendChatState(RemoteBareJid, State);
		}

		public bool IsChatStateSupported(string RemoteBareJid)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				return false;
			ChatClient? ChatClientInstance = ServiceRef.XmppService.ChatClient;
			if (ChatClientInstance is not null)
				return ChatClientInstance.IsChatStateSupported(RemoteBareJid);
			return false;
		}

		protected void OnMessageReceived(ChatMessageDescriptor Descriptor)
		{
			EventHandler<ChatMessageEventArgs>? Handler = this.messageReceived;
			if (Handler is not null)
			{
				ChatMessageEventArgs Args = new ChatMessageEventArgs(Descriptor);
				Handler.Invoke(this, Args);
			}
		}

		protected void OnMessageUpdated(ChatMessageDescriptor Descriptor)
		{
			EventHandler<ChatMessageEventArgs>? Handler = this.messageUpdated;
			if (Handler is not null)
			{
				ChatMessageEventArgs Args = new ChatMessageEventArgs(Descriptor);
				Handler.Invoke(this, Args);
			}
		}

		protected void OnDeliveryReceipt(ChatDeliveryReceiptEventArgs Args)
		{
			EventHandler<ChatDeliveryReceiptEventArgs>? Handler = this.deliveryReceiptReceived;
			if (Handler is not null)
			{
				Handler.Invoke(this, Args);
			}
		}
	}
}
