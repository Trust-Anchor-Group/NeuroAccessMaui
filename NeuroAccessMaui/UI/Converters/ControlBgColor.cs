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
			if(value is null)
				return Colors.Transparent;

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
		public static Color? ToColor(bool Ok)
		{
			if(!Ok)
				return AppColors.ErrorBackground;
			try
			{
				string key = SettingsViewModel.CurrentDisplayMode == AppTheme.Light ? "SecondaryBackgroundLight" : "SecondaryBackgroundDark";
				if (!(App.Current?.Resources.TryGetValue(key, out object Obj) ?? false))
					return Colors.Transparent;

				if (Obj is Color Color)
					return Color;
				else
					return Colors.Transparent;
			} catch (Exception)
			{
				return Colors.Transparent;
			}
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
