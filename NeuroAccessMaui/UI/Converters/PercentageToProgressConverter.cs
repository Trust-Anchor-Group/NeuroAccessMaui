using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Converters
{

    /// <summary>
    /// Converts a percentage value (0-100) to a progress value (0.0-1.0).
    /// </summary>
    public class PercentageToProgressConverter : IValueConverter
    {
        /// <summary>
        /// Converts a percentage value to a progress value.
        /// </summary>
        /// <param name="value">The percentage value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A progress value between 0.0 and 1.0.</returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int Percentage)
                return (double)Percentage / 100.0;
            return 0.0;
        }

        /// <summary>
        /// Converts a progress value back to a percentage value.
        /// </summary>
        /// <param name="value">The progress value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A percentage value between 0 and 100.</returns>
        /// <exception cref="NotImplementedException">Thrown when the method is not implemented.</exception>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
