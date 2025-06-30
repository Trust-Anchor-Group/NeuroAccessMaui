namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a nullable DateTime to a DateTime, defaulting to DateTime.Now if null.
	/// </summary>
	public class NullableDateTimeConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		{
			// Ensure value is DateTime? and provide DateTime.Now if null
			return (value as DateTime?) ?? DateTime.Today.Date;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
		{
			// If the value is a DateTime, return it; otherwise, return null
			return value is DateTime DateTime ? DateTime : null;
		}
	}
}
