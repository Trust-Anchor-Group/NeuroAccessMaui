using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	/// <summary>
	/// ViewModel for defining a PIN/password in onboarding.
	/// </summary>
	public partial class DefinePasswordOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public DefinePasswordOnboardingStepViewModel() : base(OnboardingStep.DefinePassword) { }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		[NotifyPropertyChangedFor(nameof(HasValidationError))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		private string password = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		[NotifyPropertyChangedFor(nameof(HasValidationError))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		private string confirmPassword = string.Empty;

		public bool CanContinue => !string.IsNullOrWhiteSpace(this.Password) && this.Password.Length >= Constants.Security.MinPasswordLength && this.Password == this.ConfirmPassword;
		public bool HasValidationError => !this.CanContinue && (!string.IsNullOrEmpty(this.Password) || !string.IsNullOrEmpty(this.ConfirmPassword));
		public string ValidationMessage
		{
			get
			{
				if (string.IsNullOrEmpty(this.Password) || string.IsNullOrEmpty(this.ConfirmPassword))
					return string.Empty;
				if (this.Password.Length < Constants.Security.MinPasswordLength)
					return ServiceRef.Localizer[nameof(AppResources.PasswordTooShort), Constants.Security.MinPasswordLength];
				if (this.Password != this.ConfirmPassword)
					return ServiceRef.Localizer[nameof(AppResources.PasswordsDoNotMatch)];
				return string.Empty;
			}
		}

		public override string Title => ServiceRef.Localizer[nameof(AppResources.OnboardingDefinePasswordPageTitle)];
		public override string Description => ServiceRef.Localizer[nameof(AppResources.CreatePassword)];

		internal override Task<bool> OnNextAsync()
		{
			if (!this.CanContinue)
			{
				ServiceRef.LogService.LogWarning("Cannot advance: password validation failed.");
				return Task.FromResult(false);
			}
			ServiceRef.LogService.LogInformational("Password accepted. Advancing to Biometrics step.");
			return Task.FromResult(true);
		}
	}
}
