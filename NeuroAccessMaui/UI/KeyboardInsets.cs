using System;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Specifies how keyboard insets should be applied to a view.
	/// </summary>
	public enum KeyboardInsetMode
	{
		/// <summary>
		/// The shell applies keyboard insets automatically.
		/// </summary>
		Automatic,

		/// <summary>
		/// The shell does not apply insets; the view handles keyboard updates manually.
		/// </summary>
		Manual,

		/// <summary>
		/// Keyboard insets are ignored.
		/// </summary>
		Ignore
	}

	/// <summary>
	/// Represents a component that consumes keyboard inset updates manually.
	/// </summary>
	public interface IKeyboardInsetAware
	{
		/// <summary>
		/// Called when the keyboard inset changes.
		/// </summary>
		/// <param name="args">The keyboard inset change arguments.</param>
		void OnKeyboardInsetChanged(KeyboardInsetChangedEventArgs args);
	}

	/// <summary>
	/// Attached properties for configuring keyboard inset handling on views.
	/// </summary>
	public static class KeyboardInsets
	{
		/// <summary>
		/// Identifies the <see cref="KeyboardInsetMode"/> attached property.
		/// </summary>
		public static readonly BindableProperty ModeProperty =
			BindableProperty.CreateAttached(
				"Mode",
				typeof(KeyboardInsetMode),
				typeof(KeyboardInsets),
				KeyboardInsetMode.Automatic);

		/// <summary>
		/// Gets the <see cref="KeyboardInsetMode"/> value for the provided view.
		/// </summary>
		/// <param name="view">The view for which to retrieve the mode.</param>
		/// <returns>The configured <see cref="KeyboardInsetMode"/> value.</returns>
		public static KeyboardInsetMode GetMode(BindableObject view)
		{
			ArgumentNullException.ThrowIfNull(view);
			return (KeyboardInsetMode)view.GetValue(ModeProperty);
		}

		/// <summary>
		/// Sets the <see cref="KeyboardInsetMode"/> for the provided view.
		/// </summary>
		/// <param name="view">The view to configure.</param>
		/// <param name="value">The desired <see cref="KeyboardInsetMode"/>.</param>
		public static void SetMode(BindableObject view, KeyboardInsetMode value)
		{
			ArgumentNullException.ThrowIfNull(view);
			view.SetValue(ModeProperty, value);
		}
	}
}
