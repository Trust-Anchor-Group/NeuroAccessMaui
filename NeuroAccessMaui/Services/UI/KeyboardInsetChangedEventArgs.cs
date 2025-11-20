using System;

namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// Event data describing an update to the keyboard inset height.
	/// </summary>
	public sealed class KeyboardInsetChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KeyboardInsetChangedEventArgs"/> class.
		/// </summary>
		/// <param name="keyboardHeight">The keyboard height measured in device-independent units.</param>
		/// <param name="isVisible">Indicates whether the keyboard is considered visible.</param>
		public KeyboardInsetChangedEventArgs(double keyboardHeight, bool isVisible)
		{
			this.KeyboardHeight = keyboardHeight;
			this.IsVisible = isVisible;
		}

		/// <summary>
		/// Gets the keyboard height measured in device-independent units.
		/// </summary>
		public double KeyboardHeight { get; }

		/// <summary>
		/// Gets a value indicating whether the keyboard is currently visible.
		/// </summary>
		public bool IsVisible { get; }
	}
}
