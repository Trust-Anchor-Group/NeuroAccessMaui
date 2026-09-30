using CommunityToolkit.Mvvm.Input;
using DevStorybook.Services;
using DevStorybook.Stories;

namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Represents a directly renderable story result or detail entry.
	/// </summary>
	public sealed class StoryListItemViewModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StoryListItemViewModel"/> class.
		/// </summary>
		/// <param name="Story">The represented story.</param>
		/// <param name="NavigationService">The Storybook navigation service.</param>
		public StoryListItemViewModel(StoryDefinition Story, IStorybookNavigationService NavigationService)
		{
			this.Story = Story;
			this.OpenCommand = new AsyncRelayCommand(() => NavigationService.OpenStoryAsync(this.Story));
		}

		/// <summary>Gets the represented story.</summary>
		public StoryDefinition Story { get; }

		/// <summary>Gets the story name shown inside a detail section.</summary>
		public string Name => this.Story.Name;

		/// <summary>Gets the qualified result name.</summary>
		public string DisplayName => this.Story.DisplayName;

		/// <summary>Gets the catalog breadcrumb.</summary>
		public string Breadcrumb => this.Story.Breadcrumb;

		/// <summary>Gets a readable story-type label.</summary>
		public string TypeLabel => this.Story.StoryType switch
		{
			StoryType.FullScreen => "FULL SCREEN",
			StoryType.State => "STATE",
			StoryType.Component => "COMPONENT",
			_ => this.Story.StoryType.ToString().ToUpperInvariant()
		};

		/// <summary>Gets the command that opens the story directly.</summary>
		public IAsyncRelayCommand OpenCommand { get; }
	}
}
