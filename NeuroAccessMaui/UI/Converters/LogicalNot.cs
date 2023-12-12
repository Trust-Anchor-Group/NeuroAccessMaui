using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts, or flips, a boolean value to it's inverse value. I.e. turns <c>true</c> to <c>false</c>, or the other way around.
	/// </summary>
	public class LogicalNot : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool b)
				return !b;
			else
				return false;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
