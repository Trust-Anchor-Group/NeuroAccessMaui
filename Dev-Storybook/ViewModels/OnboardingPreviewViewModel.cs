using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.UI.Pages;
using DevStorybook.Stories;

namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Supplies deterministic, backend-free data to reusable onboarding views.
	/// </summary>
	public sealed class OnboardingPreviewViewModel : BaseViewModel
	{
		private readonly Command noOpCommand = new Command(() => { });

		/// <summary>Initializes a new preview view model.</summary>
		public OnboardingPreviewViewModel()
		{
			this.PasteInviteCodeCommand = this.noOpCommand;
			this.ScanQrCodeCommand = this.noOpCommand;
			this.SelectForMeCommand = this.noOpCommand;
			this.SendCodeCommand = this.noOpCommand;
			this.ResendCodeCommand = this.noOpCommand;
			this.SelectNameCommand = this.noOpCommand;
			this.CreateAccountCommand = this.noOpCommand;
			this.TogglePasswordVisibilityCommand = this.noOpCommand;
			this.ValidatePasswordCommand = this.noOpCommand;
			this.ToggleNumericPasswordCommand = this.noOpCommand;
			this.ContinueCommand = this.noOpCommand;
			this.ShowBiometricsInfoCommand = this.noOpCommand;
			this.LaterCommand = this.noOpCommand;
			this.EnableCommand = this.noOpCommand;
			this.VerifyCommand = this.noOpCommand;
			this.CodeVerification = new VerificationCodePreviewActions(this.noOpCommand);
			this.Reset();
		}

		/// <summary>Gets or sets whether the ID-provider view is busy.</summary>
		public bool IsBusy { get; set; }
		/// <summary>Gets or sets the invitation code.</summary>
		public string InviteCode { get; set; } = string.Empty;
		/// <summary>Gets or sets whether the invitation code is valid.</summary>
		public bool InviteCodeIsValid { get; set; }
		/// <summary>Gets the paste-invitation-code command.</summary>
		public ICommand PasteInviteCodeCommand { get; }
		/// <summary>Gets the QR-code scan command.</summary>
		public ICommand ScanQrCodeCommand { get; }
		/// <summary>Gets the automatic provider-selection command.</summary>
		public ICommand SelectForMeCommand { get; }

		/// <summary>Gets or sets the screen title.</summary>
		public string Title { get; set; } = string.Empty;
		/// <summary>Gets or sets the screen description.</summary>
		public string Description { get; set; } = string.Empty;
		/// <summary>Gets or sets the e-mail address.</summary>
		public string EmailText { get; set; } = string.Empty;
		/// <summary>Gets or sets whether a validation error is visible.</summary>
		public bool ShowValidationError { get; set; }
		/// <summary>Gets or sets the validation error text.</summary>
		public string LocalizedValidationError { get; set; } = string.Empty;
		/// <summary>Gets or sets the localized send-code label.</summary>
		public string LocalizedSendCodeText { get; set; } = string.Empty;
		/// <summary>Gets or sets whether resend is enabled.</summary>
		public bool CanResendCode { get; set; }
		/// <summary>Gets the send-code command.</summary>
		public ICommand SendCodeCommand { get; }
		/// <summary>Gets the resend-code command.</summary>
		public ICommand ResendCodeCommand { get; }

		/// <summary>Gets or sets the requested username.</summary>
		public string Username { get; set; } = string.Empty;
		/// <summary>Gets or sets the username validation message.</summary>
		public string LocalizedValidationMessage { get; set; } = string.Empty;
		/// <summary>Gets or sets the suggested alternative username.</summary>
		public string AlternativeName { get; set; } = string.Empty;
		/// <summary>Gets or sets the localized continue label.</summary>
		public string LocalizedContinueText { get; set; } = string.Empty;
		/// <summary>Gets or sets whether account creation can continue.</summary>
		public bool CanCreateAccount { get; set; }
		/// <summary>Gets the suggested-name selection command.</summary>
		public ICommand SelectNameCommand { get; }
		/// <summary>Gets the create-account command.</summary>
		public ICommand CreateAccountCommand { get; }

		/// <summary>Gets or sets whether identity creation is in progress.</summary>
		public bool IsWaitingForIdentity { get; set; }
		/// <summary>Gets or sets the account-creation status message.</summary>
		public string StatusMessage { get; set; } = string.Empty;
		/// <summary>Gets or sets whether a status message is visible.</summary>
		public bool HasStatusMessage { get; set; }
		/// <summary>Gets or sets the account-creation error message.</summary>
		public string ErrorMessage { get; set; } = string.Empty;
		/// <summary>Gets or sets whether an account-creation error is visible.</summary>
		public bool HasError { get; set; }

		/// <summary>Gets or sets the first PIN value.</summary>
		public string PasswordText1 { get; set; } = string.Empty;
		/// <summary>Gets or sets the PIN confirmation.</summary>
		public string PasswordText2 { get; set; } = string.Empty;
		/// <summary>Gets or sets whether PIN values are hidden.</summary>
		public bool IsPasswordHidden { get; set; }
		/// <summary>Gets or sets the active keyboard.</summary>
		public Keyboard KeyboardType { get; set; } = Keyboard.Numeric;
		/// <summary>Gets the password-visibility icon geometry.</summary>
		public Geometry PasswordVisibilityPathData => this.IsPasswordHidden ? Geometries.VisibilityOnPath : Geometries.VisibilityOffPath;
		/// <summary>Gets or sets whether the first PIN is invalid.</summary>
		public bool IsPassword1NotValid { get; set; }
		/// <summary>Gets or sets whether the confirmation PIN is invalid.</summary>
		public bool IsPassword2NotValid { get; set; }
		/// <summary>Gets or sets whether an alphanumeric PIN is recommended.</summary>
		public bool IsAlphanumericKeyboardPreferred { get; set; }
		/// <summary>Gets or sets the first security-bar fill percentage.</summary>
		public double SecurityBar1Percentage { get; set; }
		/// <summary>Gets or sets the second security-bar fill percentage.</summary>
		public double SecurityBar2Percentage { get; set; }
		/// <summary>Gets or sets the third security-bar fill percentage.</summary>
		public double SecurityBar3Percentage { get; set; }
		/// <summary>Gets or sets the security assessment color.</summary>
		public Color SecurityTextColor { get; set; } = Colors.Orange;
		/// <summary>Gets or sets the security assessment text.</summary>
		public string SecurityText { get; set; } = string.Empty;
		/// <summary>Gets or sets the keyboard-switch label.</summary>
		public string ToggleKeyboardTypeText { get; set; } = string.Empty;
		/// <summary>Gets or sets whether PIN creation can continue.</summary>
		public bool CanContinue { get; set; }
		/// <summary>Gets the password-visibility command.</summary>
		public ICommand TogglePasswordVisibilityCommand { get; }
		/// <summary>Gets the password-validation command.</summary>
		public ICommand ValidatePasswordCommand { get; }
		/// <summary>Gets the keyboard-switch command.</summary>
		public ICommand ToggleNumericPasswordCommand { get; }
		/// <summary>Gets the continue command.</summary>
		public ICommand ContinueCommand { get; }

		/// <summary>Gets the biometric icon background size.</summary>
		public double AuthenticationBackgroundSize => 120.0;
		/// <summary>Gets the biometric icon background shape.</summary>
		public RoundRectangle AuthenticationBackgroundStroke => new RoundRectangle { CornerRadius = this.AuthenticationBackgroundSize / 2 };
		/// <summary>Gets the biometric icon size.</summary>
		public double AuthenticationIconSize => 60.0;
		/// <summary>Gets or sets whether the fingerprint illustration is visible.</summary>
		public bool IsFingerprint { get; set; }
		/// <summary>Gets or sets whether the face illustration is visible.</summary>
		public bool IsFace { get; set; }
		/// <summary>Gets or sets the biometric detail text.</summary>
		public string DetailText { get; set; } = string.Empty;
		/// <summary>Gets or sets the biometric information label.</summary>
		public string WhatIsX { get; set; } = string.Empty;
		/// <summary>Gets the biometric information command.</summary>
		public ICommand ShowBiometricsInfoCommand { get; }
		/// <summary>Gets the postpone-biometric command.</summary>
		public ICommand LaterCommand { get; }
		/// <summary>Gets the enable-biometric command.</summary>
		public ICommand EnableCommand { get; }

		/// <summary>Gets or sets the verification code.</summary>
		public string VerifyCodeText { get; set; } = string.Empty;
		/// <summary>Gets or sets whether the story represents phone or e-mail verification.</summary>
		public string VerificationContext { get; set; } = string.Empty;
		/// <summary>Gets or sets the verification-code screen details.</summary>
		public string LocalizedVerifyCodePageDetails { get; set; } = string.Empty;
		/// <summary>Gets or sets the resend-code label.</summary>
		public string LocalizedResendCodeText { get; set; } = string.Empty;
		/// <summary>Gets the local resend-code action provider.</summary>
		public VerificationCodePreviewActions CodeVerification { get; }
		/// <summary>Gets the local verify command.</summary>
		public ICommand VerifyCommand { get; }

		/// <summary>Gets the completion icon background size.</summary>
		public double CheckmarkBackgroundSize => 120.0;
		/// <summary>Gets the completion icon background shape.</summary>
		public RoundRectangle CheckmarkBackgroundStroke => new RoundRectangle { CornerRadius = this.CheckmarkBackgroundSize / 2 };
		/// <summary>Gets the completion icon size.</summary>
		public double CheckmarkIconSize => 60.0;

		/// <summary>
		/// Loads deterministic values for a selected onboarding story.
		/// </summary>
		/// <param name="State">The story state to load.</param>
		public void LoadState(PhoneVerificationStoryState State)
		{
			this.Reset();
			this.ConfigureVerificationCodeState(State);

			switch (State)
			{
				case PhoneVerificationStoryState.EmailValid:
					this.EmailText = "alex@example.com";
					break;
				case PhoneVerificationStoryState.EmailInvalid:
					this.EmailText = "not-an-email";
					this.ShowValidationError = true;
					this.LocalizedValidationError = AppResources.EmailValidationFormat;
					break;
				case PhoneVerificationStoryState.UsernameValid:
					this.Username = "alex";
					this.CanCreateAccount = true;
					break;
				case PhoneVerificationStoryState.UsernameInvalid:
					this.Username = "alex!";
					this.LocalizedValidationMessage = AppResources.InvalidUsernameCharacters;
					this.AlternativeName = "alex-01";
					break;
				case PhoneVerificationStoryState.AccountCreation:
					this.IsWaitingForIdentity = true;
					this.StatusMessage = AppResources.OnboardingAccountPageTitle;
					this.HasStatusMessage = true;
					break;
				case PhoneVerificationStoryState.PinValid:
					this.PasswordText1 = "2580";
					this.PasswordText2 = "2580";
					this.SecurityBar1Percentage = 100.0;
					this.SecurityBar2Percentage = 55.0;
					this.CanContinue = true;
					break;
				case PhoneVerificationStoryState.PinMismatch:
					this.PasswordText1 = "2580";
					this.PasswordText2 = "2581";
					this.IsPassword2NotValid = true;
					this.SecurityBar1Percentage = 100.0;
					break;
			}
		}

		/// <inheritdoc/>
		public override Task GoBack()
		{
			return Task.CompletedTask;
		}

		private void Reset()
		{
			this.IsBusy = false;
			this.InviteCode = string.Empty;
			this.InviteCodeIsValid = true;
			this.Title = AppResources.OnboardingEmailPageTitle;
			this.Description = AppResources.OnboardingEmailPageDetails;
			this.EmailText = string.Empty;
			this.ShowValidationError = false;
			this.LocalizedValidationError = string.Empty;
			this.LocalizedSendCodeText = AppResources.SendCode;
			this.CanResendCode = false;
			this.Username = string.Empty;
			this.LocalizedValidationMessage = string.Empty;
			this.AlternativeName = string.Empty;
			this.LocalizedContinueText = AppResources.Continue;
			this.CanCreateAccount = false;
			this.IsWaitingForIdentity = false;
			this.StatusMessage = string.Empty;
			this.HasStatusMessage = false;
			this.ErrorMessage = string.Empty;
			this.HasError = false;
			this.PasswordText1 = string.Empty;
			this.PasswordText2 = string.Empty;
			this.IsPasswordHidden = true;
			this.KeyboardType = Keyboard.Numeric;
			this.IsPassword1NotValid = false;
			this.IsPassword2NotValid = false;
			this.IsAlphanumericKeyboardPreferred = false;
			this.SecurityBar1Percentage = 0.0;
			this.SecurityBar2Percentage = 0.0;
			this.SecurityBar3Percentage = 0.0;
			this.SecurityTextColor = Colors.Orange;
			this.SecurityText = AppResources.PasswordWeakSecurity;
			this.ToggleKeyboardTypeText = AppResources.OnboardingDefinePasswordCreateAlphanumeric;
			this.CanContinue = false;
			this.IsFingerprint = true;
			this.IsFace = false;
			this.DetailText = AppResources.BiometricAuthentication;
			this.WhatIsX = AppResources.BiometricAuthentication;
			this.VerifyCodeText = string.Empty;
			this.VerificationContext = string.Empty;
			this.LocalizedVerifyCodePageDetails = string.Empty;
			this.LocalizedResendCodeText = AppResources.ResendCode;
		}

		private void ConfigureVerificationCodeState(PhoneVerificationStoryState State)
		{
			bool IsPhoneStory = State is >= PhoneVerificationStoryState.PhoneCodeDefault and <= PhoneVerificationStoryState.PhoneCodeBackendError;
			bool IsEmailStory = State is >= PhoneVerificationStoryState.EmailCodeDefault and <= PhoneVerificationStoryState.EmailCodeBackendError;
			if (!IsPhoneStory && !IsEmailStory)
				return;

			this.VerificationContext = IsPhoneStory ? "phone" : "email";
			string Destination = IsPhoneStory ? "+46 70 123 45 67" : "alex@example.com";
			this.LocalizedVerifyCodePageDetails = string.Format(AppResources.OnboardingVerifyCodePageDetails, Destination);

			if (State is PhoneVerificationStoryState.PhoneCodeInvalid or PhoneVerificationStoryState.EmailCodeInvalid)
				this.VerifyCodeText = "123";
			else if (State is PhoneVerificationStoryState.PhoneCodeVerifying or
				PhoneVerificationStoryState.PhoneCodeVerified or
				PhoneVerificationStoryState.PhoneCodeExpired or
				PhoneVerificationStoryState.PhoneCodeBackendError or
				PhoneVerificationStoryState.EmailCodeVerifying or
				PhoneVerificationStoryState.EmailCodeVerified or
				PhoneVerificationStoryState.EmailCodeExpired or
				PhoneVerificationStoryState.EmailCodeBackendError)
			{
				this.VerifyCodeText = "123456";
			}

			if (State is PhoneVerificationStoryState.PhoneCodeExpired or PhoneVerificationStoryState.EmailCodeExpired)
				this.LocalizedResendCodeText = AppResources.ResendCode;
		}
	}

	/// <summary>
	/// Supplies a backend-free resend action to the production verification-code view.
	/// </summary>
	public sealed class VerificationCodePreviewActions
	{
		/// <summary>Initializes a new preview action provider.</summary>
		/// <param name="ResendCodeCommand">The local resend command.</param>
		public VerificationCodePreviewActions(ICommand ResendCodeCommand)
		{
			this.ResendCodeCommand = ResendCodeCommand;
		}

		/// <summary>Gets the backend-free resend-code command.</summary>
		public ICommand ResendCodeCommand { get; }
	}
}
