using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.UI.Pages.Applications.ApplyId
{
	public class IdentityDocumentTypeToBackVisibilityConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			// Show back image only if National ID or Driver's Licence is selected.
			if (value is IdentityDocumentType Type)
				return Type is IdentityDocumentType.NationalId or IdentityDocumentType.DriverLicense;
			return false;
		}
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			throw new NotImplementedException();
	}
}
