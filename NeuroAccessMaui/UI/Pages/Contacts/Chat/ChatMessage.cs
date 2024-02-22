using System.Windows.Input;
using NeuroAccessMaui.Extensions;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// Message type
	/// </summary>
	public enum MessageType
	{
		/// <summary>
		/// An empty transparent bubble, used to fix an issue on iOS
		/// </summary>
		Empty,

		/// <summary>
		/// Message sent by the user
		/// </summary>
		Sent,

		/// <summary>
		/// Message received from remote party
		/// </summary>
		Received
	}

	/// <summary>
	/// Chat Messages
	/// </summary>
	[CollectionName("ChatMessages")]
	[TypeName(TypeNameSerialization.None)]
	[Index("RemoteBareJid", "Created")]
	[Index("RemoteBareJid", "RemoteObjectId")]
	public class ChatMessage : IUniqueItem
	{
		/// <summary>
		/// A chat message which is always the latest one and is of type <see cref="MessageType.Empty"/>.
		/// </summary>
		/// 
		/// <remarks>
		/// Because <see cref="Empty"/> has its <see cref="Created"/> and <see cref="Updated"/> properties set to <see cref="DateTime.MaxValue"/>,
		/// once it is inserted into chat messages collection at position 0, it will remain at this position forever and no new chat
		/// messages can lift it up in the chat history.
		/// </remarks>
		public static ChatMessage Empty => new()
		{
			Created = DateTime.MaxValue,
			Updated = DateTime.MaxValue,
		};

		private string? objectId = null;
		private CaseInsensitiveString? remoteBareJid = null;
		private DateTime created = DateTime.MinValue;
		private DateTime updated = DateTime.MinValue;
		private string? remoteObjectId = null;
		private MessageType messageType = MessageType.Sent;
		private string plainText = string.Empty;
		private string markdown = string.Empty;
		private string html = string.Empty;
		private object? parsedXaml = null;

		private IChatView? chatView;

		/// <summary>
		/// Chat Messages
		/// </summary>
		public ChatMessage()
		{
			this.MessageType = MessageType.Empty;
			this.Updated = DateTime.MinValue;

			this.XmppUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.Xmpp));
			this.IotIdUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.IotId));
			this.IotScUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.IotSc));
			this.NeuroFeatureUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.NeuroFeature));
			this.IotDiscoUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.IotDisco));
			this.EDalerUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter, UriScheme.EDaler));
			this.HyperlinkClicked = new Command(async Parameter => await ExecuteHyperlinkClicked(Parameter));
		}

		/// <inheritdoc/>
		public string UniqueName => this.ObjectId ?? string.Empty;

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string? ObjectId
		{
			get => this.objectId;
			set => this.objectId = value;
		}

		/// <summary>
		/// Remote Bare JID
		/// </summary>
		public CaseInsensitiveString? RemoteBareJid
		{
			get => this.remoteBareJid;
			set => this.remoteBareJid = value;
		}

		/// <summary>
		/// When message was created
		/// </summary>
		public DateTime Created
		{
			get => this.created;
			set => this.created = value;
		}

		/// <summary>
		/// When message was created
		/// </summary>
		[DefaultValueDateTimeMinValue]
		public DateTime Updated
		{
			get => this.updated;
			set => this.updated = value;
		}

		/// <summary>
		/// Remote Objcet ID. If sent by the local user, value will be null or empty.
		/// </summary>
		public string? RemoteObjectId
		{
			get => this.remoteObjectId;
			set => this.remoteObjectId = value;
		}

		/// <summary>
		/// Message Type
		/// </summary>
		public MessageType MessageType
		{
			get => this.messageType;
			set => this.messageType = value;
		}

		/// <summary>
		/// Plain text of message
		/// </summary>
		public string PlainText
		{
			get => this.plainText;
			set => this.plainText = value;
		}

		/// <summary>
		/// Markdown of message
		/// </summary>
		public string Markdown
		{
			get => this.markdown;
			set => this.markdown = value;
		}

		/// <summary>
		/// HTML of message
		/// </summary>
		public string Html
		{
			get => this.html;
			set
			{
				this.html = value;
				this.parsedXaml = null;
			}
		}

		/// <summary>
		/// Message Style ID
		/// </summary>
		public string StyleId => "Message" + this.messageType.ToString();

		/// <summary>
		/// Parses the XAML in the message.
		/// </summary>
		/// <param name="View"></param>
		public async Task GenerateXaml(IChatView View)
		{
			this.chatView = View;

			if (this.MessageType == MessageType.Empty)
			{
				this.parsedXaml = new VerticalStackLayout()
				{
					Spacing = 0
				};

				return;
			}

			if (!string.IsNullOrEmpty(this.markdown))
			{
				this.parsedXaml = await this.markdown.MarkdownToParsedXaml();
				if (this.parsedXaml is VerticalStackLayout Layout)
					Layout.StyleId = this.StyleId;
			}
			else
			{
				VerticalStackLayout Layout = new()
				{
					StyleId = string.IsNullOrEmpty(this.html) && string.IsNullOrEmpty(this.plainText) ? string.Empty : this.StyleId
				};

				if (!string.IsNullOrEmpty(this.html))
					Layout.Children.Add(new Label()
					{
						Text = this.html,
						TextType = TextType.Html
					});
				else if (!string.IsNullOrEmpty(this.plainText))
					Layout.Children.Add(new Label()
					{
						Text = this.plainText,
						TextType = TextType.Text
					});

				this.parsedXaml = Layout;
			}
		}

		/// <summary>
		/// Parsed XAML
		/// </summary>
		public object? ParsedXaml => this.parsedXaml;

		/// <summary>
		/// Command executed when a multi-media-link with the xmpp URI scheme is clicked.
		/// </summary>
		public ICommand XmppUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotid URI scheme is clicked.
		/// </summary>
		public ICommand IotIdUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotsc URI scheme is clicked.
		/// </summary>
		public ICommand IotScUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the nfeat URI scheme is clicked.
		/// </summary>
		public ICommand NeuroFeatureUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotdisco URI scheme is clicked.
		/// </summary>
		public ICommand IotDiscoUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the edaler URI scheme is clicked.
		/// </summary>
		public ICommand EDalerUriClicked { get; }

		/// <summary>
		/// Command executed when a hyperlink in rendered markdown has been clicked.
		/// </summary>
		public ICommand HyperlinkClicked { get; }

		private Task ExecuteUriClicked(object Parameter, UriScheme Scheme)
		{
			if (Parameter is string Uri && this.chatView is not null)
				return this.chatView.ExecuteUriClicked(this, Uri, Scheme);
			else
				return Task.CompletedTask;
		}

		private static async Task ExecuteHyperlinkClicked(object Parameter)
		{
			if (Parameter is not string Url)
				return;

			await App.OpenUrlAsync(Url);
		}

	}
}
