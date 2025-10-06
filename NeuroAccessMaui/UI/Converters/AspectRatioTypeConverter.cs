using System;
using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui.UI.Converters
{
	public class AspectRatioTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
		{
			string? ValueString = (value?.ToString()) ?? throw new ArgumentNullException(nameof(value));
			ValueString = ValueString.Trim();

			string[] Parts = ValueString.Split(':');
			if (Parts.Length == 2)
			{
				if (double.TryParse(Parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out double Width)
					&& double.TryParse(Parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out double Height)
					&& Height != 0)
				{
					return Width / Height;
				}
				else
				{
					throw new FormatException($"Cannot parse aspect ratio from '{ValueString}'");
				}
			}
			else if (double.TryParse(ValueString, NumberStyles.Number, CultureInfo.InvariantCulture, out double Result))
			{
				return Result;
			}
			else
			{
				throw new FormatException($"Cannot parse aspect ratio from '{ValueString}'");
			}
		}

		public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
		{
			throw new NotSupportedException();
		}
	}
}
