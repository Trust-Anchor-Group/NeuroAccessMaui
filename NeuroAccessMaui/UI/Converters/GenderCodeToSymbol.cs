﻿using System.Globalization;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a gender code to a Unicode symbol.
	/// </summary>
	public class GenderCodeToSymbol : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			ISO_5218_Gender? Rec;

			if (value is string s)
			{
				if (ISO_5218.LetterToGender(s.ToUpper(CultureInfo.InvariantCulture), out Rec) && Rec is not null)
					return new string(Rec.Unicode, 1);
				else if (int.TryParse(s, out int i) && ISO_5218.CodeToGender(i, out Rec) && Rec is not null)
					return new string(Rec.Unicode, 1);
				else
					return s;
			}
			else if (value is int Code && ISO_5218.CodeToGender(Code, out Rec) && Rec is not null)
				return new string(Rec.Unicode, 1);
			else
				return value ?? string.Empty;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value?.ToString() ?? string.Empty;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
