using CommunityToolkit.Mvvm.ComponentModel;
using DevStorybook.Services;
using DevStorybook.Stories;

namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Separates one screen's full preview, controlled states, and components.
	/// </summary>
	public partial class ScreenDetailViewModel : ObservableObject
	{
		private readonly IStorybookNavigationService navigationService;

		[ObservableProperty]
		private string title = string.Empty;

		[ObservableProperty]
		private string breadcrumb = string.Empty;

		[ObservableProperty]
		private IReadOnlyList<StoryListItemViewModel> fullScreenStories = Array.Empty<StoryListItemViewModel>();

		[ObservableProperty]
		private IReadOnlyList<StoryListItemViewModel> stateStories = Array.Empty<StoryListItemViewModel>();

		[ObservableProperty]
		private IReadOnlyList<StoryListItemViewModel> componentStories = Array.Empty<StoryListItemViewModel>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenDetailViewModel"/> class.
		/// </summary>
		/// <param name="NavigationService">The Storybook navigation service.</param>
		public ScreenDetailViewModel(IStorybookNavigationService NavigationService)
		{
			this.navigationService = NavigationService;
		}

		/// <summary>Gets whether the screen exposes isolated components.</summary>
		public bool HasComponents => this.ComponentStories.Count > 0;

		/// <summary>
		/// Loads the detail catalog for a screen.
		/// </summary>
		/// <param name="ScreenKey">The stable screen key.</param>
		public void LoadScreen(string ScreenKey)
		{
			IReadOnlyList<StoryDefinition> Stories = StoryRegistry.GetScreenStories(ScreenKey);
			StoryDefinition FirstStory = Stories.FirstOrDefault()
				?? throw new InvalidOperationException($"Unknown Storybook screen: {ScreenKey}");

			this.Title = FirstStory.Screen;
			this.Breadcrumb = FirstStory.Breadcrumb;
			this.FullScreenStories = this.CreateItems(Stories, StoryType.FullScreen);
			this.StateStories = this.CreateItems(Stories, StoryType.State);
			this.ComponentStories = this.CreateItems(Stories, StoryType.Component);
			this.OnPropertyChanged(nameof(this.HasComponents));
		}

		private IReadOnlyList<StoryListItemViewModel> CreateItems(
			IEnumerable<StoryDefinition> Stories,
			StoryType StoryType)
		{
			return Stories
				.Where(Story => Story.StoryType == StoryType)
				.OrderBy(Story => Story.Order)
				.Select(Story => new StoryListItemViewModel(Story, this.navigationService))
				.ToList();
		}
	}
}
