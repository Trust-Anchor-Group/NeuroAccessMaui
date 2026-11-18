using System.Text;
using System.Xml;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.UI.Controls.Markdown;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Persistence;
using ChatMessageType = NeuroAccessMaui.UI.Pages.Contacts.Chat.MessageType;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Sends chat messages and chat states, and reads chat states from received messages.
	/// </summary>
	public static class ChatSender
	{
		/// <summary>
		/// Chat states (XEP-0085).
		/// </summary>
		public const string NamespaceChatStates = "http://jabber.org/protocol/chatstates";

		/// <summary>
		/// Last message correction (XEP-0308).
		/// </summary>
		public const string NamespaceMessageCorrection = "urn:xmpp:message-correct:0";

		/// <summary>
		/// Message content in other formats, such as markdown.
		/// </summary>
		public const string NamespaceContent = "urn:xmpp:content";

		/// <summary>
		/// XHTML-IM (XEP-0071).
		/// </summary>
		public const string NamespaceXhtmlIm = "http://jabber.org/protocol/xhtml-im";

		/// <summary>
		/// XHTML.
		/// </summary>
		public const string NamespaceXhtml = "http://www.w3.org/1999/xhtml";

		/// <summary>
		/// Message processing hints (XEP-0334).
		/// </summary>
		public const string NamespaceHints = "urn:xmpp:hints";

		/// <summary>
		/// Sends a markdown message, or corrects a message sent earlier, and stores it.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		/// <param name="Markdown">Markdown text.</param>
		/// <param name="ReplaceObjectId">Object ID of a sent message that the new text replaces, or null to send a new message.</param>
		/// <returns>The stored message, or null if there was nothing to send.</returns>
		public static async Task<ChatMessage?> SendAsync(string BareJid, string Markdown, string? ReplaceObjectId = null)
		{
			if (string.IsNullOrWhiteSpace(Markdown))
				return null;

			// Each new line typed is a line break, also in the text and HTML other clients get.
			Markdown = ChatMarkdown.KeepLineBreaks(Markdown);

			MarkdownDocument Doc = await MarkdownModel.ParseAsync(Markdown);
			string Html = HtmlDocument.GetBody(await Doc.GenerateHTML());
			string PlainText = (await Doc.GeneratePlainText()).Trim();
			DateTime Now = DateTime.UtcNow;

			ChatMessage? Original = null;

			if (!string.IsNullOrEmpty(ReplaceObjectId))
			{
				Original = await Database.TryLoadObject<ChatMessage>(ReplaceObjectId);

				if (Original is not null &&
					(Original.MessageType != ChatMessageType.Sent ||
					!string.Equals(Original.RemoteBareJid?.Value, BareJid, StringComparison.OrdinalIgnoreCase)))
				{
					Original = null;
				}
			}

			ChatMessage Message;
			string StanzaId;
			bool IsUpdate = Original is not null;
			bool ToSelf = IsSelf(BareJid);
			bool ToGroup = GroupStore.IsGroup(BareJid);

			if (Original is null)
			{
				Message = new ChatMessage()
				{
					Created = Now,
					RemoteBareJid = BareJid,
					RemoteObjectId = string.Empty,
					MessageType = ChatMessageType.Sent,
					Html = Html,
					PlainText = PlainText,
					Markdown = Markdown,
					Delivered = ToSelf ? Now : DateTime.MinValue
				};

				await Database.Insert(Message);
				StanzaId = Message.ObjectId ?? Guid.NewGuid().ToString();
			}
			else
			{
				Original.Updated = Now;
				Original.Html = Html;
				Original.PlainText = PlainText;
				Original.Markdown = Markdown;

				await Database.Update(Original);

				Message = Original;
				StanzaId = Guid.NewGuid().ToString();
			}

			if (ToSelf)
			{
				// Notes to yourself stay on the device. Sent to the own address, they would come back as received messages.
				ChatEvents.OnMessageStored(Message, IsUpdate);
				return Message;
			}

			StringBuilder Xml = new();

			Xml.Append("<content xmlns='");
			Xml.Append(NamespaceContent);
			Xml.Append("' type='text/markdown'>");
			Xml.Append(XML.Encode(Markdown));
			Xml.Append("</content>");

			AppendXhtml(Xml, Html);

			if (IsUpdate)
			{
				// Corrections always point to the first message, whose stanza ID is its object ID.
				Xml.Append("<replace id='");
				Xml.Append(XML.Encode(Message.ObjectId ?? string.Empty));
				Xml.Append("' xmlns='");
				Xml.Append(NamespaceMessageCorrection);
				Xml.Append("'/>");
			}
			else if (!ToGroup)
			{
				Xml.Append("<request xmlns='");
				Xml.Append(XmppClient.NamespaceMessageDeliveryReceipts);
				Xml.Append("'/>");
			}

			// Groups get no delivery receipts or chat states. A message counts as sent when the group sends it back.
			if (!ToGroup)
			{
				Xml.Append("<active xmlns='");
				Xml.Append(NamespaceChatStates);
				Xml.Append("'/>");
			}

			ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged,
				ToGroup ? Waher.Networking.XMPP.MessageType.GroupChat : Waher.Networking.XMPP.MessageType.Chat, StanzaId, BareJid,
				Xml.ToString(), PlainText, string.Empty, string.Empty, string.Empty, string.Empty, null, null);

			ChatEvents.OnMessageStored(Message, IsUpdate);

			return Message;
		}

		/// <summary>
		/// If <paramref name="BareJid"/> is the user's own address, so the chat holds notes to yourself.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		/// <returns>If the chat is with the user.</returns>
		public static bool IsSelf(string? BareJid)
		{
			if (string.IsNullOrEmpty(BareJid))
				return false;

			string Own = ServiceRef.XmppService.BareJid;
			return !string.IsNullOrEmpty(Own) && string.Equals(BareJid, Own, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Adds the HTML of a message as XHTML-IM, for clients that don't read markdown.
		/// </summary>
		private static void AppendXhtml(StringBuilder Xml, string Html)
		{
			if (string.IsNullOrWhiteSpace(Html))
				return;

			try
			{
				StringBuilder Body = new();
				HtmlDocument HtmlDoc = new("<root>" + Html + "</root>");

				foreach (HtmlNode N in (HtmlDoc.Body ?? HtmlDoc.Root).Children)
					N.Export(Body);

				Xml.Append("<html xmlns='");
				Xml.Append(NamespaceXhtmlIm);
				Xml.Append("'><body xmlns='");
				Xml.Append(NamespaceXhtml);
				Xml.Append("'>");
				Xml.Append(Body);
				Xml.Append("</body></html>");
			}
			catch (Exception Ex)
			{
				// The markdown and plain text are still sent.
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Tells the other party if the user is writing. Only sent to parties that have sent chat states themselves, since
		/// older versions of the app would show such a message as an empty message.
		/// </summary>
		/// <param name="BareJid">Address of the other party.</param>
		/// <param name="State">What the user is doing.</param>
		public static void SendChatState(string BareJid, ChatState State)
		{
			if (!ChatEvents.UsesChatStates(BareJid) || IsSelf(BareJid) || !ServiceRef.XmppService.IsOnline)
				return;

			try
			{
				StringBuilder Xml = new();

				Xml.Append('<');
				Xml.Append(GetElementName(State));
				Xml.Append(" xmlns='");
				Xml.Append(NamespaceChatStates);
				Xml.Append("'/><no-store xmlns='");
				Xml.Append(NamespaceHints);
				Xml.Append("'/>");

				ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged, Waher.Networking.XMPP.MessageType.Chat, string.Empty, BareJid,
					Xml.ToString(), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Gets the chat state in a received message, if any.
		/// </summary>
		/// <param name="Message">Message stanza.</param>
		/// <returns>Chat state, or null if the message has none.</returns>
		public static ChatState? GetChatState(XmlElement Message)
		{
			foreach (XmlNode N in Message.ChildNodes)
			{
				if (N is XmlElement E && E.NamespaceURI == NamespaceChatStates)
				{
					switch (E.LocalName)
					{
						case "active":
							return ChatState.Active;

						case "composing":
							return ChatState.Composing;

						case "paused":
							return ChatState.Paused;

						case "inactive":
							return ChatState.Inactive;

						case "gone":
							return ChatState.Gone;
					}
				}
			}

			return null;
		}

		private static string GetElementName(ChatState State)
		{
			return State switch
			{
				ChatState.Composing => "composing",
				ChatState.Paused => "paused",
				ChatState.Inactive => "inactive",
				ChatState.Gone => "gone",
				_ => "active"
			};
		}
	}
}
