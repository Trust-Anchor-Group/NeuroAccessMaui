using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.Services.Chat.Events;
using NeuroAccessMaui.Services.Chat.Models;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.Session
{
	/// <summary>
	/// View model orchestrating chat session state.
	/// </summary>
	public partial class ChatSessionViewModel : XmppViewModel
	{
		private const int defaultPageSize = Constants.BatchSizes.MessageBatchSize;

		private readonly ChatNavigationArgs navigationArgs;
		private readonly IChatMessageRepository chatMessageRepository;
		private readonly IChatTransportService chatTransportService;
		private readonly IMarkdownRenderService markdownRenderService;
		private readonly IChatEventStream chatEventStream;
		private readonly IChatMessageService chatMessageService;
		private readonly ObservableCollection<ChatMessageItemViewModel> messages;
		private readonly Dictionary<string, ChatMessageItemViewModel> messageIndex;
		private CancellationTokenSource? loadCancellationTokenSource;
		private bool isLoadingOlder;
		private readonly TimeSpan typingPauseDelay = TimeSpan.FromSeconds(5);
		private CancellationTokenSource? typingPauseCancellationTokenSource;
		private ChatState localChatState = ChatState.Active;
		private ChatState remoteChatState = ChatState.Active;
		private bool remoteChatStateSupported;
		private ChatMessageItemViewModel? replyContextMessage;
		private ChatMessageItemViewModel? editingContextMessage;
		private ChatMessageItemViewModel? actionContextMessage;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatSessionViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public ChatSessionViewModel(ChatNavigationArgs Args)
		{
			this.navigationArgs = Args ?? throw new ArgumentNullException(nameof(Args));
			this.chatMessageRepository = ServiceRef.ChatMessageRepository;
			this.chatTransportService = ServiceRef.ChatTransportService;
			this.markdownRenderService = ServiceRef.MarkdownRenderService;
			this.chatEventStream = ServiceRef.ChatEventStream;
			this.chatMessageService = ServiceRef.ChatMessageService;

			this.messages = new ObservableCollection<ChatMessageItemViewModel>();
			this.messageIndex = new Dictionary<string, ChatMessageItemViewModel>(StringComparer.OrdinalIgnoreCase);
			this.HasMoreHistory = true;
			this.GoBackCommand = new AsyncRelayCommand(this.ExecuteGoBackAsync);
			this.LongPressMessageCommand = new AsyncRelayCommand<ChatMessageItemViewModel>(this.OnMessageLongPressAsync);
			this.SendMessageCommand = new AsyncRelayCommand(this.SendMessageAsync, this.CanSendMessage);
			this.CopyMessageCommand = new AsyncRelayCommand<ChatMessageItemViewModel>(this.CopyMessageAsync);
			this.ReplyToMessageCommand = new AsyncRelayCommand<ChatMessageItemViewModel>(this.ReplyToMessageAsync);
			this.EditMessageCommand = new AsyncRelayCommand<ChatMessageItemViewModel>(this.EditMessageAsync, this.CanEditMessage);
			this.DeleteMessageCommand = new AsyncRelayCommand<ChatMessageItemViewModel>(this.DeleteMessageAsync);
			this.ClearComposerContextCommand = new AsyncRelayCommand(this.ClearComposerContextAsync);
			this.ClearActionContextCommand = new AsyncRelayCommand(this.ClearActionContextAsync);

			this.chatEventStream.EventsAvailable += this.ChatEventStream_EventsAvailable;
		}

		/// <summary>
		/// Messages bound to the UI.
		/// </summary>
		public ObservableCollection<ChatMessageItemViewModel> Messages => this.messages;

		/// <summary>
		/// Indicates if the view model is loading.
		/// </summary>
		[ObservableProperty]
		private bool isLoading;

		/// <summary>
		/// Indicates if more history is available.
		/// </summary>
		[ObservableProperty]
		private bool hasMoreHistory;

		/// <summary>
		/// Current markdown input in the composer.
		/// </summary>
		[ObservableProperty]
		private string markdownInput = string.Empty;

		/// <summary>
		/// Indicates if a send operation is ongoing.
		/// </summary>
		[ObservableProperty]
		private bool isSendingMessage;

		/// <summary>
		/// Indicates if the remote party is actively typing.
		/// </summary>
		[ObservableProperty]
		private bool isRemoteTyping;

		/// <summary>
		/// Text describing the remote party typing state.
		/// </summary>
		[ObservableProperty]
		private string remoteTypingState = string.Empty;

		/// <summary>
		/// Remote bare JID for the session.
		/// </summary>
		public string RemoteBareJid => this.navigationArgs.BareJid ?? string.Empty;

		/// <summary>
		/// Friendly name of the remote party.
		/// </summary>
		public string FriendlyName => this.navigationArgs.FriendlyName ?? string.Empty;

		/// <summary>
		/// Command used to navigate back to the previous page.
		/// </summary>
		public IAsyncRelayCommand GoBackCommand { get; }

		/// <summary>
		/// Command triggered when a message bubble is long-pressed.
		/// </summary>
		public IAsyncRelayCommand<ChatMessageItemViewModel> LongPressMessageCommand { get; }

		/// <summary>
		/// Command used to send a new message.
		/// </summary>
		public IAsyncRelayCommand SendMessageCommand { get; }

		/// <summary>
		/// Command used to copy message content to clipboard.
		/// </summary>
		public IAsyncRelayCommand<ChatMessageItemViewModel> CopyMessageCommand { get; }

		/// <summary>
		/// Command used to reply to a message.
		/// </summary>
		public IAsyncRelayCommand<ChatMessageItemViewModel> ReplyToMessageCommand { get; }

		/// <summary>
		/// Command used to edit an outgoing message.
		/// </summary>
		public IAsyncRelayCommand<ChatMessageItemViewModel> EditMessageCommand { get; }

		/// <summary>
		/// Command used to delete a message locally.
		/// </summary>
		public IAsyncRelayCommand<ChatMessageItemViewModel> DeleteMessageCommand { get; }

		/// <summary>
		/// Command used to clear the composer context (reply/edit).
		/// </summary>
		public IAsyncRelayCommand ClearComposerContextCommand { get; }

		/// <summary>
		/// Command used to clear the active action context.
		/// </summary>
		public IAsyncRelayCommand ClearActionContextCommand { get; }

		/// <summary>
		/// Indicates if the composer controls should be enabled.
		/// </summary>
		public bool IsComposerEnabled => !this.IsSendingMessage;

		/// <summary>
		/// Indicates if the composer has contextual state (reply/edit).
		/// </summary>
		public bool HasComposerContext => this.editingContextMessage is not null || this.replyContextMessage is not null;

		/// <summary>
		/// Title describing the current composer context.
		/// </summary>
		public string ComposerContextTitle
		{
			get
			{
				if (this.editingContextMessage is not null)
					return AppResources.ChatEditingMessage;

				if (this.replyContextMessage is not null)
				{
					string friendly = this.replyContextMessage.Direction == ChatMessageDirection.Outgoing
						? AppResources.ChatYou
						: (string.IsNullOrWhiteSpace(this.FriendlyName) ? this.RemoteBareJid : this.FriendlyName);

					return string.Format(CultureInfo.CurrentCulture, AppResources.ChatReplyingToMessage, friendly);
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// Preview text for the current composer context.
		/// </summary>
		public string ComposerContextPreview
		{
			get
			{
				ChatMessageItemViewModel? context = this.editingContextMessage ?? this.replyContextMessage;
				if (context is null)
					return string.Empty;

				string preview = context.DisplayText ?? string.Empty;
				if (string.IsNullOrWhiteSpace(preview))
					preview = context.Descriptor.Markdown ?? context.Descriptor.PlainText ?? string.Empty;

				preview = preview.Trim();
				if (preview.Length > 120)
					preview = preview[..120] + "…";

				return preview;
			}
		}

		/// <summary>
		/// Indicates if an action context is currently active.
		/// </summary>
		public bool IsActionContextVisible => this.actionContextMessage is not null;

		/// <summary>
		/// Message currently associated with the action context.
		/// </summary>
		public ChatMessageItemViewModel? ActionContextMessage => this.actionContextMessage;

		/// <summary>
		/// Title displayed in the action context banner.
		/// </summary>
		public string ActionContextTitle => AppResources.ChatMessageActions;

		/// <summary>
		/// Preview text for the action context banner.
		/// </summary>
		public string ActionContextPreview
		{
			get
			{
				ChatMessageItemViewModel? context = this.actionContextMessage;
				if (context is null)
					return string.Empty;

				string preview = context.DisplayText ?? string.Empty;
				if (string.IsNullOrWhiteSpace(preview))
					preview = context.Descriptor.Markdown ?? context.Descriptor.PlainText ?? string.Empty;

				preview = preview.Trim();
				if (preview.Length > 120)
					preview = preview[..120] + "…";

				return preview;
			}
		}

		/// <summary>
		/// Indicates if the edit action should be enabled for the current context.
		/// </summary>
		public bool IsActionEditEnabled => this.actionContextMessage?.Direction == ChatMessageDirection.Outgoing;

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize().ConfigureAwait(false);

			if (!string.IsNullOrEmpty(this.RemoteBareJid))
			{
				await this.chatTransportService.EnsureSessionAsync(this.RemoteBareJid, CancellationToken.None).ConfigureAwait(false);
				bool supported = this.chatTransportService.IsChatStateSupported(this.RemoteBareJid);
				this.UpdateRemoteChatStateSupport(supported);
				await this.LoadRecentMessagesAsync().ConfigureAwait(false);
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.chatEventStream.EventsAvailable -= this.ChatEventStream_EventsAvailable;
			this.CancelTypingPauseTimer();

			if (this.loadCancellationTokenSource is not null)
			{
				this.loadCancellationTokenSource.Cancel();
				this.loadCancellationTokenSource.Dispose();
				this.loadCancellationTokenSource = null;
			}

			await base.OnDispose().ConfigureAwait(false);
		}

		private async Task LoadRecentMessagesAsync()
		{
			if (string.IsNullOrEmpty(this.RemoteBareJid))
				return;

			this.CancelPendingLoad();

			this.loadCancellationTokenSource = new CancellationTokenSource();
			CancellationToken cancellationToken = this.loadCancellationTokenSource.Token;

			try
			{
				this.IsLoading = true;

				IReadOnlyList<ChatMessageDescriptor> descriptors = await this.chatMessageRepository.LoadRecentAsync(
					this.RemoteBareJid,
					defaultPageSize,
					cancellationToken).ConfigureAwait(false);

				List<ChatMessageItemViewModel> items = new List<ChatMessageItemViewModel>(descriptors.Count);

				for (int i = descriptors.Count - 1; i >= 0; i--)
				{
					ChatMessageDescriptor descriptor = descriptors[i];
					ChatMessageItemViewModel item = await this.CreateMessageItemAsync(descriptor, cancellationToken).ConfigureAwait(false);
					items.Add(item);
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.messageIndex.Clear();
					this.messages.Clear();
					foreach (ChatMessageItemViewModel item in items)
					{
						this.messages.Add(item);
						this.RegisterMessage(item);
					}
				});

				this.HasMoreHistory = descriptors.Count == defaultPageSize;
			}
			catch (OperationCanceledException)
			{
				// Expected when another load supersedes this one.
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				this.IsLoading = false;
			}
		}

		private async Task<ChatMessageItemViewModel> CreateMessageItemAsync(ChatMessageDescriptor Descriptor, CancellationToken CancellationToken)
		{
			string markdown = Descriptor.Markdown ?? string.Empty;
			string plainText = Descriptor.PlainText ?? string.Empty;
			string html = Descriptor.Html ?? string.Empty;
			string fingerprint = Descriptor.ContentFingerprint ?? string.Empty;
			CultureInfo culture = CultureInfo.CurrentUICulture;

			ChatRenderRequest request = new ChatRenderRequest(
				Descriptor.MessageId,
				markdown,
				plainText,
				html,
				Descriptor.Direction,
				culture,
				fingerprint);

			ChatRenderResult renderResult = await this.markdownRenderService.RenderAsync(request, CancellationToken).ConfigureAwait(false);
			ChatMessageItemViewModel item = new ChatMessageItemViewModel(Descriptor, renderResult);

			return item;
		}

		/// <summary>
		/// Loads older messages when the user scrolls near the top of history.
		/// </summary>
		public async Task LoadOlderMessagesAsync()
		{
			if (this.isLoadingOlder || !this.HasMoreHistory || string.IsNullOrEmpty(this.RemoteBareJid))
				return;

			if (this.messages.Count == 0)
			{
				await this.LoadRecentMessagesAsync().ConfigureAwait(false);
				return;
			}

			ChatMessageItemViewModel anchor = this.messages[0];
			DateTime before = anchor.Created;

			this.isLoadingOlder = true;

			try
			{
				IReadOnlyList<ChatMessageDescriptor> descriptors = await this.chatMessageRepository.LoadOlderAsync(
					this.RemoteBareJid,
					before,
					defaultPageSize,
					CancellationToken.None).ConfigureAwait(false);

				if (descriptors.Count == 0)
				{
					this.HasMoreHistory = false;
					return;
				}

				List<ChatMessageItemViewModel> items = new List<ChatMessageItemViewModel>(descriptors.Count);

				for (int i = descriptors.Count - 1; i >= 0; i--)
				{
					ChatMessageDescriptor descriptor = descriptors[i];

					if (this.ContainsMessageDescriptor(descriptor))
					{
						await this.ReplaceMessageAsync(descriptor).ConfigureAwait(false);
						continue;
					}

					ChatMessageItemViewModel item = await this.CreateMessageItemAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
					items.Add(item);
				}

				if (items.Count > 0)
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						int insertIndex = 0;
						foreach (ChatMessageItemViewModel item in items)
						{
							this.messages.Insert(insertIndex, item);
							this.RegisterMessage(item);
							insertIndex++;
						}
					});
				}

				this.HasMoreHistory = descriptors.Count == defaultPageSize;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				this.isLoadingOlder = false;
			}
		}

		private void ChatEventStream_EventsAvailable(object? Sender, ChatEventsAvailableEventArgs Args)
		{
			if (!string.Equals(Args.RemoteBareJid, this.RemoteBareJid, StringComparison.OrdinalIgnoreCase))
				return;

			_ = Task.Run(async () =>
			{
				try
				{
					IReadOnlyList<ChatSessionEvent> events = await this.chatEventStream.DrainAsync(this.RemoteBareJid, CancellationToken.None).ConfigureAwait(false);
					foreach (ChatSessionEvent sessionEvent in events)
						await this.HandleSessionEventAsync(sessionEvent).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
		}

		private void CancelPendingLoad()
		{
			if (this.loadCancellationTokenSource is null)
				return;

			this.loadCancellationTokenSource.Cancel();
			this.loadCancellationTokenSource.Dispose();
			this.loadCancellationTokenSource = null;
		}

		private void RegisterMessage(ChatMessageItemViewModel Item)
		{
			if (!string.IsNullOrEmpty(Item.MessageId))
				this.messageIndex[Item.MessageId] = Item;

			string? localTempId = Item.Descriptor.LocalTempId;
			if (!string.IsNullOrEmpty(localTempId))
				this.messageIndex[localTempId] = Item;
		}

		private void UnregisterMessage(ChatMessageItemViewModel Item)
		{
			if (!string.IsNullOrEmpty(Item.MessageId))
				this.messageIndex.Remove(Item.MessageId);

			string? localTempId = Item.Descriptor.LocalTempId;
			if (!string.IsNullOrEmpty(localTempId))
				this.messageIndex.Remove(localTempId);
		}

		private bool ContainsMessageDescriptor(ChatMessageDescriptor Descriptor)
		{
			return this.TryGetExistingMessage(Descriptor.MessageId, Descriptor.LocalTempId, out _);
		}

		private bool TryGetExistingMessage(string? MessageId, string? LocalTempId, out ChatMessageItemViewModel Item)
		{
			if (!string.IsNullOrEmpty(MessageId) && this.messageIndex.TryGetValue(MessageId, out Item))
				return true;

			if (!string.IsNullOrEmpty(LocalTempId) && this.messageIndex.TryGetValue(LocalTempId, out Item))
				return true;

			Item = null!;
			return false;
		}

		private async Task AppendMessagesAsync(IReadOnlyList<ChatMessageDescriptor> Descriptors)
		{
			if (Descriptors is null || Descriptors.Count == 0)
				return;

			List<ChatMessageItemViewModel> toAdd = new List<ChatMessageItemViewModel>();

			foreach (ChatMessageDescriptor descriptor in Descriptors)
			{
				if (descriptor is null)
					continue;

				if (this.ContainsMessageDescriptor(descriptor))
				{
					await this.ReplaceMessageAsync(descriptor).ConfigureAwait(false);
					continue;
				}

				ChatMessageItemViewModel item = await this.CreateMessageItemAsync(descriptor, CancellationToken.None).ConfigureAwait(false);
				toAdd.Add(item);
			}

			if (toAdd.Count == 0)
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				foreach (ChatMessageItemViewModel item in toAdd)
				{
					this.messages.Add(item);
					this.RegisterMessage(item);
				}
			});
		}

		private async Task ReplaceMessageAsync(ChatMessageDescriptor Descriptor)
		{
			if (Descriptor is null)
				return;

			if (!this.TryGetExistingMessage(Descriptor.MessageId, Descriptor.LocalTempId, out ChatMessageItemViewModel? existing))
			{
				await this.AppendMessagesAsync(new[] { Descriptor }).ConfigureAwait(false);
				return;
			}

			ChatMessageItemViewModel replacement = await this.CreateMessageItemAsync(Descriptor, CancellationToken.None).ConfigureAwait(false);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				int index = this.messages.IndexOf(existing);
				if (index >= 0)
				{
					this.messages[index] = replacement;
					this.UnregisterMessage(existing);
					this.RegisterMessage(replacement);
					bool contextChanged = false;
					if (this.replyContextMessage == existing)
					{
						this.replyContextMessage = replacement;
						contextChanged = true;
					}

					if (this.editingContextMessage == existing)
					{
						this.editingContextMessage = replacement;
						contextChanged = true;
					}

					if (contextChanged)
						this.UpdateComposerContext();
				}
			});
		}

		private async Task UpdateDeliveryStatusAsync(string? MessageId, string? LocalTempId, ChatDeliveryStatus DeliveryStatus)
		{
			if (!this.TryGetExistingMessage(MessageId, LocalTempId, out ChatMessageItemViewModel? item))
				return;

			await MainThread.InvokeOnMainThreadAsync(() => item.UpdateDeliveryStatus(DeliveryStatus));
		}

		private async Task HandleSessionEventAsync(ChatSessionEvent SessionEvent)
		{
			switch (SessionEvent.EventType)
			{
				case ChatSessionEventType.MessagesAppended:
					if (SessionEvent.Messages is not null && SessionEvent.Messages.Count > 0)
						await this.AppendMessagesAsync(SessionEvent.Messages).ConfigureAwait(false);
					break;

				case ChatSessionEventType.MessageUpdated:
					if (SessionEvent.Messages is not null)
					{
						foreach (ChatMessageDescriptor descriptor in SessionEvent.Messages)
							await this.ReplaceMessageAsync(descriptor).ConfigureAwait(false);
					}
					break;

				case ChatSessionEventType.DeliveryReceipt:
					string? messageId = null;
					string? localTempId = null;
					ChatDeliveryStatus status = ChatDeliveryStatus.Received;

					if (SessionEvent.AdditionalData is not null)
					{
						if (SessionEvent.AdditionalData.TryGetValue("MessageId", out string? value1) && !string.IsNullOrEmpty(value1))
							messageId = value1;

						if (SessionEvent.AdditionalData.TryGetValue("LocalTempId", out string? value2) && !string.IsNullOrEmpty(value2))
							localTempId = value2;

						if (SessionEvent.AdditionalData.TryGetValue("DeliveryStatus", out string? statusValue) && !string.IsNullOrEmpty(statusValue) &&
							Enum.TryParse(statusValue, true, out ChatDeliveryStatus parsedStatus))
						{
							status = parsedStatus;
						}
						else if (SessionEvent.AdditionalData.TryGetValue("Status", out string? altStatus) && !string.IsNullOrEmpty(altStatus) &&
							Enum.TryParse(altStatus, true, out ChatDeliveryStatus altParsed))
						{
							status = altParsed;
						}
					}

					if (string.IsNullOrEmpty(messageId) && SessionEvent.Messages is not null && SessionEvent.Messages.Count > 0)
					{
						messageId = SessionEvent.Messages[0].MessageId;
						localTempId = SessionEvent.Messages[0].LocalTempId;
					}

					await this.UpdateDeliveryStatusAsync(messageId, localTempId, status).ConfigureAwait(false);
					break;

				case ChatSessionEventType.TypingState:
					await this.HandleTypingSessionEventAsync(SessionEvent).ConfigureAwait(false);
					break;

				default:
					break;
			}
		}

		private Task HandleTypingSessionEventAsync(ChatSessionEvent SessionEvent)
		{
			if (SessionEvent is null)
				return Task.CompletedTask;

			IReadOnlyDictionary<string, string>? additionalData = SessionEvent.AdditionalData;
			if (additionalData is null)
				return Task.CompletedTask;

			try
			{
				if (additionalData.TryGetValue("Type", out string? typeValue) && string.Equals(typeValue, "Support", StringComparison.OrdinalIgnoreCase))
				{
					bool supported = false;
					if (additionalData.TryGetValue("Supported", out string? supportedValue) && bool.TryParse(supportedValue, out bool parsedSupported))
						supported = parsedSupported;

					this.UpdateRemoteChatStateSupport(supported);
					return Task.CompletedTask;
				}

				if (additionalData.TryGetValue("State", out string? stateValue) && Enum.TryParse(stateValue, true, out ChatState state))
				{
					this.UpdateRemoteChatStateSupport(true);
					this.UpdateRemoteTypingState(state);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return Task.CompletedTask;
		}

		private void UpdateRemoteChatStateSupport(bool supported)
		{
			this.remoteChatStateSupported = supported;

			if (!supported)
			{
				this.remoteChatState = ChatState.Active;
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsRemoteTyping = false;
					this.RemoteTypingState = string.Empty;
				});
			}
		}

		private void UpdateRemoteTypingState(ChatState state)
		{
			if (!this.remoteChatStateSupported)
				this.remoteChatStateSupported = true;

			if (state == this.remoteChatState)
				return;

			this.remoteChatState = state;

			bool showIndicator = state == ChatState.Composing || state == ChatState.Paused;
			string indicatorText = string.Empty;

			if (showIndicator)
			{
				string indicatorKey = state == ChatState.Composing ? nameof(AppResources.ChatTypingIndicatorComposing) : nameof(AppResources.ChatTypingIndicatorPaused);

				try
				{
					indicatorText = ServiceRef.Localizer[indicatorKey];
				}
				catch (Exception)
				{
					indicatorText = state == ChatState.Composing ? "Typing…" : "Typing paused";
				}
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IsRemoteTyping = showIndicator;
				this.RemoteTypingState = indicatorText;
			});
		}

		private void QueueSendChatState(ChatState state)
		{
			if (string.IsNullOrEmpty(this.RemoteBareJid))
				return;

			_ = Task.Run(async () =>
			{
				await this.SendLocalChatStateAsync(state, CancellationToken.None).ConfigureAwait(false);
			});
		}

		private void ScheduleTypingPause()
		{
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			this.typingPauseCancellationTokenSource = cancellationTokenSource;

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(this.typingPauseDelay, cancellationTokenSource.Token).ConfigureAwait(false);
					await this.SendLocalChatStateAsync(ChatState.Paused, CancellationToken.None).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
		}

		private void CancelTypingPauseTimer()
		{
			if (this.typingPauseCancellationTokenSource is null)
				return;

			this.typingPauseCancellationTokenSource.Cancel();
			this.typingPauseCancellationTokenSource.Dispose();
			this.typingPauseCancellationTokenSource = null;
		}

		private async Task SendLocalChatStateAsync(ChatState state, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(this.RemoteBareJid))
				return;

			if (this.localChatState == state)
				return;

			try
			{
				await this.chatTransportService.SendChatStateAsync(this.RemoteBareJid, state, cancellationToken).ConfigureAwait(false);
				this.localChatState = state;
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private void HandleMarkdownInputChanged(string value)
		{
			string text = value ?? string.Empty;
			string trimmed = text.Trim();

			this.CancelTypingPauseTimer();

			if (string.IsNullOrEmpty(this.RemoteBareJid))
				return;

			if (string.IsNullOrEmpty(trimmed))
			{
				this.QueueSendChatState(ChatState.Active);
				return;
			}

			this.QueueSendChatState(ChatState.Composing);
			this.ScheduleTypingPause();
		}

		private Task OnMessageLongPressAsync(ChatMessageItemViewModel? item)
		{
			if (item is null)
				return Task.CompletedTask;

			this.actionContextMessage = item;
			this.UpdateActionContext();
			return Task.CompletedTask;
		}

		private Task ClearActionContextAsync()
		{
			this.ClearActionContext();
			return Task.CompletedTask;
		}

		private void ClearActionContext()
		{
			if (this.actionContextMessage is null)
				return;

			this.actionContextMessage = null;
			this.UpdateActionContext();
		}

		private void UpdateActionContext()
		{
			if (!MainThread.IsMainThread)
			{
				MainThread.BeginInvokeOnMainThread(this.UpdateActionContext);
				return;
			}

			this.OnPropertyChanged(nameof(this.IsActionContextVisible));
			this.OnPropertyChanged(nameof(this.ActionContextMessage));
			this.OnPropertyChanged(nameof(this.ActionContextTitle));
			this.OnPropertyChanged(nameof(this.ActionContextPreview));
			this.OnPropertyChanged(nameof(this.IsActionEditEnabled));
			this.EditMessageCommand.NotifyCanExecuteChanged();
		}

		private Task ExecuteGoBackAsync()
		{
			return ServiceRef.UiService.GoBackAsync();
		}

		private async Task CopyMessageAsync(ChatMessageItemViewModel? item)
		{
			if (item is null)
				return;

			string text = item.Descriptor.Markdown;

			if (string.IsNullOrWhiteSpace(text))
				text = item.DisplayText;

			if (string.IsNullOrWhiteSpace(text))
				text = item.Descriptor.PlainText;

			if (string.IsNullOrWhiteSpace(text))
				return;

			await Clipboard.SetTextAsync(text);

			if (this.actionContextMessage == item)
				this.ClearActionContext();
		}

		private async Task SendMessageAsync()
		{
			if (this.IsSendingMessage)
				return;

			string text = (this.MarkdownInput ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(text))
				return;

			if (string.IsNullOrEmpty(this.RemoteBareJid))
				return;

			this.IsSendingMessage = true;

			try
			{
				string? replyToId = this.replyContextMessage?.Descriptor.MessageId;
				string? replaceMessageId = this.editingContextMessage?.Descriptor.MessageId;

				await this.chatMessageService.SendMarkdownAsync(this.RemoteBareJid, text, CancellationToken.None, replyToId, replaceMessageId).ConfigureAwait(false);
				await this.ClearComposerContextAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.MarkdownInput = string.Empty;
				this.IsSendingMessage = false;
			}
		}

		private bool CanSendMessage()
		{
			return !this.IsSendingMessage && !string.IsNullOrWhiteSpace(this.MarkdownInput) && !string.IsNullOrWhiteSpace(this.RemoteBareJid);
		}

		partial void OnMarkdownInputChanged(string value)
		{
			this.SendMessageCommand.NotifyCanExecuteChanged();
			this.HandleMarkdownInputChanged(value);
		}

		partial void OnIsSendingMessageChanged(bool value)
		{
			this.SendMessageCommand.NotifyCanExecuteChanged();
			this.OnPropertyChanged(nameof(this.IsComposerEnabled));
		}

		private Task ReplyToMessageAsync(ChatMessageItemViewModel? item)
		{
			if (item is null)
				return Task.CompletedTask;

			this.replyContextMessage = item;
			this.editingContextMessage = null;
			this.UpdateComposerContext();
			this.ClearActionContext();
			return Task.CompletedTask;
		}

		private bool CanEditMessage(ChatMessageItemViewModel? item)
		{
			return item?.Direction == ChatMessageDirection.Outgoing;
		}

		private Task EditMessageAsync(ChatMessageItemViewModel? item)
		{
			if (item is null || item.Direction != ChatMessageDirection.Outgoing)
				return Task.CompletedTask;

			this.editingContextMessage = item;
			this.replyContextMessage = null;

			string text = item.Descriptor.Markdown;
			if (string.IsNullOrEmpty(text))
				text = item.Descriptor.PlainText ?? string.Empty;

			this.MarkdownInput = text;
			this.UpdateComposerContext();
			this.ClearActionContext();
			return Task.CompletedTask;
		}

		private async Task DeleteMessageAsync(ChatMessageItemViewModel? item)
		{
			if (item is null)
				return;

			try
			{
				string remote = item.Descriptor.RemoteBareJid ?? this.RemoteBareJid;
				await this.chatMessageRepository.DeleteAsync(remote, item.Descriptor.MessageId, CancellationToken.None).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			bool composerContextChanged = false;
			bool actionContextChanged = false;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (this.messages.Remove(item))
				{
					this.UnregisterMessage(item);
				}

				if (this.editingContextMessage == item)
				{
					this.editingContextMessage = null;
					composerContextChanged = true;
				}

				if (this.replyContextMessage == item)
				{
					this.replyContextMessage = null;
					composerContextChanged = true;
				}

				if (this.actionContextMessage == item)
				{
					this.actionContextMessage = null;
					actionContextChanged = true;
				}
			});

			if (composerContextChanged)
				this.UpdateComposerContext();

			if (actionContextChanged)
				this.UpdateActionContext();
			else if (this.actionContextMessage == item)
				this.ClearActionContext();
		}

		private Task ClearComposerContextAsync()
		{
			if (!this.HasComposerContext)
				return Task.CompletedTask;

			this.editingContextMessage = null;
			this.replyContextMessage = null;
			this.MarkdownInput = string.Empty;
			this.UpdateComposerContext();
			return Task.CompletedTask;
		}

		private void UpdateComposerContext()
		{
			if (!MainThread.IsMainThread)
			{
				MainThread.BeginInvokeOnMainThread(this.UpdateComposerContext);
				return;
			}

			this.OnPropertyChanged(nameof(this.HasComposerContext));
			this.OnPropertyChanged(nameof(this.ComposerContextTitle));
			this.OnPropertyChanged(nameof(this.ComposerContextPreview));
		}
	}
}
