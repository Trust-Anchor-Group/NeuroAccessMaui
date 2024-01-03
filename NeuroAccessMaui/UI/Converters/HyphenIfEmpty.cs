using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// The Localized String corresponding to "N/A" if empty.
	/// </summary>
	public class HyphenIfEmpty : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string s && string.IsNullOrEmpty(s))
				return "-";
			else
				return value ?? string.Empty;
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value ?? string.Empty;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
