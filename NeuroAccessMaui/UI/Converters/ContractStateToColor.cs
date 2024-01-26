using System.Globalization;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a contract state to a color.
	/// </summary>
	public class ContractStateToColor : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is ContractState State)
				return ToColor(State);
			else
				return Colors.Transparent;
		}

		/// <summary>
		/// Converts a contract state to a representative color.
		/// </summary>
		/// <param name="State">Contract State</param>
		/// <returns>Color</returns>
		public static Color ToColor(ContractState State)
		{
			return State switch
			{
				ContractState.Signed => SettingsViewModel.CurrentDisplayMode == AppTheme.Light ? Colors.Green : Colors.LightGreen,
				ContractState.Proposed or
				ContractState.Approved or
				ContractState.BeingSigned => SettingsViewModel.CurrentDisplayMode == AppTheme.Light ? Colors.DarkOrange : Colors.Orange,
				_ => SettingsViewModel.CurrentDisplayMode == AppTheme.Light ? Colors.Salmon : Colors.LightSalmon,
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
