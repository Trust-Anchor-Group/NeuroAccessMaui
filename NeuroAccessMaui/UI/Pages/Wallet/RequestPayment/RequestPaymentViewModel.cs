using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Main.Calculator;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Waher.Content;
using Waher.Script.Functions.ComplexNumbers;

namespace NeuroAccessMaui.UI.Pages.Wallet.RequestPayment
{
	/// <summary>
	/// The view model to bind to for requesting a payment.
	/// </summary>
	public partial class RequestPaymentViewModel(RequestPaymentPage page) : QrXmppViewModel()
	{
		private readonly RequestPaymentPage page = page;

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.UiService.TryGetArgs(out EDalerBalanceNavigationArgs? Args))
				this.Currency = Args.Balance?.Currency;

			this.Amount = 0;
			this.AmountText = string.Empty;
			this.AmountOk = false;

			this.AmountExtra = 0;
			this.AmountExtraText = string.Empty;
			this.AmountExtraOk = false;

			this.RemoveQrCode();
		}

		#region Properties

		/// <summary>
		/// Amount of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal amount;

		/// <summary>
		/// If <see cref="Amount"/> is OK.
		/// </summary>
		[ObservableProperty]
		private bool amountOk;

		/// <summary>
		/// <see cref="Amount"/> as text.
		/// </summary>
		[ObservableProperty]
		private string? amountText;

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.AmountText):
					if (CommonTypes.TryParse(this.AmountText, out decimal d) && d > 0)
					{
						this.Amount = d;
						this.AmountOk = true;
					}
					else
						this.AmountOk = false;
					break;

				case nameof(this.AmountExtraText):
					if (string.IsNullOrEmpty(this.AmountExtraText))
					{
						this.AmountExtra = null;
						this.AmountExtraOk = true;
					}
					else if (CommonTypes.TryParse(this.AmountExtraText, out d) && d >= 0)
					{
						this.AmountExtra = d;
						this.AmountExtraOk = true;
					}
					else
						this.AmountExtraOk = false;
					break;
			}
		}

		/// <summary>
		/// AmountExtra of eDaler to process
		/// </summary>
		[ObservableProperty]
		private decimal? amountExtra;

		/// <summary>
		/// If <see cref="AmountExtra"/> is OK.
		/// </summary>
		[ObservableProperty]
		private bool amountExtraOk;

		/// <summary>
		/// <see cref="AmountExtra"/> as text.
		/// </summary>
		[ObservableProperty]
		private string? amountExtraText;

		/// <summary>
		/// Currency of <see cref="Amount"/>.
		/// </summary>
		[ObservableProperty]
		private string? currency;

		/// <summary>
		/// Message to embed in payment.
		/// </summary>
		[ObservableProperty]
		private string? message;

		/// <summary>
		/// If <see cref="Message"/> should be encrypted in payment.
		/// </summary>
		[ObservableProperty]
		private bool encryptMessage;

		#endregion

		/// <summary>
		/// The command to bind to for generating a QR code
		/// </summary>
		[RelayCommand(CanExecute = nameof(AmountOk))]
		private Task GenerateQrCode()
		{
			string Uri;

			if (this.EncryptMessage && ServiceRef.TagProfile.LegalIdentity is not null)
			{
				Uri = ServiceRef.XmppService.CreateIncompleteEDalerPayMeUri(ServiceRef.TagProfile.LegalIdentity, this.Amount, this.AmountExtra,
					this.Currency ?? string.Empty, this.Message ?? string.Empty);
			}
			else
			{
				Uri = ServiceRef.XmppService.CreateIncompleteEDalerPayMeUri(ServiceRef.XmppService.BareJid, this.Amount, this.AmountExtra,
					this.Currency ?? string.Empty, this.Message ?? string.Empty);
			}

			if (this.IsAppearing)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					this.GenerateQrCode(Uri);

					await this.page.ShowQrCode();
				});
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// The command to bind to for sharing the QR code with a contact
		/// </summary>
		[RelayCommand(CanExecute = nameof(HasQrCode))]
		private async Task ShareContact()
		{
			try
			{
				TaskCompletionSource<ContactInfoModel?> Selected = new();
				ContactListNavigationArgs ContactListArgs = new(ServiceRef.Localizer[nameof(AppResources.SelectFromWhomToRequestPayment)], Selected)
				{
					CanScanQrCode = true
				};

				await ServiceRef.UiService.GoToAsync(nameof(MyContactsPage), ContactListArgs, BackMethod.Pop);

				ContactInfoModel? Contact = await Selected.Task;
				if (Contact is null)
					return;

				if (string.IsNullOrEmpty(Contact.BareJid))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkAddressOfContactUnknown)]);
					return;
				}

				StringBuilder Markdown = new();

				Markdown.Append("![");
				Markdown.Append(ServiceRef.Localizer[nameof(AppResources.RequestPayment)]);
				Markdown.Append("](");
				Markdown.Append(this.QrCodeUri);
				Markdown.Append(')');

				await ChatViewModel.ExecuteSendMessage(string.Empty, Markdown.ToString(), Contact.BareJid);

				if (Contact.Contact is not null)
				{
					await Task.Delay(100);  // Otherwise, page doesn't show properly. (Underlying timing issue. TODO: Find better solution.)

					ChatNavigationArgs ChatArgs = new(Contact.Contact);

					if (OperatingSystem.IsIOS())
						await ServiceRef.UiService.GoToAsync(nameof(ChatPageIos), ChatArgs, BackMethod.Inherited, Contact.BareJid);
					else
						await ServiceRef.UiService.GoToAsync(nameof(ChatPage), ChatArgs, BackMethod.Inherited, Contact.BareJid);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// The command to bind to for sharing the QR code with external applications
		/// </summary>
		[RelayCommand(CanExecute = nameof(HasQrCode))]
		private async Task ShareExternal()
		{
			if (this.QrCodeBin is null)
				return;

			try
			{
				string? Message = this.Message;
				if (string.IsNullOrEmpty(Message))
					Message = ServiceRef.Localizer[nameof(AppResources.RequestPaymentMessage)];

				ServiceRef.PlatformSpecific.ShareImage(this.QrCodeBin, string.Format(CultureInfo.CurrentCulture, Message, this.Amount, this.Currency),
					ServiceRef.Localizer[nameof(AppResources.Share)], "RequestPayment.png");
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
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
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(ServiceRef.Localizer[nameof(AppResources.RequestPayment)]);

		#endregion


	}
}
