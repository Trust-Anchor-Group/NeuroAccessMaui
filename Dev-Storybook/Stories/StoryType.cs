namespace DevStorybook.Stories
{
	/// <summary>
	/// Identifies the catalog role of a Storybook story.
	/// </summary>
	public enum StoryType
	{
		/// <summary>Renders a representative complete production screen.</summary>
		FullScreen,

		/// <summary>Renders a complete production screen in a controlled state.</summary>
		State,

		/// <summary>Renders an isolated reusable production component.</summary>
		Component
	}
}
