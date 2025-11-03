using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts binary data to base64-encoded strings.
	/// </summary>
	[AcceptEmptyServiceProvider]
    public class BinaryToBase64 : IValueConverter, IMarkupExtension
    {
        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is byte[] Bin)
                return System.Convert.ToBase64String(Bin);
            else
                return value;
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
                return System.Convert.FromBase64String(s);
            else
                return value;
        }

        /// <inheritdoc/>
        public object? ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
