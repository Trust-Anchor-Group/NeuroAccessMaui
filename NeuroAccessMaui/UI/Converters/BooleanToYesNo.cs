using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a boolean value to either Yes or No
	/// </summary>
	[AcceptEmptyServiceProvider]
	public class BooleanToYesNo : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool b)
				return b.ToYesNo();
			else
				return string.Empty;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value is string s && s == ServiceRef.Localizer[nameof(AppResources.Yes)];
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
