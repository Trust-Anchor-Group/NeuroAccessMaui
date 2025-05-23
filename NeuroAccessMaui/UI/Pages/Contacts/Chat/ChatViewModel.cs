using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using EDaler.Uris;
using Mopups.Services;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contacts.Chat.Controls;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.MyTokens;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.SendPayment;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo;
using NeuroFeatures;
using SkiaSharp;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Xml;
using Waher.Content;
using Waher.Content.Html;
using Waher.Content.Markdown;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public partial class ChatViewModel : XmppViewModel, IChatView, ILinkableView
	{
		private readonly SortedDictionary<string, LinkedListNode<MessageRecord>> messagesByObjectId = [];
		private readonly LinkedList<MessageRecord> messages = [];
		private readonly LinkedList<MessageFrame> frames = [];
		private readonly ChatPage page;
		private readonly object synchObject = new();
		private TaskCompletionSource<bool> waitUntilBound = new();
		private IView? scrollTo;

		private class MessageRecord(ChatMessage Message, LinkedListNode<MessageFrame> FrameNode)
		{
			public ChatMessage Message = Message;
			public LinkedListNode<MessageFrame> FrameNode = FrameNode;

			public DateTime Created => this.Message.Created;
			public MessageType MessageType => this.Message.MessageType;
		}

		/// <summary>
		/// Creates an instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		/// <param name="Page">Reference to chat page.</param>
		/// <param name="Args">Navigation arguments</param>
		public ChatViewModel(ChatPage Page, ChatNavigationArgs? Args)
			: base()
		{
			this.page = Page;
			this.LegalId = Args?.LegalId ?? string.Empty;
			this.BareJid = Args?.BareJid ?? string.Empty;
			this.FriendlyName = Args?.FriendlyName ?? string.Empty;
			this.UniqueId = Args?.UniqueId;

			this.page.OnAfterAppearing += this.Page_OnAfterAppearing;

			this.XmppUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter as string ?? "", UriScheme.Xmpp));
			this.IotIdUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter as string ?? "", UriScheme.IotId));
			this.IotScUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter as string ?? "", UriScheme.IotSc));
			this.NeuroFeatureUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter as string ?? "", UriScheme.NeuroFeature));
			this.IotDiscoUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter as string ?? "", UriScheme.IotDisco));
			this.EDalerUriClicked = new Command(async Parameter => await this.ExecuteUriClicked(Parameter as string ?? "", UriScheme.EDaler));
			this.HyperlinkClicked = new Command(async Parameter => await ExecuteHyperlinkClicked(Parameter));
		}

		/// <summary>
		/// Command executed when a multi-media-link with the xmpp URI scheme is clicked.
		/// </summary>
		public Command XmppUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotid URI scheme is clicked.
		/// </summary>
		public Command IotIdUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotsc URI scheme is clicked.
		/// </summary>
		public Command IotScUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the nfeat URI scheme is clicked.
		/// </summary>
		public Command NeuroFeatureUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the iotdisco URI scheme is clicked.
		/// </summary>
		public Command IotDiscoUriClicked { get; }

		/// <summary>
		/// Command executed when a multi-media-link with the edaler URI scheme is clicked.
		/// </summary>
		public Command EDalerUriClicked { get; }

		/// <summary>
		/// Command executed when a hyperlink in rendered markdown has been clicked.
		/// </summary>
		public Command HyperlinkClicked { get; }

		private static async Task ExecuteHyperlinkClicked(object Parameter)
		{
			if (Parameter is not string Url)
				return;

			await App.OpenUrlAsync(Url);
		}


		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.scrollTo = await this.LoadMessagesAsync(false);

			this.waitUntilBound.TrySetResult(true);

			await ServiceRef.NotificationService.DeleteEvents(NotificationEventType.Contacts, this.BareJid);

		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			await base.OnDispose();

			this.waitUntilBound = new TaskCompletionSource<bool>();
		}

		private Task Page_OnAfterAppearing(object Sender, EventArgs e)
		{
			if (this.scrollTo is Element)
			{
			//	await Task.Delay(100);  // TODO: Why is this necessary? ScrollToAsync does not scroll to end element without it...

			//	await this.page.ScrollView.ScrollToAsync(this.page.Bottom, ScrollToPosition.End, false);
				this.scrollTo = null;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Set the views unique ID
		/// </summary>
		[ObservableProperty]
		private string? uniqueId;

		/// <summary>
		/// Bare JID of remote party
		/// </summary>
		[ObservableProperty]
		private string? bareJid;

		/// <summary>
		/// Bare JID of remote party
		/// </summary>
		[ObservableProperty]
		private string? legalId;

		/// <summary>
		/// Friendly name of remote party
		/// </summary>
		[ObservableProperty]
		private string? friendlyName;

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCommand))]
		[NotifyCanExecuteChangedFor(nameof(CancelCommand))]
		private string markdownInput = string.Empty;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.MarkdownInput):
					this.IsWriting = !string.IsNullOrEmpty(this.MarkdownInput);
					break;

				case nameof(this.MessageId):
					this.IsWriting = !string.IsNullOrEmpty(this.MessageId);
					break;

				case nameof(this.IsWriting):
					this.IsButtonExpanded = false;
					break;

				case nameof(this.IsRecordingAudio):
					// TODO: Audio
					//
					//this.IsRecordingPaused = audioRecorder.Value.IsPaused;
					this.IsWriting = this.IsRecordingAudio;

					//if (audioRecorderTimer is null)
					//{
					//	audioRecorderTimer = new System.Timers.Timer(100);
					//	audioRecorderTimer.Elapsed += this.OnAudioRecorderTimer;
					//	audioRecorderTimer.AutoReset = true;
					//}
					//
					//audioRecorderTimer.Enabled = this.IsRecordingAudio;

					this.OnPropertyChanged(nameof(RecordingTime));
					this.CancelCommand.NotifyCanExecuteChanged();
					break;

				case nameof(this.IsConnected):
					this.SendCommand.NotifyCanExecuteChanged();
					this.CancelCommand.NotifyCanExecuteChanged();
					this.RecordAudioCommand.NotifyCanExecuteChanged();
					this.TakePhotoCommand.NotifyCanExecuteChanged();
					this.EmbedFileCommand.NotifyCanExecuteChanged();
					this.EmbedIdCommand.NotifyCanExecuteChanged();
					this.EmbedContractCommand.NotifyCanExecuteChanged();
					this.EmbedMoneyCommand.NotifyCanExecuteChanged();
					this.EmbedTokenCommand.NotifyCanExecuteChanged();
					this.EmbedThingCommand.NotifyCanExecuteChanged();
					break;
			}
		}

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		[ObservableProperty]
		private string? messageId;

		/// <summary>
		/// Current Markdown input.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(LoadMoreMessagesCommand))]
		private bool existsMoreMessages;

		/// <summary>
		/// If the user is writing markdown.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RecordAudioCommand))]
		[NotifyCanExecuteChangedFor(nameof(TakePhotoCommand))]
		[NotifyCanExecuteChangedFor(nameof(EmbedFileCommand))]
		[NotifyCanExecuteChangedFor(nameof(EmbedIdCommand))]
		[NotifyCanExecuteChangedFor(nameof(EmbedContractCommand))]
		[NotifyCanExecuteChangedFor(nameof(EmbedMoneyCommand))]
		[NotifyCanExecuteChangedFor(nameof(EmbedTokenCommand))]
		[NotifyCanExecuteChangedFor(nameof(EmbedThingCommand))]
		private bool isWriting;

		/// <summary>
		/// If the user is recording an audio message
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCommand))]
		private bool isRecordingAudio;

		/// <summary>
		/// If the audio recording is paused
		/// </summary>
		[ObservableProperty]
		private bool isRecordingPaused;

		/// <summary>
		/// If the audio recording is paused
		/// </summary>
		public static string RecordingTime
		{
			get
			{
				// TODO: Audio
				//
				//double Milliseconds = audioRecorder.Value.TotalAudioTimeout.TotalMilliseconds - audioRecorder.Value.RecordingTime.TotalMilliseconds;
				//return (Milliseconds > 0) ? string.Format(CultureInfo.CurrentCulture, "{0:F0}s left", Math.Ceiling(Milliseconds / 1000.0)) : "TIMEOUT";

				return string.Empty;
			}
		}

		/// <summary>
		/// External message has been received
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task MessageAddedAsync(ChatMessage Message)
		{
			if(Message.ParsedXaml is null)
					await Message.GenerateXaml(this);

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				IView? View = await this.MessageAddedMainThread(Message, true);

				if (View is Element)
				{
       			await Task.Delay(25);
					double Width = this.page.ScrollView.ScrollX;
					double Height = this.page.ScrollView.ContentSize.Height;
					await this.page.ScrollView.ScrollToAsync(Width, Height, true);	
				}
			});
		}

		private async Task<IView?> MessageAddedMainThread(ChatMessage Message, bool Historic)
		{
			this.HasMessages = true;

			TaskCompletionSource<IView?> Result = new();

			try
			{
				if(Message.ParsedXaml is null)
					await Message.GenerateXaml(this);   // Makes sure XAML is generated

				lock (this.synchObject)
				{
					LinkedListNode<MessageRecord>? MessageNode = Historic ? this.messages.Last : this.messages.First;
					LinkedListNode<MessageFrame>? FrameNode;
					MessageFrame? Frame;
					MessageRecord Rec;
					IView? View;
					// int i;
					if (MessageNode is null)
					{

						Frame = MessageFrame.Create(Message);
						View = Frame.AddLast(Message);
						this.page.Messages.Add(Frame);

						FrameNode = this.frames.AddLast(Frame);

						Rec = new(Message, FrameNode);
						MessageNode = this.messages.AddLast(Rec);
					}
					else if (Historic)
					{
						while (MessageNode is not null && Message.Created > MessageNode.Value.Created)
							MessageNode = MessageNode.Next;
						if (MessageNode is null)
						{
							FrameNode = this.frames.Last!;

							if (FrameNode.Value.MessageType != Message.MessageType)
							{
								FrameNode = this.frames.AddLast(MessageFrame.Create(Message));
								this.page.Messages.Add(FrameNode.Value);
							}

							View = FrameNode.Value.AddLast(Message);

							Rec = new(Message, FrameNode);
							MessageNode = this.messages.AddLast(Rec);
						}
						else
						{
							View = null;
							// TODO
						}
					}
					else
					{
						while (MessageNode is not null && Message.Created < MessageNode.Value.Created)
							MessageNode = MessageNode.Previous;

						if (MessageNode is null)
						{
							FrameNode = this.frames.First!;

							if (FrameNode.Value.MessageType != Message.MessageType)
							{
								FrameNode = this.frames.AddFirst(MessageFrame.Create(Message));
								this.page.Messages.Insert(0, FrameNode.Value);
							}

							View = FrameNode.Value.AddFirst(Message);

							Rec = new(Message, FrameNode);
							MessageNode = this.messages.AddFirst(Rec);
						}
						else
						{
							View = null;
							// TODO
						}
					}

					Result.TrySetResult(View);

					{


						//else if (MessageNode.Value.ParsedXaml is IView PrevMessageXaml &&
						//	(i = this.page.Messages.IndexOf(PrevMessageXaml)) >= 0)
						//{
						//	if (MessageNode.Value.ObjectId == Message.ObjectId)
						//	{
						//		this.page.Messages.RemoveAt(i);
						//		this.page.Messages.Insert(i, MessageXaml);
						//
						//		MessageNode.Value = Message;
						//	}
						//	else
						//	{
						//		this.page.Messages.Insert(i + 1, MessageXaml);
						//		MessageNode = this.messages.AddAfter(MessageNode, Message);
						//	}
						//}
						//else
						//{
						//	MessageNode = this.messages.AddLast(Message);
						//	this.page.Messages.Children.Add(MessageXaml);
						//}
					}

					this.messagesByObjectId[Message.ObjectId ?? string.Empty] = MessageNode;
				}
			}
			catch (Exception Ex)
			{
				Result.TrySetException(Ex);
			}

			return await Result.Task;
		}

		/// <summary>
		/// External message has been updated
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task MessageUpdatedAsync(ChatMessage Message)
		{
			try
			{
				await Message.GenerateXaml(this);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return;
			}

			if (Message.ParsedXaml is not IView MessageXaml || string.IsNullOrEmpty(Message.ObjectId))
				return;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					lock (this.synchObject)
					{
						int Index;

						if (this.messagesByObjectId.TryGetValue(Message.ObjectId, out LinkedListNode<MessageRecord>? Node) &&
							Node.Value.Message.ParsedXaml is IView PrevMessageXaml &&
							PrevMessageXaml.Parent is VerticalStackLayout Parent &&
							(Index = Parent.IndexOf(PrevMessageXaml)) >= 0)
						{
							Parent.RemoveAt(Index);
							Parent.Insert(Index, MessageXaml);

							Node.Value.Message = Message;

							// TODO: Update XAML
						}
					}
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		private async Task<IView?> LoadMessagesAsync(bool LoadMore = true)
		{
			IEnumerable<ChatMessage>? Messages = null;
			int C = Constants.BatchSizes.MessageBatchSize;
			DateTime LastTime;
			ChatMessage[] A;

			try
			{
				this.ExistsMoreMessages = false;

				lock (this.synchObject)
				{
					LastTime = LoadMore && this.messages.First is not null ? this.messages.First.Value.Created : DateTime.MaxValue;
				}

				Messages = await Database.Find<ChatMessage>(0, Constants.BatchSizes.MessageBatchSize, new FilterAnd(
					new FilterFieldEqualTo("RemoteBareJid", this.BareJid),
					new FilterFieldLesserThan("Created", LastTime)), "-Created");

				A = [.. Messages];
				C -= A.Length;

				if (!LoadMore)
					Array.Reverse(A);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.ExistsMoreMessages = false;
				return null;
			}

			TaskCompletionSource<IView?> Result = new();

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					IView? Last = null;

					foreach (ChatMessage Message in A)
						Last = await this.MessageAddedMainThread(Message, true);

					this.ExistsMoreMessages = C <= 0;

					Result.TrySetResult(Last);
				}
				catch (Exception Ex)
				{
					Result.TrySetException(Ex);
				}
			});

			return await Result.Task;
		}

		[ObservableProperty]
		private bool hasMessages = false;

		/// <summary>
		/// If the button is expanded
		/// </summary>
		[ObservableProperty]
		private bool isButtonExpanded;

		/// <summary>
		/// Toggles command buttons
		/// </summary>
		[RelayCommand]
		private void ExpandButtons()
		{
			this.IsButtonExpanded = !this.IsButtonExpanded;
		}

		/// <summary>
		/// The command to bind to for loading more messages.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteLoadMoreMessages))]
		private Task<IView?> LoadMoreMessages()
		{
			return this.LoadMessagesAsync(true);
		}

		private bool CanExecuteLoadMoreMessages()
		{
			return this.ExistsMoreMessages && this.page.Messages.Count > 0;
		}

		private bool CanExecuteSendMessage()
		{
			return this.IsConnected && (!string.IsNullOrEmpty(this.MarkdownInput) || this.IsRecordingAudio);
		}

		/// <summary>
		/// The command to bind to for sending user input
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteSendMessage))]
		private async Task Send()
		{
			if (this.IsRecordingAudio)
			{
				// TODO: Audio
				//
				// try
				// {
				// 	await audioRecorder.Value.StopRecording();
				// 	string audioPath = await this.audioRecorderTask!;
				// 
				// 	if (audioPath is not null)
				// 		await this.EmbedMedia(audioPath, true);
				// }
				// catch (Exception ex)
				// {
				// 	ServiceRef.LogService.LogException(ex);
				// }
				// finally
				// {
				// 	this.IsRecordingAudio = false;
				// }
			}
			else
			{
				await this.ExecuteSendMessage(this.MessageId, this.MarkdownInput);
				await this.Cancel();
			}
		}

		private Task ExecuteSendMessage(string? ReplaceObjectId, string MarkdownInput)
		{
			return ExecuteSendMessage(ReplaceObjectId, MarkdownInput, this.BareJid!, this);
		}

		/// <summary>
		/// Sends a Markdown-formatted chat message
		/// </summary>
		/// <param name="ReplaceObjectId">ID of message being updated, or the empty string.</param>
		/// <param name="MarkdownInput">Markdown input.</param>
		/// <param name="BareJid">Bare JID of recipient.</param>
		public static Task ExecuteSendMessage(string? ReplaceObjectId, string MarkdownInput, string BareJid)
		{
			return ExecuteSendMessage(ReplaceObjectId, MarkdownInput, BareJid, null);
		}

		/// <summary>
		/// Sends a Markdown-formatted chat message
		/// </summary>
		/// <param name="ReplaceObjectId">ID of message being updated, or the empty string.</param>
		/// <param name="MarkdownInput">Markdown input.</param>
		/// <param name="BareJid">Bare JID of recipient.</param>
		/// <param name="ChatViewModel">Optional chat view model.</param>
		public static async Task ExecuteSendMessage(string? ReplaceObjectId, string MarkdownInput, string BareJid, ChatViewModel? ChatViewModel)
		{
			try
			{
				if (string.IsNullOrEmpty(MarkdownInput))
					return;

				MarkdownSettings Settings = new()
				{
					AllowScriptTag = false,
					EmbedEmojis = false,    // TODO: Emojis
					AudioAutoplay = false,
					AudioControls = false,
					ParseMetaData = false,
					VideoAutoplay = false,
					VideoControls = false
				};

				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(MarkdownInput, Settings);

				ChatMessage Message = new()
				{
					Created = DateTime.UtcNow,
					RemoteBareJid = BareJid,
					RemoteObjectId = string.Empty,

					MessageType = MessageType.Sent,
					Html = HtmlDocument.GetBody(await Doc.GenerateHTML()),
					PlainText = (await Doc.GeneratePlainText()).Trim(),
					Markdown = MarkdownInput
				};

				StringBuilder Xml = new();

				Xml.Append("<content xmlns=\"urn:xmpp:content\" type=\"text/markdown\">");
				Xml.Append(XML.Encode(MarkdownInput));
				Xml.Append("</content><html xmlns='http://jabber.org/protocol/xhtml-im'><body xmlns='http://www.w3.org/1999/xhtml'>");

				HtmlDocument HtmlDoc = new("<root>" + Message.Html + "</root>");

				foreach (HtmlNode N in (HtmlDoc.Body ?? HtmlDoc.Root).Children)
					N.Export(Xml);

				Xml.Append("</body></html>");

				if (!string.IsNullOrEmpty(ReplaceObjectId))
				{
					Xml.Append("<replace id='");
					Xml.Append(ReplaceObjectId);
					Xml.Append("' xmlns='urn:xmpp:message-correct:0'/>");
				}

				if (string.IsNullOrEmpty(ReplaceObjectId))
				{
					await Database.Insert(Message);

					if (ChatViewModel is not null)
						await ChatViewModel.MessageAddedAsync(Message);
				}
				else
				{
					ChatMessage Old = await Database.TryLoadObject<ChatMessage>(ReplaceObjectId);

					if (Old is null)
					{
						ReplaceObjectId = null;
						await Database.Insert(Message);

						if (ChatViewModel is not null)
							await ChatViewModel.MessageAddedAsync(Message);
					}
					else
					{
						Old.Updated = Message.Created;
						Old.Html = Message.Html;
						Old.PlainText = Message.PlainText;
						Old.Markdown = Message.Markdown;

						await Database.Update(Old);

						Message = Old;

						if (ChatViewModel is not null)
							await ChatViewModel.MessageUpdatedAsync(Message);
					}
				}
				ServiceRef.XmppService.SendMessage(QoSLevel.Unacknowledged, Waher.Networking.XMPP.MessageType.Chat, Message.ObjectId ?? string.Empty,
					BareJid, Xml.ToString(), Message.PlainText, string.Empty, string.Empty, string.Empty, string.Empty, null, null);
			}
			catch (Exception Ex)
			{
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// The command to bind for pausing/resuming the audio recording
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecutePauseResume))]
		private Task PauseResume()
		{
			// TODO: Audio
			//
			// if (audioRecorder.Value.IsPaused)
			// 	await audioRecorder.Value.Resume();
			// else
			// 	await audioRecorder.Value.Pause();
			// 
			// this.IsRecordingPaused = audioRecorder.Value.IsPaused;

			return Task.CompletedTask;
		}

		private static bool CanExecutePauseResume()
		{
			// TODO: Audio
			//
			// return this.IsRecordingAudio && audioRecorder.Value.IsRecording;
			return false;
		}

		private bool CanExecuteCancelMessage()
		{
			return this.IsConnected && (!string.IsNullOrEmpty(this.MarkdownInput) || this.IsRecordingAudio);
		}

		/// <summary>
		/// The command to bind to for sending user input
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteCancelMessage))]
		private Task Cancel()
		{
			if (this.IsRecordingAudio)
			{
				// TODO: Audio
				//
				// try
				// {
				// 	return audioRecorder.Value.StopRecording();
				// }
				// catch (Exception ex)
				// {
				// 	ServiceRef.LogService.LogException(ex);
				// }
				// finally
				// {
				// 	this.IsRecordingAudio = false;
				// }
			}
			else
			{
				this.MarkdownInput = string.Empty;
				this.MessageId = string.Empty;
			}

			return Task.CompletedTask;
		}

		// TODO: Audio
		// 
		// private static System.Timers.Timer audioRecorderTimer;
		// 
		// private static readonly Lazy<AudioRecorderService> audioRecorder = new(() =>
		// {
		// 	return new AudioRecorderService()
		// 	{
		// 		StopRecordingOnSilence = false,
		// 		StopRecordingAfterTimeout = true,
		// 		TotalAudioTimeout = TimeSpan.FromSeconds(60)
		// 	};
		// }, LazyThreadSafetyMode.PublicationOnly);
		//
		//private readonly Task<string>? audioRecorderTask = null;

		private bool CanExecuteRecordAudio()
		{
			return this.IsConnected && !this.IsWriting && ServiceRef.XmppService.FileUploadIsSupported;
		}

		private void OnAudioRecorderTimer(object? source, ElapsedEventArgs e)
		{
			this.OnPropertyChanged(nameof(RecordingTime));

			// TODO: Audio
			//
			// this.IsRecordingPaused = audioRecorder.Value.IsPaused;
		}

		[RelayCommand(CanExecute = nameof(CanExecuteRecordAudio))]
		private async Task RecordAudio()
		{
			if (!ServiceRef.XmppService.FileUploadIsSupported)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
					ServiceRef.Localizer[nameof(AppResources.ServerDoesNotSupportFileUpload)]);
				return;
			}

			try
			{
				PermissionStatus Status = await Permissions.RequestAsync<Permissions.Microphone>();

				if (Status == PermissionStatus.Granted)
				{
					// TODO: Audio
					//
					// this.audioRecorderTask = await audioRecorder.Value.StartRecording();
					// this.IsRecordingAudio = true;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private bool CanExecuteTakePhoto()
		{
			return this.IsConnected && !this.IsWriting && ServiceRef.XmppService.FileUploadIsSupported;
		}

		/// <summary>
		/// Command to take and send a photo
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteTakePhoto))]
		private async Task TakePhoto()
		{
			if (!ServiceRef.XmppService.FileUploadIsSupported)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
					ServiceRef.Localizer[nameof(AppResources.ServerDoesNotSupportFileUpload)]);
				return;
			}

			bool Permitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();
			if (!Permitted)
				return;

			if (DeviceInfo.Platform == DevicePlatform.iOS)
			{
				FileResult? CapturedPhoto;

				try
				{
					CapturedPhoto = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions()
					{
						Title = ServiceRef.Localizer[nameof(AppResources.TakePhotoToShare)]
					});
				}
				catch (Exception Ex)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
						ServiceRef.Localizer[nameof(AppResources.TakingAPhotoIsNotSupported)] + ": " + Ex.Message);
					return;
				}

				if (CapturedPhoto is not null)
				{
					try
					{
						await this.EmbedMedia(CapturedPhoto.FullPath, true);
					}
					catch (Exception Ex)
					{
						await ServiceRef.UiService.DisplayException(Ex);
					}
				}
			}
			else
			{
				FileResult? CapturedPhoto;

				try
				{
					CapturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (CapturedPhoto is null)
						return;
				}
				catch (Exception Ex)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
						ServiceRef.Localizer[nameof(AppResources.TakingAPhotoIsNotSupported)] + ": " + Ex.Message);
					return;
				}

				if (CapturedPhoto is not null)
				{
					try
					{
						await this.EmbedMedia(CapturedPhoto.FullPath, true);
					}
					catch (Exception Ex)
					{
						await ServiceRef.UiService.DisplayException(Ex);
					}
				}
			}
		}

		private async Task EmbedMedia(string FilePath, bool DeleteFile)
		{
			try
			{
				byte[] Bin = File.ReadAllBytes(FilePath);
				if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
					ContentType = "application/octet-stream";

				if (Bin.Length > ServiceRef.TagProfile.HttpFileUploadMaxSize)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PhotoIsTooLarge)]);
					return;
				}

				// Taking or picking photos switches to another app, so ID app has to reconnect again after.
				if (!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnableToConnectTo), ServiceRef.TagProfile.Domain ?? string.Empty]);
					return;
				}

				string FileName = Path.GetFileName(FilePath);

				// Encrypting image

				using RandomNumberGenerator Rnd = RandomNumberGenerator.Create();
				byte[] Key = new byte[16];
				byte[] IV = new byte[16];

				Rnd.GetBytes(Key);
				Rnd.GetBytes(IV);

				Aes Aes = Aes.Create();
				Aes.BlockSize = 128;
				Aes.KeySize = 256;
				Aes.Mode = CipherMode.CBC;
				Aes.Padding = PaddingMode.PKCS7;

				using ICryptoTransform Transform = Aes.CreateEncryptor(Key, IV);
				Bin = Transform.TransformFinalBlock(Bin, 0, Bin.Length);

				// Preparing File upload service that content uploaded next is encrypted, and can be stored in encrypted storage.

				StringBuilder Xml = new();

				Xml.Append("<prepare xmlns='http://waher.se/Schema/EncryptedStorage.xsd' filename='");
				Xml.Append(XML.Encode(FileName));
				Xml.Append("' size='");
				Xml.Append(Bin.Length.ToString(CultureInfo.InvariantCulture));
				Xml.Append("' content-type='application/octet-stream'/>");

				await ServiceRef.XmppService.IqSetAsync(ServiceRef.TagProfile.HttpFileUploadJid!, Xml.ToString());
				// Empty response expected. Errors cause an exception to be raised.

				// Requesting upload slot

				HttpFileUploadEventArgs Slot = await ServiceRef.XmppService.RequestUploadSlotAsync(
					FileName, "application/octet-stream", Bin.Length);

				if (!Slot.Ok)
					throw Slot.StanzaError ?? new Exception(Slot.ErrorText);

				// Uploading encrypted image

				await Slot.PUT(Bin, "application/octet-stream", (int)Constants.Timeouts.UploadFile.TotalMilliseconds);

				// Generating Markdown message to send to recipient

				StringBuilder Markdown = new();

				Markdown.Append("![");
				Markdown.Append(MarkdownDocument.Encode(FileName));
				Markdown.Append("](");
				Markdown.Append(Constants.UriSchemes.Aes256);
				Markdown.Append(':');
				Markdown.Append(Convert.ToBase64String(Key));
				Markdown.Append(':');
				Markdown.Append(Convert.ToBase64String(IV));
				Markdown.Append(':');
				Markdown.Append(ContentType);
				Markdown.Append(':');
				Markdown.Append(Slot.GetUrl);

				SKImageInfo ImageInfo = SKBitmap.DecodeBounds(Bin);
				if (!ImageInfo.IsEmpty)
				{
					Markdown.Append(' ');
					Markdown.Append(ImageInfo.Width.ToString(CultureInfo.InvariantCulture));
					Markdown.Append(' ');
					Markdown.Append(ImageInfo.Height.ToString(CultureInfo.InvariantCulture));
				}

				Markdown.Append(')');

				await this.ExecuteSendMessage(string.Empty, Markdown.ToString());

				// TODO: End-to-End encryption, or using Elliptic Curves of recipient together with sender to deduce shared secret.

				if (DeleteFile)
					File.Delete(FilePath);
			}
			catch (Exception Ex)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Ex.Message);
				ServiceRef.LogService.LogException(Ex);
				return;
			}
		}

		private bool CanExecuteEmbedFile()
		{
			return this.IsConnected && !this.IsWriting && ServiceRef.XmppService.FileUploadIsSupported;
		}

		/// <summary>
		/// Command to embed a file
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteEmbedFile))]
		private async Task EmbedFile()
		{
			if (!ServiceRef.XmppService.FileUploadIsSupported)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PickPhoto)], ServiceRef.Localizer[nameof(AppResources.SelectingAPhotoIsNotSupported)]);
				return;
			}

			FileResult? PickedPhoto = await MediaPicker.PickPhotoAsync();

			if (PickedPhoto is not null)
				await this.EmbedMedia(PickedPhoto.FullPath, false);
		}

		private bool CanExecuteEmbedId()
		{
			return this.IsConnected && !this.IsWriting;
		}

		/// <summary>
		/// Command to embed a reference to a legal ID
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteEmbedId))]
		private async Task EmbedId()
		{
			TaskCompletionSource<ContactInfoModel?> SelectedContact = new();
			ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.SelectContactToPay)], SelectedContact)
			{
				CanScanQrCode = true
			};

			await ServiceRef.UiService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);

			ContactInfoModel? Contact = await SelectedContact.Task;
			if (Contact is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			if (Contact.LegalIdentity is not null)
			{
				StringBuilder Markdown = new();

				Markdown.Append("```");
				Markdown.AppendLine(Constants.UriSchemes.IotId);

				Contact.LegalIdentity.Serialize(Markdown, true, true, true, true, true, true, true);

				Markdown.AppendLine();
				Markdown.AppendLine("```");

				await this.ExecuteSendMessage(string.Empty, Markdown.ToString());
				return;
			}

			if (!string.IsNullOrEmpty(Contact.LegalId))
			{
				await this.ExecuteSendMessage(string.Empty, "![" + MarkdownDocument.Encode(Contact.FriendlyName) + "](" + ContractsClient.LegalIdUriString(Contact.LegalId) + ")");
				return;
			}

			if (!string.IsNullOrEmpty(Contact.BareJid))
			{
				await this.ExecuteSendMessage(string.Empty, "![" + MarkdownDocument.Encode(Contact.FriendlyName) + "](xmpp:" + Contact.BareJid + "?subscribe)");
				return;
			}
		}

		private bool CanExecuteEmbedContract()
		{
			return this.IsConnected && !this.IsWriting;
		}

		/// <summary>
		/// Command to embed a reference to a smart contract
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteEmbedContract))]
		private async Task EmbedContract()
		{
			TaskCompletionSource<Contract?> SelectedContract = new();
			MyContractsNavigationArgs Args = new(ContractsListMode.Contracts, SelectedContract);

			await ServiceRef.UiService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);

			Contract? Contract = await SelectedContract.Task;
			if (Contract is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			StringBuilder Markdown = new();

			Markdown.Append("```");
			Markdown.AppendLine(Constants.UriSchemes.IotSc);

			Contract.Serialize(Markdown, true, true, true, true, true, true, true);

			Markdown.AppendLine();
			Markdown.AppendLine("```");

			await this.ExecuteSendMessage(string.Empty, Markdown.ToString());
		}

		private bool CanExecuteEmbedMoney()
		{
			return this.IsConnected && !this.IsWriting;
		}

		/// <summary>
		/// Command to embed a payment
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteEmbedMoney))]
		private async Task EmbedMoney()
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
			EDalerUriNavigationArgs Args = new(Parsed, this.FriendlyName ?? string.Empty, UriToSend);

			await ServiceRef.UiService.GoToAsync(nameof(SendPaymentPage), Args, BackMethod.Pop);

			string? Uri = await UriToSend.Task;
			if (string.IsNullOrEmpty(Uri) || !EDalerUri.TryParse(Uri, out Parsed))
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

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

			await this.ExecuteSendMessage(string.Empty, "![" + Sb.ToString() + "](" + Uri + ")");
		}

		private bool CanExecuteEmbedToken()
		{
			return this.IsConnected && !this.IsWriting;
		}

		/// <summary>
		/// Command to embed a token reference
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteEmbedToken))]
		private async Task EmbedToken()
		{
			MyTokensNavigationArgs Args = new();

			await ServiceRef.UiService.GoToAsync(nameof(MyTokensPage), Args, BackMethod.Pop);

			TokenItem? Selected = await Args.TokenItemProvider.Task;

			if (Selected is null)
				return;

			StringBuilder Markdown = new();

			Markdown.AppendLine("```nfeat");

			Selected.Token.Serialize(Markdown);

			Markdown.AppendLine();
			Markdown.AppendLine("```");

			await this.ExecuteSendMessage(string.Empty, Markdown.ToString());
		}

		private bool CanExecuteEmbedThing()
		{
			return this.IsConnected && !this.IsWriting;
		}

		/// <summary>
		/// Command to embed a reference to a thing
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteEmbedThing))]
		private async Task EmbedThing()
		{
			TaskCompletionSource<ContactInfoModel?> ThingToShare = new();
			MyThingsNavigationArgs Args = new(ThingToShare);

			await ServiceRef.UiService.GoToAsync(nameof(MyThingsPage), Args, BackMethod.Pop);

			ContactInfoModel? Thing = await ThingToShare.Task;
			if (Thing is null)
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			StringBuilder Sb = new();

			Sb.Append("![");
			Sb.Append(MarkdownDocument.Encode(Thing.FriendlyName));
			Sb.Append("](iotdisco:JID=");
			Sb.Append(Thing.BareJid);

			if (!string.IsNullOrEmpty(Thing.SourceId))
			{
				Sb.Append(";SID=");
				Sb.Append(Thing.SourceId);
			}

			if (!string.IsNullOrEmpty(Thing.Partition))
			{
				Sb.Append(";PT=");
				Sb.Append(Thing.Partition);
			}

			if (!string.IsNullOrEmpty(Thing.NodeId))
			{
				Sb.Append(";NID=");
				Sb.Append(Thing.NodeId);
			}

			Sb.Append(')');

			await this.ExecuteSendMessage(string.Empty, Sb.ToString());
		}

		/// <summary>
		/// Command executed when a message has been selected (or deselected) in the list view.
		/// </summary>
		[RelayCommand]
		private Task MessageSelected(object Parameter)
		{
			if (Parameter is ChatMessage Message)
			{
				// TODO: Audio
				//
				// if (Message.ParsedXaml is View View)
				// {
				// 	AudioPlayerControl AudioPlayer = View.Descendants().OfType<AudioPlayerControl>().FirstOrDefault();
				// 	if (AudioPlayer is not null)
				// 	{
				// 		return Task.CompletedTask;
				// 	}
				// }

				switch (Message.MessageType)
				{

					case MessageType.Sent:
						this.MessageId = Message.ObjectId;
						this.MarkdownInput = Message.Markdown;
						break;


					case MessageType.Received:
						string S = Message.Markdown;
						if (string.IsNullOrEmpty(S))
							S = MarkdownDocument.Encode(Message.PlainText);

						string[] Rows = S.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

						StringBuilder Quote = new();

						foreach (string Row in Rows)
						{
							Quote.Append("> ");
							Quote.AppendLine(Row);
						}

						Quote.AppendLine();

						this.MessageId = string.Empty;
						this.MarkdownInput = Quote.ToString();
						break;
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Called when a Multi-media URI link using the XMPP URI scheme.
		/// </summary>
		/// <param name="Uri">URI</param>
		/// <param name="Scheme">URI Scheme</param>
		public async Task ExecuteUriClicked(string Uri, UriScheme Scheme)
		{
			try
			{
				if (Scheme == UriScheme.Xmpp)
					await ProcessXmppUri(Uri);
				else
				{
					int I = Uri.IndexOf(':');
					if (I < 0)
						return;

					string S = Uri[(I + 1)..].Trim();
					if (S.StartsWith('<') && S.EndsWith('>'))  // XML
					{
						XmlDocument Doc = new()
						{
							PreserveWhitespace = true
						};
						Doc.LoadXml(S);

						switch (Scheme)
						{
							case UriScheme.IotId:
								LegalIdentity Id = LegalIdentity.Parse(Doc.DocumentElement);
								ViewIdentityNavigationArgs ViewIdentityArgs = new(Id);

								await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage), ViewIdentityArgs, BackMethod.Pop);
								break;

							case UriScheme.IotSc:
								ParsedContract ParsedContract = await Contract.Parse(Doc.DocumentElement, ServiceRef.XmppService.ContractsClient, true);
								ViewContractNavigationArgs ViewContractArgs = new(ParsedContract.Contract, false);

								await ServiceRef.UiService.GoToAsync(nameof(ViewContractPage), ViewContractArgs, BackMethod.Pop);
								break;

							case UriScheme.NeuroFeature:
								if (!Token.TryParse(Doc.DocumentElement, out Token ParsedToken))
									throw new Exception(ServiceRef.Localizer[nameof(AppResources.InvalidNeuroFeatureToken)]);

								if (!ServiceRef.NotificationService.TryGetNotificationEvents(NotificationEventType.Wallet, ParsedToken.TokenId, out NotificationEvent[]? Events))
									Events = [];

								TokenDetailsNavigationArgs Args = new(new TokenItem(ParsedToken, Events));

								await ServiceRef.UiService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
								break;

							default:
								return;
						}
					}
					else
						await QrCode.OpenUrl(Uri);
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

					await MopupService.Instance.PushAsync(SubscribeToPopup);
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

				case "unsubscribe":
					// TODO
					return false;

				case "remove":
					ServiceRef.XmppService.GetRosterItem(Jid);
					// TODO
					return false;

				default:
					return false;
			}
		}

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public bool IsLinkable => true;

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
		public Task<string> Title => Task.FromResult<string>(this.FriendlyName ?? string.Empty);

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
