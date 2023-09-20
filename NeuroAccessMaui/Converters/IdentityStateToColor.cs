using System.Globalization;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Converters;

/// <summary>
/// Converts an identity state to a color.
/// </summary>
public class IdentityStateToColor : IValueConverter, IMarkupExtension
{
	/// <inheritdoc/>
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is IdentityState State)
			return ToColor(State);
		else
			return Color.Transparent;
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
			IdentityState.Approved => Color.LightGreen,
			IdentityState.Created => Color.LightYellow,
			_ => Color.LightSalmon,
		};
	}

	/// <inheritdoc/>
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value;
	}

	/// <inheritdoc/>
	public object ProvideValue(System.IServiceProvider serviceProvider)
	{
		return this;
	}
}
