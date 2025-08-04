using System.Globalization;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a country code to a country flag and name.
	/// </summary>
	[AcceptEmptyServiceProvider]
	public class CountryCodeToFlagAndName : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string Code && ISO_3166_1.TryGetCountryByCode(Code, out ISO_3166_Country? Country) && Country is not null)
				return Country.EmojiInfo.Unicode + "\t" + Country.Name;
			else
				return value ?? string.Empty;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value;
		}

		/// <inheritdoc/>
		public object ProvideValue(System.IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
