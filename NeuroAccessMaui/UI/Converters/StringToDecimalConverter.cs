using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Converters
{
	public class StringToDecimalConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is decimal decimalValue)
				return decimalValue.ToString(culture);

			return string.Empty;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			// First, check if the input is null or whitespace and handle it accordingly
			if (value is string str)
			{
				if (string.IsNullOrWhiteSpace(str))
					return null;  // Return null if the string is empty or whitespace

				if (decimal.TryParse(str, NumberStyles.Any, culture, out decimal decimalValue))
					return decimalValue;
			}

			return null;  // Return null if the input is null or not a valid decimal
		}
	}
}
