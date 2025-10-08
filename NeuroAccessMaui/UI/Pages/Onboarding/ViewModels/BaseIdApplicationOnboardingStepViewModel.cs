using CommunityToolkit.Mvvm.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Onboarding.ViewModels
{
	public partial class BaseIdApplicationOnboardingStepViewModel : BaseOnboardingStepViewModel
	{
		public BaseIdApplicationOnboardingStepViewModel()
			: base(OnboardingStep.BaseIdApplication)
		{
		}

		[ObservableProperty]
		private bool includePhoneVerification;

		[ObservableProperty]
		private bool includeEmailVerification = true;

		public override string Title => "Apply for your base ID";

		public override string Description => "Select the verification methods you want to complete now. You can always return later to finish.";
	}
}
