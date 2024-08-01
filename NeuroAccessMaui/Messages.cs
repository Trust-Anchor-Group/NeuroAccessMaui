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
	/// NewContractPage view change message
	/// </summary>
	public class NewContractPageMessage(NewContractStep NewState)
	{
		/// <summary>
		/// The new state to transition to
		/// </summary>
		public NewContractStep NewState { get; } = NewState;
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
