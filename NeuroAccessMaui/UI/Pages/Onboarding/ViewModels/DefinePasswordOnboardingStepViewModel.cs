using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// Onboarding step for defining local password/PIN (no strength UI, only validation rules enforced).
	/// </summary>
	public partial class DefinePasswordOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public DefinePasswordOnboardingStepViewModel() : base(OnboardingStep.DefinePassword) { }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PasswordsMatch))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private string password = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PasswordsMatch))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		private string confirmPassword = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PasswordVisibilityPathData))]
		private bool isPasswordHidden = true;

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordPageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.CreatePassword)];

		/// <summary>
		/// Toggle command.
		/// </summary>
		[RelayCommand]
		private void TogglePasswordVisibility() => this.IsPasswordHidden = !this.IsPasswordHidden;

		/// <summary>
		/// Passwords must match and be strong enough according to TagProfile rules.
		/// </summary>
		public bool PasswordsMatch => string.Equals(this.Password, this.ConfirmPassword, StringComparison.Ordinal);

		private PasswordStrength CurrentStrength => ServiceRef.TagProfile.ValidatePasswordStrength(this.Password);

		public string ValidationMessage
		{
			get
			{
				if (string.IsNullOrEmpty(this.Password) && string.IsNullOrEmpty(this.ConfirmPassword))
					return string.Empty;
				PasswordStrength s = this.CurrentStrength;
				if (s != PasswordStrength.Strong)
					return DefinePasswordOnboardingStepViewModel.GetLocalizedValidationError(s);
				if (!this.PasswordsMatch)
					return ServiceRef.Localizer[nameof(AppResources.PasswordsDoNotMatch)];
				return string.Empty;
			}
		}

		public bool HasValidationError => !string.IsNullOrEmpty(this.ValidationMessage);

		public bool CanContinue => !this.HasValidationError && !string.IsNullOrEmpty(this.Password) && !string.IsNullOrEmpty(this.ConfirmPassword);

		[RelayCommand]
		private void Validate() => this.OnPropertyChanged(nameof(this.ValidationMessage));

		[RelayCommand]
		private void Continue()
		{
			if (!this.CanContinue)
				return;
			ServiceRef.PlatformSpecific.HideKeyboard();
			ServiceRef.TagProfile.LocalPassword = this.Password;
			bool firstPassword = string.IsNullOrEmpty(ServiceRef.TagProfile.LocalPasswordHash);
			if (ServiceRef.PlatformSpecific.SupportsFingerprintAuthentication && firstPassword)
			{
				this.CoordinatorViewModel?.GoToStepCommand.Execute(OnboardingStep.Biometrics);
			}
			else
			{
				this.CoordinatorViewModel?.GoToStepCommand.Execute(OnboardingStep.Finalize);
			}
			if (ServiceRef.TagProfile.TestOtpTimestamp is not null)
			{
				ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.WarningTitle)],
					ServiceRef.Localizer[nameof(AppResources.TestOtpUsed)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		public static string GetLocalizedValidationError(PasswordStrength strength) => strength switch
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

		public Microsoft.Maui.Controls.Shapes.Geometry PasswordVisibilityPathData => this.IsPasswordHidden ? Geometries.VisibilityOnPath : Geometries.VisibilityOffPath;

		internal override Task<bool> OnNextAsync() => Task.FromResult(this.CanContinue);
	}
}
