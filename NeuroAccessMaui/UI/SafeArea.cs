using System;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI
{
	public enum SafeAreaMode
	{
		Default,
		Ignore,
		Custom,
		Top,
		Bottom,
		TopAndBottom,
		Right,
		Left,
		RightAndLeft,
		All
	}

	public static class SafeArea
	{
		public static readonly BindableProperty ModeProperty = BindableProperty.CreateAttached(
			"Mode",
			typeof(SafeAreaMode),
			typeof(SafeArea),
			SafeAreaMode.Default);

		public static readonly BindableProperty CustomPaddingProperty = BindableProperty.CreateAttached(
			"CustomPadding",
			typeof(Thickness),
			typeof(SafeArea),
			new Thickness(0));

		public static SafeAreaMode GetMode(BindableObject view)
		{
			ArgumentNullException.ThrowIfNull(view);
			return (SafeAreaMode)view.GetValue(ModeProperty);
		}

		public static void SetMode(BindableObject view, SafeAreaMode value)
		{
			ArgumentNullException.ThrowIfNull(view);
			view.SetValue(ModeProperty, value);
		}

		public static Thickness GetCustomPadding(BindableObject view)
		{
			ArgumentNullException.ThrowIfNull(view);
			return (Thickness)view.GetValue(CustomPaddingProperty);
		}

		public static void SetCustomPadding(BindableObject view, Thickness value)
		{
			ArgumentNullException.ThrowIfNull(view);
			view.SetValue(CustomPaddingProperty, value);
		}

		public static Thickness ResolveInsetsFor(BindableObject screen)
		{
			ArgumentNullException.ThrowIfNull(screen);

			SafeAreaMode mode = GetMode(screen);

			if (mode == SafeAreaMode.Ignore)
				return new Thickness(0);

			if (mode == SafeAreaMode.Custom)
				return GetCustomPadding(screen);

			Thickness insets = ServiceRef.PlatformSpecific.GetInsets();

			return mode switch
			{
				SafeAreaMode.Top => new Thickness(0, insets.Top, 0, 0),
				SafeAreaMode.Bottom => new Thickness(0, 0, 0, insets.Bottom),
				SafeAreaMode.TopAndBottom => new Thickness(0, insets.Top, 0, insets.Bottom),
				SafeAreaMode.Right => new Thickness(0, 0, insets.Right, 0),
				SafeAreaMode.Left => new Thickness(insets.Left, 0, 0, 0),
				SafeAreaMode.RightAndLeft => new Thickness(insets.Left, 0, insets.Right, 0),
				SafeAreaMode.All => insets,
				_ => insets
			};
		}
	}
}
