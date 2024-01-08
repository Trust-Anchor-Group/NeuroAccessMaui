using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Is true if a property is non-empty.
	/// </summary>
	public class OnlyShowNonEmpty : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string s)
				return !string.IsNullOrEmpty(s);
			else if (value is bool b)
				return b;
			else
				return value is not null;
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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
