using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;
using Mopups.Hosting;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		InitMauiControlsHandlers();

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

	private static void InitMauiControlsHandlers()
	{
#if IOS
		ScrollViewHandler.Mapper.AppendToMapping("ScrollViewHandler", (handler, view) =>
		{
			handler.PlatformView.Bounces = false;
		});

		CollectionViewHandler.Mapper.AppendToMapping("CollectionViewHandler", (handler, view) =>
		{
			//!!! handler.PlatformView.CollectionView.Bounces = false;
		});
#elif ANDROID
		ScrollViewHandler.Mapper.AppendToMapping("ScrollViewHandler", (handler, view) =>
		{
			handler.PlatformView.OverScrollMode = Android.Views.OverScrollMode.Never;
		});

		CollectionViewHandler.Mapper.AppendToMapping("CollectionViewHandler", (handler, view) =>
		{
			handler.PlatformView.OverScrollMode = Android.Views.OverScrollMode.Never;
		});
#endif
	}
}
