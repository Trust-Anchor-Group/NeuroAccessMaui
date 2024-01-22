using System.Globalization;
using NeuroAccessMaui.UI.Pages.Main.Settings;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Transparent color, if value is null.
	/// </summary>
	public class TransparentIfNull : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is null)
				return Colors.Transparent;

			try
			{
				string Key = SettingsViewModel.CurrentDisplayMode == AppTheme.Light ? "NormalEditForegroundLight" : "NormalEditForegroundDark";
				if (!(App.Current?.Resources.TryGetValue(Key, out object Obj) ?? false))
					return Colors.Transparent;

				if (Obj is Color Color)
					return Color;
				else
					return Colors.Transparent;
			}
			catch (Exception)
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
