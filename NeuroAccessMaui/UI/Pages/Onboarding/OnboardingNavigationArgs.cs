using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	public class OnboardingNavigationArgs : NavigationArgs
	{
		public OnboardingNavigationArgs()
		{
		}

		public OnboardingNavigationArgs(OnboardingStep initialStep)
		{
			this.InitialStep = initialStep;
		}

		public OnboardingStep? InitialStep { get; init; }
	}
}
