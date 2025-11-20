using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts an amount to a color.
	/// </summary>
	[AcceptEmptyServiceProvider]
	public class AmountToColor : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is decimal dec)
				return ToColor(dec >= 0);
			else if (value is double d)
				return ToColor(d >= 0);
			else if (value is float f)
				return ToColor(f >= 0);
			else if (value is int i)
				return ToColor(i >= 0);
			else if (value is long l)
				return ToColor(l >= 0);
			else
				return AppColors.PrimaryForeground;
		}

		/// <summary>
		/// Converts an amount to a representative color.
		/// </summary>
		/// <param name="NonNegative">If amount is not negative.</param>
		/// <returns>Color</returns>
		private static Color? ToColor(bool NonNegative)
		{
			return NonNegative ? AppColors.PrimaryForeground : AppColors.Alert;
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
