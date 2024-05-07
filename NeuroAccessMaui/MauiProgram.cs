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
using ZXing.Net.Maui.Controls;

#if ANDROID
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
#endif

namespace NeuroAccessMaui
{
	public static class MauiProgram
	{
		private static MauiApp? instance;

		public static MauiApp CreateMauiApp()
		{
			InitMauiControlsHandlers();

			MauiAppBuilder Builder = MauiApp.CreateBuilder();

			Builder.UseMauiApp<App>();

			Builder.ConfigureFonts(fonts =>
			{
				fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
				//fonts.AddFont("SpaceGrotesk-SemiBold.ttf", "SpaceGroteskSemiBold");
				fonts.AddFont("SpaceGrotesk-Medium.ttf", "SpaceGroteskMedium");
				fonts.AddFont("SpaceGrotesk-Regular.ttf", "SpaceGroteskRegular");
				//fonts.AddFont("SpaceGrotesk-Light.ttf", "SpaceGroteskLight");
				fonts.AddFont("NHaasGroteskTXPro-75Bd.ttf", "HaasGroteskBold");
				//fonts.AddFont("NHaasGroteskTXPro-65Md.ttf", "HaasGroteskMedium");
				fonts.AddFont("NHaasGroteskTXPro-55Rg.ttf", "HaasGroteskRegular");
			});

			// NuGets
			Builder.ConfigureMopups();
			Builder.UseMauiCommunityToolkit();
			Builder.UseMauiCommunityToolkitMarkup();
			Builder.UseBarcodeReader();

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

			instance = Builder.Build();

			return instance;
		}

		/// <summary>
		/// Current MAUI app instance.
		/// </summary>
		public static MauiApp? Current => instance;

		private static void InitMauiControlsHandlers()
		{
#if IOS
			ScrollViewHandler.Mapper.AppendToMapping("BouncesScrollViewHandler", (handler, view) =>
			{
				handler.PlatformView.Bounces = false;
			});

			CollectionViewHandler.Mapper.AppendToMapping("BouncesCollectionViewHandler", (handler, view) =>
			{
				//!!! not sure how this is done here yet
				//!!! handler.ViewController.PlatformView.Bounces = false;
			});

			Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BorderStyleEntryHandler", (handler, view) =>
			{
				handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
			});
#elif ANDROID
			ScrollViewHandler.Mapper.AppendToMapping("OverScrollModeScrollViewHandler", (handler, view) =>
			{
				handler.PlatformView.OverScrollMode = Android.Views.OverScrollMode.Never;
			});

			CollectionViewHandler.Mapper.AppendToMapping("OverScrollModeCollectionViewHandler", (handler, view) =>
			{
				handler.PlatformView.OverScrollMode = Android.Views.OverScrollMode.Never;
			});

			EntryHandler.Mapper.AppendToMapping("NoUnderlineEntryHandler", (handler, view) =>
			{
				handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
			});
#endif
		}
	}
}
