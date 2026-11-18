using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Events;
using Waher.Networking.XMPP.MUC;
using Waher.Persistence;
using Waher.Persistence.Filters;
using ChatMessageType = NeuroAccessMaui.UI.Pages.Contacts.Chat.MessageType;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.Services.Groups
{
	public static partial class GroupService
	{
		private const string namespaceAddresses = "http://jabber.org/protocol/address";
		private const string inviteCorrelationPrefix = "GroupInvite:";

		#region Messages

		private static async Task Groups_RoomOccupantMessage(object? Sender, RoomOccupantMessageEventArgs e)
		{
			try
			{
				await OnGroupMessageAsync(e);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Stores a message from a group. Messages the app already has are skipped: the group sends the user's own messages
		/// back, and sends missed messages again when the app joins.
		/// </summary>
		private static async Task OnGroupMessageAsync(RoomOccupantMessageEventArgs e)
		{
			string Address = e.RoomId + "@" + e.Domain;
			ChatGroup? Group = await GroupStore.FindAsync(Address);
			if (Group is null || Group.Status != GroupStatus.Joined)
				return;

			ChatMessage Message = new()
			{
				Created = DateTime.UtcNow,
				RemoteBareJid = Address,
				RemoteObjectId = e.Id,
				MessageType = ChatMessageType.Received,
				PlainText = e.Body ?? string.Empty,
				Markdown = string.Empty,
				Html = string.Empty,
				SenderName = e.NickName
			};

			string? ReplaceId = null;
			ReadContent(e.Message, Message, ref ReplaceId);

			if (string.IsNullOrWhiteSpace(Message.PlainText) && string.IsNullOrWhiteSpace(Message.Markdown) &&
				string.IsNullOrWhiteSpace(Message.Html))
			{
				return;
			}

			await CompleteTextAsync(Message);

			string Own = ServiceRef.XmppService.BareJid ?? string.Empty;
			bool FromMe = string.Equals(Message.SenderAddress, Own, StringComparison.OrdinalIgnoreCase) ||
				(string.IsNullOrEmpty(Message.SenderAddress) && string.Equals(e.NickName, Group.MyName, StringComparison.Ordinal));

			if (await IsKnownAsync(Address, e.Id, ReplaceId, Message))
			{
				await NoteSeenAsync(Group, Message.Created);
				return;
			}

			if (FromMe)
			{
				Message.MessageType = ChatMessageType.Sent;
				Message.Delivered = Message.Created;
			}

			await Database.Insert(Message);

			// Someone who writes is a member, even if the app has not seen them come online.
			if (!FromMe && !string.IsNullOrEmpty(Message.SenderAddress) &&
				AddOrUpdateMember(Group, Message.SenderAddress, e.NickName, MemberRole(Group, Message.SenderAddress)))
			{
				Group.LastSeenMessageAt = Message.Created > Group.LastSeenMessageAt ? Message.Created : Group.LastSeenMessageAt;
				await GroupStore.SaveAsync(Group);
			}
			else
				await NoteSeenAsync(Group, Message.Created);

			ChatEvents.OnMessageStored(Message, false);

			if (!FromMe && !ChatEvents.IsChatVisible(Address) && !await ConversationStore.IsMutedAsync(Address))
				await NotifyAsync(Group, Message);
		}

		/// <summary>
		/// If the app already has a message: it is one of the user's own, sent back by the group, which then counts as sent;
		/// or one the group sent before; or a correction, which then updates the message it corrects.
		/// </summary>
		private static async Task<bool> IsKnownAsync(string Address, string? Id, string? ReplaceId, ChatMessage Message)
		{
			if (!string.IsNullOrEmpty(ReplaceId))
			{
				ChatMessage? Original = await FindMessageAsync(Address, ReplaceId);
				if (Original is not null)
				{
					Original.Updated = Message.Created;
					Original.PlainText = Message.PlainText;
					Original.Markdown = Message.Markdown;
					Original.Html = Message.Html;

					await Database.Update(Original);
					ChatEvents.OnMessageStored(Original, true);
				}

				return true;
			}

			if (string.IsNullOrEmpty(Id))
				return false;

			ChatMessage? Known = await FindMessageAsync(Address, Id);
			if (Known is null)
				return false;

			if (Known.MessageType == ChatMessageType.Sent && Known.Delivered == DateTime.MinValue)
			{
				Known.Delivered = DateTime.UtcNow;
				await Database.Update(Known);
				ChatEvents.OnMessageDelivered(Known);
			}

			return true;
		}

		/// <summary>
		/// Finds a message in a group by its ID: a sent message by its object ID, or a received one by the ID it came with.
		/// </summary>
		private static async Task<ChatMessage?> FindMessageAsync(string Address, string Id)
		{
			if (Guid.TryParse(Id, out _))
			{
				ChatMessage? Own = await Database.TryLoadObject<ChatMessage>(Id);
				if (Own is not null && string.Equals(Own.RemoteBareJid?.Value, Address, StringComparison.OrdinalIgnoreCase))
					return Own;
			}

			return await Database.FindFirstIgnoreRest<ChatMessage>(new FilterAnd(
				new FilterFieldEqualTo("RemoteBareJid", Address),
				new FilterFieldEqualTo("RemoteObjectId", Id)));
		}

		/// <summary>
		/// Reads the text of a group message in its formats, when it was sent if it came late, who sent it if the group
		/// tells, and which message it corrects, if any.
		/// </summary>
		private static void ReadContent(XmlElement Stanza, ChatMessage Message, ref string? ReplaceId)
		{
			foreach (XmlNode N in Stanza.ChildNodes)
			{
				if (N is not XmlElement E)
					continue;

				switch (E.LocalName)
				{
					case "content":
						if (E.NamespaceURI == ChatSender.NamespaceContent)
						{
							switch (XML.Attribute(E, "type"))
							{
								case "text/markdown":
									Message.Markdown = E.InnerText;
									break;

								case "text/plain":
									Message.PlainText = E.InnerText;
									break;
							}
						}
						break;

					case "replace":
						if (E.NamespaceURI == ChatSender.NamespaceMessageCorrection)
							ReplaceId = XML.Attribute(E, "id");
						break;

					case "delay":
						if (E.NamespaceURI == Waher.Networking.XMPP.PubSub.PubSubClient.NamespaceDelayedDelivery && E.HasAttribute("stamp") &&
							XML.TryParse(E.GetAttribute("stamp"), out DateTime Stamp))
						{
							Message.Created = Stamp.ToUniversalTime();
						}
						break;

					case "addresses":
						if (E.NamespaceURI == namespaceAddresses)
						{
							foreach (XmlNode N2 in E.ChildNodes)
							{
								if (N2 is XmlElement E2 && E2.LocalName == "address" && XML.Attribute(E2, "type") == "ofrom")
									Message.SenderAddress = XmppClient.GetBareJID(XML.Attribute(E2, "jid"));
							}
						}
						break;
				}
			}
		}

		/// <summary>
		/// Makes the plain text and HTML of a markdown message, where the sender left them out.
		/// </summary>
		private static async Task CompleteTextAsync(ChatMessage Message)
		{
			if (string.IsNullOrEmpty(Message.Markdown))
				return;

			try
			{
				MarkdownSettings Settings = new()
				{
					AllowScriptTag = false,
					EmbedEmojis = false,
					AudioAutoplay = false,
					AudioControls = false,
					ParseMetaData = false,
					VideoAutoplay = false,
					VideoControls = false
				};

				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Message.Markdown, Settings);

				if (string.IsNullOrEmpty(Message.PlainText))
					Message.PlainText = (await Doc.GeneratePlainText()).Trim();

				Message.Html = HtmlDocument.GetBody(await Doc.GenerateHTML());
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				Message.Markdown = string.Empty;
			}
		}

		/// <summary>
		/// Remembers when the latest message in a group came, so the next join only fetches newer ones.
		/// </summary>
		private static async Task NoteSeenAsync(ChatGroup Group, DateTime Created)
		{
			if (Created > Group.LastSeenMessageAt)
			{
				Group.LastSeenMessageAt = Created;
				await GroupStore.SaveAsync(Group, false);
			}
		}

		private static GroupRole MemberRole(ChatGroup Group, string Address)
		{
			return Group.Members?.FirstOrDefault(M => string.Equals(M.Address, Address, StringComparison.OrdinalIgnoreCase))?.Role ?? GroupRole.Member;
		}

		/// <summary>
		/// Shows a notification about a new message in a group.
		/// </summary>
		private static async Task NotifyAsync(ChatGroup Group, ChatMessage Message)
		{
			try
			{
				NotificationIntent Intent = new()
				{
					Channel = Constants.PushChannels.Messages,
					Title = Group.Name,
					Body = ServiceRef.Localizer[nameof(AppResources.GroupsNotificationBody), Message.SenderName ?? string.Empty],
					Action = NotificationAction.OpenChat,
					EntityId = Group.Address.Value,
					CorrelationId = Group.Address.Value,
					Presentation = NotificationPresentation.StoreOnly
				};

				await Notifications.AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion

		#region Invites

		private static Task Groups_DirectInvitationReceived(object? Sender, DirectInvitationMessageEventArgs e)
		{
			return OnInviteAsync(e.RoomId, e.Domain, e.FromBareJID, e.Reason);
		}

		private static Task Groups_RoomInvitationReceived(object? Sender, RoomInvitationMessageEventArgs e)
		{
			return OnInviteAsync(e.RoomId, e.Domain, XmppClient.GetBareJID(e.InviteFrom), e.Reason);
		}

		/// <summary>
		/// Keeps an invite to a group, shown at the top of Chats, and shows a notification. Invites from blocked people are
		/// dropped.
		/// </summary>
		private static async Task OnInviteAsync(string RoomId, string Domain, string From, string? Reason)
		{
			try
			{
				Contact? Inviter = await ContactStore.FindByChatAddressAsync(From);
				if (Inviter is not null && Inviter.IsBlocked)
					return;

				string Address = RoomId + "@" + Domain;
				ChatGroup? Group = await GroupStore.FindAsync(Address);

				if (Group is not null && Group.Status == GroupStatus.Joined)
					return;

				Group ??= new ChatGroup()
				{
					Address = Address
				};

				Group.Status = GroupStatus.Invited;
				Group.InvitedBy = From;
				Group.Created = DateTime.UtcNow;

				if (!string.IsNullOrWhiteSpace(Reason))
					Group.Name = Reason.Trim();
				else if (string.IsNullOrWhiteSpace(Group.Name))
					Group.Name = await TryGetNameAsync(Group) ?? RoomId;

				await GroupStore.SaveAsync(Group);

				string InviterName = Inviter?.DisplayName ?? From;
				NotificationIntent Intent = new()
				{
					Channel = Constants.PushChannels.Messages,
					Title = ServiceRef.Localizer[nameof(AppResources.GroupsInvitedYou), InviterName, Group.Name],
					Body = ServiceRef.Localizer[nameof(AppResources.GroupsInviteNotificationBody)],
					Action = NotificationAction.OpenChat,
					EntityId = Address,
					CorrelationId = inviteCorrelationPrefix + Address.ToLowerInvariant(),
					Presentation = NotificationPresentation.RenderAndStore
				};

				await Notifications.AddAsync(Intent, NotificationSource.Xmpp, null, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Removes the notification about an invite, once it is answered.
		/// </summary>
		private static async Task RemoveInviteNotificationAsync(string Address)
		{
			try
			{
				string Correlation = inviteCorrelationPrefix + Address.ToLowerInvariant();
				IReadOnlyList<NotificationRecord> Records = await Notifications.GetAsync(new NotificationQuery()
				{
					Channels = [Constants.PushChannels.Messages]
				}, CancellationToken.None);

				string[] Ids = [.. Records.Where(Record => Record.CorrelationId == Correlation).Select(Record => Record.Id)];
				if (Ids.Length > 0)
					await Notifications.DeleteAsync(Ids, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion

		#region Members coming and going

		/// <summary>
		/// Keeps the list of members and who is online up to date, and notices when the user is removed.
		/// </summary>
		private static async Task Groups_OccupantPresence(object? Sender, UserPresenceEventArgs e)
		{
			try
			{
				string Address = e.RoomId + "@" + e.Domain;
				ChatGroup? Group = await GroupStore.FindAsync(Address);
				if (Group is null || Group.Status != GroupStatus.Joined)
					return;

				bool Gone = e.Type == PresenceType.Unavailable;
				bool Removed = Gone && (e.HasStatus(MucStatus.Kicked) || e.HasStatus(MucStatus.Banned) ||
					e.HasStatus(MucStatus.RemovedDueToAffiliationChange) || e.HasStatus(MucStatus.RemovedDueToNonMembership));

				MarkOnline(Address, e.NickName, !Gone);

				if (e.HasStatus(MucStatus.OwnPresence) || string.Equals(e.NickName, Group.MyName, StringComparison.Ordinal))
				{
					if (Removed)
						Group.Status = GroupStatus.Left;
					else if (e.Affiliation.HasValue)
						Group.MyRole = RoleOf(e.Affiliation);

					await GroupStore.SaveAsync(Group);
					return;
				}

				string MemberAddress = string.IsNullOrEmpty(e.FullJid) ? string.Empty : XmppClient.GetBareJID(e.FullJid);
				bool Changed;

				if (Removed || e.Affiliation == Affiliation.Outcast)
				{
					int Before = Group.MemberCount;
					Group.Members = Group.Members?.Where(M => !(string.Equals(M.Name, e.NickName, StringComparison.Ordinal) ||
						(!string.IsNullOrEmpty(MemberAddress) && string.Equals(M.Address, MemberAddress, StringComparison.OrdinalIgnoreCase)))).ToArray();
					Changed = Group.MemberCount != Before;
				}
				else
					Changed = AddOrUpdateMember(Group, MemberAddress, e.NickName, RoleOf(e.Affiliation));

				if (Changed)
					await GroupStore.SaveAsync(Group);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Marks a group as left when its owner has deleted it.
		/// </summary>
		private static async Task Groups_RoomDestroyed(object? Sender, UserPresenceEventArgs e)
		{
			try
			{
				ChatGroup? Group = await GroupStore.FindAsync(e.RoomId + "@" + e.Domain);
				if (Group is null)
					return;

				Group.Status = GroupStatus.Left;
				await GroupStore.SaveAsync(Group);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion

		private static INotificationServiceV2 Notifications => ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
	}
}
