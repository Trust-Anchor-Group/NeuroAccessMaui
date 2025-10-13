using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	/// <summary>
	/// Navigation arguments for onboarding flow. Defines initial step or scenario variant.
	/// </summary>
	public class OnboardingNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates default navigation args (full setup scenario).
		/// </summary>
		public OnboardingNavigationArgs() : this(OnboardingScenario.FullSetup) { }

		/// <summary>
		/// Creates args with explicit initial step (scenario inferred as FullSetup unless overridden).
		/// </summary>
		/// <param name="initialStep">Initial onboarding step.</param>
		public OnboardingNavigationArgs(OnboardingStep initialStep) : this(OnboardingScenario.FullSetup)
		{
			this.InitialStep = initialStep;
		}

		/// <summary>
		/// Creates args with an explicit scenario.
		/// </summary>
		/// <param name="scenario">Scenario to execute.</param>
		public OnboardingNavigationArgs(OnboardingScenario scenario)
		{
			this.Scenario = scenario;
		}

		/// <summary>
		/// Optional initial step. Must be part of the scenario's step sequence to be used.
		/// </summary>
		public OnboardingStep? InitialStep { get; init; }

		/// <summary>
		/// Onboarding scenario.
		/// </summary>
		public OnboardingScenario Scenario { get; init; } = OnboardingScenario.FullSetup;
	}

	/// <summary>
	/// Distinct onboarding scenarios.
	/// </summary>
	public enum OnboardingScenario
	{
		/// <summary>
		/// First start of app: full journey (account, 2FA, identity creation, PIN, biometrics, finalize).
		/// </summary>
		FullSetup = 0,
		/// <summary>
		/// Only change PIN (redefine local/password credentials).
		/// </summary>
		ChangePin = 1,
		/// <summary>
		/// Redo 2FA for acquiring a new identity (phone/email + identity finalize).
		/// </summary>
		ReverifyIdentity = 2
	}
}
