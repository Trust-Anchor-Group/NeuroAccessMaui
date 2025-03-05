using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.UI.Pages.Applications.ApplyId
{
	public class IdentityDocumentTypeToFrontVisibilityConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			// Show front image if any document type is selected except None.
			if (value is IdentityDocumentType Type)
				return Type != IdentityDocumentType.None;
			return false;
		}
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			throw new NotImplementedException();
	}
}
