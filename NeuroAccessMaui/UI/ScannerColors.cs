namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Feedback colors shared by camera scanner screens, which draw over a live camera preview instead of the app theme.
	/// </summary>
	public static class ScannerColors
	{
		/// <summary>
		/// Neutral guidance color, used while searching.
		/// </summary>
		public static readonly Color Neutral = Colors.White;

		/// <summary>
		/// Success color, used when something is recognized or captured.
		/// </summary>
		public static readonly Color Success = Color.FromArgb("#5CE0A0");

		/// <summary>
		/// Attention color, used for rejections and guidance that needs the user to act.
		/// </summary>
		public static readonly Color Attention = Color.FromArgb("#FFD166");

		/// <summary>
		/// Dimming color drawn over the preview outside the scan target.
		/// </summary>
		public static readonly Color Scrim = Color.FromArgb("#8C000000");

		/// <summary>
		/// Background color for guidance cards drawn over the preview.
		/// </summary>
		public static readonly Color CardBackground = Color.FromArgb("#B3101416");
	}
}
