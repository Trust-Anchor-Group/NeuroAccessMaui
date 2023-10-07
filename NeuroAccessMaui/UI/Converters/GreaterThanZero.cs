using System.Globalization;

namespace NeuroAccessMaui.UI.Converters;

/// <summary>
/// Is true if a property is greater than zero.
/// </summary>
public class GreaterThanZero : IValueConverter, IMarkupExtension
{
	/// <inheritdoc/>
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is decimal v)
		{
			return v > 0;
		}
		else if (value is double d)
		{
			return d > 0;
		}
		else if (value is int i)
		{
			return i > 0;
		}

		return false;
	}

	/// <inheritdoc/>
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException("Feature not implemented.");
	}

	/// <inheritdoc/>
	public object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
}
