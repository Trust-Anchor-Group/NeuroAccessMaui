using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Chat.Events;
using NeuroAccessMaui.Services.Chat.Models;
using NeuroAccessMaui.Services;
using NeuroAccessMaui;
using NeuroAccessMaui.Extensions;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Placeholder transport adapter that will later wrap XMPP operations.
	/// </summary>
	[Singleton]
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

			string identifier = Message.LocalTempId;
			if (string.IsNullOrEmpty(identifier))
				identifier = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);

			string xml = this.BuildContentXml(Message, replaceId: null);
			string body = Message.PlainText ?? string.Empty;

			ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged, Waher.Networking.XMPP.MessageType.Chat, identifier,
				Message.RemoteBareJid, xml, body, string.Empty, string.Empty, string.Empty, string.Empty, null, null);

			return identifier;
		}

		public async Task SendCorrectionAsync(string RemoteBareJid, string RemoteObjectId, ChatOutboundMessage Message, CancellationToken CancellationToken)
		{
			await this.EnsureSessionAsync(RemoteBareJid, CancellationToken).ConfigureAwait(false);

			string identifier = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
			string xml = this.BuildContentXml(Message, RemoteObjectId);
			string body = Message.PlainText ?? string.Empty;

			ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged, Waher.Networking.XMPP.MessageType.Chat, identifier,
				RemoteBareJid, xml, body, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
		}

		public Task AcknowledgeAsync(string RemoteBareJid, string RemoteObjectId, CancellationToken CancellationToken)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				throw new ArgumentException("Remote bare JID is required.", nameof(RemoteBareJid));

			if (string.IsNullOrEmpty(RemoteObjectId))
				throw new ArgumentException("Remote object id is required.", nameof(RemoteObjectId));

			return this.SendReceiptAsync(RemoteBareJid, RemoteObjectId, CancellationToken);
		}

		private async Task SendReceiptAsync(string RemoteBareJid, string RemoteObjectId, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			await this.EnsureSessionAsync(RemoteBareJid, CancellationToken).ConfigureAwait(false);

			string identifier = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
			string xml = "<received xmlns='urn:xmpp:receipts' id='" + XML.Encode(RemoteObjectId) + "'/>";

			ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged, Waher.Networking.XMPP.MessageType.Chat, identifier,
				RemoteBareJid, xml, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
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
			await ServiceRef.XmppService.SendChatStateAsync(RemoteBareJid, State, CancellationToken).ConfigureAwait(false);
		}

		public bool IsChatStateSupported(string RemoteBareJid)
		{
			if (string.IsNullOrEmpty(RemoteBareJid))
				return false;

			return ServiceRef.XmppService.IsChatStateSupported(RemoteBareJid);
		}

		private string BuildContentXml(ChatOutboundMessage Message, string? replaceId)
		{
			StringBuilder xml = new StringBuilder();

			if (!string.IsNullOrWhiteSpace(Message.Markdown))
			{
				xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\">");
				xml.Append(XML.Encode(Message.Markdown));
				xml.Append("</content>");
			}

			if (!string.IsNullOrWhiteSpace(Message.Html))
			{
				xml.Append("<html xmlns='http://jabber.org/protocol/xhtml-im'><body xmlns='http://www.w3.org/1999/xhtml'>");
				xml.Append(Message.Html);
				xml.Append("</body></html>");
			}

			if (!string.IsNullOrWhiteSpace(replaceId))
			{
				xml.Append("<replace id=\"");
				xml.Append(XML.Encode(replaceId));
				xml.Append("\" xmlns='urn:xmpp:message-correct:0'/> ");
			}

			xml.Append("<request xmlns='urn:xmpp:receipts'/>");

			return xml.ToString();
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
