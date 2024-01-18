using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Automatic length if a property is non-empty.
	/// </summary>
	public class AutoIfNonEmpty : IValueConverter, IMarkupExtension
	{
		private static readonly GridLength zeroLength = new(0, GridUnitType.Absolute);

		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string s)
				return !string.IsNullOrEmpty(s) ? GridLength.Auto : zeroLength;
			else if (value is bool b)
				return b ? GridLength.Auto : zeroLength;
			else
				return value is not null ? GridLength.Auto : zeroLength;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value?.ToString() ?? string.Empty;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
