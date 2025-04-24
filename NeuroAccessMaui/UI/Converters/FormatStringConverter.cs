using System.Globalization;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.EventLog;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// A converter that formats a string using a format string and arguments.
	/// </summary>
	public class FormatStringConverter : IMultiValueConverter
	{
		// values[0] is the format string
		// values[1..] are the format arguments
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values is null || values.Length == 0)
				return string.Empty;

			string Format = values[0]?.ToString() ?? string.Empty;
			object[] Args = values.Skip(1).ToArray();

			try
			{
				return string.Format(culture, Format, Args);
			}
			catch(Exception Ex)
			{
				// Fallback: if something went wrong, just return format + args joined
				ServiceRef.LogService.LogWarning($"FormatStringConverter failed: {Ex.Message}");
				return Format + " " + string.Join(", ", Args);
			}
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}
