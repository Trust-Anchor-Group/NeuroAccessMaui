using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using NeuroAccessMaui.Services;
using System;
using System.Linq;

namespace NeuroAccessMaui.MarkupExtensions
{
	public enum InsetsType
	{
		Top,
		Bottom,
		TopAndBottom,
		Right,
		Left,
		RightAndLeft,
		All
	}

	[AcceptEmptyServiceProvider]
	[ContentProperty(nameof(Type))]
	public class SafeInsetsExtension : IMarkupExtension
	{
		public InsetsType Type { get; set; } = InsetsType.TopAndBottom;

		public object ProvideValue(IServiceProvider serviceProvider)
		{
			Thickness Insets = ServiceRef.PlatformSpecific.GetInsets();


			return this.Type switch
			{
				InsetsType.Top => new Thickness(0, Insets.Top, 0, 0),
				InsetsType.Bottom => new Thickness(0, 0, 0, Insets.Bottom),
				InsetsType.TopAndBottom => new Thickness(0, Insets.Top, 0, Insets.Bottom),
				InsetsType.Right => new Thickness(0, 0, Insets.Right, 0),
				InsetsType.Left => new Thickness(Insets.Left, 0, 0, 0),
				InsetsType.RightAndLeft => new Thickness(Insets.Left, 0, Insets.Right, 0),
				InsetsType.All => new Thickness(Insets.Left, Insets.Top, Insets.Right, Insets.Bottom),
				_ => new Thickness(0)
			};
		}
	}
}
