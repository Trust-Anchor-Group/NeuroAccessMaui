using System;

namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// Provides normalized keyboard inset information for UI components.
	/// </summary>
	public interface IKeyboardInsetsService
	{
		/// <summary>
		/// Raised when the keyboard inset changes.
		/// </summary>
		event EventHandler<KeyboardInsetChangedEventArgs> KeyboardInsetChanged;

		/// <summary>
		/// Gets the current keyboard height in device-independent units.
		/// </summary>
		double KeyboardHeight { get; }

		/// <summary>
		/// Gets a value indicating whether the software keyboard is currently visible.
		/// </summary>
		bool IsKeyboardVisible { get; }
	}
}
