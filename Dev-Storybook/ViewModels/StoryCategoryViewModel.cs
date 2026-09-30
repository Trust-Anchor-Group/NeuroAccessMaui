using CommunityToolkit.Mvvm.ComponentModel;
using DevStorybook.Services;
using DevStorybook.Stories;

namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Presents a grouped screen catalog or the shared component catalog.
	/// </summary>
	public partial class StoryCategoryViewModel : ObservableObject
	{
		private readonly IStorybookNavigationService navigationService;

		[ObservableProperty]
		private string title = string.Empty;

		[ObservableProperty]
		private string description = string.Empty;

		[ObservableProperty]
		private IReadOnlyList<StoryCatalogGroupViewModel> groups = Array.Empty<StoryCatalogGroupViewModel>();

		/// <summary>
		/// Initializes a new instance of the <see cref="StoryCategoryViewModel"/> class.
		/// </summary>
		/// <param name="NavigationService">The Storybook navigation service.</param>
		public StoryCategoryViewModel(IStorybookNavigationService NavigationService)
		{
			this.navigationService = NavigationService;
		}

		/// <summary>
		/// Loads the requested catalog category.
		/// </summary>
		/// <param name="Category">The category name.</param>
		public void LoadCategory(string Category)
		{
			if (Category == "Shared Components")
			{
				this.Title = Category;
				this.Description = "Reusable production controls, grouped by purpose.";
				this.Groups = this.CreateComponentGroups();
				return;
			}

			this.Title = "Onboarding";
			this.Description = "Choose a screen, then render its full preview, states, or components.";
			this.Groups = this.CreateScreenGroups();
		}

		private IReadOnlyList<StoryCatalogGroupViewModel> CreateScreenGroups()
		{
			return StoryRegistry.Stories
				.Where(Story => Story.StoryType == StoryType.FullScreen)
				.OrderBy(Story => Story.Order)
				.GroupBy(Story => Story.Subcategory)
				.Select(Group => new StoryCatalogGroupViewModel(
					Group.Key,
					Group.Select(Story => new CatalogNavigationItemViewModel(
						Story.Screen,
						$"{StoryRegistry.GetScreenStories(Story.ScreenKey).Count} stories",
						() => this.navigationService.OpenScreenAsync(Story.ScreenKey)))
						.ToList()))
				.ToList();
		}

		private IReadOnlyList<StoryCatalogGroupViewModel> CreateComponentGroups()
		{
			return StoryRegistry.Stories
				.Where(Story => Story.StoryType == StoryType.Component)
				.GroupBy(Story => Story.ComponentGroup)
				.OrderBy(Group => Group.Key == "Inputs" ? 0 : 1)
				.ThenBy(Group => Group.Key)
				.Select(Group => new StoryCatalogGroupViewModel(
					Group.Key,
					Group.OrderBy(Story => Story.Feature)
						.ThenBy(Story => Story.Order)
						.Select(Story => new CatalogNavigationItemViewModel(
							Story.Name,
							Story.Breadcrumb,
							() => this.navigationService.OpenStoryAsync(Story)))
						.ToList()))
				.ToList();
		}
	}
}
