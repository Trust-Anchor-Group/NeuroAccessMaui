using DevStorybook.Stories;

namespace DevStorybook.Services
{
	/// <summary>
	/// Defines navigation owned entirely by the developer catalog.
	/// </summary>
	public interface IStorybookNavigationService
	{
		/// <summary>Opens a grouped top-level catalog category.</summary>
		/// <param name="Category">The category name.</param>
		/// <returns>A task representing navigation.</returns>
		Task OpenCategoryAsync(string Category);

		/// <summary>Opens the detail catalog for one production screen.</summary>
		/// <param name="ScreenKey">The stable screen key.</param>
		/// <returns>A task representing navigation.</returns>
		Task OpenScreenAsync(string ScreenKey);

		/// <summary>Opens an independently renderable story.</summary>
		/// <param name="Story">The story to display.</param>
		/// <returns>A task representing navigation.</returns>
		Task OpenStoryAsync(StoryDefinition Story);
	}
}
