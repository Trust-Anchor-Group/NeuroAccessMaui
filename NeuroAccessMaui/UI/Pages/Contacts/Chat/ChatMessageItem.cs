using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Chat;
using NeuroAccessMaui.UI.Controls.Markdown;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// A message in the list of a chat.
	/// </summary>
	public sealed partial class ChatMessageItem : ChatItem
	{
		private const double cornerRadius = 18;
		private const double tailRadius = 6;
		private static readonly Thickness textPadding = new(12, 8, 12, 6);
		private static readonly Thickness mediaPadding = new(4, 4, 4, 6);

		private readonly bool isSelfChat;

		/// <summary>
		/// A message in the list of a chat.
		/// </summary>
		/// <param name="Message">Stored message.</param>
		/// <param name="Model">Its text, built for a <see cref="MarkdownView"/>.</param>
		/// <param name="IsSelfChat">If the chat holds notes to yourself, which are never sent and have no delivery status.</param>
		/// <param name="LinkCommand">Runs when a link or card in the message is tapped.</param>
		/// <param name="ActionsCommand">Shows what can be done with the message.</param>
		/// <param name="ReplyCommand">Starts a reply to the message, as when it is swiped.</param>
		public ChatMessageItem(ChatMessage Message, MarkdownModel Model, bool IsSelfChat, ICommand LinkCommand, ICommand ActionsCommand,
			ICommand ReplyCommand)
			: base(Message.Created.ToLocalTime())
		{
			this.Message = Message;
			this.model = Model;
			this.isSelfChat = IsSelfChat;
			this.LinkCommand = LinkCommand;
			this.ActionsCommand = ActionsCommand;
			this.ReplyCommand = ReplyCommand;
			this.isDelivered = Message.Delivered != DateTime.MinValue;
			this.isEdited = Message.Updated != DateTime.MinValue;
		}

		/// <summary>
		/// Stored message.
		/// </summary>
		public ChatMessage Message { get; private set; }

		/// <summary>
		/// Object ID of the stored message.
		/// </summary>
		public string ObjectId => this.Message.ObjectId ?? string.Empty;

		/// <summary>
		/// If the user sent the message.
		/// </summary>
		public bool IsSent => this.Message.MessageType == MessageType.Sent;

		/// <summary>
		/// If the message can be replied to. Notes to yourself can't.
		/// </summary>
		public bool CanReply => !this.isSelfChat;

		/// <summary>
		/// The message's text, built for a <see cref="MarkdownView"/>.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsMediaOnly))]
		[NotifyPropertyChangedFor(nameof(BubblePadding))]
		[NotifyPropertyChangedFor(nameof(MetaMargin))]
		private MarkdownModel model;

		/// <summary>
		/// If the message has been corrected.
		/// </summary>
		[ObservableProperty]
		private bool isEdited;

		/// <summary>
		/// If the other party has confirmed that the message arrived.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(StatusText))]
		[NotifyPropertyChangedFor(nameof(ShowsSentCheck))]
		[NotifyPropertyChangedFor(nameof(ShowsDeliveredCheck))]
		private bool isDelivered;

		/// <summary>
		/// If the message starts a run of messages from the same party, which gets more room above it.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Spacing))]
		[NotifyPropertyChangedFor(nameof(Corners))]
		[NotifyPropertyChangedFor(nameof(ShowsSender))]
		private bool isFirstInGroup = true;

		/// <summary>
		/// If the message is in a group's chat, where only "sent" shows for the user's own messages.
		/// </summary>
		public bool IsInGroup { get; init; }

		/// <summary>
		/// In a group: the sender's name in the group.
		/// </summary>
		public string? SenderName { get; init; }

		/// <summary>
		/// In a group: the colour of the sender's name.
		/// </summary>
		public Color SenderColor { get; init; } = Colors.Gray;

		/// <summary>
		/// If the sender's name shows above the bubble: in a group, on the first message of a run from someone else.
		/// </summary>
		public bool ShowsSender => this.IsInGroup && !this.IsSent && this.IsFirstInGroup && !string.IsNullOrEmpty(this.SenderName);

		/// <summary>
		/// Room around the sender's name, which keeps it clear of the bubble's edge when a photo or card fills it.
		/// </summary>
		public Thickness SenderMargin => this.IsMediaOnly ? new Thickness(8, 4, 8, 2) : new Thickness(0, 0, 0, 2);

		/// <summary>
		/// If one check shows: sent. In a group it always does, since a group tells no one when a message arrives.
		/// </summary>
		public bool ShowsSentCheck => this.IsInGroup || !this.IsDelivered;

		/// <summary>
		/// If two checks show: delivered. Not in groups.
		/// </summary>
		public bool ShowsDeliveredCheck => !this.IsInGroup && this.IsDelivered;

		/// <summary>
		/// If the message ends a run of messages from the same party, so its bubble gets a tail.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Corners))]
		private bool isLastInGroup = true;

		/// <summary>
		/// Room around the message: more above the first message of a run.
		/// </summary>
		public Thickness Spacing => this.IsFirstInGroup ? new Thickness(12, 10, 12, 0) : new Thickness(12, 2, 12, 0);

		/// <summary>
		/// The corners of the bubble. On the sender's side, a message that follows another of the same run has a joined top
		/// corner, and the last one of the run has a tail at the bottom.
		/// </summary>
		public CornerRadius Corners
		{
			get
			{
				double Top = this.IsFirstInGroup ? cornerRadius : tailRadius;
				double Bottom = this.IsLastInGroup ? tailRadius : cornerRadius;

				return this.IsSent ?
					new CornerRadius(cornerRadius, Top, cornerRadius, Bottom) :
					new CornerRadius(Top, cornerRadius, Bottom, cornerRadius);
			}
		}

		/// <summary>
		/// If the message is only a photo or a card, which then fills the bubble.
		/// </summary>
		public bool IsMediaOnly => this.Model.Blocks.Count == 1 && this.Model.Blocks[0] is MdImage or MdEmbed;

		/// <summary>
		/// Room between the bubble and its content.
		/// </summary>
		public Thickness BubblePadding => this.IsMediaOnly ? mediaPadding : textPadding;

		/// <summary>
		/// Room around the time and status, which keeps them clear of the bubble's edge when a photo or card fills it.
		/// </summary>
		public Thickness MetaMargin => this.IsMediaOnly ? new Thickness(8, 0) : new Thickness(0);

		/// <summary>
		/// When the message was written, as shown.
		/// </summary>
		public string TimeText => this.Timestamp.ToString("t", CultureInfo.CurrentCulture);

		/// <summary>
		/// The line under a photo of the message in the image viewer: who sent it, and when.
		/// </summary>
		public string? ImageSubtitle { get; init; }

		/// <summary>
		/// If the message shows if it has been delivered.
		/// </summary>
		public bool ShowsStatus => this.IsSent && !this.isSelfChat;

		/// <summary>
		/// If the message has been delivered, for screen readers.
		/// </summary>
		public string StatusText => this.IsDelivered && !this.IsInGroup ?
			ServiceRef.Localizer[nameof(AppResources.ChatDelivered)] :
			ServiceRef.Localizer[nameof(AppResources.ChatSent)];

		/// <summary>
		/// Runs when a link or card in the message is tapped.
		/// </summary>
		public ICommand LinkCommand { get; }

		/// <summary>
		/// Shows what can be done with the message.
		/// </summary>
		public ICommand ActionsCommand { get; }

		/// <summary>
		/// Starts a reply to the message.
		/// </summary>
		public ICommand ReplyCommand { get; }

		/// <summary>
		/// Shows a corrected message.
		/// </summary>
		/// <param name="Message">The stored message.</param>
		/// <param name="Model">Its new text.</param>
		public void Update(ChatMessage Message, MarkdownModel Model)
		{
			this.Message = Message;
			this.Model = Model;
			this.IsEdited = Message.Updated != DateTime.MinValue;
			this.IsDelivered = Message.Delivered != DateTime.MinValue;
		}

		/// <summary>
		/// Shows that the message has arrived.
		/// </summary>
		public void MarkDelivered()
		{
			this.IsDelivered = true;
		}

		/// <summary>
		/// A moment, as said in a sentence: today or yesterday at a time, or a date at a time.
		/// </summary>
		/// <param name="Moment">The moment, in UTC or local time.</param>
		/// <returns>The moment, as words.</returns>
		public static string FormatMoment(DateTime Moment)
		{
			DateTime Local = Moment.Kind == DateTimeKind.Utc ? Moment.ToLocalTime() : Moment;
			CultureInfo Culture = CultureInfo.CurrentCulture;
			string Time = Local.ToString("t", Culture);

			if (Local.Date == DateTime.Today)
				return ServiceRef.Localizer[nameof(AppResources.ChatTimeToday), Time];

			if (Local.Date == DateTime.Today.AddDays(-1))
				return ServiceRef.Localizer[nameof(AppResources.ChatTimeYesterday), Time];

			string Day = Local.Year == DateTime.Today.Year ? Local.ToString("M", Culture) : Local.ToString("d", Culture);
			return ServiceRef.Localizer[nameof(AppResources.ChatTimeOnDay), Day, Time];
		}

		/// <summary>
		/// Builds the text of a message for a <see cref="MarkdownView"/>. Runs off the UI thread.
		/// </summary>
		/// <param name="Message">Stored message.</param>
		/// <returns>Text model.</returns>
		public static async Task<MarkdownModel> BuildModelAsync(ChatMessage Message)
		{
			if (!string.IsNullOrWhiteSpace(Message.Markdown))
			{
				try
				{
					// Older messages, and messages from other clients, keep their line breaks too.
					return await MarkdownModel.FromMarkdownAsync(ChatMarkdown.KeepLineBreaks(Message.Markdown));
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			if (!string.IsNullOrWhiteSpace(Message.PlainText))
				return PlainText(Message.PlainText);

			if (!string.IsNullOrWhiteSpace(Message.Html))
				return new MarkdownModel([new MdHtml { Html = Message.Html }]);

			return MarkdownModel.Empty;
		}

		private static MarkdownModel PlainText(string Text)
		{
			return new MarkdownModel([new MdParagraph { Runs = [new MdRun(Text, MdTextStyle.None, null)] }]);
		}
	}
}
