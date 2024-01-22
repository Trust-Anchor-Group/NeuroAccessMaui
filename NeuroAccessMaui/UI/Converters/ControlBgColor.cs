using System.Globalization;
using NeuroAccessMaui.UI.Pages.Main.Settings;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts an input control OK state to a background color.
	/// </summary>
	public class ControlBgColor : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool Ok)
				return ToColor(Ok);
			else if (value is Color Color)
				return Color;
			else
				return Colors.Transparent;
		}

		/// <summary>
		/// Converts a control state to a representative color.
		/// </summary>
		/// <param name="Ok">If the control is OK</param>
		/// <returns>Color</returns>
		public static Color ToColor(bool Ok)
		{
			return Ok switch
			{
				true => Colors.Transparent,
				false => SettingsViewModel.CurrentDisplayMode == AppTheme.Light ? Colors.Salmon : Colors.LightSalmon,
			};
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
