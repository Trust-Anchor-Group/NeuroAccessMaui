namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Identifies the scene drawn by <see cref="StatusVisual"/>.
	/// </summary>
	public enum StatusVisualKind
	{
		/// <summary>
		/// Work has started but has not been sent; shows a pencil.
		/// </summary>
		Draft,

		/// <summary>
		/// Waiting for someone else; shows a flipping hourglass with an orbiting arc.
		/// </summary>
		Pending,

		/// <summary>
		/// Completed successfully; shows a check mark.
		/// </summary>
		Success,

		/// <summary>
		/// Needs the user's attention; shows an exclamation mark in the warning color.
		/// </summary>
		Attention
	}
}
