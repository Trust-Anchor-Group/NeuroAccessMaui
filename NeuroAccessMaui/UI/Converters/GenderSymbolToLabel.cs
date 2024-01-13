using System.Globalization;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Automatic length if a property is non-empty.
	/// </summary>
	public class GenderSymbolToLabel : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string s)
			{
				if (ISO_5218.LetterToGender(s.ToUpper(CultureInfo.InvariantCulture), out ISO_5218.Record? Rec) && Rec is not null)
					return ServiceRef.Localizer[Rec.LocalizedNameId];
				else
					return s;
			}
			else
				return value ?? string.Empty;
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
