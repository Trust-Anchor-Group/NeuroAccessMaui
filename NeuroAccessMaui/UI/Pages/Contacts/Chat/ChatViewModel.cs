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
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Controls.Markdown;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.SendPayment;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo;
using NeuroFeatures;
using Waher.Content.Markdown;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Events;
using Waher.Persistence;
using Waher.Persistence.Filters;

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

		private readonly Dictionary<string, ChatMessageItem> itemsByObjectId = new(StringComparer.Ordinal);
		private readonly bool isSelfChat;
		private DateTime oldestLoaded = DateTime.MaxValue;
		private bool loadingOlder;
		private bool isComposing;
		private bool isAtBottom = true;
		private CancellationTokenSource? pausedTimer;
		private CancellationTokenSource? remoteTypingTimer;

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

			string Name = this.isSelfChat ? ServiceRef.Localizer[nameof(AppResources.ChatMe)] :
				string.IsNullOrEmpty(Args?.FriendlyName) ? this.bareJid : Args.FriendlyName;

			this.friendlyName = Name;
			this.Initials = GetInitials(Name, this.isSelfChat);
			this.LinkCommand = new AsyncRelayCommand<string>(this.OpenLinkAsync);
			this.ActionsCommand = new AsyncRelayCommand<ChatMessageItem>(this.ShowMessageActionsAsync);
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
		/// The rows of the chat: messages and days.
		/// </summary>
		public ChatItemCollection Items { get; } = [];

		/// <summary>
		/// Runs when a link or card in a message is tapped.
		/// </summary>
		public IAsyncRelayCommand<string> LinkCommand { get; }

		/// <summary>
		/// Shows what can be done with a message.
		/// </summary>
		public IAsyncRelayCommand<ChatMessageItem> ActionsCommand { get; }

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
						ServiceRef.Localizer[nameof(AppResources.ChatReplyingTo), this.FriendlyName];
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

			this.UpdatePresence();

			await this.LoadLatestAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ChatEvents.MessageStored -= this.ChatEvents_MessageStored;
			ChatEvents.MessageDelivered -= this.ChatEvents_MessageDelivered;
			ChatEvents.ChatStateChanged -= this.ChatEvents_ChatStateChanged;
			ServiceRef.XmppService.OnPresence -= this.XmppService_OnPresence;

			this.pausedTimer?.Cancel();
			this.remoteTypingTimer?.Cancel();

			await base.OnDisposeAsync();
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			ChatEvents.SetVisibleChat(this.BareJid);

			try
			{
				await ServiceRef.NotificationService.DeleteEvents(NotificationEventType.Contacts, this.BareJid);
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

					if (this.Items.Count > 0 && this.Items[0] is ChatMessageItem First)
						First.IsFirstInGroup = StartsGroup(First.Message, Last);

					this.Register(Rows);
					this.Items.InsertRange(0, Rows);

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

				Rows.Add(new ChatMessageItem(Message, Models[i], this.isSelfChat, this.LinkCommand, this.ActionsCommand)
				{
					IsFirstInGroup = StartsGroup(Message, Previous)
				});

				Previous = Message;
			}

			return Rows;
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
				(Message.Created - Previous.Created).Duration() > groupInterval;
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

					ChatMessageItem Item = new(Message, Model, this.isSelfChat, this.LinkCommand, this.ActionsCommand)
					{
						IsFirstInGroup = StartsGroup(Message, Previous)
					};

					if (!string.IsNullOrEmpty(ObjectId))
						this.itemsByObjectId[ObjectId] = Item;

					this.Items.Add(Item);
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
			return (this.IsConnected || this.isSelfChat) && !this.IsSending && !string.IsNullOrWhiteSpace(this.MarkdownInput);
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
				Text = Quote(this.ReplyItem.Message) + Environment.NewLine + Environment.NewLine + Text;

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

		private static string Quote(ChatMessage Message)
		{
			string Text = Shorten(Message.PlainText);
			StringBuilder Result = new();

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

		private async Task ShowMessageActionsAsync(ChatMessageItem? Item)
		{
			if (Item is null)
				return;

			ServiceRef.LogService.LogDebug("ChatMessageActionsOpened", new KeyValuePair<string, object?>("Sent", Item.IsSent));

			List<ChatSheetAction> Actions =
			[
				new(ServiceRef.Localizer[nameof(AppResources.MarkdownCopy)], Geometries.ClipboardIconPath, async () =>
				{
					string Text = string.IsNullOrEmpty(Item.Message.Markdown) ? Item.Message.PlainText : Item.Message.Markdown;
					await Clipboard.SetTextAsync(Text);
				})
			];

			if (!this.isSelfChat)
			{
				Actions.Add(new(ServiceRef.Localizer[nameof(AppResources.ChatReply)], Geometries.ReplyPath, () =>
				{
					this.EditingItem = null;
					this.ReplyItem = Item;
					this.FocusComposerRequested?.Invoke(this, EventArgs.Empty);
					return Task.CompletedTask;
				}));
			}

			if (Item.IsSent)
			{
				Actions.Add(new(ServiceRef.Localizer[nameof(AppResources.ChatEdit)], Geometries.EditPath, () =>
				{
					this.ReplyItem = null;
					this.EditingItem = Item;
					this.MarkdownInput = string.IsNullOrEmpty(Item.Message.Markdown) ?
						MarkdownDocument.Encode(Item.Message.PlainText) : Item.Message.Markdown;
					this.FocusComposerRequested?.Invoke(this, EventArgs.Empty);
					return Task.CompletedTask;
				}));
			}

			await ChatSheetPopup.ShowAsync(null, Actions, false);
		}

		#endregion

		#region Sharing

		private bool CanAttach()
		{
			return (this.IsConnected || this.isSelfChat) && !this.IsSending;
		}

		/// <summary>
		/// Shows what can be shared in the chat.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanAttach))]
		private async Task Attach()
		{
			List<ChatSheetAction> Actions =
			[
				new(ServiceRef.Localizer[nameof(AppResources.Photos)], Geometries.CameraImageButtonPath, this.PickPhotoAsync),
				new(ServiceRef.Localizer[nameof(AppResources.ChatShareCamera)], Geometries.CameraPath, this.TakePhotoAsync),
				new(ServiceRef.Localizer[nameof(AppResources.ChatShareContact)], Geometries.PersonPath, this.ShareContactAsync)
			];

			if (ServiceRef.TagProfile.LegalIdentity is not null)
				Actions.Add(new(ServiceRef.Localizer[nameof(AppResources.ChatShareMyId)], Geometries.ShowIdIconPath, this.ShareMyIdAsync));

			Actions.Add(new(ServiceRef.Localizer[nameof(AppResources.MarkdownContract)], Geometries.ContractPath, this.ShareContractAsync));

			if (!this.isSelfChat)
				Actions.Add(new(ServiceRef.Localizer[nameof(AppResources.Payment)], Geometries.MoneyPath, this.SharePaymentAsync));

			Actions.Add(new(ServiceRef.Localizer[nameof(AppResources.MarkdownToken)], Geometries.TokenPath, this.ShareTokenAsync));
			Actions.Add(new(ServiceRef.Localizer[nameof(AppResources.Thing)], Geometries.ThingPath, this.ShareThingAsync));

			await ChatSheetPopup.ShowAsync(ServiceRef.Localizer[nameof(AppResources.ChatShareTitle)], Actions, true);
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
				if (this.isSelfChat)
				{
					LegalIdentity? Own = ServiceRef.TagProfile.LegalIdentity;
					if (Own is not null)
						await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Own));

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
				case "subscribe":
					SubscribeToViewModel SubscribeToViewModel = new(Jid);
					SubscribeToPopup SubscribeToPopup = new(SubscribeToViewModel);

					await ServiceRef.PopupService.PushAsync(SubscribeToPopup);
					bool? SubscribeTo = await SubscribeToViewModel.Result;

					if (SubscribeTo.HasValue && SubscribeTo.Value)
					{
						string IdXml;

						if (ServiceRef.TagProfile.LegalIdentity is null)
							IdXml = string.Empty;
						else
						{
							StringBuilder Xml = new();
							ServiceRef.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
							IdXml = Xml.ToString();
						}

						ServiceRef.XmppService.RequestPresenceSubscription(Jid, IdXml);
					}
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
