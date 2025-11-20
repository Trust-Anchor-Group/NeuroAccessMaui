using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts <see cref="DateTime"/> values into localized strings, optionally forcing UTC output.
	/// Date-only formatting is inferred from the supplied <see cref="DateTime"/> value instead of a parameter.
	/// </summary>
	public class DateTimeToStringConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not DateTime DateTime)
				return string.Empty;

			bool ForceUtc = IsForceUtc(parameter);

			DateTime Formatted = ForceUtc ? DateTime.ToUniversalTime() : DateTime;
			bool DateOnly = IsDateOnly(Formatted);

			string Result = DateOnly
				? Formatted.ToString("d", culture)
				: Formatted.ToString("G", culture);

			if (Formatted.Kind == DateTimeKind.Utc || ForceUtc)
				Result = $"{Result} (UTC)";

			return Result;
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string s && DateTime.TryParse(s, culture, DateTimeStyles.None, out DateTime Parsed))
				return Parsed;

			return DateTime.MinValue;
		}

		private static bool IsDateOnly(DateTime dateTime)
		{
			return dateTime.TimeOfDay == TimeSpan.Zero;
		}

		private static bool IsForceUtc(object? parameter)
		{
			if (parameter is null)
				return false;

			if (parameter is bool BoolParam)
				return BoolParam;

			if (parameter is string StrParam)
				return string.Equals(StrParam, "utc", StringComparison.OrdinalIgnoreCase);

			return false;
		}
	}
}
