using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Popups.Chats;
using NeuroAccessMaui.UI.Popups.Contacts;
using Waher.Networking.XMPP.Events;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Chats
{
	/// <summary>
	/// The view model of the Chats tab: the user's chats, pinned ones first and then the latest first, with contact requests
	/// at the top.
	/// </summary>
	public partial class ChatsViewModel : BaseViewModel
	{
		private readonly List<ConversationItem> all = [];
		private int loading;
		private bool loadAgain;
		private bool initialized;
		private CancellationTokenSource? searchDelay;
		private CancellationTokenSource? reloadDelay;

		#region Properties

		/// <summary>
		/// The chats shown, after search and filter.
		/// </summary>
		public ObservableCollection<ConversationItem> Items { get; } = [];

		/// <summary>
		/// Contact requests that wait for an answer.
		/// </summary>
		public ObservableCollection<ContactRequestItem> Requests { get; } = [];

		/// <summary>
		/// The text the chats are searched for.
		/// </summary>
		[ObservableProperty]
		private string searchText = string.Empty;

		/// <summary>
		/// If only chats with unread messages are shown.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowsRequests))]
		private bool showsUnreadOnly;

		/// <summary>
		/// If the chats are being loaded the first time.
		/// </summary>
		[ObservableProperty]
		private bool isLoading = true;

		/// <summary>
		/// If no chats are shown.
		/// </summary>
		[ObservableProperty]
		private bool isEmpty;

		/// <summary>
		/// Why no chats are shown.
		/// </summary>
		[ObservableProperty]
		private string emptyText = string.Empty;

		/// <summary>
		/// The text of the chip that shows only unread chats, with how many there are.
		/// </summary>
		[ObservableProperty]
		private string unreadChipText = string.Empty;

		/// <summary>
		/// If the contact requests show: when there are any, and the whole list is shown.
		/// </summary>
		public bool ShowsRequests => this.Requests.Count > 0 && !this.ShowsUnreadOnly && string.IsNullOrWhiteSpace(this.SearchText);

		/// <summary>
		/// The number of requests waiting, as shown on their heading.
		/// </summary>
		public string RequestCount => this.Requests.Count.ToString(CultureInfo.CurrentCulture);

		#endregion

		#region Life cycle

		/// <inheritdoc/>
		/// <remarks>
		/// A tab can be started both through its page and directly, so the events are only followed once.
		/// </remarks>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.initialized)
				return;

			this.initialized = true;

			ConversationStore.Changed += this.Store_Changed;
			ContactStore.Changed += this.Store_Changed;
			ContactRequests.Changed += this.ContactRequests_Changed;
			ServiceRef.XmppService.OnPresence += this.Xmpp_OnPresence;

			try
			{
				await ConversationStore.EnsureNotesAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			await this.LoadAsync();
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
			if (!this.initialized)
				return base.OnDisposeAsync();

			this.initialized = false;

			ConversationStore.Changed -= this.Store_Changed;
			ContactStore.Changed -= this.Store_Changed;
			ContactRequests.Changed -= this.ContactRequests_Changed;
			ServiceRef.XmppService.OnPresence -= this.Xmpp_OnPresence;

			this.searchDelay?.Cancel();
			this.reloadDelay?.Cancel();

			return base.OnDisposeAsync();
		}

		#endregion

		#region Loading and filtering

		/// <summary>
		/// Reads the chats and the contacts they are with, and shows them. A load asked for while one runs is done right after
		/// it, so the last change is always shown.
		/// </summary>
		private async Task LoadAsync()
		{
			if (Interlocked.Exchange(ref this.loading, 1) == 1)
			{
				this.loadAgain = true;
				return;
			}

			try
			{
				do
				{
					this.loadAgain = false;
					await this.LoadOnceAsync();
				}
				while (this.loadAgain);
			}
			finally
			{
				Volatile.Write(ref this.loading, 0);
			}
		}

		/// <summary>
		/// Reads the chats and the contacts they are with, and shows them.
		/// </summary>
		private async Task LoadOnceAsync()
		{
			try
			{
				IReadOnlyList<Conversation> Conversations = await ConversationStore.GetAllAsync();
				Dictionary<string, Contact> Contacts = new(StringComparer.OrdinalIgnoreCase);

				foreach (Contact Contact in await ContactStore.GetAllAsync())
				{
					if (!string.IsNullOrEmpty(Contact.ChatAddress.Value))
						Contacts[Contact.ChatAddress.Value] = Contact;
				}

				List<ConversationItem> Items = [];

				foreach (Conversation Conversation in Conversations)
				{
					Contacts.TryGetValue(Conversation.Address.Value, out Contact? Contact);
					Items.Add(new ConversationItem(Conversation, Contact, this.OpenChatAsync, this.ShowActionsAsync));
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.all.Clear();
					this.all.AddRange(Items);
					this.LoadRequests();
					this.ApplyFilter();
					this.IsLoading = false;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
			}
		}

		/// <summary>
		/// Shows the chats that match the search and the filter.
		/// </summary>
		private void ApplyFilter()
		{
			string Search = this.SearchText.Trim();
			int UnreadChats = this.all.Count(Item => Item.HasUnread);

			this.Items.Clear();

			foreach (ConversationItem Item in this.all)
			{
				if (this.ShowsUnreadOnly && !Item.HasUnread)
					continue;

				if (Search.Length > 0 && Item.SearchText.IndexOf(Search, StringComparison.CurrentCultureIgnoreCase) < 0)
					continue;

				this.Items.Add(Item);
			}

			for (int i = 0; i < this.Items.Count; i++)
			{
				this.Items[i].CardPosition = this.Items.Count == 1 ? ListCardPosition.Only :
					i == 0 ? ListCardPosition.First :
					i == this.Items.Count - 1 ? ListCardPosition.Last :
					ListCardPosition.Middle;
			}

			this.UnreadChipText = UnreadChats > 0 ?
				ServiceRef.Localizer[nameof(AppResources.ChatsFilterUnreadCount), UnreadChats] :
				ServiceRef.Localizer[nameof(AppResources.ChatsFilterUnread)];

			this.IsEmpty = this.Items.Count == 0;

			if (Search.Length > 0)
				this.EmptyText = ServiceRef.Localizer[nameof(AppResources.ChatsNoMatches)];
			else if (this.ShowsUnreadOnly)
				this.EmptyText = ServiceRef.Localizer[nameof(AppResources.ChatsNoUnread)];
			else
				this.EmptyText = ServiceRef.Localizer[nameof(AppResources.ChatsEmpty)];

			this.OnPropertyChanged(nameof(this.ShowsRequests));
		}

		/// <summary>
		/// Shows the contact requests that wait for an answer.
		/// </summary>
		private void LoadRequests()
		{
			this.Requests.Clear();

			foreach (ContactRequest Request in ContactRequests.Pending)
				this.Requests.Add(new ContactRequestItem(Request));

			this.OnPropertyChanged(nameof(this.RequestCount));
			this.OnPropertyChanged(nameof(this.ShowsRequests));
		}

		partial void OnSearchTextChanged(string value)
		{
			this.searchDelay?.Cancel();
			CancellationTokenSource Delay = new();
			this.searchDelay = Delay;

			_ = Task.Delay(250, Delay.Token).ContinueWith(T =>
			{
				if (!T.IsCanceled)
					MainThread.BeginInvokeOnMainThread(this.ApplyFilter);
			}, TaskScheduler.Default);
		}

		/// <summary>
		/// Shows all chats, or only the ones with unread messages.
		/// </summary>
		/// <param name="UnreadOnly">If only unread chats are shown, as text.</param>
		[RelayCommand]
		private void SelectFilter(string? UnreadOnly)
		{
			this.ShowsUnreadOnly = string.Equals(UnreadOnly, bool.TrueString, StringComparison.OrdinalIgnoreCase);
			this.ApplyFilter();
		}

		#endregion

		#region Opening

		/// <summary>
		/// Opens a chat.
		/// </summary>
		/// <param name="Item">The chat.</param>
		private async Task OpenChatAsync(ConversationItem Item)
		{
			try
			{
				string? LegalId = Item.IsNotes ? ServiceRef.TagProfile.LegalIdentity?.Id : Item.Contact?.LegalId.Value;
				ChatNavigationArgs Args = new(LegalId, Item.Address, Item.Title);

				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Pop, Item.Address);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Picks a contact, and opens the chat with them.
		/// </summary>
		[RelayCommand]
		private async Task NewChatAsync()
		{
			try
			{
				TaskCompletionSource<ContactInfoModel?> Selection = new();
				ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.ChatsNewChatTitle)], Selection);

				await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);

				ContactInfoModel? Picked = await Selection.Task;
				string? Address = Picked?.BareJid?.Value;
				if (Picked is null || string.IsNullOrEmpty(Address))
					return;

				ChatNavigationArgs ChatArgs = new(Picked.LegalId?.Value, Address, Picked.FriendlyName);
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), ChatArgs, BackMethod.Pop, Address);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Opens the contacts.
		/// </summary>
		[RelayCommand]
		private async Task OpenContactsAsync()
		{
			try
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), new ContactListNavigationArgs(), BackMethod.Pop);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Shows a contact request, where the user can accept it or not.
		/// </summary>
		/// <param name="Item">The request.</param>
		[RelayCommand]
		private Task ReviewRequestAsync(ContactRequestItem? Item)
		{
			return Item is null ? Task.CompletedTask : ContactRequestPopup.ShowAsync(Item.Request);
		}

		#endregion

		#region Chat actions

		/// <summary>
		/// Shows what can be done with a chat, and does what the user chooses.
		/// </summary>
		/// <param name="Item">The chat.</param>
		private async Task ShowActionsAsync(ConversationItem Item)
		{
			try
			{
				ChatAction? Action = await ChatActionsPopup.ShowAsync(Item.Title, Item.IsPinned, Item.IsMuted, Item.HasUnread);

				switch (Action)
				{
					case ChatAction.Pin:
					case ChatAction.Unpin:
						await ConversationStore.SetPinnedAsync(Item.Address, Action == ChatAction.Pin);
						break;

					case ChatAction.Mute:
					case ChatAction.Unmute:
						await ConversationStore.SetMutedAsync(Item.Address, Action == ChatAction.Mute);
						break;

					case ChatAction.MarkRead:
						await ConversationStore.MarkReadAsync(Item.Address);
						break;

					case ChatAction.Delete:
						await this.DeleteChatAsync(Item);
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
		/// Deletes a chat from this phone, after asking.
		/// </summary>
		private async Task DeleteChatAsync(ConversationItem Item)
		{
			bool Delete = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ChatsDeleteQuestion), Item.Title],
				ServiceRef.Localizer[Item.IsNotes ? nameof(AppResources.ChatsDeleteNotesMessage) : nameof(AppResources.ChatsDeleteMessage), Item.Title],
				ServiceRef.Localizer[nameof(AppResources.ChatsDeleteConfirm)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);

			if (!Delete)
				return;

			await ConversationStore.DeleteChatAsync(Item.Address);
			await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ChatsDeleted)]);
		}

		#endregion

		#region Events

		private void Store_Changed(object? Sender, EventArgs e)
		{
			// Several changes often come together, such as when messages arrive after being offline, so they are shown at once.
			this.reloadDelay?.Cancel();
			CancellationTokenSource Delay = new();
			this.reloadDelay = Delay;

			_ = Task.Delay(300, Delay.Token).ContinueWith(T =>
			{
				if (!T.IsCanceled)
					_ = this.LoadAsync();
			}, TaskScheduler.Default);
		}

		private void ContactRequests_Changed(object? Sender, EventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(this.LoadRequests);
		}

		private Task Xmpp_OnPresence(object? Sender, PresenceEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (ConversationItem Item in this.all)
				{
					if (string.Equals(Item.Address, e.FromBareJID, StringComparison.OrdinalIgnoreCase))
						Item.UpdatePresence();
				}
			});

			return Task.CompletedTask;
		}

		#endregion
	}
}
