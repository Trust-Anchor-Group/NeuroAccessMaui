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
		private readonly bool isSelfChat;

		/// <summary>
		/// A message in the list of a chat.
		/// </summary>
		/// <param name="Message">Stored message.</param>
		/// <param name="Model">Its text, built for a <see cref="MarkdownView"/>.</param>
		/// <param name="IsSelfChat">If the chat holds notes to yourself, which are never sent and have no delivery status.</param>
		/// <param name="LinkCommand">Runs when a link or card in the message is tapped.</param>
		/// <param name="ActionsCommand">Shows what can be done with the message.</param>
		public ChatMessageItem(ChatMessage Message, MarkdownModel Model, bool IsSelfChat, ICommand LinkCommand, ICommand ActionsCommand)
			: base(Message.Created.ToLocalTime())
		{
			this.Message = Message;
			this.model = Model;
			this.isSelfChat = IsSelfChat;
			this.LinkCommand = LinkCommand;
			this.ActionsCommand = ActionsCommand;
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
		/// The message's text, built for a <see cref="MarkdownView"/>.
		/// </summary>
		[ObservableProperty]
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
		private bool isDelivered;

		/// <summary>
		/// If the message starts a run of messages from the same party, which gets more room above it.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(Spacing))]
		private bool isFirstInGroup = true;

		/// <summary>
		/// Room around the message.
		/// </summary>
		public Thickness Spacing => this.IsFirstInGroup ? new Thickness(16, 8, 16, 0) : new Thickness(16, 2, 16, 0);

		/// <summary>
		/// When the message was written, as shown.
		/// </summary>
		public string TimeText => this.Timestamp.ToString("t", CultureInfo.CurrentCulture);

		/// <summary>
		/// If the message shows if it has been delivered.
		/// </summary>
		public bool ShowsStatus => this.IsSent && !this.isSelfChat;

		/// <summary>
		/// If the message has been delivered, for screen readers.
		/// </summary>
		public string StatusText => this.IsDelivered ?
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
