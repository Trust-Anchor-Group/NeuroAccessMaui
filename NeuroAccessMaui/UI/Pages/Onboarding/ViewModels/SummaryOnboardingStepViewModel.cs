using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public partial class SummaryOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public SummaryOnboardingStepViewModel()
			: base(OnboardingStep.Summary)
		{
		}

		[ObservableProperty]
		private string accountSummary = string.Empty;

		[ObservableProperty]
		private string verificationSummary = string.Empty;

		public override string Title => AppResources.OnboardingFinalizePageTitle;

		public override string NextButtonText => AppResources.Continue;

		internal override Task OnActivatedAsync()
		{
			if (this.Coordinator is null)
				return Task.CompletedTask;

			AccountSetupOnboardingStepViewModel account = this.Coordinator.GetStepViewModel<AccountSetupOnboardingStepViewModel>(OnboardingStep.AccountSetup);
			PinSetupOnboardingStepViewModel pin = this.Coordinator.GetStepViewModel<PinSetupOnboardingStepViewModel>(OnboardingStep.PinSetup);
			BaseIdApplicationOnboardingStepViewModel baseId = this.Coordinator.GetStepViewModel<BaseIdApplicationOnboardingStepViewModel>(OnboardingStep.BaseIdApplication);

			this.AccountSummary = string.Format("Display name: {0}{1}Email: {2}", account.DisplayName, Environment.NewLine, account.EmailAddress);
			this.VerificationSummary = string.Format("PIN set: {0}{1}Email verification: {2}{3}Phone verification: {4}",
				string.IsNullOrWhiteSpace(pin.Pin) ? "No" : "Yes",
				Environment.NewLine,
				baseId.IncludeEmailVerification ? "Included" : "Deferred",
				Environment.NewLine,
				baseId.IncludePhoneVerification ? "Included" : "Deferred");

			return Task.CompletedTask;
		}
	}
}
