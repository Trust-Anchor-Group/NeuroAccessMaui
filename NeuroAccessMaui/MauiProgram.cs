using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.LifecycleEvents;
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
using SkiaSharp.Views.Maui.Handlers;


#if DEBUG
using DotNet.Meteor.HotReload.Plugin;
using SkiaSharp.Views.Maui.Handlers;
using Microsoft.Maui.Hosting;
#endif
#if ANDROID
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
#endif
#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;
using WinThickness = Microsoft.UI.Xaml.Thickness;
using WinSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
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
#elif WINDOWS
				lifecycle.AddWindows(windows =>
					windows.OnActivated((window, args) =>
					{
						// App has resumed on Windows
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
				handler.PlatformView.Bounces = false;
			});

			Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("BorderStyleEntryHandler", (handler, view) =>
			{
				handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
			});

			EditorHandler.Mapper.AppendToMapping("BorderlessEditorHandler", (handler, view) =>
			{
				handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
			});

			PickerHandler.Mapper.AppendToMapping("BorderlessPickerHandler", (handler, view) =>
			{
				handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
			});

			DatePickerHandler.Mapper.AppendToMapping("BorderlessDatePickerHandler", (handler, view) =>
			{
				handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
				handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
			});

			TimePickerHandler.Mapper.AppendToMapping("BorderlessTimePickerHandler", (handler, view) =>
			{
				handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
				handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
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
#elif WINDOWS
			ScrollViewHandler.Mapper.AppendToMapping("HideScrollBarsScrollViewHandler", (handler, view) =>
			{
				if (handler.PlatformView is ScrollViewer ScrollViewer)
				{
					ScrollViewer.VerticalScrollBarVisibility = WinScrollBarVisibility.Hidden;
					ScrollViewer.HorizontalScrollBarVisibility = WinScrollBarVisibility.Hidden;
				}
			});

			CollectionViewHandler.Mapper.AppendToMapping("HideScrollBarsCollectionViewHandler", (handler, view) =>
			{
				if (handler.PlatformView is ListViewBase ListViewBase)
				{
					ScrollViewer? NestedScrollViewer = FindDescendantScrollViewer(ListViewBase);
					if (NestedScrollViewer is not null)
					{
						NestedScrollViewer.VerticalScrollBarVisibility = WinScrollBarVisibility.Hidden;
						NestedScrollViewer.HorizontalScrollBarVisibility = WinScrollBarVisibility.Hidden;
					}
				}
			});

			EntryHandler.Mapper.AppendToMapping("NoBorderEntryHandler", (handler, view) =>
			{
				if (handler.PlatformView is TextBox TextBox)
				{
					TextBox.BorderThickness = new WinThickness(0);
					TextBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					TextBox.Background = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					TextBox.GotFocus += (sender, args) =>
					{
						TextBox.BorderThickness = new WinThickness(0);
						TextBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
					TextBox.Loaded += (sender, args) =>
					{
						TextBox.BorderThickness = new WinThickness(0);
						TextBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
				}
			});

			EditorHandler.Mapper.AppendToMapping("NoBorderEditorHandler", (handler, view) =>
			{
				if (handler.PlatformView is TextBox TextBox)
				{
					TextBox.BorderThickness = new WinThickness(0);
					TextBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					TextBox.Background = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					TextBox.GotFocus += (sender, args) =>
					{
						TextBox.BorderThickness = new WinThickness(0);
						TextBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
					TextBox.Loaded += (sender, args) =>
					{
						TextBox.BorderThickness = new WinThickness(0);
						TextBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
				}
			});

			PickerHandler.Mapper.AppendToMapping("NoBorderPickerHandler", (handler, view) =>
			{
				if (handler.PlatformView is ComboBox ComboBox)
				{
					ComboBox.BorderThickness = new WinThickness(0);
					ComboBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					ComboBox.Background = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					ComboBox.GotFocus += (sender, args) =>
					{
						ComboBox.BorderThickness = new WinThickness(0);
						ComboBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
					ComboBox.Loaded += (sender, args) =>
					{
						ComboBox.BorderThickness = new WinThickness(0);
						ComboBox.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
				}
			});

			DatePickerHandler.Mapper.AppendToMapping("NoBorderDatePickerHandler", (handler, view) =>
			{
				if (handler.PlatformView is CalendarDatePicker CalendarDatePicker)
				{
					CalendarDatePicker.BorderThickness = new WinThickness(0);
					CalendarDatePicker.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					CalendarDatePicker.Background = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					CalendarDatePicker.GotFocus += (sender, args) =>
					{
						CalendarDatePicker.BorderThickness = new WinThickness(0);
						CalendarDatePicker.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
					CalendarDatePicker.Loaded += (sender, args) =>
					{
						CalendarDatePicker.BorderThickness = new WinThickness(0);
						CalendarDatePicker.BorderBrush = new WinSolidColorBrush(Microsoft.UI.Colors.Transparent);
					};
				}
			});
			// Windows TimePicker styling skipped (native control lacks BorderThickness). Add custom template if needed.
#endif
		}

#if WINDOWS
		private static ScrollViewer? FindDescendantScrollViewer(DependencyObject Root)
		{
			int Count = VisualTreeHelper.GetChildrenCount(Root);
			for (int i = 0; i < Count; i++)
			{
				DependencyObject Child = VisualTreeHelper.GetChild(Root, i);
				if (Child is ScrollViewer Found)
				{
					return Found;
				}

				ScrollViewer? Nested = FindDescendantScrollViewer(Child);
				if (Nested is not null)
				{
					return Nested;
				}
			}

			return null;
		}
#endif
	}
}
