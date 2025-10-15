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
	/// Reuses registration password-strength experience for onboarding.
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
		}

		private void UpdateSecurityScore()
		{
			double score = ServiceRef.TagProfile.CalculatePasswordScore(this.PasswordText1);

			const double low = Constants.Security.MediumSecurityScoreThreshold;
			const double medium = Constants.Security.HighSecurityPasswordScoreThreshold;
			const double high = Constants.Security.MaxSecurityPasswordScoreThreshold;

			this.SecurityBar1Percentage = (Math.Min(score, low) / low) * 100.0;
			this.SecurityBar2Percentage = Math.Max((Math.Min(score - low, medium - low) / (medium - low) * 100.0), 0);
			this.SecurityBar3Percentage = Math.Max((Math.Min(score - medium, high - medium) / (high - medium) * 100.0), 0);

			if (score >= medium)
			{
				this.SecurityTextColor = AppColors.StrongPasswordForeground;
				this.SecurityText = ServiceRef.Localizer[nameof(AppResources.PasswordStrongSecurity)];
			}
			else if (score >= low)
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

			this.CanContinue = true;
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

		public bool IsPassword1NotValid => !string.IsNullOrEmpty(this.PasswordText1) && !this.PasswordIsStrong();

		public bool IsPassword2NotValid => !this.IsPassword1NotValid && !this.PasswordsMatch;

		public string LocalizedValidationError => GetLocalizedValidationError(this.PasswordStrength);

		public static string GetLocalizedValidationError(PasswordStrength passwordStrength) => passwordStrength switch
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
		private bool canContinue;

		[RelayCommand]
		private void ToggleNumericPassword()
		{
			this.PasswordText1 = string.Empty;
			this.PasswordText2 = string.Empty;
			this.KeyboardType = this.KeyboardType == Keyboard.Numeric ? Keyboard.Default : Keyboard.Numeric;
			this.UpdateToggleKeyboardText();
		}

		private void UpdateToggleKeyboardText()
		{
			if (this.KeyboardType == Keyboard.Default)
				this.ToggleKeyboardTypeText = ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordCreateNumeric)];
			else
				this.ToggleKeyboardTypeText = ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordCreateAlphanumeric)];
		}

		private bool PasswordIsStrong() => this.PasswordStrength == PasswordStrength.Strong;

		[RelayCommand]
		private async Task Continue()
		{
			if (!this.PasswordIsStrong() || !this.PasswordsMatch)
			{
				this.CanContinue = false;
				return;
			}

			ServiceRef.PlatformSpecific.HideKeyboard();

			bool isFirstPassword = string.IsNullOrEmpty(ServiceRef.TagProfile.LocalPasswordHash);
			ServiceRef.TagProfile.LocalPassword = this.PasswordText1!;

			if (this.CoordinatorViewModel is not null)
			{
				if (ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication && isFirstPassword)
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
