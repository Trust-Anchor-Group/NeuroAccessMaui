using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;

namespace NeuroAccessMaui
{
	/// <summary>
	/// RegistrationPage view change message
	/// </summary>
	public class RegistrationPageMessage(RegistrationStep Step)
	{
		/// <summary>
		/// Transition to this step
		/// </summary>
		public RegistrationStep Step { get; } = Step;
	}

	/// <summary>
	/// Signals that an onboarding link is being processed (start/stop).
	/// Used by GetStarted view to toggle a loading indicator when an onboarding link is opened externally.
	/// </summary>
	public class OnboardingLinkProcessingMessage(bool isProcessing)
	{
		/// <summary>
		/// True when processing begins, false when finished or aborted.
		/// </summary>
		public bool IsProcessing { get; } = isProcessing;
	}

	/// <summary>
	/// Keyboard size change message
	/// </summary>
	public class KeyboardSizeMessage(float KeyboardSize)
	{
		/// <summary>
		/// Keyboard height
		/// </summary>
		public float KeyboardSize { get; } = KeyboardSize;
	}
}
