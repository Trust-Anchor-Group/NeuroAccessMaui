using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using EDaler.Uris;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Groups;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Controls.Markdown;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contacts.ContactCard;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Groups.GroupInfo;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.SendPayment;
using NeuroFeatures;
using Waher.Content.Markdown;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Events;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// The view model of a chat with a contact, or of notes to yourself.
	/// </summary>
	/// <remarks>
	/// Messages are loaded in batches, newest first, and their text is built off the UI thread before the rows are added.
	/// Messages that arrive or are sent while the chat is open come through <see cref="ChatEvents"/>.
	/// </remarks>
	public partial class ChatViewModel : XmppViewModel, ILinkableView
	{
		private const int batchSize = 30;
		private const int quoteMaxLength = 200;
		private const int quoteMaxLines = 3;
		private static readonly TimeSpan groupInterval = TimeSpan.FromMinutes(2);
		private static readonly TimeSpan pausedAfter = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan typingTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan previewDelay = TimeSpan.FromMilliseconds(250);

		private readonly Dictionary<string, ChatMessageItem> itemsByObjectId = new(StringComparer.Ordinal);
		private readonly bool isSelfChat;
		private readonly bool isGroup;
		private string? groupSubtitle;
		private bool groupLeft;
		private DateTime oldestLoaded = DateTime.MaxValue;
		private bool loadingOlder;
		private bool isComposing;
		private bool isAtBottom = true;
		private CancellationTokenSource? pausedTimer;
		private CancellationTokenSource? remoteTypingTimer;
		private CancellationTokenSource? previewTimer;

		static ChatViewModel()
		{
			ChatAttachments.Register();
		}

		/// <summary>
		/// Creates the view model of a chat.
		/// </summary>
		/// <param name="Args">Navigation arguments</param>
		public ChatViewModel(ChatNavigationArgs? Args)
			: base()
		{
			this.legalId = Args?.LegalId ?? string.Empty;
			this.bareJid = Args?.BareJid ?? string.Empty;
			this.uniqueId = Args?.UniqueId;
			this.isSelfChat = ChatSender.IsSelf(this.bareJid);
			this.isGroup = GroupStore.IsGroup(this.bareJid);

			string Name = this.isSelfChat ? ServiceRef.Localizer[nameof(AppResources.ChatMe)] :
				string.IsNullOrEmpty(Args?.FriendlyName) ? this.bareJid : Args.FriendlyName;

			this.friendlyName = Name;
			this.Initials = GetInitials(Name, this.isSelfChat);
			this.LinkCommand = new AsyncRelayCommand<string>(this.OpenLinkAsync);
			this.ActionsCommand = new RelayCommand<ChatMessageItem>(this.OpenMenu);
			this.ReplyCommand = new RelayCommand<ChatMessageItem>(this.StartReply);
		}

		#region Properties

		/// <summary>
		/// The view's unique ID.
		/// </summary>
		[ObservableProperty]
		private string? uniqueId;

		/// <summary>
		/// Address of the other party.
		/// </summary>
		[ObservableProperty]
		private string bareJid;

		/// <summary>
		/// Legal ID of the other party, if known.
		/// </summary>
		[ObservableProperty]
		private string legalId;

		/// <summary>
		/// Name of the other party.
		/// </summary>
		[ObservableProperty]
		private string friendlyName;

		/// <summary>
		/// Initials on the avatar.
		/// </summary>
		public string Initials { get; }

		/// <summary>
		/// If the chat holds notes to yourself, which stay on the phone.
		/// </summary>
		public bool IsSelfChat => this.isSelfChat;

		/// <summary>
		/// If the chat is a group's.
		/// </summary>
		public bool IsGroup => this.isGroup;

		/// <summary>
		/// If the avatar in the header shows letters. A group's shows a group icon.
		/// </summary>
		public bool ShowsInitials => !this.isGroup;

		/// <summary>
		/// The rows of the chat: messages and days.
		/// </summary>
		public ChatItemCollection Items { get; } = [];

		/// <summary>
		/// Runs when a link or card in a message is tapped.
		/// </summary>
		public IAsyncRelayCommand<string> LinkCommand { get; }

		/// <summary>
		/// Shows what can be done with a message, in a menu next to it.
		/// </summary>
		public IRelayCommand<ChatMessageItem> ActionsCommand { get; }

		/// <summary>
		/// Starts a reply to a message, as when it is swiped.
		/// </summary>
		public IRelayCommand<ChatMessageItem> ReplyCommand { get; }

		/// <summary>
		/// If the latest messages are being loaded.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsEmpty))]
		private bool isLoading = true;

		/// <summary>
		/// If the chat has no messages.
		/// </summary>
		public bool IsEmpty => !this.IsLoading && this.Items.Count == 0;

		/// <summary>
		/// Text shown when the chat has no messages.
		/// </summary>
		public string EmptyText => this.isSelfChat ?
			ServiceRef.Localizer[nameof(AppResources.ChatSelfEmptyText)] :
			ServiceRef.Localizer[nameof(AppResources.ChatEmptyText)];

		/// <summary>
		/// If there are older messages than those loaded.
		/// </summary>
		[ObservableProperty]
		private bool existsMoreMessages;

		/// <summary>
		/// If the other party is writing.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Subtitle))]
		[NotifyPropertyChangedFor(nameof(HasSubtitle))]
		private bool isRemoteTyping;

		/// <summary>
		/// If the other party is online.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Subtitle))]
		[NotifyPropertyChangedFor(nameof(HasSubtitle))]
		private bool isOnline;

		/// <summary>
		/// The line under the name: if the other party is writing or online, or that notes stay on the phone.
		/// </summary>
		public string? Subtitle
		{
			get
			{
				if (this.isSelfChat)
					return ServiceRef.Localizer[nameof(AppResources.ChatSelfSubtitle)];

				if (this.isGroup)
					return this.groupSubtitle;

				if (this.IsRemoteTyping)
					return ServiceRef.Localizer[nameof(AppResources.ChatTyping)];

				if (this.IsOnline)
					return ServiceRef.Localizer[nameof(AppResources.ChatOnline)];

				return null;
			}
		}

		/// <summary>
		/// If there is a line under the name.
		/// </summary>
		public bool HasSubtitle => !string.IsNullOrEmpty(this.Subtitle);

		/// <summary>
		/// The text being written.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCommand))]
		private string markdownInput = string.Empty;

		/// <summary>
		/// The sent message being corrected, if any.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasContext))]
		[NotifyPropertyChangedFor(nameof(ContextTitle))]
		[NotifyPropertyChangedFor(nameof(ContextText))]
		private ChatMessageItem? editingItem;

		/// <summary>
		/// The message being replied to, if any.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasContext))]
		[NotifyPropertyChangedFor(nameof(ContextTitle))]
		[NotifyPropertyChangedFor(nameof(ContextText))]
		private ChatMessageItem? replyItem;

		/// <summary>
		/// If the composer shows a message being corrected or replied to.
		/// </summary>
		public bool HasContext => this.EditingItem is not null || this.ReplyItem is not null;

		/// <summary>
		/// What the composer is doing: correcting or replying.
		/// </summary>
		public string ContextTitle
		{
			get
			{
				if (this.EditingItem is not null)
					return ServiceRef.Localizer[nameof(AppResources.ChatEditing)];

				if (this.ReplyItem is not null)
				{
					return this.ReplyItem.IsSent ?
						ServiceRef.Localizer[nameof(AppResources.ChatReplyingToYourMessage)] :
						ServiceRef.Localizer[nameof(AppResources.ChatReplyingTo), this.ReplyItem.SenderName ?? this.FriendlyName];
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// The start of the message being corrected or replied to.
		/// </summary>
		public string ContextText => Shorten((this.EditingItem ?? this.ReplyItem)?.Message.PlainText ?? string.Empty);

		/// <summary>
		/// If a message is being sent, or an image prepared.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCommand))]
		[NotifyCanExecuteChangedFor(nameof(AttachCommand))]
		private bool isSending;

		/// <summary>
		/// If the button that scrolls to the latest message shows.
		/// </summary>
		[ObservableProperty]
		private bool showsScrollToLatest;

		/// <summary>
		/// If messages have arrived below what is on screen.
		/// </summary>
		[ObservableProperty]
		private bool hasUnseenMessages;

		/// <summary>
		/// If the panel of things to share shows, in place of the keyboard.
		/// </summary>
		[ObservableProperty]
		private bool isShareTrayOpen;

		/// <summary>
		/// If the user's ID can be shared.
		/// </summary>
		public bool CanShareMyId => ServiceRef.TagProfile.LegalIdentity is not null;

		/// <summary>
		/// If a payment can be shared. Notes to yourself have no one to pay.
		/// </summary>
		public bool CanSharePayment => !this.isSelfChat && !this.isGroup;

		/// <summary>
		/// If the formatting bar shows under the text field.
		/// </summary>
		[ObservableProperty]
		private bool isFormatBarOpen;

		/// <summary>
		/// If the text field has the focus.
		/// </summary>
		[ObservableProperty]
		private bool isComposerFocused;

		/// <summary>
		/// The text being written, as it will look when sent, while it has formatting.
		/// </summary>
		[ObservableProperty]
		private MarkdownModel? previewModel;

		/// <summary>
		/// If the preview of the text being written shows.
		/// </summary>
		[ObservableProperty]
		private bool showsPreview;

		/// <summary>
		/// The message whose menu is open, if any.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsMenuOpen))]
		[NotifyPropertyChangedFor(nameof(MenuInfo))]
		[NotifyPropertyChangedFor(nameof(MenuHasInfo))]
		[NotifyPropertyChangedFor(nameof(MenuCanReply))]
		[NotifyPropertyChangedFor(nameof(MenuCanEdit))]
		[NotifyPropertyChangedFor(nameof(MenuShowsDelivered))]
		private ChatMessageItem? menuItem;

		/// <summary>
		/// If the menu of a message is open.
		/// </summary>
		public bool IsMenuOpen => this.MenuItem is not null;

		/// <summary>
		/// The first line of the menu: when the message was delivered, sent, saved or received.
		/// </summary>
		public string MenuInfo
		{
			get
			{
				ChatMessage? Message = this.MenuItem?.Message;
				if (Message is null)
					return string.Empty;

				if (this.isSelfChat)
					return ServiceRef.Localizer[nameof(AppResources.ChatInfoSaved), ChatMessageItem.FormatMoment(Message.Created)];

				if (Message.MessageType != MessageType.Sent)
					return ServiceRef.Localizer[nameof(AppResources.ChatInfoReceived), ChatMessageItem.FormatMoment(Message.Created)];

				if (Message.Delivered != DateTime.MinValue && !this.isGroup)
					return ServiceRef.Localizer[nameof(AppResources.ChatInfoDelivered), ChatMessageItem.FormatMoment(Message.Delivered)];

				return ServiceRef.Localizer[nameof(AppResources.ChatInfoSent), ChatMessageItem.FormatMoment(Message.Created)];
			}
		}

		/// <summary>
		/// If the menu has a first line.
		/// </summary>
		public bool MenuHasInfo => this.MenuItem is not null;

		/// <summary>
		/// If the menu's first line shows the two checks of a delivered message.
		/// </summary>
		public bool MenuShowsDelivered => this.MenuItem is not null && this.MenuItem.IsSent && this.MenuItem.IsDelivered && !this.isSelfChat &&
			!this.isGroup;

		/// <summary>
		/// If the message in the menu can be replied to.
		/// </summary>
		public bool MenuCanReply => this.MenuItem is not null && !this.isSelfChat;

		/// <summary>
		/// If the message in the menu can be corrected: only the user's own.
		/// </summary>
		public bool MenuCanEdit => this.MenuItem?.IsSent ?? false;

		#endregion

		#region Events to the page

		/// <summary>
		/// Asks the page to scroll to the latest message. The argument says if the scrolling is animated.
		/// </summary>
		public event EventHandler<bool>? ScrollToEndRequested;

		/// <summary>
		/// Tells the page that older rows were added at the top, so it can keep what was on screen in place. The argument is
		/// the number of rows added.
		/// </summary>
		public event EventHandler<int>? OlderRowsAdded;

		/// <summary>
		/// Asks the page to focus the text field.
		/// </summary>
		public event EventHandler? FocusComposerRequested;

		/// <summary>
		/// Asks the page to hide the keyboard, as when the share panel opens in its place.
		/// </summary>
		public event EventHandler? HideKeyboardRequested;

		#endregion

		#region Life cycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			ChatEvents.MessageStored += this.ChatEvents_MessageStored;
			ChatEvents.MessageDelivered += this.ChatEvents_MessageDelivered;
			ChatEvents.ChatStateChanged += this.ChatEvents_ChatStateChanged;
			ServiceRef.XmppService.OnPresence += this.XmppService_OnPresence;

			if (this.isGroup)
			{
				GroupStore.Changed += this.GroupStore_Changed;
				await this.LoadGroupAsync();
			}

			this.UpdatePresence();

			await this.LoadLatestAsync();
		}

		private void GroupStore_Changed(object? Sender, EventArgs e)
		{
			_ = this.LoadGroupAsync();
		}

		/// <summary>
		/// Shows the group's name, how many members it has and how many are online, and if the user has left it.
		/// </summary>
		private async Task LoadGroupAsync()
		{
			try
			{
				ChatGroup? Group = await GroupStore.FindAsync(this.BareJid);
				if (Group is null)
					return;

				int Online = GroupService.OnlineCount(Group.Address.Value);
				string Subtitle;

				if (Group.Status != GroupStatus.Joined)
					Subtitle = ServiceRef.Localizer[nameof(AppResources.GroupsYouLeft)];
				else if (Online > 1)
					Subtitle = ServiceRef.Localizer[nameof(AppResources.GroupsMembersOnline), Group.MemberCount, Online];
				else if (Group.MemberCount == 1)
					Subtitle = ServiceRef.Localizer[nameof(AppResources.GroupsOneMember)];
				else
					Subtitle = ServiceRef.Localizer[nameof(AppResources.GroupsMemberCount), Group.MemberCount];

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.groupLeft = Group.Status != GroupStatus.Joined;
					this.groupSubtitle = Subtitle;

					if (!string.IsNullOrWhiteSpace(Group.Name))
						this.FriendlyName = Group.Name;

					this.OnPropertyChanged(nameof(this.Subtitle));
					this.OnPropertyChanged(nameof(this.HasSubtitle));
					this.SendCommand.NotifyCanExecuteChanged();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ChatEvents.MessageStored -= this.ChatEvents_MessageStored;
			ChatEvents.MessageDelivered -= this.ChatEvents_MessageDelivered;
			ChatEvents.ChatStateChanged -= this.ChatEvents_ChatStateChanged;
			ServiceRef.XmppService.OnPresence -= this.XmppService_OnPresence;
			GroupStore.Changed -= this.GroupStore_Changed;

			this.pausedTimer?.Cancel();
			this.remoteTypingTimer?.Cancel();
			this.previewTimer?.Cancel();

			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Closes the menu of a message or the share panel, if one is open, and otherwise leaves the chat.
		/// </summary>
		public override async Task GoBack()
		{
			if (this.MenuItem is not null)
			{
				this.MenuItem = null;
				return;
			}

			if (this.IsShareTrayOpen)
			{
				this.IsShareTrayOpen = false;
				return;
			}

			await base.GoBack();
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			ChatEvents.SetVisibleChat(this.BareJid);

			try
			{
				await ServiceRef.NotificationService.DeleteEvents(NotificationEventType.Contacts, this.BareJid);
				await ConversationStore.MarkReadAsync(this.BareJid);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			ChatEvents.ClearVisibleChat(this.BareJid);

			if (this.isComposing)
				this.SetComposing(false, ChatState.Paused);

			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName == nameof(this.IsConnected))
			{
				this.SendCommand.NotifyCanExecuteChanged();
				this.AttachCommand.NotifyCanExecuteChanged();
			}
		}

		#endregion

		#region Loading

		private async Task LoadLatestAsync()
		{
			try
			{
				IEnumerable<ChatMessage> Found = await Database.Find<ChatMessage>(0, batchSize,
					new FilterFieldEqualTo("RemoteBareJid", this.BareJid), "-Created");

				List<ChatMessage> Messages = [.. Found];
				Messages.Reverse();

				List<ChatItem> Rows = await this.BuildRowsAsync(Messages);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.itemsByObjectId.Clear();
					this.Register(Rows);
					this.Items.Reset(Rows);
					this.RefreshGroups();

					this.oldestLoaded = Messages.Count > 0 ? Messages[0].Created : DateTime.MaxValue;
					this.ExistsMoreMessages = Messages.Count >= batchSize;
					this.IsLoading = false;
					this.OnPropertyChanged(nameof(this.IsEmpty));

					this.ScrollToEndRequested?.Invoke(this, false);
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
			}
		}

		private async Task LoadOlderAsync()
		{
			if (this.loadingOlder || !this.ExistsMoreMessages)
				return;

			this.loadingOlder = true;

			try
			{
				IEnumerable<ChatMessage> Found = await Database.Find<ChatMessage>(0, batchSize, new FilterAnd(
					new FilterFieldEqualTo("RemoteBareJid", this.BareJid),
					new FilterFieldLesserThan("Created", this.oldestLoaded)), "-Created");

				List<ChatMessage> Messages = [.. Found];
				Messages.Reverse();

				List<ChatItem> Rows = await this.BuildRowsAsync(Messages);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.ExistsMoreMessages = Messages.Count >= batchSize;

					if (Messages.Count == 0)
						return;

					this.oldestLoaded = Messages[0].Created;

					// The day of the first loaded row is shown again at the top of the new rows.
					ChatMessage Last = Messages[^1];
					int Removed = 0;

					if (this.Items.Count > 0 && this.Items[0] is ChatDayItem Day && Day.Timestamp == Last.Created.ToLocalTime().Date)
					{
						this.Items.RemoveAt(0);
						Removed = 1;
					}

					this.Register(Rows);
					this.Items.InsertRange(0, Rows);
					this.RefreshGroups();

					this.OlderRowsAdded?.Invoke(this, Rows.Count - Removed);
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.loadingOlder = false;
			}
		}

		/// <summary>
		/// Builds the rows of messages, oldest first, with a row for each day. The texts are built in parallel, off the UI
		/// thread.
		/// </summary>
		private async Task<List<ChatItem>> BuildRowsAsync(List<ChatMessage> Messages)
		{
			MarkdownModel[] Models = await Task.WhenAll(Messages.Select(ChatMessageItem.BuildModelAsync));
			List<ChatItem> Rows = new(Messages.Count + 4);
			ChatMessage? Previous = null;

			for (int i = 0; i < Messages.Count; i++)
			{
				ChatMessage Message = Messages[i];
				DateTime Day = Message.Created.ToLocalTime().Date;

				if (Previous is null || Previous.Created.ToLocalTime().Date != Day)
					Rows.Add(new ChatDayItem(Day));

				Rows.Add(this.CreateItem(Message, Models[i]));
				Previous = Message;
			}

			return Rows;
		}

		private ChatMessageItem CreateItem(ChatMessage Message, MarkdownModel Model)
		{
			string Sender = Message.MessageType == MessageType.Sent ?
				ServiceRef.Localizer[nameof(AppResources.ChatYou)] : Message.SenderName ?? this.FriendlyName;

			return new ChatMessageItem(Message, Model, this.isSelfChat, this.LinkCommand, this.ActionsCommand, this.ReplyCommand)
			{
				ImageSubtitle = Sender + ", " + ChatMessageItem.FormatMoment(Message.Created),
				IsInGroup = this.isGroup,
				SenderName = Message.SenderName,
				SenderColor = SenderColorOf(Message.SenderName)
			};
		}

		/// <summary>
		/// The colour of a sender's name in a group: the same one each time for the same name.
		/// </summary>
		private static Color SenderColorOf(string? Name)
		{
			if (string.IsNullOrEmpty(Name))
				return Colors.Gray;

			int i = (int)((uint)StringComparer.Ordinal.GetHashCode(Name) % (uint)senderColors.Length);
			return Color.FromArgb(senderColors[i]);
		}

		private static readonly string[] senderColors = ["#085E50", "#24519C", "#A84700", "#722FAD", "#444A4F"];

		/// <summary>
		/// Works out which messages start and end a run of messages, so bubbles get their spacing and corners, and removes
		/// days that have no messages left.
		/// </summary>
		private void RefreshGroups()
		{
			for (int i = this.Items.Count - 1; i >= 0; i--)
			{
				if (this.Items[i] is ChatDayItem && (i + 1 >= this.Items.Count || this.Items[i + 1] is ChatDayItem))
					this.Items.RemoveAt(i);
			}

			ChatMessageItem? Previous = null;

			foreach (ChatItem Row in this.Items)
			{
				if (Row is not ChatMessageItem Item)
					continue;

				bool Starts = StartsGroup(Item.Message, Previous?.Message);
				Item.IsFirstInGroup = Starts;

				if (Previous is not null)
					Previous.IsLastInGroup = Starts;

				Previous = Item;
			}

			if (Previous is not null)
				Previous.IsLastInGroup = true;
		}

		private void Register(IEnumerable<ChatItem> Rows)
		{
			foreach (ChatItem Row in Rows)
			{
				if (Row is ChatMessageItem Item && !string.IsNullOrEmpty(Item.ObjectId))
					this.itemsByObjectId[Item.ObjectId] = Item;
			}
		}

		/// <summary>
		/// If a message starts a new run: another day, another party, or a while after the one before.
		/// </summary>
		private static bool StartsGroup(ChatMessage Message, ChatMessage? Previous)
		{
			return Previous is null ||
				Previous.MessageType != Message.MessageType ||
				Previous.Created.ToLocalTime().Date != Message.Created.ToLocalTime().Date ||
				(Message.Created - Previous.Created).Duration() > groupInterval ||
				!string.Equals(Previous.SenderName, Message.SenderName, StringComparison.Ordinal);
		}

		/// <summary>
		/// Called by the page when the list has scrolled.
		/// </summary>
		/// <param name="FirstVisible">Index of the first row on screen.</param>
		/// <param name="LastVisible">Index of the last row on screen.</param>
		public void OnListScrolled(int FirstVisible, int LastVisible)
		{
			this.isAtBottom = LastVisible >= this.Items.Count - 2;
			this.ShowsScrollToLatest = !this.isAtBottom && this.Items.Count > 0;

			if (this.isAtBottom)
				this.HasUnseenMessages = false;

			if (FirstVisible <= 2 && this.ExistsMoreMessages && !this.IsLoading)
				_ = this.LoadOlderAsync();
		}

		#endregion

		#region Messages from the connection

		private void ChatEvents_MessageStored(object? Sender, ChatMessageEventArgs e)
		{
			if (!string.Equals(e.RemoteBareJid, this.BareJid, StringComparison.OrdinalIgnoreCase))
				return;

			_ = this.ShowStoredMessageAsync(e.Message, e.IsUpdate);
		}

		private async Task ShowStoredMessageAsync(ChatMessage Message, bool IsUpdate)
		{
			try
			{
				MarkdownModel Model = await ChatMessageItem.BuildModelAsync(Message);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					string ObjectId = Message.ObjectId ?? string.Empty;

					if (this.itemsByObjectId.TryGetValue(ObjectId, out ChatMessageItem? Existing))
					{
						Existing.Update(Message, Model);
						return;
					}

					if (IsUpdate)
						return;     // A correction of a message that is not loaded.

					if (Message.MessageType == MessageType.Received)
						this.IsRemoteTyping = false;

					ChatMessage? Previous = null;
					for (int i = this.Items.Count - 1; i >= 0; i--)
					{
						if (this.Items[i] is ChatMessageItem Row)
						{
							Previous = Row.Message;
							break;
						}
					}

					DateTime Day = Message.Created.ToLocalTime().Date;

					if (Previous is null || Previous.Created.ToLocalTime().Date != Day)
						this.Items.Add(new ChatDayItem(Day));

					ChatMessageItem Item = this.CreateItem(Message, Model);

					if (!string.IsNullOrEmpty(ObjectId))
						this.itemsByObjectId[ObjectId] = Item;

					this.Items.Add(Item);
					this.RefreshGroups();
					this.OnPropertyChanged(nameof(this.IsEmpty));

					if (this.isAtBottom || Item.IsSent)
						this.ScrollToEndRequested?.Invoke(this, true);
					else
						this.HasUnseenMessages = true;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void ChatEvents_MessageDelivered(object? Sender, ChatMessageEventArgs e)
		{
			if (!string.Equals(e.RemoteBareJid, this.BareJid, StringComparison.OrdinalIgnoreCase))
				return;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (this.itemsByObjectId.TryGetValue(e.Message.ObjectId ?? string.Empty, out ChatMessageItem? Item))
					Item.MarkDelivered();
			});
		}

		private void ChatEvents_ChatStateChanged(object? Sender, ChatStateEventArgs e)
		{
			if (!string.Equals(e.RemoteBareJid, this.BareJid, StringComparison.OrdinalIgnoreCase))
				return;

			bool Typing = e.State == ChatState.Composing;

			this.remoteTypingTimer?.Cancel();
			this.remoteTypingTimer = null;

			if (Typing)
			{
				// If the other party leaves without saying so, the indicator goes away by itself.
				CancellationTokenSource Timer = new();
				this.remoteTypingTimer = Timer;

				_ = Task.Delay(typingTimeout, Timer.Token).ContinueWith(T =>
				{
					if (!T.IsCanceled)
						MainThread.BeginInvokeOnMainThread(() => this.IsRemoteTyping = false);
				}, TaskScheduler.Default);
			}

			MainThread.BeginInvokeOnMainThread(() => this.IsRemoteTyping = Typing);
		}

		private Task XmppService_OnPresence(object? Sender, PresenceEventArgs e)
		{
			if (string.Equals(e.FromBareJID, this.BareJid, StringComparison.OrdinalIgnoreCase))
				MainThread.BeginInvokeOnMainThread(this.UpdatePresence);

			return Task.CompletedTask;
		}

		private void UpdatePresence()
		{
			if (this.isSelfChat)
			{
				this.IsOnline = false;
				return;
			}

			try
			{
				RosterItem? Item = ServiceRef.XmppService.GetRosterItem(this.BareJid);

				this.IsOnline = Item is not null &&
					(Item.State == SubscriptionState.To || Item.State == SubscriptionState.Both) &&
					Item.HasLastPresence &&
					(Item.LastPresence.Availability == Availability.Online || Item.LastPresence.Availability == Availability.Chat);
			}
			catch (Exception)
			{
				this.IsOnline = false;
			}
		}

		#endregion

		#region Writing and sending

		partial void OnMarkdownInputChanged(string value)
		{
			this.SchedulePreview(value);

			if (this.isSelfChat)
				return;

			if (string.IsNullOrEmpty(value))
			{
				if (this.isComposing)
					this.SetComposing(false, ChatState.Active);

				return;
			}

			if (!this.isComposing)
				this.SetComposing(true, ChatState.Composing);

			// Paused when the user stops writing for a while.
			this.pausedTimer?.Cancel();
			CancellationTokenSource Timer = new();
			this.pausedTimer = Timer;

			_ = Task.Delay(pausedAfter, Timer.Token).ContinueWith(T =>
			{
				if (!T.IsCanceled)
					MainThread.BeginInvokeOnMainThread(() => this.SetComposing(false, ChatState.Paused));
			}, TaskScheduler.Default);
		}

		/// <summary>
		/// Shows how the text being written will look, once the user has stopped typing for a moment, while it has
		/// formatting. The preview is built off the UI thread.
		/// </summary>
		private void SchedulePreview(string Text)
		{
			this.previewTimer?.Cancel();
			this.previewTimer = null;

			if (!ChatFormatting.HasFormatting(Text))
			{
				this.ShowsPreview = false;
				this.PreviewModel = null;
				return;
			}

			CancellationTokenSource Timer = new();
			this.previewTimer = Timer;

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(previewDelay, Timer.Token);

					MarkdownModel Model = await MarkdownModel.FromMarkdownAsync(ChatMarkdown.KeepLineBreaks(Text));

					if (Timer.IsCancellationRequested)
						return;

					MainThread.BeginInvokeOnMainThread(() =>
					{
						if (Timer.IsCancellationRequested)
							return;

						this.PreviewModel = Model;
						this.ShowsPreview = true;
					});
				}
				catch (TaskCanceledException)
				{
					// A newer text replaced this one.
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		private void SetComposing(bool Composing, ChatState State)
		{
			if (!Composing)
			{
				this.pausedTimer?.Cancel();
				this.pausedTimer = null;
			}

			if (this.isComposing == Composing)
				return;

			this.isComposing = Composing;
			ChatSender.SendChatState(this.BareJid, State);
		}

		private bool CanSend()
		{
			return (this.IsConnected || this.isSelfChat) && !this.IsSending && !this.groupLeft && !string.IsNullOrWhiteSpace(this.MarkdownInput);
		}

		/// <summary>
		/// Sends what has been written, or the correction of a sent message.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanSend))]
		private async Task Send()
		{
			string Text = this.MarkdownInput.TrimEnd();
			if (string.IsNullOrWhiteSpace(Text))
				return;

			string? ReplaceObjectId = this.EditingItem?.ObjectId;

			if (this.ReplyItem is not null)
			{
				// In a group, the quote says whose message it is, by their name in the group.
				string? QuotedSender = this.isGroup && !this.ReplyItem.IsSent ? this.ReplyItem.SenderName : null;
				Text = Quote(this.ReplyItem.Message, QuotedSender) + Environment.NewLine + Environment.NewLine + Text;
			}

			this.isComposing = false;
			this.pausedTimer?.Cancel();
			this.pausedTimer = null;

			this.IsSending = true;

			try
			{
				await ChatSender.SendAsync(this.BareJid, Text, ReplaceObjectId);

				this.MarkdownInput = string.Empty;
				this.EditingItem = null;
				this.ReplyItem = null;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsSending = false;
			}
		}

		/// <summary>
		/// Stops correcting or replying to a message.
		/// </summary>
		[RelayCommand]
		private void CancelContext()
		{
			if (this.EditingItem is not null)
				this.MarkdownInput = string.Empty;

			this.EditingItem = null;
			this.ReplyItem = null;
		}

		/// <summary>
		/// Scrolls to the latest message.
		/// </summary>
		[RelayCommand]
		private void ScrollToLatest()
		{
			this.HasUnseenMessages = false;
			this.ScrollToEndRequested?.Invoke(this, true);
		}

		/// <summary>
		/// The start of a message as a quote, with the sender's name first when it is given.
		/// </summary>
		private static string Quote(ChatMessage Message, string? Sender = null)
		{
			string Text = Shorten(Message.PlainText);
			StringBuilder Result = new();

			if (!string.IsNullOrWhiteSpace(Sender))
			{
				Result.Append("> **");
				Result.Append(MarkdownDocument.Encode(Sender));
				Result.Append("**");
			}

			foreach (string Row in Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
			{
				if (Result.Length > 0)
					Result.AppendLine();

				Result.Append("> ");
				Result.Append(MarkdownDocument.Encode(Row));
			}

			return Result.ToString();
		}

		/// <summary>
		/// The start of a text: its first lines, at most a few hundred characters.
		/// </summary>
		private static string Shorten(string Text)
		{
			string[] Rows = Text.Trim().Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			string Result = string.Join("\n", Rows.Take(quoteMaxLines));
			bool Cut = Rows.Length > quoteMaxLines;

			if (Result.Length > quoteMaxLength)
			{
				Result = Result[..quoteMaxLength].TrimEnd();
				Cut = true;
			}

			return Cut ? Result + "…" : Result;
		}

		#endregion

		#region Message actions

		/// <summary>
		/// Opens the menu of a message, next to it. The page shows the message lifted over the dimmed chat.
		/// </summary>
		private void OpenMenu(ChatMessageItem? Item)
		{
			if (Item is null)
				return;

			ServiceRef.LogService.LogDebug("ChatMessageActionsOpened", new KeyValuePair<string, object?>("Sent", Item.IsSent));

			this.IsShareTrayOpen = false;
			this.HideKeyboardRequested?.Invoke(this, EventArgs.Empty);
			this.MenuItem = Item;
		}

		/// <summary>
		/// Closes the menu of a message.
		/// </summary>
		[RelayCommand]
		private void CloseMenu()
		{
			this.MenuItem = null;
		}

		/// <summary>
		/// Starts a reply to a message.
		/// </summary>
		private void StartReply(ChatMessageItem? Item)
		{
			if (Item is null || this.isSelfChat)
				return;

			this.EditingItem = null;
			this.ReplyItem = Item;
			this.FocusComposerRequested?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Replies to the message in the menu.
		/// </summary>
		[RelayCommand]
		private void ReplyToMenuItem()
		{
			ChatMessageItem? Item = this.MenuItem;
			this.MenuItem = null;
			this.StartReply(Item);
		}

		/// <summary>
		/// Copies the text of the message in the menu.
		/// </summary>
		[RelayCommand]
		private async Task CopyMenuItem()
		{
			ChatMessageItem? Item = this.MenuItem;
			this.MenuItem = null;

			if (Item is null)
				return;

			try
			{
				await Clipboard.SetTextAsync(MarkdownOf(Item.Message));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Corrects the message in the menu: its text goes back into the text field.
		/// </summary>
		[RelayCommand]
		private void EditMenuItem()
		{
			ChatMessageItem? Item = this.MenuItem;
			this.MenuItem = null;

			if (Item is null || !Item.IsSent)
				return;

			this.ReplyItem = null;
			this.EditingItem = Item;
			this.MarkdownInput = MarkdownOf(Item.Message);
			this.FocusComposerRequested?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Sends the message in the menu to another contact, picked from the contact list.
		/// </summary>
		[RelayCommand]
		private async Task ForwardMenuItem()
		{
			ChatMessageItem? Item = this.MenuItem;
			this.MenuItem = null;

			if (Item is null)
				return;

			try
			{
				TaskCompletionSource<ContactInfoModel?> Selected = new();
				ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.ChatForwardTo)], Selected);

				await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);

				ContactInfoModel? Contact = await Selected.Task;
				if (Contact is null)
					return;

				string? To = Contact.BareJid?.Value;
				if (string.IsNullOrEmpty(To))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.ChatForwardNotPossible)]);
					return;
				}

				// Photos kept only on this phone, as in notes to yourself, are uploaded first, so the contact can see them.
				string Markdown = MarkdownOf(Item.Message);
				if (!ChatSender.IsSelf(To))
					Markdown = await ChatAttachments.UploadLocalImagesAsync(Markdown);

				await ChatSender.SendAsync(To, Markdown);
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ChatForwarded)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Deletes the message in the menu from this phone. The other party keeps their copy.
		/// </summary>
		[RelayCommand]
		private async Task DeleteMenuItem()
		{
			ChatMessageItem? Item = this.MenuItem;
			this.MenuItem = null;

			if (Item is null)
				return;

			try
			{
				await Database.Delete(Item.Message);
				ChatEvents.OnMessageDeleted(Item.Message);

				if (this.EditingItem == Item)
				{
					this.EditingItem = null;
					this.MarkdownInput = string.Empty;
				}

				if (this.ReplyItem == Item)
					this.ReplyItem = null;

				this.itemsByObjectId.Remove(Item.ObjectId);
				this.Items.Remove(Item);
				this.RefreshGroups();
				this.OnPropertyChanged(nameof(this.IsEmpty));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// The markdown of a message, or its plain text written as markdown.
		/// </summary>
		private static string MarkdownOf(ChatMessage Message)
		{
			return string.IsNullOrEmpty(Message.Markdown) ? MarkdownDocument.Encode(Message.PlainText) : Message.Markdown;
		}

		#endregion

		#region Sharing

		private bool CanAttach()
		{
			return (this.IsConnected || this.isSelfChat) && !this.IsSending;
		}

		/// <summary>
		/// Opens the panel of things to share in place of the keyboard, or closes it.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanAttach))]
		private void Attach()
		{
			if (this.IsShareTrayOpen)
			{
				this.IsShareTrayOpen = false;
				this.FocusComposerRequested?.Invoke(this, EventArgs.Empty);
				return;
			}

			this.IsFormatBarOpen = false;
			this.HideKeyboardRequested?.Invoke(this, EventArgs.Empty);
			this.IsShareTrayOpen = true;
			this.OnPropertyChanged(nameof(this.CanShareMyId));
		}

		/// <summary>
		/// Called by the page when the text field gets or loses the focus. Typing closes the share panel.
		/// </summary>
		/// <param name="Focused">If the text field has the focus.</param>
		public void OnComposerFocusChanged(bool Focused)
		{
			this.IsComposerFocused = Focused;

			if (Focused)
				this.IsShareTrayOpen = false;
		}

		/// <summary>
		/// Shows or hides the formatting bar.
		/// </summary>
		[RelayCommand]
		private void ToggleFormatBar()
		{
			this.IsFormatBarOpen = !this.IsFormatBarOpen;

			if (this.IsFormatBarOpen)
			{
				this.IsShareTrayOpen = false;
				this.FocusComposerRequested?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Shares something from the share panel, which closes.
		/// </summary>
		/// <param name="Kind">What to share: Photos, Camera, Contact, MyId, Contract, Payment, Token or Thing.</param>
		[RelayCommand]
		private async Task Share(string? Kind)
		{
			this.IsShareTrayOpen = false;

			try
			{
				switch (Kind)
				{
					case "Photos":
						await this.PickPhotoAsync();
						break;

					case "Camera":
						await this.TakePhotoAsync();
						break;

					case "Contact":
						await this.ShareContactAsync();
						break;

					case "MyId":
						await this.ShareMyIdAsync();
						break;

					case "Contract":
						await this.ShareContractAsync();
						break;

					case "Payment":
						if (!this.isSelfChat)
							await this.SharePaymentAsync();
						break;

					case "Token":
						await this.ShareTokenAsync();
						break;

					case "Thing":
						await this.ShareThingAsync();
						break;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		private async Task SendMarkdownAsync(string Markdown)
		{
			try
			{
				await ChatSender.SendAsync(this.BareJid, Markdown);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		private async Task PickPhotoAsync()
		{
			if (!this.isSelfChat && !ServiceRef.XmppService.FileUploadIsSupported)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PickPhoto)],
					ServiceRef.Localizer[nameof(AppResources.SelectingAPhotoIsNotSupported)]);
				return;
			}

			IReadOnlyList<FileResult> Picked = await MediaPicker.PickPhotosAsync(new MediaPickerOptions()
			{
				SelectionLimit = 1
			});
			FileResult? Photo = Picked.FirstOrDefault();

			if (Photo is not null)
				await this.SendPhotoAsync(Photo.FullPath, false);
		}

		private async Task TakePhotoAsync()
		{
			if (!this.isSelfChat && !ServiceRef.XmppService.FileUploadIsSupported)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
					ServiceRef.Localizer[nameof(AppResources.ServerDoesNotSupportFileUpload)]);
				return;
			}

			if (!await ServiceRef.PermissionService.CheckCameraPermissionAsync())
				return;

			FileResult? Captured;

			try
			{
				if (DeviceInfo.Platform == DevicePlatform.iOS)
				{
					Captured = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions()
					{
						Title = ServiceRef.Localizer[nameof(AppResources.TakePhotoToShare)]
					});
				}
				else
					Captured = await MediaPicker.CapturePhotoAsync();
			}
			catch (Exception Ex)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
					ServiceRef.Localizer[nameof(AppResources.TakingAPhotoIsNotSupported)] + ": " + Ex.Message);
				return;
			}

			if (Captured is not null)
				await this.SendPhotoAsync(Captured.FullPath, true);
		}

		private async Task SendPhotoAsync(string FilePath, bool DeleteFile)
		{
			this.IsSending = true;

			try
			{
				byte[] Data = await File.ReadAllBytesAsync(FilePath);
				string FileName = Path.GetFileName(FilePath);

				string Markdown = this.isSelfChat ?
					await ChatAttachments.StoreNoteImageAsync(Data, FileName) :
					await ChatAttachments.UploadImageAsync(Data, FileName);

				await ChatSender.SendAsync(this.BareJid, Markdown);

				if (DeleteFile)
					File.Delete(FilePath);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Ex.Message);
			}
			finally
			{
				this.IsSending = false;
			}
		}

		private async Task ShareContactAsync()
		{
			TaskCompletionSource<ContactInfoModel?> Selected = new();
			ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.SelectContact)], Selected)
			{
				CanScanQrCode = true
			};

			await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);

			ContactInfoModel? Contact = await Selected.Task;
			if (Contact is null)
				return;

			if (Contact.LegalIdentity is not null)
				await this.SendMarkdownAsync(IdentityMarkdown(Contact.LegalIdentity));
			else if (!string.IsNullOrEmpty(Contact.LegalId))
				await this.SendMarkdownAsync("![" + MarkdownDocument.Encode(Contact.FriendlyName) + "](" + ContractsClient.LegalIdUriString(Contact.LegalId) + ")");
			else if (!string.IsNullOrEmpty(Contact.BareJid))
				await this.SendMarkdownAsync("![" + MarkdownDocument.Encode(Contact.FriendlyName) + "](xmpp:" + Contact.BareJid + "?subscribe)");
		}

		private Task ShareMyIdAsync()
		{
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			return Identity is null ? Task.CompletedTask : this.SendMarkdownAsync(IdentityMarkdown(Identity));
		}

		private static string IdentityMarkdown(LegalIdentity Identity)
		{
			StringBuilder Markdown = new();

			Markdown.Append("```");
			Markdown.AppendLine(Constants.UriSchemes.IotId);
			Identity.Serialize(Markdown, true, true, true, true, true, true, true);
			Markdown.AppendLine();
			Markdown.AppendLine("```");

			return Markdown.ToString();
		}

		private async Task ShareContractAsync()
		{
			TaskCompletionSource<Contract?> Selected = new();
			MyContractsNavigationArgs Args = new(ContractsListMode.Contracts, Selected);

			await ServiceRef.NavigationService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);

			Contract? Contract = await Selected.Task;
			if (Contract is null)
				return;

			StringBuilder Markdown = new();

			Markdown.Append("```");
			Markdown.AppendLine(Constants.UriSchemes.IotSc);
			Contract.Serialize(Markdown, true, true, true, true, true, true, true);
			Markdown.AppendLine();
			Markdown.AppendLine("```");

			await this.SendMarkdownAsync(Markdown.ToString());
		}

		private async Task SharePaymentAsync()
		{
			StringBuilder Sb = new();

			Sb.Append("edaler:");

			if (!string.IsNullOrEmpty(this.LegalId))
			{
				Sb.Append("ti=");
				Sb.Append(this.LegalId);
			}
			else if (!string.IsNullOrEmpty(this.BareJid))
			{
				Sb.Append("t=");
				Sb.Append(this.BareJid);
			}
			else
				return;

			Balance CurrentBalance = await ServiceRef.XmppService.GetEDalerBalance();

			Sb.Append(";cu=");
			Sb.Append(CurrentBalance.Currency);

			if (!EDalerUri.TryParse(Sb.ToString(), out EDalerUri Parsed))
				return;

			TaskCompletionSource<string?> UriToSend = new();
			EDalerUriNavigationArgs Args = new(Parsed, this.FriendlyName, UriToSend);

			await ServiceRef.NavigationService.GoToAsync(nameof(SendPaymentPage), Args, BackMethod.Pop);

			string? Uri = await UriToSend.Task;
			if (string.IsNullOrEmpty(Uri) || !EDalerUri.TryParse(Uri, out Parsed))
				return;

			Sb.Clear();
			Sb.Append(MoneyToString.ToString(Parsed.Amount));

			if (Parsed.AmountExtra.HasValue)
			{
				Sb.Append(" (+");
				Sb.Append(MoneyToString.ToString(Parsed.AmountExtra.Value));
				Sb.Append(')');
			}

			Sb.Append(' ');
			Sb.Append(Parsed.Currency);

			await this.SendMarkdownAsync("![" + Sb.ToString() + "](" + Uri + ")");
		}

		private async Task ShareTokenAsync()
		{
			MyTokensNavigationArgs Args = new();

			await ServiceRef.NavigationService.GoToAsync(nameof(MyTokensPage), Args, BackMethod.Pop);

			TokenItem? Selected = await Args.TokenItemProvider.Task;
			if (Selected is null)
				return;

			if (!this.isSelfChat && !await TokenShareFormatter.ConfirmShareAsync(Selected.Token, this.FriendlyName))
				return;

			await this.SendMarkdownAsync(TokenShareFormatter.ToChatMarkdown(Selected.Token));
		}

		private async Task ShareThingAsync()
		{
			TaskCompletionSource<ContactInfoModel?> ThingToShare = new();
			MyThingsNavigationArgs Args = new(ThingToShare);

			await ServiceRef.NavigationService.GoToAsync(nameof(MyThingsPage), Args, BackMethod.Pop);

			ContactInfoModel? Thing = await ThingToShare.Task;
			string? BareJid = Thing?.BareJid?.Value;
			if (Thing is null || string.IsNullOrEmpty(BareJid))
				return;

			StringBuilder Sb = new();

			Sb.Append("![");
			Sb.Append(MarkdownDocument.Encode(string.IsNullOrWhiteSpace(Thing.FriendlyName) ? BareJid : Thing.FriendlyName));
			Sb.Append("](");
			Sb.Append(ThingLink(BareJid, Thing.SourceId, Thing.Partition, Thing.NodeId));
			Sb.Append(')');

			await this.SendMarkdownAsync(Sb.ToString());
		}

		/// <summary>
		/// Makes the iotdisco link of a thing. The values are escaped, as the thing registry reads them, so node IDs with
		/// spaces, semicolons or parentheses keep the link, and the markdown around it, whole.
		/// </summary>
		private static string ThingLink(string BareJid, string? SourceId, string? Partition, string? NodeId)
		{
			StringBuilder Sb = new();

			Sb.Append(Constants.UriSchemes.IotDisco);
			Sb.Append(":JID=");
			Sb.Append(Uri.EscapeDataString(BareJid));

			if (!string.IsNullOrEmpty(SourceId))
			{
				Sb.Append(";SID=");
				Sb.Append(Uri.EscapeDataString(SourceId));
			}

			if (!string.IsNullOrEmpty(Partition))
			{
				Sb.Append(";PT=");
				Sb.Append(Uri.EscapeDataString(Partition));
			}

			if (!string.IsNullOrEmpty(NodeId))
			{
				Sb.Append(";NID=");
				Sb.Append(Uri.EscapeDataString(NodeId));
			}

			return Sb.ToString();
		}

		#endregion

		#region Links

		/// <summary>
		/// Opens a link or card in a message. Identities, contracts and tokens sent in the message itself open from there.
		/// </summary>
		private async Task OpenLinkAsync(string? Uri)
		{
			if (string.IsNullOrEmpty(Uri))
				return;

			try
			{
				string? Scheme = Constants.UriSchemes.GetScheme(Uri);

				if (Scheme == Constants.UriSchemes.Xmpp)
				{
					await ProcessXmppUri(Uri);
					return;
				}

				int i = Uri.IndexOf(':');
				string Xml = i < 0 ? string.Empty : Uri[(i + 1)..].Trim();

				if (Xml.StartsWith('<') && Xml.EndsWith('>'))
				{
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(Xml);

					switch (Scheme)
					{
						case Constants.UriSchemes.IotId:
							LegalIdentity Identity = LegalIdentity.Parse(Doc.DocumentElement);
							await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity), BackMethod.Pop);
							return;

						case Constants.UriSchemes.IotSc:
							ParsedContract? Parsed = await Contract.Parse(Doc.DocumentElement, ServiceRef.XmppService.ContractsClient, true);
							if (Parsed?.Contract is null)
								throw new Exception(ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeOpened)]);

							await ServiceRef.NavigationService.GoToAsync(nameof(ViewContractPage), new ViewContractNavigationArgs(Parsed.Contract, false), BackMethod.Pop);
							return;

						case Constants.UriSchemes.NeuroFeature:
							Token? Token = await Token.TryParse(Doc.DocumentElement);
							if (Token is null)
								throw new Exception(ServiceRef.Localizer[nameof(AppResources.InvalidNeuroFeatureToken)]);

							// A shared copy is only shown as genuine after it has been checked with the token's Neuron.
							await ServiceRef.NeuroWalletOrchestratorService.OpenTokenAsync(Token.TokenId, Token);
							return;
					}
				}

				await App.OpenUrlAsync(Uri);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Opens the contact's card, or the user's own ID in notes to yourself.
		/// </summary>
		[RelayCommand]
		private async Task OpenContact()
		{
			try
			{
				if (this.isGroup)
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(GroupInfoPage), new GroupInfoNavigationArgs(this.BareJid), BackMethod.Pop);
					return;
				}

				if (this.isSelfChat)
				{
					LegalIdentity? Own = ServiceRef.TagProfile.LegalIdentity;
					if (Own is not null)
						await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Own));

					return;
				}

				Contact? Card = await ContactStore.FindAsync(this.BareJid, this.LegalId);
				if (Card is not null)
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(ContactCardPage), new ContactCardNavigationArgs(Card), BackMethod.Pop);
					return;
				}

				ContactInfo? Info = await ContactInfo.FindByBareJid(this.BareJid);

				if (Info?.LegalIdentity is not null)
					await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Info.LegalIdentity));
				else
				{
					string? Id = string.IsNullOrEmpty(Info?.LegalId) ? this.LegalId : Info.LegalId;

					if (!string.IsNullOrEmpty(Id))
						await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(Id, ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);
				}
			}
			catch (Exception Ex)
			{
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Processes an XMPP URI
		/// </summary>
		/// <param name="Uri">XMPP URI</param>
		/// <returns>If URI could be processed.</returns>
		public static async Task<bool> ProcessXmppUri(string Uri)
		{
			int I = Uri.IndexOf(':');
			if (I < 0)
				return false;

			string Jid = Uri[(I + 1)..].TrimStart();
			string Command;

			I = Jid.IndexOf('?');
			if (I < 0)
				Command = "subscribe";
			else
			{
				Command = Jid[(I + 1)..].TrimStart();
				Jid = Jid[..I].TrimEnd();
			}

			Jid = System.Web.HttpUtility.UrlDecode(Jid);
			Jid = XmppClient.GetBareJID(Jid);

			switch (Command.ToLower(CultureInfo.InvariantCulture))
			{
				case "join":
					// A group's invite link: the user is asked, then joins and sees the chat.
					if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.GroupsJoin)],
						ServiceRef.Localizer[nameof(AppResources.GroupsJoinLinkQuestion)],
						ServiceRef.Localizer[nameof(AppResources.GroupsJoin)], ServiceRef.Localizer[nameof(AppResources.Cancel)]))
					{
						return true;
					}

					ChatGroup Joined = await GroupService.JoinByLinkAsync(Jid);
					await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(null, Joined.Address.Value, Joined.Name),
						BackMethod.Pop, Joined.Address.Value);
					return true;

				case "subscribe":
					// A shared contact: someone the user has is opened, and anyone else can be added.
					Contact? Existing = await ContactStore.FindByChatAddressAsync(Jid);
					if (Existing is not null)
					{
						await ServiceRef.NavigationService.GoToAsync(nameof(ContactCardPage), new ContactCardNavigationArgs(Existing), BackMethod.Pop);
						return true;
					}

					int At = Jid.IndexOf('@');
					string Name = At > 0 ? Jid[..At] : Jid;

					if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactAddTitle)],
						ServiceRef.Localizer[nameof(AppResources.ContactAddQuestion), Name],
						ServiceRef.Localizer[nameof(AppResources.ContactAdd)], ServiceRef.Localizer[nameof(AppResources.Cancel)]))
					{
						return true;
					}

					Contact Added = await ContactStore.AddOrUpdateAsync(Jid, null, null);
					ServiceRef.XmppService.RequestPresenceSubscription(Jid, ContactRequests.OwnIdentityXml());
					await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactRequestSent), Added.DisplayName]);
					return true;

				default:
					return false;
			}
		}

		#endregion

		/// <summary>
		/// Initials of a name: the first letters of its first two words, or the name itself if it is short, such as "Me".
		/// </summary>
		private static string GetInitials(string Name, bool Whole)
		{
			Name = Name.Trim();

			if (Whole && Name.Length <= 3)
				return Name;

			if (Name.Contains('@'))
				return Name.Length > 0 ? char.ToUpper(Name[0], CultureInfo.CurrentCulture).ToString() : string.Empty;

			string[] Words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			StringBuilder Result = new();

			foreach (string Word in Words.Take(2))
				Result.Append(char.ToUpper(Word[0], CultureInfo.CurrentCulture));

			return Result.ToString();
		}

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public bool IsLinkable => !this.isSelfChat;

		/// <summary>
		/// If App links should be encoded with the link.
		/// </summary>
		public bool EncodeAppLinks => true;

		/// <summary>
		/// Link to the current view
		/// </summary>
		public string Link => Constants.UriSchemes.Xmpp + ":" + this.BareJid;

		/// <summary>
		/// Title of the current view
		/// </summary>
		public Task<string> Title => Task.FromResult(this.FriendlyName);

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		public bool HasMedia => false;

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public byte[]? Media => null;

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public string? MediaContentType => null;

		#endregion
	}
}
