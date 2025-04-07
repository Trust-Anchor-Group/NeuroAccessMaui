using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// A value converter that converts a <see cref="DateTime"/> to a localized string representation.
	/// If a parameter is supplied and evaluates to <c>true</c>, only the date portion of the 
	/// <see cref="DateTime"/> is displayed using the short date format (e.g., "MM/dd/yyyy" in US culture).
	/// If the parameter is not supplied or is <c>false</c>, the converter displays both date and time 
	/// using a localized pattern (long date + short time).
	/// 
	/// Examples:
	/// - With parameter = true: Displays only the date (e.g., "11/30/2024")
	/// - With parameter = false or omitted: Displays date and time (e.g., "November 30, 2024 1:23 PM")
	/// </summary>
	public class DateTimeToStringConverter : IValueConverter
	{
		/// <summary>
		/// Converts a DateTime to a localized string. If the parameter indicates date-only mode,
		/// it shows only the date; otherwise, it shows both date and time.
		/// </summary>
		/// <param name="value">The DateTime value to convert.</param>
		/// <param name="targetType">The target type (string).</param>
		/// <param name="parameter">An optional parameter that, if true, indicates only a date should be shown.</param>
		/// <param name="culture">The culture used for localization.</param>
		/// <returns>A localized string representing the date and optionally time.</returns>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not DateTime DateTime)
				return string.Empty;

			bool DateOnly = false;

			if (parameter is bool BoolParam)
			{
				DateOnly = BoolParam;
			}
			else if (parameter is string StrParam && bool.TryParse(StrParam, out bool Parsed))
			{
				DateOnly = Parsed;
			}

			// If DateOnly is true, use short date format; otherwise, use date and time.
			return DateOnly
				 ? DateTime.ToString("d", culture)    // Short date pattern
				 : DateTime.ToString("G", culture);   // short date + short time pattern
		}

		/// <summary>
		/// Attempts to parse a string back into a DateTime.
		/// </summary>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string s && DateTime.TryParse(s, culture, DateTimeStyles.None, out DateTime dt))
				return dt;

			return DateTime.MinValue;
		}
	}
}
