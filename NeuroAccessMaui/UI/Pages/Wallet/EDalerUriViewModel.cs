using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Main.Calculator;
using NeuroAccessMaui.UI.Popups.Transaction;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP.StanzaErrors;

namespace NeuroAccessMaui.UI.Pages.Wallet
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of an eDaler URI.
	/// </summary>
	public partial class EDalerUriViewModel : QrXmppViewModel
	{
		private readonly EDalerUriNavigationArgs? navigationArguments;
		private readonly IShareQrCode? shareQrCode;
		private readonly TaskCompletionSource<string?>? uriToSend = null;
		private readonly TaskCompletionSource<string?>? messageToSend = null;

		public ObservableTask<bool> GetBalanceTask { get; } = new();

		/// <summary>
		/// The view model to bind to for when displaying the contents of an eDaler URI.
		/// </summary>
		/// <param name="ShareQrCode">Interface for pages with a share button.</param>
		/// <param name="Args">Navigation arguments</param>
		public EDalerUriViewModel(IShareQrCode? ShareQrCode, EDalerUriNavigationArgs? Args)
			: base()
		{
			this.navigationArguments = Args;
			this.shareQrCode = ShareQrCode;

			this.uriToSend = Args?.UriToSend;
			this.messageToSend = Args?.MessageToSend;
			this.FriendlyName = Args?.FriendlyName;

			if (Args?.Uri is not null)
			{
				this.Uri = Args.Uri.UriString;
				this.Id = Args.Uri.Id;
				this.Amount = Args.Uri.Amount;
				this.AmountExtra = Args.Uri.AmountExtra;
				this.Currency = Args.Uri.Currency;
				this.Created = Args.Uri.Created;
				this.Expires = Args.Uri.Expires;
				this.ExpiresStr = this.Expires.ToShortDateString();
				this.From = Args.Uri.From;
				this.FromType = Args.Uri.FromType;
				this.To = Args.Uri.To;
				this.ToType = Args.Uri.ToType;
				this.ToPreset = !string.IsNullOrEmpty(Args.Uri.To);
				this.Complete = Args.Uri.Complete;
			}

			this.NotPaid = true;

			this.AmountText = !this.Amount.HasValue || this.Amount.Value <= 0 ? string.Empty : MoneyToString.ToString(this.Amount.Value);
			this.AmountOk = CommonTypes.TryParse(this.AmountText, out decimal D) && D > 0;
			this.AmountPreset = !string.IsNullOrEmpty(this.AmountText) && this.AmountOk;
			this.AmountAndCurrency = this.AmountText + " " + this.Currency;

			this.AmountExtraText = this.AmountExtra.HasValue ? MoneyToString.ToString(this.AmountExtra.Value) : string.Empty;
			this.AmountExtraOk = !this.AmountExtra.HasValue || this.AmountExtra.Value >= 0;
			this.AmountExtraPreset = this.AmountExtra.HasValue;
			this.AmountExtraAndCurrency = this.AmountExtraText + " " + this.Currency;

			StringBuilder Url = new();

			Url.Append("https://");
			Url.Append(this.From);
			Url.Append("/Images/eDalerFront200.png");

			this.EDalerFrontGlyph = Url.ToString();

			Url.Clear();
			Url.Append("https://");
			Url.Append(ServiceRef.TagProfile.Domain);
			Url.Append("/Images/eDalerBack200.png");
			this.EDalerBackGlyph = Url.ToString();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			// Subscribe to petitioned identity responses (only once)
			ServiceRef.XmppService.PetitionedIdentityResponseReceived += this.XmppService_PetitionedIdentityResponseReceived;

			if (this.navigationArguments is not null)
			{
				if (this.navigationArguments.Uri?.EncryptedMessage is not null)
				{
					if (this.navigationArguments.Uri.EncryptionPublicKey is null)
						this.Message = Encoding.UTF8.GetString(this.navigationArguments.Uri.EncryptedMessage);
					else
					{
						//TODO: Fix LocalIsRecipient argument
						bool LocalIsRecipient = this.navigationArguments.Uri.ToType == EntityType.LegalId &&
							this.navigationArguments.Uri.To == ServiceRef.TagProfile?.LegalIdentity?.Id;

						this.Message = await ServiceRef.XmppService.TryDecryptMessage(this.navigationArguments.Uri.EncryptedMessage,
						this.navigationArguments.Uri.EncryptionPublicKey, this.navigationArguments.Uri.Id, this.navigationArguments.Uri.From, LocalIsRecipient);
					}
					this.HasMessage = !string.IsNullOrEmpty(this.Message);
				}

				this.MessagePreset = !string.IsNullOrEmpty(this.Message);
				this.CanEncryptMessage = false;//this.navigationArguments.Uri?.ToType == EntityType.LegalId;
				this.EncryptMessage = this.CanEncryptMessage;
			}

			this.GetBalanceTask.Load(this.LoadBalanceAsync);
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.uriToSend?.TrySetResult(null);
			this.messageToSend?.TrySetResult(null);

			ServiceRef.XmppService.PetitionedIdentityResponseReceived -= this.XmppService_PetitionedIdentityResponseReceived;

			await base.OnDispose();
		}

		private async Task XmppService_PetitionedIdentityResponseReceived(object? Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			try
			{
				// If we have petitioned this identity and response is positive, capture JID
				if (e.Response && e.RequestedIdentity is not null && this.ToType == EntityType.LegalId &&
					string.Equals(this.To, e.RequestedIdentity.Id, StringComparison.OrdinalIgnoreCase))
				{
					string Jid = e.RequestedIdentity.GetJid();
					if (!string.IsNullOrEmpty(Jid))
					{
						MainThread.BeginInvokeOnMainThread(async () =>
						{
							this.To = Jid;
							this.ToType = EntityType.NetworkJid;

							// Enrich contact info if possible
							ContactInfo? InfoByJid = await ContactInfo.FindByBareJid(Jid);
							if (InfoByJid is not null)
							{
								this.ToContact = new ContactInfoModel(InfoByJid);
								this.ContactSelected = true;
							}
						});
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		#region Properties

		/// <summary>
		/// edaler URI to process
		/// </summary>
		[ObservableProperty]
		private string? uri;

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal? amount;

		/// <summary>
		/// If <see cref="Amount"/> is OK.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(PayOnlineCommand))]
		[NotifyCanExecuteChangedFor(nameof(GenerateQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(SendPaymentCommand))]
		private bool amountOk;

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.IsConnected):
					this.AcceptCommand.NotifyCanExecuteChanged();
					this.PayOnlineCommand.NotifyCanExecuteChanged();
					this.SubmitCommand.NotifyCanExecuteChanged();
					break;

				case nameof(this.HasQrCode):
					this.PayOnlineCommand.NotifyCanExecuteChanged();
					this.GenerateQrCodeCommand.NotifyCanExecuteChanged();
					this.ShareCommand.NotifyCanExecuteChanged();
					break;

				case nameof(this.AmountText):
					if (CommonTypes.TryParse(this.AmountText, out decimal D2) && D2 > 0)
					{
						this.Amount = D2;
						this.AmountOk = true;
					}
					else
						this.AmountOk = false;

					this.AmountAndCurrency = this.AmountText + " " + this.Currency;
					break;

				case nameof(this.AmountExtraText):
					if (string.IsNullOrEmpty(this.AmountExtraText))
					{
						this.AmountExtra = null;
						this.AmountExtraOk = true;
					}
					else if (CommonTypes.TryParse(this.AmountExtraText, out decimal D3) && D3 >= 0)
					{
						this.AmountExtra = D3;
						this.AmountExtraOk = true;
					}
					else
						this.AmountExtraOk = false;

					this.AmountExtraAndCurrency = this.AmountExtraText + " " + this.Currency;
					break;
			}
		}

		/// <summary>
		/// The last known update time of the balance.
		/// </summary>
		[ObservableProperty]
		private DateTime? balanceUpdated = ServiceRef.TagProfile.LastEDalerBalanceUpdate;


		/// <summary>
		/// Exposes the current balance as a decimal.
		/// </summary>
		public decimal BalanceDecimal =>
			this.FetchedBalance?.Amount ?? ServiceRef.TagProfile.LastEDalerBalanceDecimal;

		public string BalanceString =>
			this.BalanceDecimal + " " + this.Currency;

		public decimal ReservedDecimal =>
			this.FetchedBalance?.Reserved ?? -1;

		public string ReservedString
		{
			get
			{
				if (this.ReservedDecimal == -1)
					return ServiceRef.Localizer[nameof(AppResources.UnknownPleaseRefresh)];
				else
					return this.ReservedDecimal + " " + this.Currency;
			}
		}

		public bool HasReserved => this.ReservedDecimal > 0 || this.ReservedDecimal == -1;


		/// <summary>
		/// Last fetched balance. Changing this notifies <see cref="BalanceDecimal"/>.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(BalanceDecimal))]
		[NotifyPropertyChangedFor(nameof(BalanceString))]
		[NotifyPropertyChangedFor(nameof(ReservedDecimal))]
		[NotifyPropertyChangedFor(nameof(ReservedString))]
		[NotifyPropertyChangedFor(nameof(HasReserved))]
		Balance? fetchedBalance;

		/// <summary>
		/// <see cref="Amount"/> as text.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(PayOnlineCommand))]
		[NotifyCanExecuteChangedFor(nameof(GenerateQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(SendPaymentCommand))]
		private string? amountText;

		/// <summary>
		/// <see cref="AmountText"/> and <see cref="Currency"/>.
		/// </summary>
		[ObservableProperty]
		private string? amountAndCurrency;

		/// <summary>
		/// If <see cref="Amount"/> is preset.
		/// </summary>
		[ObservableProperty]
		private bool amountPreset;

		/// <summary>
		/// AmountExtra of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal? amountExtra;

		/// <summary>
		/// If <see cref="AmountExtra"/> is OK.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(PayOnlineCommand))]
		[NotifyCanExecuteChangedFor(nameof(GenerateQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(SendPaymentCommand))]
		private bool amountExtraOk;

		/// <summary>
		/// <see cref="AmountExtra"/> as text.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(PayOnlineCommand))]
		[NotifyCanExecuteChangedFor(nameof(GenerateQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(SendPaymentCommand))]
		private string? amountExtraText;

		/// <summary>
		/// <see cref="AmountExtraText"/> and <see cref="Currency"/>.
		/// </summary>
		[ObservableProperty]
		private string? amountExtraAndCurrency;

		/// <summary>
		/// If <see cref="AmountExtra"/> is preset.
		/// </summary>
		[ObservableProperty]
		private bool amountExtraPreset;

		/// <summary>
		/// Currency of eDaler to process
		/// </summary>
		[ObservableProperty]
		private string? currency;

		/// <summary>
		/// When code was created.
		/// </summary>
		[ObservableProperty]
		private DateTime created;

		/// <summary>
		/// When code expires
		/// </summary>
		[ObservableProperty]
		private DateTime expires;

		/// <summary>
		/// When code expires
		/// </summary>
		[ObservableProperty]
		private string? expiresStr;

		/// <summary>
		/// Globally unique identifier of code
		/// </summary>
		[ObservableProperty]
		private Guid id;

		/// <summary>
		/// From who eDaler is to be transferred
		/// </summary>
		[ObservableProperty]
		private string? from;

		/// <summary>
		/// Type of identity specified in <see cref="From"/>
		/// </summary>
		[ObservableProperty]
		private EntityType fromType;

		/// <summary>
		/// To whom eDaler is to be transferred
		/// </summary>
		[ObservableProperty]
		private string? to;

		/// <summary>
		/// Used for UI rendering of selected contact, use property this.to for transfer
		/// </summary>
		[ObservableProperty]
		private ContactInfoModel? toContact;

		/// <summary>
		/// If a contact has been selected
		/// </summary>
		[ObservableProperty]
		private bool contactSelected = false;

		/// <summary>
		/// If <see cref="To"/> is preset
		/// </summary>
		[ObservableProperty]
		private bool toPreset;

		/// <summary>
		/// Type of identity specified in <see cref="To"/>
		/// </summary>
		[ObservableProperty]
		private EntityType toType;

		/// <summary>
		/// Optional FriendlyName associated with URI
		/// </summary>
		[ObservableProperty]
		private string? friendlyName;

		/// <summary>
		/// If the URI is complete or not.
		/// </summary>
		[ObservableProperty]
		private bool complete;

		/// <summary>
		/// Message to recipient
		/// </summary>
		[ObservableProperty]
		private string? message;

		/// <summary>
		/// If <see cref="Message"/> should be encrypted in payment.
		/// </summary>
		[ObservableProperty]
		private bool encryptMessage;

		/// <summary>
		/// If <see cref="Message"/> should be encrypted in payment.
		/// </summary>
		[ObservableProperty]
		private bool canEncryptMessage;

		/// <summary>
		/// If a message is defined
		/// </summary>
		[ObservableProperty]
		private bool hasMessage;

		/// <summary>
		/// If <see cref="Message"/> is preset.
		/// </summary>
		[ObservableProperty]
		private bool messagePreset;

		/// <summary>
		/// If the URI is complete or not.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(PayOnlineCommand))]
		[NotifyCanExecuteChangedFor(nameof(GenerateQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(SendPaymentCommand))]
		private bool notPaid;

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		[ObservableProperty]
		private string? eDalerFrontGlyph;

		/// <summary>
		/// eDaler glyph URL
		/// </summary>
		[ObservableProperty]
		private string? eDalerBackGlyph;

		#endregion

		/// <summary>
		/// Command to bind to for detecting when a from label has been clicked on.
		/// </summary>
		[RelayCommand]
		private async Task FromClick()
		{
			try
			{
				string? Value = this.From;
				if (Value is null)
					return;

				if ((Value.StartsWith("http://", StringComparison.CurrentCultureIgnoreCase) ||
					Value.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase)) &&
					System.Uri.TryCreate(Value, UriKind.Absolute, out Uri? Uri) && await Launcher.TryOpenAsync(Uri))
				{
					return;
				}

				if (System.Uri.TryCreate("https://" + Value, UriKind.Absolute, out Uri) && await Launcher.TryOpenAsync(Uri))
					return;

				await Clipboard.SetTextAsync(Value);
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// The command to bind to for accepting the URI
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsConnected))]
		private async Task Accept()
		{
			try
			{
				if (this.Uri is null)
					return;

				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.AcceptEDalerUri, true))
					return;

				Transaction? Transaction = await ServiceRef.XmppService.SendEDalerUri(this.Uri);

				await this.GoBack();
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
					ServiceRef.Localizer[nameof(AppResources.TransactionAccepted)]);

			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.UnableToProcessEDalerUri)], Ex.Message);
			}
		}

		/// <summary>
		/// The command to bind to for declining the URI
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsConnected))]
		private async Task Decline()
		{
			await this.GoBack();
		}

		/// <summary>
		/// The command to bind to for paying online
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanPayOnline))]
		private async Task PayOnline()
		{
			try
			{
				if (!this.NotPaid)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.PaymentAlreadySent)]);
					return;
				}

				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.PayOnline, true))
					return;

				string Uri;

				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					try
					{
						LegalIdentity LegalIdentity = await ServiceRef.XmppService.GetLegalIdentity(this.To);
						Uri = await ServiceRef.XmppService.CreateFullEDalerPaymentUri(LegalIdentity, this.Amount ?? 0, this.AmountExtra,
							this.Currency ?? string.Empty, 3, this.Message ?? string.Empty);
					}
					catch (ForbiddenException)
					{
						// This happens if you try to view someone else's legal identity.
						// When this happens, try to send a petition to view it instead.
						// Normal operation. Should not be logged.

						this.NotPaid = true;

						MainThread.BeginInvokeOnMainThread(async () =>
						{
							bool Succeeded = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.PetitionIdentity(
								this.To, Guid.NewGuid().ToString(), ServiceRef.Localizer[nameof(AppResources.EncryptedPayment)]));

							if (Succeeded)
							{
								await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
									ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentForEncryption)]);
							}
						});

						return;
					}
				}
				else
				{
					Uri = await ServiceRef.XmppService.CreateFullEDalerPaymentUri(this.To!, this.Amount ?? 0, this.AmountExtra,
						this.Currency!, 3, this.Message ?? string.Empty);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				this.NotPaid = false;

				(bool Succeeded, Transaction? Transaction) = await ServiceRef.NetworkService.TryRequest(
					() => ServiceRef.XmppService.SendEDalerUri(Uri));

				if (Succeeded)
				{
					await this.GoBack();
					PaymentSuccessPopup Popup = new(Transaction!, this.Message);
					await ServiceRef.UiService.PushAsync(Popup);
				}
				else
				{
					this.NotPaid = true;
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnableToProcessEDalerUri)]);
				}
			}
			catch (Exception Ex)
			{
				this.NotPaid = true;
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// The command to bind to for generating a QR code
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanGenerateQrCode))]
		private async Task GenerateQrCode()
		{
			if (!this.NotPaid)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.PaymentAlreadySent)]);
				return;
			}

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.PayOffline, true))
				return;

			try
			{
				string Uri;
				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					LegalIdentity LegalIdentity = await ServiceRef.XmppService.GetLegalIdentity(this.To);
					Uri = await ServiceRef.XmppService.CreateFullEDalerPaymentUri(LegalIdentity, this.Amount ?? 0, this.AmountExtra,
						this.Currency ?? string.Empty, 3, this.Message ?? string.Empty);
				}
				else
				{
					Uri = await ServiceRef.XmppService.CreateFullEDalerPaymentUri(this.To!, this.Amount ?? 0, this.AmountExtra,
						this.Currency ?? string.Empty, 3, this.Message ?? string.Empty);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				if (this.IsAppearing)
				{
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						this.GenerateQrCode(Uri);

						if (this.shareQrCode is not null)
							await this.shareQrCode.ShowQrCode();
					});
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		private bool CanPayOnline => this.AmountOk && this.AmountExtraOk && !this.HasQrCode && this.IsConnected && this.NotPaid; // TODO: Add To field OK
		private bool CanGenerateQrCode => this.AmountOk && this.AmountExtraOk && !this.HasQrCode && this.NotPaid; // TODO: Add To field OK
		private bool CanShare => this.HasQrCode;

		/// <summary>
		/// The command to bind to for sharing the QR code
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanShare))]
		private async Task Share()
		{
			if (this.QrCodeBin is null)
				return;

			try
			{
				string? Message = this.Message ?? this.AmountAndCurrency;

				ServiceRef.PlatformSpecific.ShareImage(this.QrCodeBin,
					string.Format(CultureInfo.CurrentCulture, Message ?? string.Empty, this.Amount, this.Currency),
					ServiceRef.Localizer[nameof(AppResources.Share)], "RequestPayment.png");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// The command to bind to for resubmitting a payment.
		/// </summary>
		[RelayCommand(CanExecute = nameof(IsConnected))]
		private async Task Submit()
		{
			if (this.Uri is null)
				return;

			try
			{
				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.SubmitEDalerUri))
					return;

				(bool Succeeded, Transaction? Transaction) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.SendEDalerUri(this.Uri));
				if (Succeeded)
				{
					await this.GoBack();
					PaymentSuccessPopup Popup = new(Transaction!, this.Message);
					await ServiceRef.UiService.PushAsync(Popup);
				}
				else
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnableToProcessEDalerUri)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// The command to bind to for displaying eDaler URI as a QR Code
		/// </summary>
		[RelayCommand]
		private async Task ShowCode()
		{
			if (this.Uri is null)
				return;

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.ShowUriAsQr, true))
				return;

			try
			{
				if (this.IsAppearing)
				{
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						this.GenerateQrCode(this.Uri);

						if (this.shareQrCode is not null)
							await this.shareQrCode.ShowQrCode();
					});
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		private bool CanSendPayment()
		{
			return this.uriToSend is not null && this.AmountOk && this.AmountExtraOk && this.NotPaid;
		}

		/// <summary>
		/// The command to bind to send the payment via the parent page.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanSendPayment))]
		private async Task SendPayment()
		{
			if (!this.NotPaid)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.PaymentAlreadySent)]);
				return;
			}

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.SendPayment, true))
				return;

			try
			{
				string Uri;

				if (this.EncryptMessage && this.ToType == EntityType.LegalId)
				{
					LegalIdentity LegalIdentity = await ServiceRef.XmppService.GetLegalIdentity(this.To);
					Uri = await ServiceRef.XmppService.CreateFullEDalerPaymentUri(LegalIdentity, this.Amount ?? 0, this.AmountExtra,
						this.Currency!, 3, this.Message ?? string.Empty);
				}
				else if(this.ToType == EntityType.LegalId)
				{
					//TODO: Verify that JID is available
					LegalIdentity LegalIdentity = await ServiceRef.XmppService.GetLegalIdentity(this.To!);
					Uri = await ServiceRef.XmppService.CreateFullEDalerPaymentUri(LegalIdentity.GetJid(), this.Amount ?? 0, this.AmountExtra,
						this.Currency!, 3, this.Message ?? string.Empty);
				}
				else
				{
					Uri = await ServiceRef.XmppService.CreateFullEDalerPaymentUri(this.To!, this.Amount ?? 0, this.AmountExtra,
						this.Currency!, 3, this.Message ?? string.Empty);
				}

				// TODO: Validate To is a Bare JID or proper Legal Identity
				// TODO: Offline options: Expiry days

				this.uriToSend?.TrySetResult(Uri);
				this.messageToSend?.TrySetResult(this.Message);
				await this.GoBack();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Opens the calculator for calculating the value of a numerical property.
		/// </summary>
		/// <param name="Parameter">Property to calculate</param>
		[RelayCommand]
		public async Task OpenCalculator(object Parameter)
		{
			try
			{
				switch (Parameter?.ToString())
				{
					case "AmountText":
						CalculatorNavigationArgs AmountArgs = new(this, nameof(this.AmountText));

						await ServiceRef.UiService.GoToAsync(nameof(CalculatorPage), AmountArgs, BackMethod.Pop);
						break;

					case "AmountExtraText":
						CalculatorNavigationArgs ExtraArgs = new(this, nameof(this.AmountExtraText));

						await ServiceRef.UiService.GoToAsync(nameof(CalculatorPage), ExtraArgs, BackMethod.Pop);
						break;
				}
			}
			catch (Exception Ex)
			{
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(ServiceRef.Localizer[nameof(AppResources.Payment)]);

		#endregion

		[RelayCommand]
		private async Task SelectRecipient()
		{
			try
			{
				TaskCompletionSource<ContactInfoModel?> Selected = new();
				string Description = ServiceRef.Localizer[nameof(AppResources.SelectFromWhomToRequestPayment)];
				ContactListNavigationArgs ContactListArgs = new(Description, Selected)
				{
					CanScanQrCode = true
				};

				await ServiceRef.UiService.GoToAsync(nameof(MyContactsPage), ContactListArgs, BackMethod.Pop);

				ContactInfoModel? Contact = await Selected.Task;
				if (Contact is null)
					return;

				this.ToContact = Contact;
				this.ContactSelected = true;

				if (!string.IsNullOrEmpty(Contact.LegalId))
				{
					this.To = Contact.LegalId;
					this.ToType = EntityType.LegalId;
				}
				else if (!string.IsNullOrEmpty(Contact.BareJid))
				{
					this.To = Contact.BareJid;
					this.ToType = EntityType.NetworkJid; // Using Network to represent a bare JID
				}
				else
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkAddressOfContactUnknown)]);
					return;
				}

				this.ToPreset = false;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		[RelayCommand]
		private Task ClearRecipient()
		{
			this.To = string.Empty;
			this.ToContact = null;
			this.ContactSelected = false;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Loads the current balance from the backend and updates observable properties.
		/// </summary>
		/// <param name="Ctx">Task context</param>
		private async Task LoadBalanceAsync(TaskContext<bool> Ctx)
		{
			ServiceRef.LogService.LogDebug("Refreshing Edaler...");

			if (!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect))
				return;

			Balance CurrentBalance = await ServiceRef.XmppService.GetEDalerBalance();

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.FetchedBalance = CurrentBalance;
				this.BalanceUpdated = DateTime.UtcNow;
				ServiceRef.TagProfile.LastEDalerBalanceDecimal = this.BalanceDecimal;
				ServiceRef.TagProfile.LastEDalerBalanceUpdate = DateTime.UtcNow;
			});

			ServiceRef.LogService.LogDebug("Refreshing Edaler Completed");
		}

		[RelayCommand]
		private async Task ScanQr()
		{
			try
			{
				string[] AllowedSchemas = [Constants.UriSchemes.IotId];
				string? Url = await Services.UI.QR.QrCode.ScanQrCode(ServiceRef.Localizer[nameof(AppResources.QrScanCode)], AllowedSchemas);
				if (string.IsNullOrEmpty(Url))
					return;

				string? NeuroId = null;

				// Accept formats:
				// 1. iotid:LegalIdentityId
				// 2. Raw LegalIdentityId (no scheme)
				if (Url.StartsWith(Constants.UriSchemes.IotId + ":", StringComparison.OrdinalIgnoreCase))
				{
					int i = Url.IndexOf(':');
					NeuroId = Url[(i + 1)..].Trim();
				}
				else
					NeuroId = Url.Trim();

				if (string.IsNullOrEmpty(NeuroId))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.CodeNotRecognized)]);
					return;
				}

				// Set recipient as Legal ID initially
				this.To = NeuroId;
				this.ToType = EntityType.LegalId;
				this.ToPreset = false;
				this.ToContact = null;
				this.ContactSelected = false;

				// Petition identity to obtain JID (asynchronous response via event)
				try
				{
					await ServiceRef.NetworkService.TryRequest(() =>
						ServiceRef.XmppService.PetitionIdentity(NeuroId, Guid.NewGuid().ToString(), ServiceRef.Localizer[nameof(AppResources.Payment)])
					);
				}
				catch (Exception Ex2)
				{
					ServiceRef.LogService.LogException(Ex2);
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnableToOpenLink)]);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

	}
}
