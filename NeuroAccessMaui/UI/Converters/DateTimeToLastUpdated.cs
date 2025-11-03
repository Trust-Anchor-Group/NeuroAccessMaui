
using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Converters
{
	public class DateTimeToLastUpdatedConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not DateTime DtNullable)
				return ServiceRef.Localizer[nameof(AppResources.Never)].Value.ToLower(culture);

			DateTime Dt = DtNullable.ToLocalTime(); // ensure local time
			DateTime Now = DateTime.Now;
			TimeSpan Span = Now - Dt;

			string TimeString;

			/*
			if (Span.TotalMinutes < 1)
				TimeString = ServiceRef.Localizer[nameof(AppResources.Now)].Value.ToLower(culture);
			else if (Span.TotalHours < 1)
				TimeString = ServiceRef.Localizer[nameof(AppResources.MinutesAgoFormat), false, (int)Span.TotalMinutes];
			else if (Span.TotalDays < 1)
				TimeString = ServiceRef.Localizer[nameof(AppResources.HoursAgoFormat), false, (int)Span.TotalHours];
			*/
			if (Span.TotalDays < 1)
				TimeString = Dt.ToString("t", culture); // "t" = short time, respects locale
			else
				TimeString = Dt.ToString("d", culture); // "d" = short date, respects locale
			return ServiceRef.Localizer[nameof(AppResources.LastUpdatedFormat), false, TimeString];
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

}
