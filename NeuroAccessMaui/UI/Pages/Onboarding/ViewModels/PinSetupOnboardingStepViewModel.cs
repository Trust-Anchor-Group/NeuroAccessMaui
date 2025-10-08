using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public partial class PinSetupOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public PinSetupOnboardingStepViewModel()
			: base(OnboardingStep.PinSetup)
		{
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasValidationError))]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		private string pin = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasValidationError))]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		private string confirmPin = string.Empty;

		public bool PinsMatch => !string.IsNullOrWhiteSpace(this.Pin) && this.Pin == this.ConfirmPin;

		public bool CanContinue => this.PinsMatch && this.Pin.Length >= 4;

		public bool HasValidationError => !this.CanContinue && (!string.IsNullOrEmpty(this.Pin) || !string.IsNullOrEmpty(this.ConfirmPin));

		public string ValidationMessage
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.Pin) || string.IsNullOrWhiteSpace(this.ConfirmPin))
					return "Choose a secure PIN and confirm it.";

				if (this.Pin.Length < 4)
					return "PIN must be at least 4 digits.";

				if (!this.PinsMatch)
					return AppResources.PasswordsDoNotMatch;

				return string.Empty;
			}
		}

		public override string Title => AppResources.OnboardingDefinePasswordPageTitle;

		public override string Description => "You will use this PIN to quickly verify yourself on this device.";

		internal override Task<bool> OnNextAsync()
		{
			return Task.FromResult(this.CanContinue);
		}
	}
}
