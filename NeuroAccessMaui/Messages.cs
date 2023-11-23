using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui;

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
/// Keyboard size change message
/// </summary>
public class KeyboardSizeMessage(float KeyboardSize)
{
	/// <summary>
	/// Keyboard height
	/// </summary>
	public float KeyboardSize { get; } = KeyboardSize;
}
