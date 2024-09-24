using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups;
using Waher.Content.Xml;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using System.Globalization;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class ContactSupportViewModel() : BaseRegistrationViewModel(RegistrationStep.ContactSupport), ICodeVerification
	{

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;

			if (App.Current is not null)
			{
				this.CountDownTimer = App.Current.Dispatcher.CreateTimer();
				this.CountDownTimer.Interval = TimeSpan.FromMilliseconds(1000);
				this.CountDownTimer.Tick += this.CountDownEventHandler;
			}

			try
			{
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
					new KeyValuePair<string, string>("Accept", "application/json"));

				if ((Result is Dictionary<string, object> Response) &&
					 Response.TryGetValue("CountryCode", out object? cc) &&
					 (cc is string CountryCode))
				{
					if (ISO_3166_1.TryGetCountryByCode(CountryCode, out ISO_3166_Country? Country))
						this.SelectedCountry = Country;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		public void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedSendCodeText));
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.PropertyName == nameof(this.IsBusy))
				this.SendCodeCommand.NotifyCanExecuteChanged();
		}


		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		[NotifyPropertyChangedFor(nameof(EmailValidationError))]
		private bool emailIsValid;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		[NotifyPropertyChangedFor(nameof(PhoneValidationError))]
		private bool phoneIsValid;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendCode))]
		private string emailText = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanSendCode))]
		private string phoneText = string.Empty;

		public string EmailValidationError => !this.EmailIsValid ? ServiceRef.Localizer[nameof(AppResources.EmailValidationFormat)] : string.Empty;

		public string PhoneValidationError => !this.PhoneIsValid ? ServiceRef.Localizer[nameof(AppResources.PhoneValidationDigits)] : string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(LocalizedSendCodeText))]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(ResendCodeCommand))]
		private int countDownSeconds;

		[ObservableProperty]
		private IDispatcherTimer? countDownTimer;

		public bool CanSendCode => this.EmailIsValid && this.PhoneIsValid && 
									!this.IsBusy && 
									(this.CountDownSeconds <= 0) &&
									!string.IsNullOrEmpty(this.EmailText) && !string.IsNullOrEmpty(this.PhoneText);

		[ObservableProperty]
		ISO_3166_Country selectedCountry = ISO_3166_1.DefaultCountry;


		[ObservableProperty]
		private string? localizedPhoneValidationError;
		public string LocalizedSendCodeText
		{
			get
			{
				if (this.CountDownSeconds > 0)
					return ServiceRef.Localizer[nameof(AppResources.SendCodeSeconds), this.CountDownSeconds];

				return ServiceRef.Localizer[nameof(AppResources.SendCode)];
			}
		}



		public bool CanResendCode => this.CountDownSeconds <= 0;
		[RelayCommand]
		private async Task SelectPhoneCode()
		{
			SelectPhoneCodePopup Page = new();
			await MopupService.Instance.PushAsync(Page);

			ISO_3166_Country? Result = await Page.Result;

			if (Result is not null)
				this.SelectedCountry = Result;

			return;
		}

		[RelayCommand(CanExecute = nameof(CanSendCode))]
		private async Task SendCode()
		{
			IsBusy = true;
			try
			{
				if (!ServiceRef.NetworkService.IsOnline)
				{
					await ServiceRef.UiService.DisplayAlert(
						 ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						 ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)]);
					return;
				}

				string fullPhoneNumber = $"+{SelectedCountry.DialCode}{PhoneText}";

				if (SelectedCountry.DialCode == "46") // Adjust for Swedish numbers
					fullPhoneNumber = $"+{SelectedCountry.DialCode}{PhoneText.TrimStart('0')}";

				// Send email verification code
				object emailSendResult = await InternetContent.PostAsync(
					 new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					 new Dictionary<string, object>
					 {
					 { "EMail", EmailText },
					 { "AppName", Constants.Application.Name },
					 { "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					 }, new KeyValuePair<string, string>("Accept", "application/json"));

				// Send phone verification code
				object phoneSendResult = await InternetContent.PostAsync(
					 new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					 new Dictionary<string, object>
					 {
					 { "Nr", fullPhoneNumber },
					 { "AppName", Constants.Application.Name },
					 { "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					 }, new KeyValuePair<string, string>("Accept", "application/json"));

				// Check if both codes were sent successfully
				bool emailSent = emailSendResult is Dictionary<string, object> emailResponse &&
									  emailResponse.TryGetValue("Status", out var emailObj) &&
									  emailObj is bool emailStatus && emailStatus;

				bool phoneSent = phoneSendResult is Dictionary<string, object> phoneResponse &&
									  phoneResponse.TryGetValue("Status", out var phoneObj) &&
									  phoneObj is bool phoneStatus && phoneStatus;

				if (emailSent && phoneSent)
				{
					StartTimer();

					// Navigate to VerifyCodePage for email code
					if (!await VerifyCodeAsync(EmailText, isEmail: true))
					{
						return;
					}

					// Navigate to VerifyCodePage for phone code
					if (!await VerifyCodeAsync(fullPhoneNumber, isEmail: false))
					{
						return;
					}

					// Both codes verified successfully
					// Perform your logic here
					//	await PerformPostVerificationLogic();
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(
					 ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
					 ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			finally
			{
				IsBusy = false;
			}
		}

		[RelayCommand(CanExecute = nameof(CanResendCode))]
		private async Task ResendCode()
		{

		}

		private async Task<bool> VerifyCodeAsync(string identifier, bool isEmail)
		{
			VerifyCodeNavigationArgs navigationArgs = new(this, identifier);
			await ServiceRef.UiService.GoToAsync(nameof(VerifyCodePage), navigationArgs, BackMethod.Pop);
			string? code = await navigationArgs.VarifyCode!.Task;

			if (!string.IsNullOrEmpty(code))
			{
				var parameters = new Dictionary<string, object>
				{
					{ isEmail ? "EMail" : "Nr", identifier },
					{ "Code", int.Parse(code, NumberStyles.None, CultureInfo.InvariantCulture) }
				};

				object verifyResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					parameters, new KeyValuePair<string, string>("Accept", "application/json"));

				bool verified = verifyResult is Dictionary<string, object> verifyResponse &&
									 verifyResponse.TryGetValue("Status", out var obj) &&
									 obj is bool status && status;

				return verified;
			}

			return false;
		}


		private void StartTimer()
		{
			if (this.CountDownTimer is not null)
			{
				this.CountDownSeconds = 300;

				if (!this.CountDownTimer.IsRunning)
					this.CountDownTimer.Start();
			}
		}

		private void CountDownEventHandler(object? sender, EventArgs e)
		{
			if (this.CountDownTimer is not null)
			{
				if (this.CountDownSeconds > 0)
					this.CountDownSeconds--;
				else
					this.CountDownTimer.Stop();
			}
		}

	}
}
