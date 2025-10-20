using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Onboarding step for defining a password or PIN. Shows strength feedback but only hard validation rules block continuation.
	/// </summary>
	public partial class DefinePasswordOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public DefinePasswordOnboardingStepViewModel() : base(OnboardingStep.DefinePassword) { }

		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;
		}

		public override async Task OnDisposeAsync()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;
			await base.OnDisposeAsync();
		}

		private void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
			this.UpdateToggleKeyboardText();
			this.UpdateSecurityScore();
			this.UpdateCanContinue();
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PasswordsMatch))]
		private string? passwordText1;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsPassword2NotValid))]
		[NotifyPropertyChangedFor(nameof(PasswordsMatch))]
		private string? passwordText2;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PasswordVisibilityPathData))]
		private bool isPasswordHidden = true;

		[ObservableProperty]
		private double securityBar1Percentage;

		[ObservableProperty]
		private double securityBar2Percentage;

		[ObservableProperty]
		private double securityBar3Percentage;

		[ObservableProperty]
		private Color securityTextColor = AppColors.WeakPasswordForeground;

		[ObservableProperty]
		private string securityText = ServiceRef.Localizer[nameof(AppResources.PasswordWeakSecurity)];

		[ObservableProperty]
		private Keyboard keyboardType = Keyboard.Numeric;

		[ObservableProperty]
		private string toggleKeyboardTypeText = ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordCreateAlphanumeric)];

		partial void OnPasswordText1Changed(string? value)
		{
			this.UpdateSecurityScore();
			this.UpdateCanContinue();
		}

		partial void OnPasswordText2Changed(string? value)
		{
			this.UpdateCanContinue();
		}

		private void UpdateSecurityScore()
		{
			double Score = ServiceRef.TagProfile.CalculatePasswordScore(this.PasswordText1);

			const double Low = Constants.Security.MediumSecurityScoreThreshold;
			const double Medium = Constants.Security.HighSecurityPasswordScoreThreshold;
			const double High = Constants.Security.MaxSecurityPasswordScoreThreshold;

			this.SecurityBar1Percentage = (Math.Min(Score, Low) / Low) * 100.0;
			this.SecurityBar2Percentage = Math.Max((Math.Min(Score - Low, Medium - Low) / (Medium - Low) * 100.0), 0);
			this.SecurityBar3Percentage = Math.Max((Math.Min(Score - Medium, High - Medium) / (High - Medium) * 100.0), 0);

			if (Score >= Medium)
			{
				this.SecurityTextColor = AppColors.StrongPasswordForeground;
				this.SecurityText = ServiceRef.Localizer[nameof(AppResources.PasswordStrongSecurity)];
			}
			else if (Score >= Low)
			{
				this.SecurityTextColor = AppColors.MediumPasswordForeground;
				this.SecurityText = ServiceRef.Localizer[nameof(AppResources.PasswordMediumSecurity)];
			}
			else
			{
				this.SecurityTextColor = AppColors.WeakPasswordForeground;
				this.SecurityText = ServiceRef.Localizer[nameof(AppResources.PasswordWeakSecurity)];
			}
		}

		[RelayCommand]
		private void TogglePasswordVisibility()
		{
			this.IsPasswordHidden = !this.IsPasswordHidden;
		}

		[RelayCommand]
		private void ValidatePassword()
		{
			this.OnPropertyChanged(nameof(this.PasswordStrength));
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
			this.OnPropertyChanged(nameof(this.IsPassword1NotValid));
			this.OnPropertyChanged(nameof(this.IsPassword2NotValid));
			this.UpdateCanContinue();
		}

		public Geometry PasswordVisibilityPathData => this.IsPasswordHidden ? Geometries.VisibilityOnPath : Geometries.VisibilityOffPath;

		public PasswordStrength PasswordStrength => ServiceRef.TagProfile.ValidatePasswordStrength(this.PasswordText1);

		public bool PasswordsMatch
		{
			get
			{
				if (string.IsNullOrEmpty(this.PasswordText1))
					return string.IsNullOrEmpty(this.PasswordText2);
				return string.Equals(this.PasswordText1, this.PasswordText2, StringComparison.Ordinal);
			}
		}

		/// <summary>
		/// True if first password violates hard rules. Soft warnings do not mark invalid.
		/// </summary>
		public bool IsPassword1NotValid => !string.IsNullOrEmpty(this.PasswordText1) && !this.PasswordIsAcceptable();

		public bool IsPassword2NotValid => !this.IsPassword1NotValid && !this.PasswordsMatch;

		public string LocalizedValidationError => GetLocalizedValidationError(this.PasswordStrength);

		public static string GetLocalizedValidationError(PasswordStrength PasswordStrength) => PasswordStrength switch
		{
			PasswordStrength.NotEnoughDigitsLettersSigns => ServiceRef.Localizer[nameof(AppResources.PasswordWithNotEnoughDigitsLettersSigns), Constants.Security.MinPasswordSymbolsFromDifferentClasses],
			PasswordStrength.NotEnoughDigitsOrSigns => ServiceRef.Localizer[nameof(AppResources.PasswordWithNotEnoughDigitsOrSigns), Constants.Security.MinPasswordSymbolsFromDifferentClasses],
			PasswordStrength.NotEnoughLettersOrDigits => ServiceRef.Localizer[nameof(AppResources.PasswordWithNotEnoughLettersOrDigits), Constants.Security.MinPasswordSymbolsFromDifferentClasses],
			PasswordStrength.NotEnoughLettersOrSigns => ServiceRef.Localizer[nameof(AppResources.PasswordWithNotEnoughLettersOrSigns), Constants.Security.MinPasswordSymbolsFromDifferentClasses],
			PasswordStrength.TooManyIdenticalSymbols => ServiceRef.Localizer[nameof(AppResources.PasswordWithTooManyIdenticalSymbols), Constants.Security.MaxPasswordIdenticalSymbols],
			PasswordStrength.TooManySequencedSymbols => ServiceRef.Localizer[nameof(AppResources.PasswordWithTooManySequencedSymbols), Constants.Security.MaxPasswordSequencedSymbols],
			PasswordStrength.TooShort => ServiceRef.Localizer[nameof(AppResources.PasswordTooShort), Constants.Security.MinPasswordLength],
			PasswordStrength.ContainsAddress => ServiceRef.Localizer[nameof(AppResources.PasswordContainsAddress)],
			PasswordStrength.ContainsName => ServiceRef.Localizer[nameof(AppResources.PasswordContainsName)],
			PasswordStrength.ContainsPersonalNumber => ServiceRef.Localizer[nameof(AppResources.PasswordContainsPersonalNumber)],
			PasswordStrength.ContainsPhoneNumber => ServiceRef.Localizer[nameof(AppResources.PasswordContainsPhoneNumber)],
			PasswordStrength.ContainsEMail => ServiceRef.Localizer[nameof(AppResources.PasswordContainsEMail)],
			PasswordStrength.Strong => string.Empty,
			_ => string.Empty
		};

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(this.ContinueCommand))]
		private bool canContinue; // Backing for command can-execute

		private void UpdateCanContinue()
		{
			this.CanContinue = this.PasswordIsAcceptable() && this.PasswordsMatch;
		}

		[RelayCommand]
		private void ToggleNumericPassword()
		{
			this.PasswordText1 = string.Empty;
			this.PasswordText2 = string.Empty;
			this.KeyboardType = this.KeyboardType == Keyboard.Numeric ? Keyboard.Default : Keyboard.Numeric;
			this.UpdateToggleKeyboardText();
			this.UpdateCanContinue();
		}

		private void UpdateToggleKeyboardText()
		{
			if (this.KeyboardType == Keyboard.Default)
				this.ToggleKeyboardTypeText = ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordCreateNumeric)];
			else
				this.ToggleKeyboardTypeText = ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordCreateAlphanumeric)];
		}

		/// <summary>
		/// Determines if password passes hard validation rules (acceptable). Soft warnings allowed.
		/// Hard blocking: TooShort, TooManyIdenticalSymbols, TooManySequencedSymbols, Contains* personal data.
		/// </summary>
		private bool PasswordIsAcceptable()
		{
			PasswordStrength Strength = this.PasswordStrength;

			if (string.IsNullOrEmpty(this.PasswordText1))
				return false; // Empty not acceptable

			switch (Strength)
			{
				case PasswordStrength.Strong:
				case PasswordStrength.NotEnoughDigitsLettersSigns:
				case PasswordStrength.NotEnoughDigitsOrSigns:
				case PasswordStrength.NotEnoughLettersOrDigits:
				case PasswordStrength.NotEnoughLettersOrSigns:
					return true; // Soft warnings
				case PasswordStrength.TooShort:
				case PasswordStrength.TooManyIdenticalSymbols:
				case PasswordStrength.TooManySequencedSymbols:
				case PasswordStrength.ContainsAddress:
				case PasswordStrength.ContainsName:
				case PasswordStrength.ContainsPersonalNumber:
				case PasswordStrength.ContainsPhoneNumber:
				case PasswordStrength.ContainsEMail:
				default:
					return false; // Hard block
			}
		}

		private bool CanExecuteContinue() => this.CanContinue;

		[RelayCommand(CanExecute = nameof(CanExecuteContinue))]
		private async Task Continue()
		{
			if (!this.PasswordIsAcceptable() || !this.PasswordsMatch)
			{
				return; // Should not be invoked when CanExecute false, but guard anyway.
			}

			ServiceRef.PlatformSpecific.HideKeyboard();

			bool IsFirstPassword = string.IsNullOrEmpty(ServiceRef.TagProfile.LocalPasswordHash);
			ServiceRef.TagProfile.LocalPassword = this.PasswordText1!;

			if (this.CoordinatorViewModel is not null)
			{
				if (ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication && IsFirstPassword)
					await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.Biometrics);
				else
					await this.CoordinatorViewModel.GoToStepCommand.ExecuteAsync(OnboardingStep.Finalize);
			}

			if (ServiceRef.TagProfile.TestOtpTimestamp is not null)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.WarningTitle)],
					ServiceRef.Localizer[nameof(AppResources.TestOtpUsed)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}
	}
}
