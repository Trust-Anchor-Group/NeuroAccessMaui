using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts decimal editor values while distinguishing clearing from incomplete numeric input.
	/// </summary>
	public class StringToDecimalConverter : IValueConverter
	{
		/// <summary>
		/// Formats a decimal value for display in the editor.
		/// </summary>
		/// <param name="Value">The decimal value.</param>
		/// <param name="TargetType">The binding target type.</param>
		/// <param name="Parameter">The optional converter parameter.</param>
		/// <param name="Culture">The culture used to format the number.</param>
		/// <returns>The formatted number, or an empty string for an unset value.</returns>
		public object? Convert(object? Value, Type TargetType, object? Parameter, CultureInfo Culture)
		{
			if (Value is decimal DecimalValue)
				return DecimalValue.ToString(Culture);

			return string.Empty;
		}

		/// <summary>
		/// Parses editor text, clearing empty input and ignoring invalid nonempty input.
		/// </summary>
		/// <param name="Value">The editor text.</param>
		/// <param name="TargetType">The binding source type.</param>
		/// <param name="Parameter">The optional converter parameter.</param>
		/// <param name="Culture">The culture used to parse the number.</param>
		/// <returns>A decimal, null for cleared input, or Binding.DoNothing for invalid input.</returns>
		public object? ConvertBack(object? Value, Type TargetType, object? Parameter, CultureInfo Culture)
		{
			if (Value is null)
				return null;

			if (Value is string Str)
			{
				if (string.IsNullOrWhiteSpace(Str))
					return null;

				if (decimal.TryParse(Str, NumberStyles.Any, Culture, out decimal DecimalValue))
					return DecimalValue;
			}

			return Binding.DoNothing;
		}
	}
}
