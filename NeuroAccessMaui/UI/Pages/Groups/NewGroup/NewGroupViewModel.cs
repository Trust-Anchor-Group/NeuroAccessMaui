using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Groups.NewGroup
{
	/// <summary>
	/// The view model of the page where a group is made: its name, who can join, and the people in it. The same page adds
	/// people to a group the user is in.
	/// </summary>
	public partial class NewGroupViewModel : BaseViewModel
	{
		private readonly List<GroupPersonItem> all = [];
		private readonly string? preselectedAddress;
		private readonly string? groupAddress;
		private bool loaded;

		/// <summary>
		/// Creates the view model of the page where a group is made, or people are added to one.
		/// </summary>
		/// <param name="Args">Navigation arguments, if any.</param>
		public NewGroupViewModel(NewGroupNavigationArgs? Args)
		{
			this.preselectedAddress = Args?.PreselectedAddress;
			this.groupAddress = Args?.GroupAddress;
		}

		#region Properties

		/// <summary>
		/// If people are added to a group the user is in, instead of making a new group.
		/// </summary>
		public bool IsAddMode => !string.IsNullOrEmpty(this.groupAddress);

		/// <summary>
		/// If a new group is made.
		/// </summary>
		public bool IsNewGroup => !this.IsAddMode;

		/// <summary>
		/// The page's title.
		/// </summary>
		public string Title => this.IsAddMode ?
			ServiceRef.Localizer[nameof(AppResources.GroupsAddPeopleTitle)] :
			ServiceRef.Localizer[nameof(AppResources.GroupsNewTitle)];

		/// <summary>
		/// The text of the button at the bottom.
		/// </summary>
		public string ButtonText => this.IsAddMode ?
			ServiceRef.Localizer[nameof(AppResources.GroupsAddSelected)] :
			ServiceRef.Localizer[nameof(AppResources.GroupsCreate)];

		/// <summary>
		/// The group's name.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private string groupName = string.Empty;

		/// <summary>
		/// If anyone with the invite link can join, instead of only people a member adds.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsMembersOnly))]
		private bool isInviteLink;

		/// <summary>
		/// If only people a member adds can join.
		/// </summary>
		public bool IsMembersOnly => !this.IsInviteLink;

		/// <summary>
		/// The text the people are searched for.
		/// </summary>
		[ObservableProperty]
		private string searchText = string.Empty;

		/// <summary>
		/// If the group is being made, or the people added.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private bool isWorking;

		/// <summary>
		/// If the contacts are being read.
		/// </summary>
		[ObservableProperty]
		private bool isLoading = true;

		/// <summary>
		/// If there is no one to add.
		/// </summary>
		[ObservableProperty]
		private bool isEmpty;

		/// <summary>
		/// The people shown in the list, after search.
		/// </summary>
		public ObservableCollection<GroupPersonItem> People { get; } = [];

		/// <summary>
		/// The people chosen, shown as chips.
		/// </summary>
		public ObservableCollection<GroupPersonItem> Selected { get; } = [];

		/// <summary>
		/// The heading of the chosen people, with how many there are.
		/// </summary>
		public string SelectedText => ServiceRef.Localizer[nameof(AppResources.GroupsMembersAdded), this.Selected.Count];

		/// <summary>
		/// If anyone is chosen.
		/// </summary>
		public bool HasSelected => this.Selected.Count > 0;

		/// <summary>
		/// How many people a group has room for.
		/// </summary>
		public string LimitText => ServiceRef.Localizer[nameof(AppResources.GroupsLimitHint), GroupService.MemberLimit];

		#endregion

		#region Life cycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.loaded)
				return;

			this.loaded = true;
			await this.LoadAsync();
		}

		/// <summary>
		/// Reads the contacts who can be added: not blocked, not the user, and not in the group already.
		/// </summary>
		private async Task LoadAsync()
		{
			try
			{
				HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase);
				string Own = ServiceRef.XmppService.BareJid ?? string.Empty;

				if (!string.IsNullOrEmpty(Own))
					Excluded.Add(Own);

				if (this.IsAddMode)
				{
					ChatGroup? Group = await GroupStore.FindAsync(this.groupAddress);
					foreach (GroupMember Member in Group?.Members ?? [])
					{
						if (!string.IsNullOrEmpty(Member.Address))
							Excluded.Add(Member.Address);
					}
				}

				List<GroupPersonItem> Items = [];

				foreach (Contact Contact in await ContactStore.GetAllAsync())
				{
					string Address = Contact.ChatAddress.Value;
					if (Contact.IsBlocked || string.IsNullOrEmpty(Address) || Excluded.Contains(Address))
						continue;

					GroupPersonItem Item = new(Address, Contact.DisplayName, this.Toggle)
					{
						IsSelected = string.Equals(Address, this.preselectedAddress, StringComparison.OrdinalIgnoreCase)
					};

					Items.Add(Item);
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.all.Clear();
					this.all.AddRange(Items);

					foreach (GroupPersonItem Item in Items.Where(Item => Item.IsSelected))
						this.Selected.Add(Item);

					this.SelectionChanged();
					this.ApplySearch();
					this.IsLoading = false;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
			}
		}

		#endregion

		#region Choosing people

		partial void OnSearchTextChanged(string value)
		{
			this.ApplySearch();
		}

		/// <summary>
		/// Shows the people whose name matches the search.
		/// </summary>
		private void ApplySearch()
		{
			string Search = this.SearchText.Trim();

			this.People.Clear();

			foreach (GroupPersonItem Item in this.all)
			{
				if (Search.Length == 0 || Item.Name.Contains(Search, StringComparison.CurrentCultureIgnoreCase))
					this.People.Add(Item);
			}

			this.IsEmpty = this.People.Count == 0;
		}

		/// <summary>
		/// Chooses a person, or stops choosing them. A group has room for a limited number of people, the user included.
		/// </summary>
		private void Toggle(GroupPersonItem Item)
		{
			if (!Item.IsSelected && this.Selected.Count + 1 >= GroupService.MemberLimit)
			{
				_ = ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.GroupsLimitReached), GroupService.MemberLimit]);
				return;
			}

			Item.IsSelected = !Item.IsSelected;

			if (Item.IsSelected)
				this.Selected.Add(Item);
			else
				this.Selected.Remove(Item);

			this.SelectionChanged();
		}

		/// <summary>
		/// Stops choosing a person, from their chip.
		/// </summary>
		/// <param name="Item">The person.</param>
		[RelayCommand]
		private void RemoveSelected(GroupPersonItem? Item)
		{
			if (Item is null || !Item.IsSelected)
				return;

			Item.IsSelected = false;
			this.Selected.Remove(Item);
			this.SelectionChanged();
		}

		/// <summary>
		/// Sets who can join the group.
		/// </summary>
		/// <param name="InviteLink">If anyone with the invite link can join, as text.</param>
		[RelayCommand]
		private void SelectJoinRule(string? InviteLink)
		{
			this.IsInviteLink = string.Equals(InviteLink, bool.TrueString, StringComparison.OrdinalIgnoreCase);
		}

		private void SelectionChanged()
		{
			this.OnPropertyChanged(nameof(this.SelectedText));
			this.OnPropertyChanged(nameof(this.HasSelected));
			this.CreateCommand.NotifyCanExecuteChanged();
		}

		#endregion

		#region Creating

		private bool CanCreate()
		{
			if (this.IsWorking)
				return false;

			return this.IsAddMode ? this.Selected.Count > 0 : !string.IsNullOrWhiteSpace(this.GroupName);
		}

		/// <summary>
		/// Makes the group and opens its chat, or adds the people chosen to the group and goes back.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanCreate))]
		private async Task CreateAsync()
		{
			this.IsWorking = true;

			try
			{
				List<string> Addresses = [.. this.Selected.Select(Item => Item.Address)];

				if (this.IsAddMode)
				{
					ChatGroup Group = await GroupStore.FindAsync(this.groupAddress) ??
						throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.GroupsNotFound)]);

					await GroupService.AddPeopleAsync(Group, Addresses);
					await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.GroupsPeopleAdded)]);
					await this.GoBack();
				}
				else
				{
					GroupJoinRule Rule = this.IsInviteLink ? GroupJoinRule.InviteLink : GroupJoinRule.MembersOnly;
					ChatGroup Group = await GroupService.CreateAsync(this.GroupName, string.Empty, Rule, Addresses);

					// The chat replaces this page, so going back from it returns to where the group was made from.
					ChatNavigationArgs Args = new(null, Group.Address.Value, Group.Name);
					await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Pop2);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsWorking = false;
			}
		}

		#endregion
	}
}
