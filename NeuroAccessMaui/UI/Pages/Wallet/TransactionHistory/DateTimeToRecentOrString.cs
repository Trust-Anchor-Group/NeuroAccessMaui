
using System.Globalization;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory
{
	public class DateTimeToRecentOrString : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not DateTime DtNullable)
				return ServiceRef.Localizer[nameof(AppResources.Never)].Value.ToLower(culture);

			DateTime Dt = DtNullable.ToUniversalTime(); // ensure local time
			DateTime Now = DateTime.UtcNow;
			TimeSpan Span = Now - Dt;

			string TimeString;

			if (Span.TotalMinutes < 1)
				TimeString = ServiceRef.Localizer[nameof(AppResources.Now)].Value;
			else if (Span.TotalHours < 1)
				TimeString = ServiceRef.Localizer[nameof(AppResources.MinutesAgoFormat), false, (int)Span.TotalMinutes];
			else if (Span.TotalDays < 1)
				TimeString = ServiceRef.Localizer[nameof(AppResources.HoursAgoFormat), false, (int)Span.TotalHours];
			else
				TimeString = Dt.ToString("m", culture);
			return TimeString;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

}
