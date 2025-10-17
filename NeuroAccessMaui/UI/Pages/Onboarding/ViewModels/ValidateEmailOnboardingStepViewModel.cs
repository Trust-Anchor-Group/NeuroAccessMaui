using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Main.VerifyCode;
using Waher.Content;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Onboarding step ViewModel for validating e-mail. Updated to mirror registration validation (continuous validation, symbol toggling).
	/// </summary>
	public partial class ValidateEmailOnboardingStepViewModel : BaseOnboardingStepViewModel, ICodeVerification
	{
		private static readonly Regex EmailRegex = new Regex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private bool codeVerified;

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidateEmailOnboardingStepViewModel"/> class.
		/// </summary>
		public ValidateEmailOnboardingStepViewModel() : base(OnboardingStep.ValidateEmail) { }

		#region Lifecycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;

			if (App.Current is not null)
			{
				this.CountDownTimer = App.Current.Dispatcher.CreateTimer();
				this.CountDownTimer.Interval = TimeSpan.FromSeconds(1);
				this.CountDownTimer.Tick += this.CountDownEventHandler;
			}

			if (!string.IsNullOrEmpty(ServiceRef.TagProfile.EMail))
			{
				this.EmailText = ServiceRef.TagProfile.EMail;
				this.EmailIsValid = EmailRegex.IsMatch(this.EmailText);
				this.SendCodeCommand.NotifyCanExecuteChanged();
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

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.PropertyName == nameof(this.IsBusy))
				this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		private void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedSendCodeText));
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
			this.OnPropertyChanged(nameof(this.ShowValidationError));
		}

		#endregion

		#region Observable Properties

		/// <summary>
		/// Backing field indicating if email format is valid.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		[NotifyPropertyChangedFor(nameof(ShowValidationError))]
		private bool emailIsValid = true;

		/// <summary>
		/// Countdown seconds for enabling resend.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(LocalizedSendCodeText))]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(ResendCodeCommand))]
		private int countDownSeconds;

		/// <summary>
		/// Countdown timer instance.
		/// </summary>
		[ObservableProperty]
		private IDispatcherTimer? countDownTimer;

		/// <summary>
		/// E-mail text.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowValidationError))]
		[NotifyPropertyChangedFor(nameof(LocalizedValidationError))]
		[NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
		private string emailText = string.Empty;

		#endregion

		#region Localization & Validation

		/// <summary>
		/// Localized send code button text (with countdown state).
		/// </summary>
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
		/// Localized validation error text for email.
		/// </summary>
		public string LocalizedValidationError
		{
			get
			{
				if (!this.EmailIsValid && this.EmailText.Length > 0)
					return ServiceRef.Localizer[nameof(AppResources.EmailValidationFormat)];
				return string.Empty;
			}
		}

		/// <summary>
		/// If validation error should be shown.
		/// </summary>
		public bool ShowValidationError => !this.EmailIsValid && this.EmailText.Length > 0;

		#endregion

		#region Computed State

		/// <summary>
		/// Can send a verification code.
		/// </summary>
		public bool CanSendCode => this.EmailIsValid && !this.IsBusy && this.EmailText.Length > 0 && this.CountDownSeconds <= 0;

		/// <summary>
		/// Can resend verification code (timer complete).
		/// </summary>
		public bool CanResendCode => this.CountDownSeconds <= 0;

		/// <summary>
		/// Can continue to next onboarding step (code verified).
		/// </summary>
		public bool CanContinue => this.codeVerified;

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingEmailPageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.OnboardingEmailPageDetails)];

		#endregion

		#region Property Change Hooks

		partial void OnEmailTextChanged(string value)
		{
			bool WasValid = this.EmailIsValid;
			bool IsValid = string.IsNullOrWhiteSpace(value) || EmailRegex.IsMatch(value);
			this.EmailIsValid = IsValid;
			if (WasValid != IsValid)
			{
				this.OnPropertyChanged(nameof(this.LocalizedValidationError));
				this.OnPropertyChanged(nameof(this.ShowValidationError));
			}
			this.SendCodeCommand.NotifyCanExecuteChanged();
		}

		#endregion

		#region Commands

		/// <summary>
		/// Sends a verification code to the entered e-mail address.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanSendCode))]
		private async Task SendCode()
		{
			if (!this.EmailIsValid)
				return;

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

				ContentResponse SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EmailText },
						{ "AppName", Constants.Application.Name },
						{ "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					},
					null,
					App.ValidateCertificateCallback,
					new KeyValuePair<string, string>("Accept", "application/json"));
				SendResult.AssertOk();

				if (SendResult.Decoded is Dictionary<string, object> SendResponse &&
					SendResponse.TryGetValue("Status", out object? Obj) && Obj is bool SendStatus && SendStatus)
				{
					this.StartTimer();
					VerifyCodeNavigationArgs NavigationArgs = new(this, this.EmailText);
					await ServiceRef.NavigationService.GoToAsync(nameof(VerifyCodePage), NavigationArgs, BackMethod.Pop);
					string? Code = await NavigationArgs.VarifyCode!.Task;
					if (!string.IsNullOrEmpty(Code))
					{
						await this.VerifyCodeAsync(Code).ConfigureAwait(false);
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
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		/// <summary>
		/// Resends the verification code (if countdown complete).
		/// </summary>
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

				ContentResponse SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EmailText },
						{ "AppName", Constants.Application.Name },
						{ "Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
					},
					null,
					App.ValidateCertificateCallback,
					new KeyValuePair<string, string>("Accept", "application/json"));
				SendResult.AssertOk();

				if (SendResult.Decoded is Dictionary<string, object> SendResponse &&
					SendResponse.TryGetValue("Status", out object? Obj) && Obj is bool SendStatus && SendStatus)
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
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], Ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		#endregion

		#region Verification

		private async Task VerifyCodeAsync(string code)
		{
			try
			{
				this.IsBusy = true;
				this.CoordinatorViewModel?.SetIsBusy(true);
				ContentResponse VerifyResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EmailText },
						{ "Code", int.Parse(code, NumberStyles.None, CultureInfo.InvariantCulture) }
					},
					null,
					App.ValidateCertificateCallback,
					new KeyValuePair<string, string>("Accept", "application/json"));
				VerifyResult.AssertOk();

				if (VerifyResult.Decoded is Dictionary<string, object> VerifyResponse &&
					VerifyResponse.TryGetValue("Status", out object? Obj) && Obj is bool VerifyStatus && VerifyStatus)
				{
					ServiceRef.TagProfile.EMail = this.EmailText;
					this.codeVerified = true;
					this.OnPropertyChanged(nameof(this.CanContinue));

					if (this.CoordinatorViewModel is not null)
					{
						await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.NameEntry);
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
			finally
			{
				this.CoordinatorViewModel?.SetIsBusy(false);
				this.IsBusy = false;
			}
		}

		#endregion

		#region Timer

		private void StartTimer()
		{
			if (this.CountDownTimer is not null)
			{
				this.CountDownSeconds = 30;
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

		#endregion
	}
}
