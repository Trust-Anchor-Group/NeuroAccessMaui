using System;
using System.Globalization;
using Microsoft.Maui.Controls;


namespace NeuroAccessMaui.UI.Converters
{
	public class EnumToBoolean : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value?.Equals(parameter) ?? false;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return (bool)(value ?? false) ? parameter : null;
		}
	}
}
