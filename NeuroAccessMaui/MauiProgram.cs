using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using NeuroAccessMaui.Pages;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder Builder = MauiApp.CreateBuilder();

		Builder.UseMauiApp<App>();

		Builder.ConfigureFonts(fonts =>
		{
			fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
			fonts.AddFont("SpaceGrotesk-SemiBold.ttf", "SpaceGroteskSemiBold");
			fonts.AddFont("SpaceGrotesk-Medium.ttf", "SpaceGroteskMedium");
			fonts.AddFont("SpaceGrotesk-Regular.ttf", "SpaceGroteskRegular");
			fonts.AddFont("SpaceGrotesk-Light.ttf", "SpaceGroteskLight");
			fonts.AddFont("NHaasGroteskTXPro-75Bd.ttf", "HaasGroteskBold");
			fonts.AddFont("NHaasGroteskTXPro-65Md.ttf", "HaasGroteskMedium");
			fonts.AddFont("NHaasGroteskTXPro-55Rg.ttf", "HaasGroteskRegular");
		});

		// NuGets
		Builder.ConfigureMopups();
		Builder.UseMauiCommunityToolkit();
		Builder.UseMauiCommunityToolkitMarkup();

		// Localization service
		Builder.UseLocalizationManager<AppResources>();

		// Singleton app's services
		Builder.Services.AddSingleton<IPlatformSpecific, PlatformSpecific>();

		// Apps pages & models
		Builder.RegisterPagesManager();

		//Builder.Services.AddLogging();
#if DEBUG
		Builder.Logging.AddDebug();
#endif

		return Builder.Build();
	}
}
