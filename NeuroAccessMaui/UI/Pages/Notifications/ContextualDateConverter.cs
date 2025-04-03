using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	public class ContextualDateConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is DateTime Date)
			{
				DateTime Now = DateTime.Now;

				// Check if the date is today.
				if (Date.Date == Now.Date)
				{
					return ServiceRef.Localizer[nameof(AppResources.Today)];
				}
				// Check if the date is yesterday.
				else if (Date.Date == Now.Date.AddDays(-1))
				{
					return ServiceRef.Localizer[nameof(AppResources.Yesterday)];
				}
				// If the date is in the current year, show month and day.
				else if (Date.Year == Now.Year)
				{
					return Date.ToString("MMMM dd", culture);
				}
				// Otherwise, show the full date.
				else
				{
					return Date.ToString("MM/dd/yyyy", culture);
				}
			}
			return value;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
