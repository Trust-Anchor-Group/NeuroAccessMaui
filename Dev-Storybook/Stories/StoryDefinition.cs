namespace DevStorybook.Stories
{
	/// <summary>
	/// Describes one independently renderable catalog story and its deterministic state.
	/// </summary>
	/// <param name="Category">The top-level catalog category.</param>
	/// <param name="Subcategory">The logical area within the category.</param>
	/// <param name="Feature">The feature that owns the story.</param>
	/// <param name="Screen">The production screen associated with the story.</param>
	/// <param name="StoryType">The story's catalog role.</param>
	/// <param name="Name">The story's display name.</param>
	/// <param name="State">The controlled state represented by the story.</param>
	/// <param name="Tags">Additional searchable terms.</param>
	/// <param name="Order">The explicit display order within its section.</param>
	/// <param name="ComponentGroup">The shared-component group, when applicable.</param>
	public sealed record StoryDefinition(
		string Category,
		string Subcategory,
		string Feature,
		string Screen,
		StoryType StoryType,
		string Name,
		PhoneVerificationStoryState State,
		IReadOnlyList<string> Tags,
		int Order,
		string ComponentGroup = "")
	{
		/// <summary>Gets a stable key for the associated screen.</summary>
		public string ScreenKey => $"{this.Category}/{this.Subcategory}/{this.Screen}";

		/// <summary>Gets the feature and story name formatted for result lists.</summary>
		public string DisplayName => this.StoryType == StoryType.FullScreen
			? this.Screen
			: $"{this.Screen} — {this.Name}";

		/// <summary>Gets the catalog breadcrumb for the story.</summary>
		public string Breadcrumb => $"{this.Category} / {this.Subcategory} / {this.Screen}";

		/// <summary>Gets all metadata as normalized searchable text.</summary>
		public string SearchText => string.Join(' ', new[]
		{
			this.Category,
			this.Subcategory,
			this.Feature,
			this.Screen,
			this.StoryType.ToString(),
			this.Name,
			this.ComponentGroup,
			string.Join(' ', this.Tags)
		});
	}
}
