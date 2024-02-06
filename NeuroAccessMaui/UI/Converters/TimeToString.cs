using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
    /// <summary>
    /// Converts the time part of a <see cref="DateTime"/> value, or a <see cref="TimeSpan"/> value
	/// to a <see cref="String"/> value. If the value is <see cref="DateTime.MinValue"/> or <see cref="DateTime.MaxValue"/>,
	/// the empty string is returned.
    /// </summary>
    public class TimeToString : IValueConverter, IMarkupExtension
    {
        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime TP)
            {
                if (TP == DateTime.MinValue || TP == DateTime.MaxValue)
                    return string.Empty;
                else
                    return TP.ToLongTimeString();
            }
			else if (value is TimeSpan TS)
				return TS.ToString();
			else
				return string.Empty;
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && TimeSpan.TryParse(s, out TimeSpan TS))
                return TS;
            else
                return TimeSpan.Zero;
        }

        /// <inheritdoc/>
        public object? ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
