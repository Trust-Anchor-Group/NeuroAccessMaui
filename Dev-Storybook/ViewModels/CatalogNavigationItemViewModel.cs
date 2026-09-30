using CommunityToolkit.Mvvm.Input;

namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Represents one navigable screen or shared component in a catalog group.
	/// </summary>
	public sealed class CatalogNavigationItemViewModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CatalogNavigationItemViewModel"/> class.
		/// </summary>
		/// <param name="Title">The primary item label.</param>
		/// <param name="Subtitle">The secondary item label.</param>
		/// <param name="OpenAsync">The local catalog navigation action.</param>
		public CatalogNavigationItemViewModel(string Title, string Subtitle, Func<Task> OpenAsync)
		{
			this.Title = Title;
			this.Subtitle = Subtitle;
			this.OpenCommand = new AsyncRelayCommand(OpenAsync);
		}

		/// <summary>Gets the primary item label.</summary>
		public string Title { get; }

		/// <summary>Gets the secondary item label.</summary>
		public string Subtitle { get; }

		/// <summary>Gets whether the secondary label is visible.</summary>
		public bool HasSubtitle => !string.IsNullOrWhiteSpace(this.Subtitle);

		/// <summary>Gets the command that opens the catalog item.</summary>
		public IAsyncRelayCommand OpenCommand { get; }
	}
}
