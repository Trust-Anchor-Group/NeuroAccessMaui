using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.ContactCard;
using NeuroAccessMaui.UI.Pages.Groups.NewGroup;
using NeuroAccessMaui.UI.Popups.Groups;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Groups.GroupInfo
{
	/// <summary>
	/// The view model of a group's page: what the group is, who is in it, and what the user can do with it.
	/// </summary>
	public partial class GroupInfoViewModel : BaseViewModel
	{
		private readonly string? address;
		private ChatGroup? group;
		private bool initialized;
		private bool refreshing;
		private CancellationTokenSource? reloadDelay;

		/// <summary>
		/// Creates the view model of a group's page.
		/// </summary>
		/// <param name="Args">Which group to show.</param>
		public GroupInfoViewModel(GroupInfoNavigationArgs? Args)
		{
			this.address = Args?.GroupAddress;
		}

		#region Properties

		/// <summary>
		/// If the group is on the phone.
		/// </summary>
		[ObservableProperty]
		private bool isFound;

		/// <summary>
		/// The group's name.
		/// </summary>
		[ObservableProperty]
		private string name = string.Empty;

		/// <summary>
		/// What the group is for.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasDescription))]
		private string description = string.Empty;

		/// <summary>
		/// If the group has a description.
		/// </summary>
		public bool HasDescription => !string.IsNullOrWhiteSpace(this.Description);

		/// <summary>
		/// Who can join the group.
		/// </summary>
		[ObservableProperty]
		private string joinRuleText = string.Empty;

		/// <summary>
		/// The user's role, if they are an owner or an admin.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasRole))]
		private string roleText = string.Empty;

		/// <summary>
		/// If the user is an owner or an admin.
		/// </summary>
		public bool HasRole => !string.IsNullOrEmpty(this.RoleText);

		/// <summary>
		/// If the user is in the group.
		/// </summary>
		[ObservableProperty]
		private bool isActive;

		/// <summary>
		/// If the user has left the group, or it is gone.
		/// </summary>
		[ObservableProperty]
		private bool isLeft;

		/// <summary>
		/// If the user owns the group, and is in it.
		/// </summary>
		[ObservableProperty]
		private bool isOwner;

		/// <summary>
		/// If the user can add people.
		/// </summary>
		[ObservableProperty]
		private bool canAddPeople;

		/// <summary>
		/// If anyone with the invite link can join, so the link can be shared.
		/// </summary>
		[ObservableProperty]
		private bool canShareLink;

		/// <summary>
		/// The text of the mute tile.
		/// </summary>
		[ObservableProperty]
		private string muteText = string.Empty;

		/// <summary>
		/// If new messages in the group give notifications.
		/// </summary>
		[ObservableProperty]
		private bool notificationsOn = true;

		/// <summary>
		/// If members who are not admins can add people.
		/// </summary>
		[ObservableProperty]
		private bool membersCanAdd;

		/// <summary>
		/// The heading of the members, with how many there are.
		/// </summary>
		[ObservableProperty]
		private string membersTitle = string.Empty;

		/// <summary>
		/// The user's name in the group.
		/// </summary>
		[ObservableProperty]
		private string myName = string.Empty;

		/// <summary>
		/// The members, the user first, then owners, admins and the others by name.
		/// </summary>
		public ObservableCollection<GroupMemberItem> Members { get; } = [];

		#endregion

		#region Life cycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.initialized)
				return;

			this.initialized = true;

			GroupStore.Changed += this.Store_Changed;
			ContactStore.Changed += this.Store_Changed;
			ConversationStore.Changed += this.Store_Changed;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.LoadAsync();
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			if (this.initialized)
			{
				this.initialized = false;

				GroupStore.Changed -= this.Store_Changed;
				ContactStore.Changed -= this.Store_Changed;
				ConversationStore.Changed -= this.Store_Changed;
			}

			this.reloadDelay?.Cancel();
			return base.OnDisposeAsync();
		}

		private void Store_Changed(object? Sender, EventArgs e)
		{
			// Several changes often come together, such as when messages arrive, so the page is shown again once.
			this.reloadDelay?.Cancel();
			CancellationTokenSource Delay = new();
			this.reloadDelay = Delay;

			_ = Task.Delay(300, Delay.Token).ContinueWith(T =>
			{
				if (!T.IsCanceled)
					_ = this.LoadAsync();
			}, TaskScheduler.Default);
		}

		#endregion

		#region Showing the group

		/// <summary>
		/// Reads the group, its members' contacts and if it is muted, and shows them.
		/// </summary>
		private async Task LoadAsync()
		{
			try
			{
				ChatGroup? Group = await GroupStore.FindAsync(this.address);
				Dictionary<string, Contact> Contacts = new(StringComparer.OrdinalIgnoreCase);

				foreach (Contact Contact in await ContactStore.GetAllAsync())
				{
					if (!string.IsNullOrEmpty(Contact.ChatAddress.Value))
						Contacts[Contact.ChatAddress.Value] = Contact;
				}

				Conversation? Conversation = await ConversationStore.FindAsync(this.address);
				bool Muted = Conversation is not null && Conversation.Muted;

				await MainThread.InvokeOnMainThreadAsync(() => this.Show(Group, Contacts, Muted));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void Show(ChatGroup? Group, Dictionary<string, Contact> Contacts, bool Muted)
		{
			this.refreshing = true;

			try
			{
				this.group = Group;
				this.IsFound = Group is not null;
				if (Group is null)
					return;

				bool Joined = Group.Status == GroupStatus.Joined;

				this.Name = Group.Name;
				this.Description = Group.Description;
				this.IsActive = Joined;
				this.IsLeft = Group.Status == GroupStatus.Left;
				this.IsOwner = Joined && Group.MyRole == GroupRole.Owner;
				this.CanAddPeople = Group.CanAddPeople;
				this.CanShareLink = Joined && Group.JoinRule == GroupJoinRule.InviteLink;
				this.MembersCanAdd = Group.MembersCanAdd;
				this.NotificationsOn = !Muted;
				this.MyName = Group.MyName;
				this.MuteText = Muted ?
					ServiceRef.Localizer[nameof(AppResources.GroupsUnmute)] :
					ServiceRef.Localizer[nameof(AppResources.GroupsMute)];

				this.JoinRuleText = Group.JoinRule == GroupJoinRule.InviteLink ?
					ServiceRef.Localizer[nameof(AppResources.GroupsAnyoneWithLinkCanJoin)] :
					ServiceRef.Localizer[nameof(AppResources.GroupsOnlyPeopleYouAddCanJoin)];

				this.RoleText = !Joined ? string.Empty : Group.MyRole switch
				{
					GroupRole.Owner => ServiceRef.Localizer[nameof(AppResources.GroupsYoureOwner)],
					GroupRole.Admin => ServiceRef.Localizer[nameof(AppResources.GroupsYoureAdmin)],
					_ => string.Empty
				};

				this.ShowMembers(Group, Contacts);
			}
			finally
			{
				this.refreshing = false;
			}
		}

		private void ShowMembers(ChatGroup Group, Dictionary<string, Contact> Contacts)
		{
			string Own = ServiceRef.XmppService.BareJid ?? string.Empty;
			List<GroupMemberItem> Items = [];
			bool HasMe = false;

			foreach (GroupMember Member in Group.Members ?? [])
			{
				bool IsMe = string.Equals(Member.Address, Own, StringComparison.OrdinalIgnoreCase) ||
					(string.IsNullOrEmpty(Member.Address) && string.Equals(Member.Name, Group.MyName, StringComparison.Ordinal));

				if (IsMe)
				{
					if (HasMe)
						continue;

					HasMe = true;
				}

				Contacts.TryGetValue(Member.Address, out Contact? Contact);
				Items.Add(new GroupMemberItem(Member, Contact?.DisplayName, IsMe, this.OpenMemberAsync, this.AddMemberContactAsync));
			}

			List<GroupMemberItem> Sorted = [.. Items
				.OrderByDescending(Item => Item.IsMe)
				.ThenByDescending(Item => Item.Member.Role)
				.ThenBy(Item => Item.Name, StringComparer.CurrentCultureIgnoreCase)];

			this.Members.Clear();

			for (int i = 0; i < Sorted.Count; i++)
			{
				Sorted[i].CardPosition = Sorted.Count == 1 ? ListCardPosition.Only :
					i == 0 ? ListCardPosition.First :
					i == Sorted.Count - 1 ? ListCardPosition.Last :
					ListCardPosition.Middle;

				this.Members.Add(Sorted[i]);
			}

			this.MembersTitle = Sorted.Count == 1 ?
				ServiceRef.Localizer[nameof(AppResources.GroupsOneMember)] :
				ServiceRef.Localizer[nameof(AppResources.GroupsMemberCount), Sorted.Count];
		}

		#endregion

		#region Settings

		partial void OnNotificationsOnChanged(bool value)
		{
			if (this.refreshing || string.IsNullOrEmpty(this.address))
				return;

			_ = this.SetMutedAsync(!value);
		}

		partial void OnMembersCanAddChanged(bool value)
		{
			if (this.refreshing || this.group is null)
				return;

			_ = this.SetMembersCanAddAsync(value);
		}

		/// <summary>
		/// Mutes the group or turns its notifications back on, from the tile.
		/// </summary>
		[RelayCommand]
		private Task ToggleMuteAsync()
		{
			return this.SetMutedAsync(this.NotificationsOn);
		}

		private async Task SetMutedAsync(bool Muted)
		{
			try
			{
				if (!string.IsNullOrEmpty(this.address))
					await ConversationStore.SetMutedAsync(this.address, Muted);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		private async Task SetMembersCanAddAsync(bool MembersCanAdd)
		{
			try
			{
				if (this.group is not null)
					await GroupService.SetMembersCanAddAsync(this.group, MembersCanAdd);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
				await this.LoadAsync();
			}
		}

		#endregion

		#region Actions

		/// <summary>
		/// Changes the group's name and description. Only the owner can.
		/// </summary>
		[RelayCommand]
		private async Task EditAsync()
		{
			if (this.group is null || !this.IsOwner)
				return;

			try
			{
				string[]? Texts = await GroupTextEditPopup.ShowAsync(ServiceRef.Localizer[nameof(AppResources.GroupsEdit)].Value,
					(ServiceRef.Localizer[nameof(AppResources.GroupsName)].Value, this.group.Name),
					(ServiceRef.Localizer[nameof(AppResources.Description)].Value, this.group.Description));

				if (Texts is null || Texts.Length < 2)
					return;

				await GroupService.EditAsync(this.group, Texts[0], Texts[1]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Opens the page where people are added to the group.
		/// </summary>
		[RelayCommand]
		private async Task AddPeopleAsync()
		{
			if (string.IsNullOrEmpty(this.address))
				return;

			await ServiceRef.NavigationService.GoToAsync(nameof(NewGroupPage), NewGroupNavigationArgs.AddPeopleTo(this.address), BackMethod.Pop);
		}

		/// <summary>
		/// Shares the group's invite link.
		/// </summary>
		[RelayCommand]
		private async Task ShareLinkAsync()
		{
			if (this.group is null)
				return;

			try
			{
				await Share.Default.RequestAsync(new ShareTextRequest()
				{
					Title = ServiceRef.Localizer[nameof(AppResources.GroupsInviteLinkTitle), this.group.Name],
					Text = GroupService.InviteLinkOf(this.group)
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Changes the user's name in the group.
		/// </summary>
		[RelayCommand]
		private async Task ChangeMyNameAsync()
		{
			if (this.group is null || this.group.Status != GroupStatus.Joined)
				return;

			try
			{
				string[]? Texts = await GroupTextEditPopup.ShowAsync(ServiceRef.Localizer[nameof(AppResources.GroupsYourName)].Value,
					(ServiceRef.Localizer[nameof(AppResources.GroupsYourName)].Value, this.group.MyName));

				if (Texts is null || Texts.Length == 0 || string.Equals(Texts[0], this.group.MyName, StringComparison.Ordinal))
					return;

				await GroupService.ChangeMyNameAsync(this.group, Texts[0]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Leaves the group, after asking. Its messages stay on the phone.
		/// </summary>
		[RelayCommand]
		private async Task LeaveAsync()
		{
			if (this.group is null)
				return;

			try
			{
				bool Leave = await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.GroupsLeaveQuestion), this.group.Name],
					ServiceRef.Localizer[nameof(AppResources.GroupsLeaveMessage)],
					ServiceRef.Localizer[nameof(AppResources.GroupsLeave)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]);

				if (Leave)
					await GroupService.LeaveAsync(this.group);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Deletes the group for everyone, after asking. Only the owner can.
		/// </summary>
		[RelayCommand]
		private async Task DeleteForEveryoneAsync()
		{
			if (this.group is null || !this.IsOwner)
				return;

			try
			{
				bool Delete = await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.GroupsDeleteQuestion), this.group.Name],
					ServiceRef.Localizer[nameof(AppResources.GroupsDeleteMessage)],
					ServiceRef.Localizer[nameof(AppResources.ChatsDeleteConfirm)],
					ServiceRef.Localizer[nameof(AppResources.Cancel)]);

				if (Delete)
					await GroupService.DeleteForEveryoneAsync(this.group);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		#endregion

		#region Members

		/// <summary>
		/// Shows what can be done with a member, and does what the user chooses.
		/// </summary>
		private async Task OpenMemberAsync(GroupMemberItem Item)
		{
			ChatGroup? Group = this.group;
			if (Group is null || Item.IsMe)
				return;

			try
			{
				bool HasAddress = !string.IsNullOrEmpty(Item.Address);
				bool Joined = Group.Status == GroupStatus.Joined;
				bool CanMakeAdmin = Joined && HasAddress && Group.MyRole == GroupRole.Owner && Item.Member.Role == GroupRole.Member;
				bool CanRemove = Group.CanManage && HasAddress && Item.Member.Role != GroupRole.Owner &&
					(Group.MyRole == GroupRole.Owner || Item.Member.Role == GroupRole.Member);

				GroupMemberAction? Action = await GroupMemberPopup.ShowAsync(Item.Name, HasAddress, Item.IsContact, CanMakeAdmin, CanRemove);

				switch (Action)
				{
					case GroupMemberAction.Message:
						await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(null, Item.Address, Item.Name),
							BackMethod.Pop, Item.Address);
						break;

					case GroupMemberAction.ViewContact:
						await ServiceRef.NavigationService.GoToAsync(nameof(ContactCardPage), new ContactCardNavigationArgs(Item.Address, null),
							BackMethod.Pop);
						break;

					case GroupMemberAction.MakeAdmin:
						await GroupService.MakeAdminAsync(Group, Item.Address);
						await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.GroupsMadeAdmin), Item.Name]);
						break;

					case GroupMemberAction.Remove:
						await this.RemoveMemberAsync(Group, Item);
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Removes a member from the group, after asking.
		/// </summary>
		private async Task RemoveMemberAsync(ChatGroup Group, GroupMemberItem Item)
		{
			bool Remove = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.GroupsRemoveQuestion), Item.Name],
				ServiceRef.Localizer[nameof(AppResources.GroupsRemoveMessage)],
				ServiceRef.Localizer[nameof(AppResources.GroupsRemoveConfirm)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);

			if (!Remove)
				return;

			await GroupService.RemoveAsync(Group, Item.Address);
			await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.GroupsRemoved), Item.Name]);
		}

		/// <summary>
		/// Adds a member to the user's contacts, and sends them a request.
		/// </summary>
		private async Task AddMemberContactAsync(GroupMemberItem Item)
		{
			if (!Item.CanAddContact)
				return;

			try
			{
				Contact Added = await ContactStore.AddOrUpdateAsync(Item.Address, null, Item.Member.Name);
				ServiceRef.XmppService.RequestPresenceSubscription(Item.Address, ContactRequests.OwnIdentityXml());
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactRequestSent), Added.DisplayName]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		#endregion
	}
}
