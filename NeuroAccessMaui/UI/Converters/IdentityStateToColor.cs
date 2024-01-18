using System.Globalization;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts an identity state to a color.
	/// </summary>
	public class IdentityStateToColor : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is IdentityState State)
				return ToColor(State);
			else
				return Colors.Transparent;
		}

		/// <summary>
		/// Converts a contract state to a representative color.
		/// </summary>
		/// <param name="State">Contract State</param>
		/// <returns>Color</returns>
		public static Color ToColor(IdentityState State)
		{
			return State switch
			{
				IdentityState.Approved => ServiceRef.TagProfile.Theme == AppTheme.Light	? Colors.Green : Colors.LightGreen,
				IdentityState.Created => ServiceRef.TagProfile.Theme == AppTheme.Light ? Colors.DarkOrange: Colors.Orange,
				_ => ServiceRef.TagProfile.Theme == AppTheme.Light ? Colors.Salmon : Colors.LightSalmon,
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
