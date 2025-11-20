using System;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Determines how <see cref="ViewSwitcher"/> handles attempts to set a selection outside the available range.
	/// </summary>
	public enum ViewSwitcherSelectedIndexBehavior
	{
		/// <summary>
		/// Clamp the requested index to the nearest valid value.
		/// </summary>
		Clamp,

		/// <summary>
		/// Ignore the request and keep the current selection.
		/// </summary>
		Ignore,

		/// <summary>
		/// Wrap around when the requested index is outside the range.
		/// </summary>
		Wrap,

		/// <summary>
		/// Throw an <see cref="ArgumentOutOfRangeException"/> when the requested index is invalid.
		/// </summary>
		Throw
	}
}
