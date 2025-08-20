using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Converters
{
    /// <summary>
    /// Returns true if bound string value is NOT equal (case-sensitive by default) to ConverterParameter.
    /// If IgnoreCase (optional second parameter) is specified (e.g. parameter="TargetValue|ignorecase"), comparison is case-insensitive.
    /// </summary>
    public class StringNotEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is null)
                return true; // no parameter -> always true (not equal by definition)

            string? input = value?.ToString();
            string paramRaw = parameter.ToString() ?? string.Empty;
            bool ignoreCase = false;

            // Allow syntax: "Value|ignorecase"
            int pipe = paramRaw.IndexOf('|');
            if (pipe >= 0)
            {
                string flags = paramRaw[(pipe + 1)..];
                paramRaw = paramRaw[..pipe];
                ignoreCase = flags.Contains("ignorecase", StringComparison.OrdinalIgnoreCase);
            }

            StringComparison cmp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return !string.Equals(input, paramRaw, cmp);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
