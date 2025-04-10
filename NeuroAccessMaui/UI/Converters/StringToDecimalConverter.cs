using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Converters
{
	public class StringToDecimalConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is decimal DecimalValue)
				return DecimalValue.ToString(culture);

			return string.Empty;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			// First, check if the input is null or whitespace and handle it accordingly
			if (value is string Str)
			{
				if (string.IsNullOrWhiteSpace(Str))
					return null;  // Return null if the string is empty or whitespace

				if (decimal.TryParse(Str, NumberStyles.Any, culture, out decimal DecimalValue))
					return DecimalValue;
			}

			return null;  // Return null if the input is null or not a valid decimal
		}
	}
}
