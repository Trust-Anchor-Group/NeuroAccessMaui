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
using System.Collections.ObjectModel;
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
		private TaskCompletionSource<bool> waitUntilBound = new();

		/// <summary>
		/// Creates an instance of the <see cref="ChatViewModel"/> class.
		/// </summary>
		protected internal ChatViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.UiService.TryGetArgs(out ChatNavigationArgs? args, this.UniqueId))
			{
				this.LegalId = args.LegalId;
				this.BareJid = args.BareJid;
				this.FriendlyName = args.FriendlyName;
			}
			else
			{
				this.LegalId = string.Empty;
				this.BareJid = string.Empty;
				this.FriendlyName = string.Empty;
			}

			await this.ExecuteLoadMessagesAsync(false);

			this.waitUntilBound.TrySetResult(true);

			await ServiceRef.NotificationService.DeleteEvents(NotificationEventType.Contacts, this.BareJid);
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			await base.OnDispose();

			this.waitUntilBound = new TaskCompletionSource<bool>();
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
		/// Holds the list of chat messages to display.
		/// </summary>
		public ObservableCollection<ChatMessage> Messages { get; } = [];

		/// <summary>
		/// External message has been received
		/// </summary>
		/// <param name="Message">Message</param>
		public async Task MessageAddedAsync(ChatMessage Message)
		{
			try
			{
				await Message.GenerateXaml(this);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					int i = 0;

					for (; i < this.Messages.Count; i++)
					{
						ChatMessage Item = this.Messages[i];

						if (Item.Created <= Message.Created)
							break;
					}

					if (i >= this.Messages.Count)
						this.Messages.Add(Message);
					else if (this.Messages[i].ObjectId != Message.ObjectId)
						this.Messages.Insert(i, Message);

					this.EnsureFirstMessageIsEmpty();
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
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
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					for (int i = 0; i < this.Messages.Count; i++)
					{
						ChatMessage Item = this.Messages[i];

						if (Item.ObjectId == Message.ObjectId)
						{
							this.Messages[i] = Message;
							break;
						}

						this.EnsureFirstMessageIsEmpty();
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
		}

		private async Task ExecuteLoadMessagesAsync(bool LoadMore = true)
		{
			IEnumerable<ChatMessage>? Messages = null;
			int c = Constants.BatchSizes.MessageBatchSize;

			try
			{
				this.ExistsMoreMessages = false;

				DateTime LastTime = LoadMore ? this.Messages[^1].Created : DateTime.MaxValue;

				Messages = await Database.Find<ChatMessage>(0, Constants.BatchSizes.MessageBatchSize,
					new FilterAnd(
						new FilterFieldEqualTo("RemoteBareJid", this.BareJid),
						new FilterFieldLesserThan("Created", LastTime)), "-Created");

				foreach (ChatMessage Message in Messages)
				{
					await Message.GenerateXaml(this);
					c--;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				this.ExistsMoreMessages = false;
				return;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					this.MergeObservableCollections(LoadMore, Messages.ToList());
					this.ExistsMoreMessages = c <= 0;
					this.EnsureFirstMessageIsEmpty();
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					this.ExistsMoreMessages = false;
				}
			});
		}

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
		private Task LoadMoreMessages()
		{
			return this.LoadMoreMessages(true);
		}

		private bool CanExecuteLoadMoreMessages()
		{
			return this.ExistsMoreMessages && this.Messages.Count > 0;
		}

		private async Task LoadMoreMessages(bool LoadMore)
		{
			IEnumerable<ChatMessage>? Messages = null;
			int c = Constants.BatchSizes.MessageBatchSize;

			try
			{
				this.ExistsMoreMessages = false;

				DateTime LastTime = LoadMore ? this.Messages[^1].Created : DateTime.MaxValue;

				Messages = await Database.Find<ChatMessage>(0, Constants.BatchSizes.MessageBatchSize,
					new FilterAnd(
						new FilterFieldEqualTo("RemoteBareJid", this.BareJid),
						new FilterFieldLesserThan("Created", LastTime)), "-Created");

				foreach (ChatMessage Message in Messages)
				{
					await Message.GenerateXaml(this);
					c--;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				this.ExistsMoreMessages = false;
				return;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					this.MergeObservableCollections(LoadMore, Messages.ToList());
					this.ExistsMoreMessages = c <= 0;
					this.EnsureFirstMessageIsEmpty();
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					this.ExistsMoreMessages = false;
				}
			});
		}

		private void MergeObservableCollections(bool LoadMore, List<ChatMessage> NewMessages)
		{
			if (LoadMore || (this.Messages.Count == 0))
			{
				foreach (ChatMessage Message in NewMessages)
					this.Messages.Add(Message);
				return;
			}

			List<ChatMessage> RemoveItems = this.Messages.Where(oel => NewMessages.All(nel => nel.UniqueName != oel.UniqueName)).ToList();

			foreach (ChatMessage Message in RemoveItems)
				this.Messages.Remove(Message);

			for (int i = 0; i < NewMessages.Count; i++)
			{
				ChatMessage Item = NewMessages[i];

				if (i >= this.Messages.Count)
					this.Messages.Add(Item);
				else if (this.Messages[i].UniqueName != Item.UniqueName)
					this.Messages.Insert(i, Item);
			}
		}

		private void EnsureFirstMessageIsEmpty()
		{
			if (this.Messages.Count > 0 && this.Messages[0].MessageType != MessageType.Empty)
				this.Messages.Insert(0, ChatMessage.Empty);
		}

		/// <summary>
		/// The command to bind to for sending user input
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteSendMessage))]
		private Task Send()
		{
			return this.ExecuteSendMessage();
		}

		private bool CanExecuteSendMessage()
		{
			return this.IsConnected && (!string.IsNullOrEmpty(this.MarkdownInput) || this.IsRecordingAudio);
		}

		private async Task ExecuteSendMessage()
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
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
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
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
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

			if (DeviceInfo.Platform == DevicePlatform.iOS)
			{
				FileResult capturedPhoto;

				try
				{
					capturedPhoto = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions()
					{
						Title = ServiceRef.Localizer[nameof(AppResources.TakePhotoToShare)]
					});
				}
				catch (Exception ex)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
						ServiceRef.Localizer[nameof(AppResources.TakingAPhotoIsNotSupported)] + ": " + ex.Message);
					return;
				}

				if (capturedPhoto is not null)
				{
					try
					{
						await this.EmbedMedia(capturedPhoto.FullPath, true);
					}
					catch (Exception ex)
					{
						await ServiceRef.UiService.DisplayException(ex);
					}
				}
			}
			else
			{
				FileResult capturedPhoto;

				try
				{
					capturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (capturedPhoto is null)
						return;
				}
				catch (Exception ex)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.TakePhoto)],
						ServiceRef.Localizer[nameof(AppResources.TakingAPhotoIsNotSupported)] + ": " + ex.Message);
					return;
				}

				if (capturedPhoto is not null)
				{
					try
					{
						await this.EmbedMedia(capturedPhoto.FullPath, true);
					}
					catch (Exception ex)
					{
						await ServiceRef.UiService.DisplayException(ex);
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
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
				ServiceRef.LogService.LogException(ex);
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

			FileResult pickedPhoto = await MediaPicker.PickPhotoAsync();

			if (pickedPhoto is not null)
				await this.EmbedMedia(pickedPhoto.FullPath, false);
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
			StringBuilder sb = new();

			sb.Append("edaler:");

			if (!string.IsNullOrEmpty(this.LegalId))
			{
				sb.Append("ti=");
				sb.Append(this.LegalId);
			}
			else if (!string.IsNullOrEmpty(this.BareJid))
			{
				sb.Append("t=");
				sb.Append(this.BareJid);
			}
			else
				return;

			Balance CurrentBalance = await ServiceRef.XmppService.GetEDalerBalance();

			sb.Append(";cu=");
			sb.Append(CurrentBalance.Currency);

			if (!EDalerUri.TryParse(sb.ToString(), out EDalerUri Parsed))
				return;

			TaskCompletionSource<string?> UriToSend = new();
			EDalerUriNavigationArgs Args = new(Parsed, this.FriendlyName ?? string.Empty, UriToSend);

			await ServiceRef.UiService.GoToAsync(nameof(SendPaymentPage), Args, BackMethod.Pop);

			string? Uri = await UriToSend.Task;
			if (string.IsNullOrEmpty(Uri) || !EDalerUri.TryParse(Uri, out Parsed))
				return;

			await this.waitUntilBound.Task;     // Wait until view is bound again.

			sb.Clear();
			sb.Append(MoneyToString.ToString(Parsed.Amount));

			if (Parsed.AmountExtra.HasValue)
			{
				sb.Append(" (+");
				sb.Append(MoneyToString.ToString(Parsed.AmountExtra.Value));
				sb.Append(')');
			}

			sb.Append(' ');
			sb.Append(Parsed.Currency);

			await this.ExecuteSendMessage(string.Empty, "![" + sb.ToString() + "](" + Uri + ")");
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

			StringBuilder sb = new();

			sb.Append("![");
			sb.Append(MarkdownDocument.Encode(Thing.FriendlyName));
			sb.Append("](iotdisco:JID=");
			sb.Append(Thing.BareJid);

			if (!string.IsNullOrEmpty(Thing.SourceId))
			{
				sb.Append(";SID=");
				sb.Append(Thing.SourceId);
			}

			if (!string.IsNullOrEmpty(Thing.Partition))
			{
				sb.Append(";PT=");
				sb.Append(Thing.Partition);
			}

			if (!string.IsNullOrEmpty(Thing.NodeId))
			{
				sb.Append(";NID=");
				sb.Append(Thing.NodeId);
			}

			sb.Append(')');

			await this.ExecuteSendMessage(string.Empty, sb.ToString());
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
						string s = Message.Markdown;
						if (string.IsNullOrEmpty(s))
							s = MarkdownDocument.Encode(Message.PlainText);

						string[] Rows = s.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

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
		/// <param name="Message">Message containing the URI.</param>
		/// <param name="Uri">URI</param>
		/// <param name="Scheme">URI Scheme</param>
		public async Task ExecuteUriClicked(ChatMessage Message, string Uri, UriScheme Scheme)
		{
			try
			{
				if (Scheme == UriScheme.Xmpp)
					await ProcessXmppUri(Uri);
				else
				{
					int i = Uri.IndexOf(':');
					if (i < 0)
						return;

					string s = Uri[(i + 1)..].Trim();
					if (s.StartsWith('<') && s.EndsWith('>'))  // XML
					{
						XmlDocument Doc = new()
						{
							PreserveWhitespace = true
						};
						Doc.LoadXml(s);

						switch (Scheme)
						{
							case UriScheme.IotId:
								LegalIdentity Id = LegalIdentity.Parse(Doc.DocumentElement);
								ViewIdentityNavigationArgs ViewIdentityArgs = new(Id);

								await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage), ViewIdentityArgs, BackMethod.Pop);
								break;

							case UriScheme.IotSc:
								ParsedContract ParsedContract = await Contract.Parse(Doc.DocumentElement, ServiceRef.XmppService.ContractsClient);
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
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Processes an XMPP URI
		/// </summary>
		/// <param name="Uri">XMPP URI</param>
		/// <returns>If URI could be processed.</returns>
		public static async Task<bool> ProcessXmppUri(string Uri)
		{
			int i = Uri.IndexOf(':');
			if (i < 0)
				return false;

			string Jid = Uri[(i + 1)..].TrimStart();
			string Command;

			i = Jid.IndexOf('?');
			if (i < 0)
				Command = "subscribe";
			else
			{
				Command = Jid[(i + 1)..].TrimStart();
				Jid = Jid[..i].TrimEnd();
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
