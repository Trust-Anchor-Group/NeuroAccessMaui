using System;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;
using Waher.Content.Html.Elements;

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

			SafeAreaMode Mode = GetMode(screen);

			if (Mode == SafeAreaMode.Ignore)
				return new Thickness(0);

			if (Mode == SafeAreaMode.Custom)
				return GetCustomPadding(screen);

			Thickness Insets = ServiceRef.PlatformSpecific.GetInsets();

			return Mode switch
			{
				SafeAreaMode.Top => new Thickness(0, Insets.Top, 0, 0),
				SafeAreaMode.Bottom => new Thickness(0, 0, 0, Insets.Bottom),
				SafeAreaMode.TopAndBottom => new Thickness(0, Insets.Top, 0, Insets.Bottom),
				SafeAreaMode.Right => new Thickness(0, 0, Insets.Right, 0),
				SafeAreaMode.Left => new Thickness(Insets.Left, 0, 0, 0),
				SafeAreaMode.RightAndLeft => new Thickness(Insets.Left, 0, Insets.Right, 0),
				SafeAreaMode.All => Insets,
				_ => Insets
			};
		}

		public static Thickness ResolveInsetsForMode(SafeAreaMode mode)
		{

			if (mode == SafeAreaMode.Ignore)
				return new Thickness(0);

			if (mode == SafeAreaMode.Custom)
				return new Thickness(0);

			Thickness Insets = ServiceRef.PlatformSpecific.GetInsets();

			return mode switch
			{
				SafeAreaMode.Top => new Thickness(0, Insets.Top, 0, 0),
				SafeAreaMode.Bottom => new Thickness(0, 0, 0, Insets.Bottom),
				SafeAreaMode.TopAndBottom => new Thickness(0, Insets.Top, 0, Insets.Bottom),
				SafeAreaMode.Right => new Thickness(0, 0, Insets.Right, 0),
				SafeAreaMode.Left => new Thickness(Insets.Left, 0, 0, 0),
				SafeAreaMode.RightAndLeft => new Thickness(Insets.Left, 0, Insets.Right, 0),
				SafeAreaMode.All => Insets,
				_ => Insets
			};
		}
	}
}
