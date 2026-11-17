namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// What a <see cref="MarkdownView"/> shows its text on.
	/// </summary>
	public enum MarkdownTone
	{
		/// <summary>
		/// The page's own surfaces. Text uses the theme's colors.
		/// </summary>
		Default,

		/// <summary>
		/// A surface in the accent color, such as a sent chat message. Text, links and markers are white, and code, quotes
		/// and tables sit on a light white tint. Cards and images keep their own look.
		/// </summary>
		OnAccent
	}
}
