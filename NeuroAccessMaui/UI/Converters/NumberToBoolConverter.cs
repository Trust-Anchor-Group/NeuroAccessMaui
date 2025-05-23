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

				NumericValue = value switch
				{
					int Int => Int,
					long Long => Long,
					float Float => (double)Float,
					double Double => Double,
					decimal Dec => (double)Dec,
					short Short => Short,
					byte Byte => Byte,
					sbyte Sbyte => Sbyte,
					uint Uint => Uint,
					ushort Ushort => Ushort,
					ulong Ulong => Ulong,
					_ => System.Convert.ToDouble(value, culture),// If the value is not a recognized numeric type,
																 // attempt a generic conversion.
				};

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
