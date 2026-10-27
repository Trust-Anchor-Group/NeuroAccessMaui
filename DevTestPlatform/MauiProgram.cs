using CommunityToolkit.Maui;
using DevTestPlatform.Catalog;
using DevTestPlatform.Fakes;
using DevTestPlatform.Navigation;
using DevTestPlatform.ProductionAdapters;
using DevTestPlatform.UI.Pages;
using DevTestPlatform.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Controls;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Handlers;

namespace DevTestPlatform;

/// <summary>Configures the independent developer application using current production controls.</summary>
public static class MauiProgram
{
    /// <summary>Creates the developer application's isolated service provider and MAUI host.</summary>
    /// <returns>The configured developer application.</returns>
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder Builder = MauiApp.CreateBuilder();
        Builder.UseMauiApp<App>().UseMauiCommunityToolkit().UseSkiaSharp();
        Builder.UseLocalizationManager<AppResources>();
        NeuroAccessMaui.UI.Automation.AutomationMetadata.Configure();
        Builder.ConfigureMauiHandlers(Handlers =>
        {
            Handlers.AddHandler<AutoHeightSKCanvasView, SKCanvasViewHandler>();
            Handlers.AddHandler(typeof(AspectRatioLayout), typeof(LayoutHandler));
        });
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
        Builder.Services.AddSingleton<ITagProfile, TagProfile>();
        Builder.Services.AddSingleton<IPlatformSpecific, NeuroAccessMaui.Services.PlatformSpecific>();
        Builder.Services.AddSingleton<IKeyboardInsetsService, KeyboardInsetsService>();
        Builder.Services.AddSingleton<IMotionSettings, MotionSettings>();
        Builder.Services.AddSingleton<IAnimationRegistry>(_ =>
        {
            AnimationRegistry Registry = new AnimationRegistry();
            AnimationCatalog.RegisterDefaults(Registry);
            return Registry;
        });
        Builder.Services.AddSingleton<IAnimationContextProvider, AnimationContextProvider>();
        Builder.Services.AddSingleton<IAnimationCoordinator, AnimationCoordinator>();
        Builder.Services.AddSingleton<CatalogRegistry>();
        Builder.Services.AddSingleton<ProductionScreenAdapter>();
        Builder.Services.AddSingleton<ScenarioSimulationService>();
        Builder.Services.AddSingleton<DevNavigationService>();
        Builder.Services.AddSingleton<INavigationService>(Provider => Provider.GetRequiredService<DevNavigationService>());
        Builder.Services.AddSingleton<CatalogViewModel>();
        Builder.Services.AddSingleton<CatalogPage>();
        Builder.Services.AddTransient<PreviewViewModel>();
        Builder.Services.AddTransient<PreviewPage>();
        Builder.Services.AddSingleton<DevShell>();
        MauiApp Application = Builder.Build();
        ServiceRef.Initialize(Application.Services);
        return Application;
    }
}
