using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public partial class AccountSetupOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		private static readonly Regex emailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

		public AccountSetupOnboardingStepViewModel()
			: base(OnboardingStep.AccountSetup)
		{
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		[NotifyPropertyChangedFor(nameof(HasValidationError))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		private string displayName = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanContinue))]
		[NotifyPropertyChangedFor(nameof(HasValidationError))]
		[NotifyPropertyChangedFor(nameof(ValidationMessage))]
		private string emailAddress = string.Empty;

		public bool HasValidationError => !this.CanContinue;

		public bool CanContinue => !string.IsNullOrWhiteSpace(this.DisplayName) && emailRegex.IsMatch(this.EmailAddress);

		public string ValidationMessage
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(this.DisplayName) && !emailRegex.IsMatch(this.EmailAddress))
					return AppResources.EmailValidationFormat;

				if (string.IsNullOrWhiteSpace(this.DisplayName))
					return "Pick a display name so others know it's you.";

				return string.Empty;
			}
		}

		public override string Title => AppResources.OnboardingNameEntryTitle;

		public override string Description => AppResources.OnboardingNameEntryDetails;

		internal override Task<bool> OnNextAsync()
		{
			return Task.FromResult(this.CanContinue);
		}
	}
}
