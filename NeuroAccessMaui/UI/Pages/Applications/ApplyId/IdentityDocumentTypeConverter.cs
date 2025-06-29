using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Applications.ApplyId
{
	public class IdentityDocumentTypeConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is IdentityDocumentType Type)
			{
				return Type switch
				{
					IdentityDocumentType.None => ServiceRef.Localizer[nameof(AppResources.None)], 
					IdentityDocumentType.Passport => ServiceRef.Localizer[nameof(AppResources.Passport)],
					IdentityDocumentType.NationalId => ServiceRef.Localizer[nameof(AppResources.NationalId)],
					IdentityDocumentType.DriverLicense => ServiceRef.Localizer[nameof(AppResources.DriverLicense)],
					_ => Type.ToString()
				};
			}
			return value;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
