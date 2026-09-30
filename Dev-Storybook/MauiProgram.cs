using CommunityToolkit.Maui;
using DevStorybook.FakeServices;
using DevStorybook.Pages;
using DevStorybook.Services;
using DevStorybook.ViewModels;
using Microsoft.Extensions.Logging;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Localization;

namespace DevStorybook
{
	/// <summary>
	/// Configures the developer Storybook application and its isolated dependency graph.
	/// </summary>
	public static class MauiProgram
	{
		/// <summary>
		/// Creates the configured MAUI application.
		/// </summary>
		/// <returns>The configured MAUI application.</returns>
		public static MauiApp CreateMauiApp()
		{
			MauiAppBuilder Builder = MauiApp.CreateBuilder();
			Builder.UseMauiApp<App>();
			Builder.UseMauiCommunityToolkit();
			Builder.UseLocalizationManager<AppResources>();

			Builder.ConfigureFonts(Fonts =>
			{
				Fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
				Fonts.AddFont("SpaceGrotesk-SemiBold.ttf", "SpaceGroteskSemiBold");
				Fonts.AddFont("SpaceGrotesk-Medium.ttf", "SpaceGroteskMedium");
				Fonts.AddFont("SpaceGrotesk-Regular.ttf", "SpaceGroteskRegular");
				Fonts.AddFont("NHaasGroteskTXPro-75Bd.ttf", "HaasGroteskBold");
				Fonts.AddFont("NHaasGroteskTXPro-55Rg.ttf", "HaasGroteskRegular");
			});

#if DEBUG
			Builder.Logging.AddDebug();
#endif

			Builder.Services.AddSingleton<IPhoneVerificationService, FakePhoneVerificationService>();
			Builder.Services.AddSingleton<IStorybookNavigationService, StorybookNavigationService>();
			Builder.Services.AddSingleton<StorybookHomeViewModel>();
			Builder.Services.AddSingleton<StorybookHomePage>();
			Builder.Services.AddTransient<StoryCategoryViewModel>();
			Builder.Services.AddTransient<StoryCategoryPage>();
			Builder.Services.AddTransient<ScreenDetailViewModel>();
			Builder.Services.AddTransient<ScreenDetailPage>();
			Builder.Services.AddTransient<StoryViewerViewModel>();
			Builder.Services.AddTransient<StoryViewerPage>();

			return Builder.Build();
		}
	}
}
