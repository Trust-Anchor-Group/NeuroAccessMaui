using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class DefinePasswordViewModel : BaseRegistrationViewModel
	{
		public DefinePasswordViewModel()
			: base(RegistrationStep.DefinePassword)
		{
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LocalizationManager.Current.PropertyChanged += this.LocalizationManagerEventHandler;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			LocalizationManager.Current.PropertyChanged -= this.LocalizationManagerEventHandler;

			await base.OnDispose();
		}

		/// <summary>
		/// EventHandler for localization change
		/// </summary>
		public void LocalizationManagerEventHandler(object? sender, PropertyChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.LocalizedValidationError));
			this.UpdateToggleKeyboardText();
			this.UpdateSecurityScore();
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PasswordsMatch))]
		[NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
		private string? passwordText1;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsPassword2NotValid))]
		[NotifyPropertyChangedFor(nameof(PasswordsMatch))]
		[NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
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
		private string toggleNumericPasswordText = ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordCreateNumeric)];

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
		}

		/// <summary>
		/// Gets the path data for the password visibility icon depending if the password is hidden or not.
		/// </summary>
		public Geometry PasswordVisibilityPathData => this.IsPasswordHidden ? Geometries.VisibilityOnPath : Geometries.VisibilityOffPath;

		/// <summary>
		/// Gets the value indicating how strong the <see cref="PasswordText1"/> is.
		/// </summary>
		public PasswordStrength PasswordStrength => ServiceRef.TagProfile.ValidatePasswordStrength(this.PasswordText1);

		/// <summary>
		/// Gets the value indicating whether the entered <see cref="PasswordText1"/> is the same as the entered <see cref="PasswordText2"/>.
		/// </summary>
		public bool PasswordsMatch
		{
			get
			{
				if (string.IsNullOrEmpty(this.PasswordText1))
					return string.IsNullOrEmpty(this.PasswordText2);
				else
					return string.Equals(this.PasswordText1, this.PasswordText2, StringComparison.Ordinal);
			}
		}

		/// <summary>
		/// If First Password entry is not valid.
		/// </summary>
		public bool IsPassword1NotValid => !string.IsNullOrEmpty(this.PasswordText1) && this.PasswordStrength != PasswordStrength.Strong;

		/// <summary>
		/// If Second Password entry is not valid.
		/// </summary>
		public bool IsPassword2NotValid => !string.IsNullOrEmpty(this.PasswordText2) && !this.PasswordsMatch;

		/// <summary>
		/// Localized validation error message.
		/// </summary>
		public string LocalizedValidationError => GetLocalizedValidationError(this.PasswordStrength);

		/// <summary>
		/// Gets a localized error message, given a Password strength.
		/// </summary>
		/// <param name="PasswordStrength">Password strength.</param>
		/// <returns>Localized error message (or empty string if OK).</returns>
		public static string GetLocalizedValidationError(PasswordStrength PasswordStrength)
		{
			return PasswordStrength switch
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
				_ => throw new NotImplementedException()
			};
		}

		public bool CanContinue => this.PasswordStrength == PasswordStrength.Strong && this.PasswordsMatch;




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


		[RelayCommand(CanExecute = nameof(CanContinue))]
		private void Continue()
		{
			ServiceRef.PlatformSpecific.HideKeyboard();

			bool IsFirstPassword = string.IsNullOrEmpty(ServiceRef.TagProfile.LocalPasswordHash); //Wheter or not to go to the Finalize view

			ServiceRef.TagProfile.LocalPassword = this.PasswordText1!;
			if(ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication && IsFirstPassword)
				GoToRegistrationStep(RegistrationStep.Biometrics);
			else
				GoToRegistrationStep(RegistrationStep.Complete);

			if (ServiceRef.TagProfile.TestOtpTimestamp is not null)
			{
				ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.WarningTitle)],
					ServiceRef.Localizer[nameof(AppResources.TestOtpUsed)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}
	}
}
