using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
using Mopups.Hosting;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Localization;
using NeuroAccessMaui.Services.Push;
using ZXing.Net.Maui.Controls;
using Microsoft.Maui.Platform;
using SkiaSharp.Views.Maui.Controls.Hosting;
using SkiaSharp.Views.Maui.Controls;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.Services.Xml;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.Services.UI.Toasts;
using Waher.Runtime.Inventory;

#if DEBUG
using DotNet.Meteor.HotReload.Plugin;
using SkiaSharp.Views.Maui.Handlers;
using Microsoft.Maui.Hosting;
#endif
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

			Builder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<AutoHeightSKCanvasView, SKCanvasViewHandler>();
				handlers.AddHandler(typeof(AspectRatioLayout), typeof(LayoutHandler));
			});

			Builder.ConfigureLifecycleEvents(lifecycle =>
			{
#if ANDROID
				lifecycle.AddAndroid(android =>
					android.OnResume(activity =>
					{
						// App has resumed on Android
						App.RaiseAppActivated();
					}));
#elif IOS
				lifecycle.AddiOS(ios =>
                ios.OnActivated(app =>
                {
                    // App has resumed on iOS
                    App.RaiseAppActivated();
                }));
#endif
			});

			Builder.UseSkiaSharp();
			//Builder.RegisterFirebaseServices();
#if DEBUG
			Builder.EnableHotReload();
			Builder.Logging.AddDebug();
#endif

			Builder.ConfigureFonts(fonts =>
			{
				fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
				fonts.AddFont("SpaceGrotesk-SemiBold.ttf", "SpaceGroteskSemiBold");
				fonts.AddFont("SpaceGrotesk-Medium.ttf", "SpaceGroteskMedium");
				fonts.AddFont("SpaceGrotesk-Regular.ttf", "SpaceGroteskRegular");
				fonts.AddFont("NHaasGroteskTXPro-75Bd.ttf", "HaasGroteskBold");
				fonts.AddFont("NHaasGroteskTXPro-55Rg.ttf", "HaasGroteskRegular");
			});

			// NuGets
			Builder.ConfigureMopups();
#if DEBUG
			Builder.UseMauiCommunityToolkit();
#else
			Builder.UseMauiCommunityToolkit(Options =>
			{
				Options.SetShouldSuppressExceptionsInAnimations(true);
				Options.SetShouldSuppressExceptionsInBehaviors(true);
				Options.SetShouldSuppressExceptionsInConverters(true);
			});
#endif
			Builder.UseMauiCommunityToolkitMarkup();
			Builder.UseBarcodeReader();
			Builder.UseLocalizationManager<AppResources>();

			// Register platform specific implementation
#if ANDROID || IOS || WINDOWS
			Builder.Services.AddSingleton<IPlatformSpecific, PlatformSpecific>();
#endif

			Builder.RegisterTypes();
			Builder.RegisterPages();

			instance = Builder.Build();

			// Setup the service provider
			ServiceRef.Provider = new HybridServiceProvider(instance.Services);

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
			PickerHandler.Mapper.AppendToMapping("NoUnderlinePickerHandler", (handler, view) =>
			{
				handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
			});
			TimePickerHandler.Mapper.AppendToMapping("NoUnderlineTimePickerHandler", (handler, view) =>
			{
				handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
			});
			DatePickerHandler.Mapper.AppendToMapping("NoUnderlineDatePickerHandler", (handler, view) =>
			{
				handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
			});
			ImageHandler.Mapper.PrependToMapping(nameof(Microsoft.Maui.IImage.Source), (handler, view) =>
			{
				handler.PlatformView?.Clear();
			});
#endif
		}
	}
}
