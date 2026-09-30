using DevStorybook.Pages;
using DevStorybook.Stories;
using Microsoft.Extensions.DependencyInjection;

namespace DevStorybook.Services
{
	/// <summary>
	/// Navigates within the independent Storybook catalog stack.
	/// </summary>
	public sealed class StorybookNavigationService : IStorybookNavigationService
	{
		private readonly IServiceProvider serviceProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorybookNavigationService"/> class.
		/// </summary>
		/// <param name="ServiceProvider">The Storybook dependency provider.</param>
		public StorybookNavigationService(IServiceProvider ServiceProvider)
		{
			this.serviceProvider = ServiceProvider;
		}

		/// <inheritdoc/>
		public async Task OpenCategoryAsync(string Category)
		{
			StoryCategoryPage Page = this.serviceProvider.GetRequiredService<StoryCategoryPage>();
			Page.LoadCategory(Category);
			await GetNavigation().PushAsync(Page);
		}

		/// <inheritdoc/>
		public async Task OpenScreenAsync(string ScreenKey)
		{
			ScreenDetailPage Page = this.serviceProvider.GetRequiredService<ScreenDetailPage>();
			Page.LoadScreen(ScreenKey);
			await GetNavigation().PushAsync(Page);
		}

		/// <inheritdoc/>
		public async Task OpenStoryAsync(StoryDefinition Story)
		{
			StoryViewerPage Page = this.serviceProvider.GetRequiredService<StoryViewerPage>();
			Page.LoadStory(Story);
			await GetNavigation().PushAsync(Page);
		}

		private static INavigation GetNavigation()
		{
			Window Window = Application.Current?.Windows.FirstOrDefault()
				?? throw new InvalidOperationException("The Storybook window is not available.");
			Page RootPage = Window.Page
				?? throw new InvalidOperationException("The Storybook root page is not available.");
			return RootPage.Navigation;
		}
	}
}
