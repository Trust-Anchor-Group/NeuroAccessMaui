using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts null references into boolean values, with optional inversion.
	/// </summary>
	public class NullToBooleanConverter : IValueConverter
	{
		/// <summary>
		/// Gets or sets a value indicating whether the converter should invert the result.
		/// </summary>
		public bool Invert { get; set; }

		/// <inheritdoc />
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			bool Result = value is not null;
			return this.Invert ? !Result : Result;
		}

		/// <inheritdoc />
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotSupportedException("NullToBooleanConverter does not support ConvertBack.");
		}
	}
}
