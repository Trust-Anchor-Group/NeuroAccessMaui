namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	/// <summary>
	/// Unified onboarding steps matching the registration journey.
	/// </summary>
	public enum OnboardingStep
	{
		Welcome = 0,
		ValidatePhone = 20,
		ValidateEmail = 30,
		NameEntry = 40,
		CreateAccount = 50,
		DefinePassword = 60,
		Biometrics = 80,
		Finalize = 90,
		ContactSupport = 100
	}

	public static class OnboardingStepExtensions
	{
		/// <summary>
		/// Converts a step value to its string StateKey.
		/// </summary>
		public static string ToStateKey(this OnboardingStep step) => step.ToString();
	}
}
