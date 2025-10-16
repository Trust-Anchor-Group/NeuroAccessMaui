using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using NeuroAccessMaui.UI.Popups;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Onboarding step ViewModel for validating the phone number. Mirrors the registration experience but defers validation until the user presses Send.
	/// </summary>
	public partial class ValidatePhoneOnboardingStepViewModel : BaseOnboardingStepViewModel, ICodeVerification
	{
		private bool codeVerified;
		private bool hasValidated;

		/// <summary>
		/// Creates a new instance of the <see cref="ValidatePhoneOnboardingStepViewModel"/> class.
		/// </summary>
		public ValidatePhoneOnboardingStepViewModel() : base(OnboardingStep.ValidatePhone)
		{
		}

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;
			this.NumberIsValid = true;
			this.TypeIsValid = true;
			this.LengthIsValid = true;

			if (App.Current is not null)
			{
				this.CountDownTimer = App.Current.Dispatcher.CreateTimer();
				this.CountDownTimer.Interval = TimeSpan.FromMilliseconds(1000);
				this.CountDownTimer.Tick += this.CountDownEventHandler;
			}

			string? ExistingNumber = ServiceRef.TagProfile.PhoneNumber;
			if (string.IsNullOrEmpty(ExistingNumber))
			{
				try
				{
					ContentResponse Result = await InternetContent.PostAsync(
						new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"),
						string.Empty,
						null,
						App.ValidateCertificateCallback,
						new KeyValuePair<string, string>("Accept", "application/json"));
					Result.AssertOk();

					if ((Result.Decoded is Dictionary<string, object> Response) &&
						Response.TryGetValue("CountryCode", out object? CcObj) &&
						CcObj is string CountryCodeStr &&
						ISO_3166_1.TryGetCountryByCode(CountryCodeStr, out ISO_3166_Country? Country))
					{
						this.SelectedCountry = Country;
					}
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}
			else
			{
				this.ApplyStoredPhoneNumber(ExistingNumber);
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;

			if (this.CountDownTimer is not null)
			{
				this.CountDownTimer.Stop();
				this.CountDownTimer.Tick -= this.CountDownEventHandler;
				this.CountDownTimer = null;
			}

			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Handles localization manager property changes to refresh localized strings.
		/// </summary>
		public void LocalizationManagerEventHandler(object? Sender, PropertyChangedEventArgs E)
		{
			this.OnPropertyChanged(nameof(this.Title));
			this.OnPropertyChanged(nameof(this.Description));
			this.OnPropertyChanged(nameof(this.LocalizedSendCodeText));
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
			this.OnPropertyChanged(nameof(this.ShowValidationError));
		}

		#region Observable Properties

		/// <summary>
		/// Gets or sets the currently selected country.
		/// </summary>
		[ObservableProperty]
		private ISO_3166_Country selectedCountry = ISO_3166_1.DefaultCountry;

		/// <summary>
		/// Flag set when the number (digits &amp; length) is valid after validation.
		/// </summary>
		[ObservableProperty]
		private bool numberIsValid;

		/// <summary>
		/// Flag set when only digits are present after validation.
		/// </summary>
		[ObservableProperty]
		private bool typeIsValid;

		/// <summary>
		/// Flag set when length is valid after validation.
		/// </summary>
		[ObservableProperty]
		private bool lengthIsValid;

		/// <summary>
		/// Gets or sets the remaining countdown seconds before resend is allowed.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(LocalizedSendCodeText))]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(ResendCodeCommand))]
		private int countDownSeconds;

		/// <summary>
		/// Gets or sets the countdown timer instance.
		/// </summary>
		[ObservableProperty]
		private IDispatcherTimer? countDownTimer;

		/// <summary>
		/// Raw phone number (without country code). Validation happens only when Send is pressed.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		private string phoneNumber = string.Empty;

		#endregion

		#region Localization Helpers

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingPhonePageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingPhonePageDetails)];

		public string LocalizedSendCodeText
		{
			get
			{
				if (this.CountDownSeconds > 0)
					return ServiceRef.Localizer[nameof(AppResources.SendCodeSeconds), this.CountDownSeconds];
				return ServiceRef.Localizer[nameof(AppResources.SendCode)];
			}
		}

		/// <summary>
		/// Localized validation error text (blank until user has validated and an error exists).
		/// </summary>
		public string LocalizedValidationError
		{
			get
			{
				if (!this.ShowValidationError)
					return string.Empty;
				if (!this.TypeIsValid)
					return ServiceRef.Localizer[nameof(AppResources.PhoneValidationDigits)];
				if (!this.LengthIsValid)
					return ServiceRef.Localizer[nameof(AppResources.PhoneValidationLength)];
				return string.Empty;
			}
		}

		/// <summary>
		/// True if a validation error should be displayed.
		/// </summary>
		public bool ShowValidationError => this.hasValidated && !this.NumberIsValid;

		#endregion

		#region State & Derived

		/// <summary>
		/// Button enabled if user has entered something (no pre-validation) and timer allows.
		/// </summary>
		public bool CanSendCode => this.PhoneNumber.Length > 0 && this.CountDownSeconds <= 0 && !this.IsBusy;
		public bool CanResendCode => this.CountDownSeconds <= 0;
		public bool CanContinue => this.codeVerified;

		#endregion

		private void ApplyStoredPhoneNumber(string StoredNumber)
		{
			string Trimmed = StoredNumber.Trim();
			ISO_3166_Country? ResolvedCountry = null;

			if (ISO_3166_1.TryGetCountryByCode(ServiceRef.TagProfile.SelectedCountry, out ISO_3166_Country? CountryFromProfile))
			{
				ResolvedCountry = CountryFromProfile;
			}
			else if (ISO_3166_1.TryGetCountryByPhone(Trimmed, out ISO_3166_Country? CountryFromNumber))
			{
				ResolvedCountry = CountryFromNumber;
			}

			if (ResolvedCountry is not null)
			{
				this.SelectedCountry = ResolvedCountry;
				string Digits = Trimmed;
				if (Digits.StartsWith("+", StringComparison.Ordinal))
				{
					Digits = Digits[1..];
				}

				if (Digits.StartsWith(ResolvedCountry.DialCode, StringComparison.Ordinal))
				{
					Digits = Digits[ResolvedCountry.DialCode.Length..];
				}

				this.PhoneNumber = Digits;
			}
			else
			{
			this.PhoneNumber = Trimmed.TrimStart('+');
		}

			this.NumberIsValid = true;
			this.TypeIsValid = true;
			this.LengthIsValid = true;

			this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		internal override Task OnActivatedAsync()
		{
			if (string.IsNullOrEmpty(this.PhoneNumber) && !string.IsNullOrEmpty(ServiceRef.TagProfile.PhoneNumber))
			{
				this.ApplyStoredPhoneNumber(ServiceRef.TagProfile.PhoneNumber);
			}

			return base.OnActivatedAsync();
		}

		#region Commands

		[RelayCommand]
		private async Task SelectPhoneCode()
		{
			SelectPhoneCodePopup Popup = new();
			await ServiceRef.PopupService.PushAsync(Popup);
			ISO_3166_Country? Result = await Popup.Result;
			if (Result is not null)
				this.SelectedCountry = Result;
		}

		[RelayCommand(CanExecute = nameof(CanSendCode))]
		private async Task SendCode()
		{
			// Perform validation now (only when user presses Send)
			this.hasValidated = true;
			bool TypeValid = this.PhoneNumber.All(char.IsDigit);
			bool LengthValid = this.PhoneNumber.Length >= 4; // minimum length requirement
			this.TypeIsValid = TypeValid;
			this.LengthIsValid = LengthValid;
			this.NumberIsValid = TypeValid && LengthValid;
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
			this.OnPropertyChanged(nameof(this.ShowValidationError));

			if (!this.NumberIsValid)
			{
				return; // Do not proceed if invalid
			}

			this.IsBusy = true;
			try
			{
				if (!ServiceRef.NetworkService.IsOnline)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)]);
					return;
				}

				string FullPhoneNumber = "+" + this.SelectedCountry.DialCode + this.PhoneNumber;
				if (this.SelectedCountry.DialCode == "46")
					FullPhoneNumber = "+" + this.SelectedCountry.DialCode + this.PhoneNumber.TrimStart('0');

				ContentResponse SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", FullPhoneNumber },
						{ "AppName", Constants.Application.Name },
						{ "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					},
					null,
					App.ValidateCertificateCallback,
					new KeyValuePair<string, string>("Accept", "application/json"));
				SendResult.AssertOk();

				if (SendResult.Decoded is Dictionary<string, object> SendResponse &&
					SendResponse.TryGetValue("Status", out object? Obj) && Obj is bool SendStatus && SendStatus &&
					SendResponse.TryGetValue("IsTemporary", out Obj) && Obj is bool SendIsTemporary)
				{
					if (!string.IsNullOrEmpty(ServiceRef.TagProfile.PhoneNumber) && (ServiceRef.TagProfile.TestOtpTimestamp is null) && SendIsTemporary)
					{
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
							ServiceRef.Localizer[nameof(AppResources.SwitchingToTestPhoneNumberNotAllowed)]);
					}
					else
					{
						this.StartTimer();
						VerifyCodeNavigationArgs NavigationArgs = new(this, FullPhoneNumber);
						await ServiceRef.NavigationService.GoToAsync(nameof(VerifyCodePage), NavigationArgs, BackMethod.Pop);
						string? Code = await NavigationArgs.VarifyCode!.Task;
						if (!string.IsNullOrEmpty(Code))
						{
							await this.VerifyCodeAsync(Code, FullPhoneNumber).ConfigureAwait(false);
						}
					}
				}
				else
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		[RelayCommand(CanExecute = nameof(CanResendCode))]
		private async Task ResendCode()
		{
			try
			{
				if (!ServiceRef.NetworkService.IsOnline)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.NetworkSeemsToBeMissing)]);
					return;
				}
				string FullPhoneNumber = "+" + this.SelectedCountry.DialCode + this.PhoneNumber;
				if (this.SelectedCountry.DialCode == "46")
					FullPhoneNumber = "+" + this.SelectedCountry.DialCode + this.PhoneNumber.TrimStart('0');

				ContentResponse SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", FullPhoneNumber },
						{ "AppName", Constants.Application.Name },
						{ "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					},
					null,
					App.ValidateCertificateCallback,
					new KeyValuePair<string, string>("Accept", "application/json"));
				SendResult.AssertOk();

				if (SendResult.Decoded is Dictionary<string, object> SendResponse &&
					SendResponse.TryGetValue("Status", out object? Obj) &&
					Obj is bool SendStatus && SendStatus)
				{
					this.StartTimer();
				}
				else
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrongWhenSendingPhoneCode)]);
			}
		}

		#endregion

		#region Verification Logic

		private async Task VerifyCodeAsync(string code, string fullPhoneNumber)
		{
			try
			{
				PurposeUse Purpose = ServiceRef.TagProfile.Purpose;
				bool IsTest = Purpose == PurposeUse.Educational || Purpose == PurposeUse.Experimental;

				ContentResponse VerifyResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", fullPhoneNumber },
						{ "Code", int.Parse(code, NumberStyles.None, CultureInfo.InvariantCulture) },
						{ "Test", IsTest }
					},
					null,
					App.ValidateCertificateCallback,
					new KeyValuePair<string, string>("Accept", "application/json"));
				VerifyResult.AssertOk();

				if (VerifyResult.Decoded is Dictionary<string, object> VerifyResponse &&
					VerifyResponse.TryGetValue("Status", out object? Obj) && Obj is bool VerifyStatus && VerifyStatus &&
					VerifyResponse.TryGetValue("Domain", out Obj) && Obj is string VerifyDomain &&
					VerifyResponse.TryGetValue("Key", out Obj) && Obj is string VerifyKey &&
					VerifyResponse.TryGetValue("Secret", out Obj) && Obj is string VerifySecret &&
					VerifyResponse.TryGetValue("Temporary", out Obj) && Obj is bool VerifyIsTemporary)
				{
					ServiceRef.TagProfile.SetPhone(this.SelectedCountry.Alpha2, fullPhoneNumber);
					ServiceRef.TagProfile.SetPurpose(IsTest, Purpose);
					ServiceRef.TagProfile.TestOtpTimestamp = VerifyIsTemporary ? DateTime.Now : null;

					if (string.IsNullOrEmpty(ServiceRef.TagProfile.Domain))
					{
						bool DefaultConnectivity;
						try
						{
							(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(VerifyDomain);
							DefaultConnectivity = HostName == VerifyDomain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
						}
						catch (Exception)
						{
							DefaultConnectivity = false;
						}
						ServiceRef.TagProfile.SetDomain(VerifyDomain, DefaultConnectivity, VerifyKey, VerifySecret);
					}

					this.codeVerified = true;
					this.OnPropertyChanged(nameof(this.CanContinue));

					if (this.CoordinatorViewModel is not null)
					{
						OnboardingStep Next = VerifyIsTemporary ? OnboardingStep.CreateAccount : OnboardingStep.ValidateEmail;
						await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(Next);
					}
				}
				else
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnableToVerifyCode)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.UnableToVerifyCode)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		#endregion

		#region Timer

		private void StartTimer()
		{
			if (this.CountDownTimer is not null)
			{
#if DEBUG
				this.CountDownSeconds = 10;
#else
				this.CountDownSeconds = 300;
#endif
				if (!this.CountDownTimer.IsRunning)
					this.CountDownTimer.Start();
			}
		}

		private void CountDownEventHandler(object? Sender, EventArgs E)
		{
			if (this.CountDownTimer is not null)
			{
				if (this.CountDownSeconds > 0)
					this.CountDownSeconds--;
				else
					this.CountDownTimer.Stop();
			}
		}

		#endregion
	}
}
