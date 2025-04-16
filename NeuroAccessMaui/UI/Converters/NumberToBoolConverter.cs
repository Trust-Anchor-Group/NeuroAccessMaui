using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	public class NumberToBoolConverter : IValueConverter
	{
		/// <summary>
		/// Converts a numeric value (decimal, double, etc.) into a boolean.
		/// Returns true if the numeric value is nonzero; otherwise, false.
		/// </summary>
		/// <param name="value">The value to convert. Expected to be numeric.</param>
		/// <param name="targetType">The target type (boolean).</param>
		/// <param name="parameter">Optional parameter, not used here.</param>
		/// <param name="culture">The culture to use.</param>
		/// <returns>A boolean value indicating whether the provided numeric value is nonzero.</returns>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is null)
				return false;

			try
			{
				// Convert the incoming value to a double. This handles common numeric types.
				double NumericValue = 0;

				switch (value)
				{
					case int i:
						NumericValue = i;
						break;
					case long l:
						NumericValue = l;
						break;
					case float f:
						NumericValue = f;
						break;
					case double d:
						NumericValue = d;
						break;
					case decimal dec:
						NumericValue = (double)dec;
						break;
					case short s:
						NumericValue = s;
						break;
					case byte b:
						NumericValue = b;
						break;
					case sbyte sb:
						NumericValue = sb;
						break;
					case uint ui:
						NumericValue = ui;
						break;
					case ushort us:
						NumericValue = us;
						break;
					case ulong ul:
						NumericValue = ul;
						break;
					default:
						// If the value is not a recognized numeric type,
						// attempt a generic conversion.
						NumericValue = System.Convert.ToDouble(value, culture);
						break;
				}

				// If the numeric value is nonzero, return true; otherwise, false.
				return NumericValue != 0.0;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// ConvertBack is not implemented.
		/// </summary>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
