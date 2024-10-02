using System;
using System.Globalization;
using Microsoft.Maui.Controls;


namespace NeuroAccessMaui.UI.Converters
{
	public class EnumToVisibility : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value == null || parameter == null)
				return false;

			return value.Equals(parameter);
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
