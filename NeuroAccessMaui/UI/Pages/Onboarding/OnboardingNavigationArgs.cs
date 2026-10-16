using NeuroAccessMaui.Models;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Onboarding
{
	/// <summary>
	/// Navigation arguments for onboarding flow. Scenario determines dynamic starting step.
	/// </summary>
	public class OnboardingNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates default navigation args (full setup scenario).
		/// </summary>
		public OnboardingNavigationArgs() : this(OnboardingScenario.FullSetup) { }

		/// <summary>
		/// Creates args with an explicit scenario.
		/// </summary>
		/// <param name="scenario">Scenario to execute.</param>
		/// <param name="PendingIntentUri">Pending intent URI to process before onboarding.</param>
		public OnboardingNavigationArgs(OnboardingScenario scenario, string PendingIntentUri)
		{
			this.Scenario = scenario;
			this.PendingIntentUri = PendingIntentUri;
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
		/// Onboarding scenario.
		/// </summary>
		public OnboardingScenario Scenario { get; init; } = OnboardingScenario.FullSetup;

		/// <summary>
		/// Gets the invitation URI to process when onboarding starts, or an empty string when there is none.
		/// </summary>
		public string PendingIntentUri { get; init; } = string.Empty;

		/// <summary>
		/// Gets or sets an invitation that has already been retrieved and validated for <see cref="PendingIntentUri"/>, so it is not read again.
		/// </summary>
		/// <remarks>
		/// The invitation holds its decryption key and decrypted contents, such as account credentials, so it is cleared once
		/// onboarding has consumed it.
		/// </remarks>
		public Invitation? PreparedInvitation { get; set; }
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
