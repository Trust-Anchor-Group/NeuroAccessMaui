using System.Globalization;
using System.Text;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.Services.Contacts;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.DataForms;
using Waher.Networking.XMPP.Events;
using Waher.Networking.XMPP.MUC;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Groups
{
	/// <summary>
	/// Works with groups on the server's group service: creates them, adds and removes people, joins them again after
	/// every reconnect with the messages missed, and takes in invites and group messages.
	/// </summary>
	public static partial class GroupService
	{
		/// <summary>
		/// The protocol of group services (XEP-0045).
		/// </summary>
		public const string NamespaceGroups = "http://jabber.org/protocol/muc";

		/// <summary>
		/// The most members a new group has room for.
		/// </summary>
		public const int MemberLimit = 20;

		private const string serviceSetting = "GROUPS.SERVICE_ADDRESS";
		private const int firstJoinHistory = 50;
		private static readonly TimeSpan joinTimeout = TimeSpan.FromSeconds(30);

		private static readonly object synch = new();
		private static readonly Dictionary<string, HashSet<string>> onlineByGroup = new(StringComparer.OrdinalIgnoreCase);
		private static XmppClient? xmpp;
		private static MultiUserChatClient? groups;
		private static string? serviceAddress;

		#region Attaching

		/// <summary>
		/// Starts working with groups on a new connection. Called before the connection is made, so invites and messages that
		/// arrive at once are not missed.
		/// </summary>
		/// <param name="Client">The connection.</param>
		public static void Attach(XmppClient Client)
		{
			Detach();

			MultiUserChatClient Groups = new(Client, serviceAddress ?? ("muc." + ServiceRef.TagProfile.Domain));

			Groups.RoomOccupantMessage += Groups_RoomOccupantMessage;
			Groups.DirectInvitationReceived += Groups_DirectInvitationReceived;
			Groups.RoomInvitationReceived += Groups_RoomInvitationReceived;
			Groups.OccupantPresence += Groups_OccupantPresence;
			Groups.RoomDestroyed += Groups_RoomDestroyed;
			Client.OnStateChanged += Client_OnStateChanged;

			lock (synch)
			{
				xmpp = Client;
				groups = Groups;
				onlineByGroup.Clear();
			}

			_ = GroupStore.LoadAsync();
		}

		/// <summary>
		/// Stops working with groups on the connection, when it is closed.
		/// </summary>
		public static void Detach()
		{
			XmppClient? Client;
			MultiUserChatClient? Groups;

			lock (synch)
			{
				Client = xmpp;
				Groups = groups;
				xmpp = null;
				groups = null;
				onlineByGroup.Clear();
			}

			if (Client is not null)
				Client.OnStateChanged -= Client_OnStateChanged;

			if (Groups is not null)
			{
				Groups.RoomOccupantMessage -= Groups_RoomOccupantMessage;
				Groups.DirectInvitationReceived -= Groups_DirectInvitationReceived;
				Groups.RoomInvitationReceived -= Groups_RoomInvitationReceived;
				Groups.OccupantPresence -= Groups_OccupantPresence;
				Groups.RoomDestroyed -= Groups_RoomDestroyed;

				try
				{
					Groups.Dispose();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}
		}

		/// <summary>
		/// If groups can be used now: the app is connected.
		/// </summary>
		public static bool IsAvailable
		{
			get
			{
				lock (synch)
				{
					return groups is not null && xmpp is not null && xmpp.State == XmppState.Connected;
				}
			}
		}

		/// <summary>
		/// How many members of a group are online now, as far as the app has seen.
		/// </summary>
		/// <param name="Address">The group's address.</param>
		/// <returns>The number of members online, the user included.</returns>
		public static int OnlineCount(string Address)
		{
			lock (synch)
			{
				return onlineByGroup.TryGetValue(Address, out HashSet<string>? Online) ? Online.Count : 0;
			}
		}

		private static MultiUserChatClient RequireGroups()
		{
			lock (synch)
			{
				if (groups is null || xmpp is null || xmpp.State != XmppState.Connected)
					throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsOffline)].Value);

				return groups;
			}
		}

		private static Task Client_OnStateChanged(object? Sender, XmppState NewState)
		{
			if (NewState == XmppState.Connected)
				_ = Task.Run(OnConnectedAsync);

			return Task.CompletedTask;
		}

		/// <summary>
		/// After every connect: finds the group service, and joins each group again, fetching the messages missed.
		/// </summary>
		private static async Task OnConnectedAsync()
		{
			try
			{
				await GroupStore.LoadAsync();
				await GetServiceAddressAsync();

				foreach (ChatGroup Group in await GroupStore.GetAllAsync())
				{
					if (Group.Status == GroupStatus.Joined)
						await JoinAgainAsync(Group);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Joins a group again after a reconnect. If the user is no longer allowed in, the group is marked as left.
		/// </summary>
		private static async Task JoinAgainAsync(ChatGroup Group)
		{
			try
			{
				PresenceEventArgs Result = await EnterAsync(Group, false);

				if (!Result.Ok && (Result.StanzaError is RegistrationRequiredException || Result.StanzaError is ForbiddenException ||
					Result.StanzaError is ItemNotFoundException))
				{
					Group.Status = GroupStatus.Left;
					await GroupStore.SaveAsync(Group);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#endregion

		#region The group service

		/// <summary>
		/// Gets the address of the server's group service. It is looked for among the server's services, and remembered.
		/// If the server does not list one, the usual address is used.
		/// </summary>
		/// <returns>The address of the group service.</returns>
		public static async Task<string> GetServiceAddressAsync()
		{
			if (!string.IsNullOrEmpty(serviceAddress))
				return serviceAddress;

			string Saved = await RuntimeSettings.GetAsync(serviceSetting, string.Empty);
			string? Found = await DiscoverServiceAsync();

			if (Found is not null)
			{
				if (!string.Equals(Found, Saved, StringComparison.OrdinalIgnoreCase))
					await RuntimeSettings.SetAsync(serviceSetting, Found);

				serviceAddress = Found;
			}
			else if (!string.IsNullOrEmpty(Saved))
				serviceAddress = Saved;
			else
				serviceAddress = "muc." + ServiceRef.TagProfile.Domain;

			return serviceAddress;
		}

		/// <summary>
		/// Looks for the group service among the server's services.
		/// </summary>
		private static async Task<string?> DiscoverServiceAsync()
		{
			XmppClient? Client;

			lock (synch)
			{
				Client = xmpp;
			}

			if (Client is null || Client.State != XmppState.Connected)
				return null;

			try
			{
				ServiceItemsDiscoveryEventArgs Items = await Client.ServiceItemsDiscoveryAsync(null, string.Empty, string.Empty);
				if (!Items.Ok)
					return null;

				foreach (Item Item in Items.Items)
				{
					ServiceDiscoveryEventArgs Info = await Client.ServiceDiscoveryAsync(null, Item.JID, Item.Node);
					if (Info.Ok && Info.HasFeature(NamespaceGroups))
						return Item.JID;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			return null;
		}

		#endregion

		#region Joining

		/// <summary>
		/// Enters a group, asking for the messages since the latest one the app has, or the latest ones the first time.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="FirstTime">If the user joins the group for the first time.</param>
		/// <returns>The group's answer.</returns>
		private static async Task<PresenceEventArgs> EnterAsync(ChatGroup Group, bool FirstTime)
		{
			XmppClient Client;

			lock (synch)
			{
				Client = xmpp ?? throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsOffline)].Value);
			}

			StringBuilder Xml = new();

			Xml.Append("<x xmlns='");
			Xml.Append(NamespaceGroups);
			Xml.Append("'><history");

			if (FirstTime || Group.LastSeenMessageAt == DateTime.MinValue)
			{
				Xml.Append(" maxstanzas='");
				Xml.Append(firstJoinHistory.ToString(CultureInfo.InvariantCulture));
				Xml.Append('\'');
			}
			else
			{
				Xml.Append(" since='");
				Xml.Append(XML.Encode(Group.LastSeenMessageAt.ToUniversalTime()));
				Xml.Append('\'');
			}

			Xml.Append("/></x>");

			TaskCompletionSource<PresenceEventArgs> Result = new();

			await Client.SendDirectedPresence(string.Empty, Group.Address.Value + "/" + Group.MyName, Xml.ToString(), (Sender, e) =>
			{
				Result.TrySetResult(e);
				return Task.CompletedTask;
			}, null);

			Task Done = await Task.WhenAny(Result.Task, Task.Delay(joinTimeout));
			if (Done != Result.Task)
				throw new TimeoutException(ServiceRef.Localizer[nameof(AppResources.GroupsNoAnswer)].Value);

			PresenceEventArgs Response = await Result.Task;

			if (!Response.Ok && Response.StanzaError is ConflictException && !Group.MyName.EndsWith(')'))
			{
				// Someone else in the group uses the name already.
				Group.MyName += " (" + Random.Shared.Next(2, 100).ToString(CultureInfo.InvariantCulture) + ")";
				await GroupStore.SaveAsync(Group);
				return await EnterAsync(Group, FirstTime);
			}

			if (Response.Ok)
				MarkOnline(Group.Address.Value, Group.MyName, true);

			return Response;
		}

		/// <summary>
		/// Joins a group the user has been invited to.
		/// </summary>
		/// <param name="Group">The group.</param>
		public static async Task JoinAsync(ChatGroup Group)
		{
			RequireGroups();

			Group.MyName = string.IsNullOrWhiteSpace(Group.MyName) ? await MyDefaultNameAsync() : Group.MyName;
			Group.Status = GroupStatus.Joined;

			PresenceEventArgs Result = await EnterAsync(Group, true);
			if (!Result.Ok)
			{
				Group.Status = GroupStatus.Invited;
				throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsCantJoin)].Value);
			}

			Group.InvitedBy = string.Empty;
			AddOrUpdateMember(Group, ServiceRef.XmppService.BareJid, Group.MyName, Group.MyRole);
			await GroupStore.SaveAsync(Group);
			await ConversationStore.EnsureGroupAsync(Group.Address.Value, Group.Name);
			await RemoveInviteNotificationAsync(Group.Address.Value);
		}

		/// <summary>
		/// Turns down an invite. The group is removed from the phone.
		/// </summary>
		/// <param name="Group">The group.</param>
		public static async Task DeclineAsync(ChatGroup Group)
		{
			await GroupStore.DeleteAsync(Group);
			await RemoveInviteNotificationAsync(Group.Address.Value);
		}

		/// <summary>
		/// Joins a group from its invite link. The group is added on the phone first.
		/// </summary>
		/// <param name="Address">The group's address.</param>
		/// <returns>The group.</returns>
		public static async Task<ChatGroup> JoinByLinkAsync(string Address)
		{
			ChatGroup Group = await GroupStore.FindAsync(Address) ?? new ChatGroup()
			{
				Address = Address,
				Status = GroupStatus.Invited
			};

			if (Group.Status == GroupStatus.Joined)
				return Group;

			if (string.IsNullOrWhiteSpace(Group.Name))
				Group.Name = await TryGetNameAsync(Group) ?? Address;

			await JoinAsync(Group);
			return Group;
		}

		/// <summary>
		/// The name the user gets in a new group: the first name in their ID, or else the first part of their chat address.
		/// </summary>
		private static Task<string> MyDefaultNameAsync()
		{
			string? FirstName = ServiceRef.TagProfile.LegalIdentity?[Constants.XmppProperties.FirstName];
			if (!string.IsNullOrWhiteSpace(FirstName))
				return Task.FromResult(FirstName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]);

			string Own = ServiceRef.XmppService.BareJid ?? string.Empty;
			int i = Own.IndexOf('@');
			return Task.FromResult(i > 0 ? Own[..i] : ServiceRef.Localizer[nameof(AppResources.ChatMe)].Value);
		}

		/// <summary>
		/// Asks the group service for a group's name.
		/// </summary>
		private static async Task<string?> TryGetNameAsync(ChatGroup Group)
		{
			try
			{
				RoomInformationEventArgs Info = await RequireGroups().GetRoomInformationAsync(Group.RoomId, Group.Domain);
				string? Name = Info.Ok ? Info.Identities?.FirstOrDefault()?.Name : null;
				return string.IsNullOrWhiteSpace(Name) ? null : Name;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}

		#endregion

		#region Creating and changing

		/// <summary>
		/// Creates a group with the user as owner, sets it up, and adds people to it. Each person gets an invite.
		/// </summary>
		/// <param name="Name">The group's name.</param>
		/// <param name="Description">What the group is for.</param>
		/// <param name="JoinRule">Who can join.</param>
		/// <param name="People">The chat addresses of the people to add.</param>
		/// <returns>The group.</returns>
		public static async Task<ChatGroup> CreateAsync(string Name, string Description, GroupJoinRule JoinRule, IReadOnlyList<string> People)
		{
			MultiUserChatClient Groups = RequireGroups();
			string Service = await GetServiceAddressAsync();
			string RoomId = Guid.NewGuid().ToString("N")[..16];

			ChatGroup Group = new()
			{
				Address = RoomId + "@" + Service,
				Name = Name.Trim(),
				Description = Description.Trim(),
				MyName = await MyDefaultNameAsync(),
				MyRole = GroupRole.Owner,
				Status = GroupStatus.Joined,
				JoinRule = JoinRule,
				Created = DateTime.UtcNow
			};

			PresenceEventArgs Entered = await EnterAsync(Group, false);
			if (!Entered.Ok)
				throw (Exception?)Entered.StanzaError ?? new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsCantCreate)].Value);

			DataForm Settings = await Groups.GetRoomConfigurationAsync(RoomId, Service);
			await SetAsync(Settings, "muc#roomconfig_roomname", Group.Name);
			await SetAsync(Settings, "muc#roomconfig_roomdesc", Group.Description);
			await SetAsync(Settings, "muc#roomconfig_persistentroom", "1");
			await SetAsync(Settings, "muc#roomconfig_membersonly", JoinRule == GroupJoinRule.MembersOnly ? "1" : "0");
			await SetAsync(Settings, "muc#roomconfig_publicroom", "0");
			await SetAsync(Settings, "muc#roomconfig_enablelogging", "1");
			await SetAsync(Settings, "muc#roomconfig_passwordprotectedroom", "0");
			await SetAsync(Settings, "muc#roomconfig_moderatedroom", "0");
			await SetAsync(Settings, "muc#roomconfig_allowinvites", "0");
			await SetAsync(Settings, "muc#roomconfig_changesubject", "0");
			await SetAsync(Settings, "muc#roomconfig_allowpm", "none");
			await SetAsync(Settings, "muc#roomconfig_whois", "anyone");

			// The service fills the member limit from another setting, so it is always set here.
			await SetAsync(Settings, "muc#roomconfig_maxusers", MemberLimit.ToString(CultureInfo.InvariantCulture));

			await Groups.SubmitConfigurationForm(Settings);

			AddOrUpdateMember(Group, ServiceRef.XmppService.BareJid, Group.MyName, GroupRole.Owner);
			await GroupStore.SaveAsync(Group);
			await ConversationStore.EnsureGroupAsync(Group.Address.Value, Group.Name);

			if (People.Count > 0)
				await AddPeopleAsync(Group, People);

			return Group;
		}

		/// <summary>
		/// Adds people to a group. Admins make them members and invite them; members, where the group lets them, ask the
		/// group to invite them.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="People">The chat addresses of the people to add.</param>
		public static async Task AddPeopleAsync(ChatGroup Group, IReadOnlyList<string> People)
		{
			MultiUserChatClient Groups = RequireGroups();

			if (Group.CanManage)
			{
				MucOccupantConfiguration[] Changes = [.. People.Select(Address => new MucOccupantConfiguration(Address, Affiliation.Member))];
				IqResultEventArgs Result = await Groups.ConfigureOccupantsAsync(Group.RoomId, Group.Domain, Changes);
				if (!Result.Ok)
					throw (Exception?)Result.StanzaError ?? new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsCantChange)].Value);

				foreach (string Address in People)
					await Groups.InviteDirect(Group.RoomId, Group.Domain, Address, Group.Name);
			}
			else
			{
				foreach (string Address in People)
					await Groups.Invite(Group.RoomId, Group.Domain, Address, Group.Name);
			}

			foreach (string Address in People)
				AddOrUpdateMember(Group, Address, await ContactStore.TryGetNameAsync(Address) ?? string.Empty, GroupRole.Member);

			await GroupStore.SaveAsync(Group);
		}

		/// <summary>
		/// Removes a person from a group.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="Address">The person's chat address.</param>
		public static async Task RemoveAsync(ChatGroup Group, string Address)
		{
			IqResultEventArgs Result = await RequireGroups().RevokeMembershipAsync(Group.RoomId, Group.Domain, Address);
			if (!Result.Ok)
				throw (Exception?)Result.StanzaError ?? new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsCantChange)].Value);

			Group.Members = Group.Members?.Where(Member => !string.Equals(Member.Address, Address, StringComparison.OrdinalIgnoreCase)).ToArray();
			await GroupStore.SaveAsync(Group);
		}

		/// <summary>
		/// Makes a member an admin.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="Address">The member's chat address.</param>
		public static async Task MakeAdminAsync(ChatGroup Group, string Address)
		{
			IqResultEventArgs Result = await RequireGroups().GrantAdministratorAsync(Group.RoomId, Group.Domain, Address);
			if (!Result.Ok)
				throw (Exception?)Result.StanzaError ?? new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsCantChange)].Value);

			GroupMember? Member = Group.Members?.FirstOrDefault(M => string.Equals(M.Address, Address, StringComparison.OrdinalIgnoreCase));
			if (Member is not null)
				Member.Role = GroupRole.Admin;

			await GroupStore.SaveAsync(Group);
		}

		/// <summary>
		/// Changes the group's name and description. Only the owner can.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="Name">The new name.</param>
		/// <param name="Description">The new description.</param>
		public static async Task EditAsync(ChatGroup Group, string Name, string Description)
		{
			MultiUserChatClient Groups = RequireGroups();
			DataForm Settings = await Groups.GetRoomConfigurationAsync(Group.RoomId, Group.Domain);

			await SetAsync(Settings, "muc#roomconfig_roomname", Name.Trim());
			await SetAsync(Settings, "muc#roomconfig_roomdesc", Description.Trim());
			await SetAsync(Settings, "muc#roomconfig_maxusers", MemberLimit.ToString(CultureInfo.InvariantCulture));
			await Groups.SubmitConfigurationForm(Settings);

			Group.Name = Name.Trim();
			Group.Description = Description.Trim();
			await GroupStore.SaveAsync(Group);
			await ConversationStore.EnsureGroupAsync(Group.Address.Value, Group.Name);
		}

		/// <summary>
		/// Lets members who are not admins add people, or stops letting them. Only the owner can.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="MembersCanAdd">If members can add people.</param>
		public static async Task SetMembersCanAddAsync(ChatGroup Group, bool MembersCanAdd)
		{
			MultiUserChatClient Groups = RequireGroups();
			DataForm Settings = await Groups.GetRoomConfigurationAsync(Group.RoomId, Group.Domain);

			await SetAsync(Settings, "muc#roomconfig_allowinvites", MembersCanAdd ? "1" : "0");
			await SetAsync(Settings, "muc#roomconfig_maxusers", MemberLimit.ToString(CultureInfo.InvariantCulture));
			await Groups.SubmitConfigurationForm(Settings);

			Group.MembersCanAdd = MembersCanAdd;
			await GroupStore.SaveAsync(Group);
		}

		/// <summary>
		/// Changes the user's name in a group.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <param name="Name">The new name.</param>
		public static async Task ChangeMyNameAsync(ChatGroup Group, string Name)
		{
			string NewName = Name.Trim();
			UserPresenceEventArgs Result = await RequireGroups().ChangeNickNameAsync(Group.RoomId, Group.Domain, NewName);
			if (!Result.Ok)
				throw (Exception?)Result.StanzaError ?? new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsNameTaken)].Value);

			MarkOnline(Group.Address.Value, Group.MyName, false);
			Group.MyName = NewName;
			AddOrUpdateMember(Group, ServiceRef.XmppService.BareJid, NewName, Group.MyRole);
			MarkOnline(Group.Address.Value, NewName, true);
			await GroupStore.SaveAsync(Group);
		}

		/// <summary>
		/// Leaves a group. Its messages stay on the phone.
		/// </summary>
		/// <param name="Group">The group.</param>
		public static async Task LeaveAsync(ChatGroup Group)
		{
			try
			{
				await RequireGroups().LeaveRoomAsync(Group.RoomId, Group.Domain, Group.MyName);
			}
			finally
			{
				Group.Status = GroupStatus.Left;
				await GroupStore.SaveAsync(Group);

				lock (synch)
				{
					onlineByGroup.Remove(Group.Address.Value);
				}
			}
		}

		/// <summary>
		/// Deletes a group for everyone. Only the owner can. Its messages stay on the phone.
		/// </summary>
		/// <param name="Group">The group.</param>
		public static async Task DeleteForEveryoneAsync(ChatGroup Group)
		{
			IqResultEventArgs Result = await RequireGroups().DestroyRoomAsync(Group.RoomId, Group.Domain);
			if (!Result.Ok)
				throw (Exception?)Result.StanzaError ?? new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsCantChange)].Value);

			Group.Status = GroupStatus.Left;
			await GroupStore.SaveAsync(Group);
		}

		/// <summary>
		/// The link others can use to join a group that lets anyone with the link join.
		/// </summary>
		/// <param name="Group">The group.</param>
		/// <returns>The link.</returns>
		public static string InviteLinkOf(ChatGroup Group)
		{
			return Constants.UriSchemes.Xmpp + ":" + Group.Address.Value + "?join";
		}

		/// <summary>
		/// Sets a field in a settings form, if the form has it.
		/// </summary>
		private static async Task SetAsync(DataForm Form, string Var, string Value)
		{
			Field? Field = Form[Var];
			if (Field is not null)
				await Field.SetValue(Value);
		}

		/// <summary>
		/// Adds a member to the group's list, or updates them.
		/// </summary>
		private static bool AddOrUpdateMember(ChatGroup Group, string? Address, string Name, GroupRole Role)
		{
			List<GroupMember> Members = [.. Group.Members ?? []];
			GroupMember? Member = null;

			if (!string.IsNullOrEmpty(Address))
				Member = Members.FirstOrDefault(M => string.Equals(M.Address, Address, StringComparison.OrdinalIgnoreCase));

			if (Member is null && !string.IsNullOrEmpty(Name))
				Member = Members.FirstOrDefault(M => string.IsNullOrEmpty(M.Address) && string.Equals(M.Name, Name, StringComparison.Ordinal));

			if (Member is null)
			{
				Members.Add(new GroupMember()
				{
					Address = Address ?? string.Empty,
					Name = Name,
					Role = Role
				});

				Group.Members = [.. Members];
				return true;
			}

			bool Changed = false;

			if (!string.IsNullOrEmpty(Address) && !string.Equals(Member.Address, Address, StringComparison.OrdinalIgnoreCase))
			{
				Member.Address = Address;
				Changed = true;
			}

			if (!string.IsNullOrEmpty(Name) && !string.Equals(Member.Name, Name, StringComparison.Ordinal))
			{
				Member.Name = Name;
				Changed = true;
			}

			if (Member.Role != Role)
			{
				Member.Role = Role;
				Changed = true;
			}

			return Changed;
		}

		private static void MarkOnline(string Address, string Name, bool Online)
		{
			lock (synch)
			{
				if (!onlineByGroup.TryGetValue(Address, out HashSet<string>? Names))
				{
					Names = new HashSet<string>(StringComparer.Ordinal);
					onlineByGroup[Address] = Names;
				}

				if (Online)
					Names.Add(Name);
				else
					Names.Remove(Name);
			}
		}

		private static GroupRole RoleOf(Affiliation? Affiliation)
		{
			return Affiliation switch
			{
				Waher.Networking.XMPP.MUC.Affiliation.Owner => GroupRole.Owner,
				Waher.Networking.XMPP.MUC.Affiliation.Admin => GroupRole.Admin,
				_ => GroupRole.Member
			};
		}

		#endregion
	}
}
