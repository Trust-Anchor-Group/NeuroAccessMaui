namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	public enum OnboardingStep
	{
		Welcome = 0,
		AccountSetup = 1,
		PinSetup = 2,
		BaseIdApplication = 3,
		Summary = 4
	}

	public static class OnboardingStepExtensions
	{
		public static string ToStateKey(this OnboardingStep step)
		{
			return step.ToString();
		}
	}
}
