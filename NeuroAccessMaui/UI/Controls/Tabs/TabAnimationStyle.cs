namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Specifies the animation strategy used when the selected tab changes.
	/// </summary>
	public enum TabAnimationStyle
	{
		/// <summary>
		/// No transition animation is applied.
		/// </summary>
		None,

		/// <summary>
		/// An underline indicator animates between selected items.
		/// </summary>
		UnderlineSlide,

		/// <summary>
		/// The selected tab performs a subtle fade and scale animation.
		/// </summary>
		FadeScale
	}
}
