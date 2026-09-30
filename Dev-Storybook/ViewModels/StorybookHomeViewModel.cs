using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevStorybook.Services;
using DevStorybook.Stories;

namespace DevStorybook.ViewModels
{
	/// <summary>
	/// Provides category entry points and metadata-driven direct search.
	/// </summary>
	public partial class StorybookHomeViewModel : ObservableObject
	{
		private readonly IStorybookNavigationService navigationService;

		[ObservableProperty]
		private string searchText = string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorybookHomeViewModel"/> class.
		/// </summary>
		/// <param name="NavigationService">The independent Storybook navigation service.</param>
		public StorybookHomeViewModel(IStorybookNavigationService NavigationService)
		{
			this.navigationService = NavigationService;
			this.SearchResults = new ObservableCollection<StoryListItemViewModel>();
		}

		/// <summary>Gets the stories matching the current metadata query.</summary>
		public ObservableCollection<StoryListItemViewModel> SearchResults { get; }

		/// <summary>Gets whether search mode is active.</summary>
		public bool IsSearching => !string.IsNullOrWhiteSpace(this.SearchText);

		/// <summary>Gets whether category entry points should be displayed.</summary>
		public bool ShowCategories => !this.IsSearching;

		/// <summary>Gets whether the active search returned no matches.</summary>
		public bool HasNoSearchResults => this.IsSearching && this.SearchResults.Count == 0;

		partial void OnSearchTextChanged(string Value)
		{
			this.SearchResults.Clear();
			string Query = Value.Trim();
			if (Query.Length > 0)
			{
				IEnumerable<StoryDefinition> Matches = StoryRegistry.Stories
					.Where(Story => Story.SearchText.Contains(Query, StringComparison.OrdinalIgnoreCase))
					.OrderBy(Story => Story.Category)
					.ThenBy(Story => Story.Subcategory)
					.ThenBy(Story => Story.Screen)
					.ThenBy(Story => Story.StoryType)
					.ThenBy(Story => Story.Order);

				foreach (StoryDefinition Story in Matches)
					this.SearchResults.Add(new StoryListItemViewModel(Story, this.navigationService));
			}

			this.OnPropertyChanged(nameof(this.IsSearching));
			this.OnPropertyChanged(nameof(this.ShowCategories));
			this.OnPropertyChanged(nameof(this.HasNoSearchResults));
		}

		[RelayCommand]
		private Task OpenOnboardingAsync() => this.navigationService.OpenCategoryAsync("Onboarding");

		[RelayCommand]
		private Task OpenSharedComponentsAsync() => this.navigationService.OpenCategoryAsync("Shared Components");
	}
}
