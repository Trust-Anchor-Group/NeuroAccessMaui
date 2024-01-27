using System.Globalization;
using System.Text;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Content;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a <see cref="Duration"/> value to a <see cref="String"/> value.
	/// </summary>
	public class DurationToString : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not Duration D)
			{
				if (value is string s)
				{
					if (!Duration.TryParse(s, out D))
						return string.Empty;
				}
				else
					return string.Empty;
			}

			return ToString(D);
		}

		/// <summary>
		/// Converts a Duration to a human-readable text.
		/// </summary>
		/// <param name="Duration">Duration to convert.</param>
		/// <returns>Human-readable text.</returns>
		public static string ToString(Duration Duration)
		{
			StringBuilder sb = new();
			bool First = true;

			if (Duration.Negation)
			{
				sb.Append(ServiceRef.Localizer[nameof(AppResources.Negative)]);
				sb.Append(' ');
			}

			Append(sb, Duration.Years, ref First, ServiceRef.Localizer[nameof(AppResources.Year)], ServiceRef.Localizer[nameof(AppResources.Years)]);
			Append(sb, Duration.Months, ref First, ServiceRef.Localizer[nameof(AppResources.Month)], ServiceRef.Localizer[nameof(AppResources.Months)]);
			Append(sb, Duration.Days, ref First, ServiceRef.Localizer[nameof(AppResources.Day)], ServiceRef.Localizer[nameof(AppResources.Days)]);
			Append(sb, Duration.Hours, ref First, ServiceRef.Localizer[nameof(AppResources.Hour)], ServiceRef.Localizer[nameof(AppResources.Hours)]);
			Append(sb, Duration.Minutes, ref First, ServiceRef.Localizer[nameof(AppResources.Minute)], ServiceRef.Localizer[nameof(AppResources.Minutes)]);
			Append(sb, Duration.Seconds, ref First, ServiceRef.Localizer[nameof(AppResources.Second)], ServiceRef.Localizer[nameof(AppResources.Seconds)]);

			if (First)
				sb.Append('-');

			return sb.ToString();
		}

		private static void Append(StringBuilder sb, int Nr, ref bool First, string SingularUnit, string PluralUnit)
		{
			if (Nr != 0)
			{
				if (First)
					First = false;
				else
					sb.Append(", ");

				sb.Append(Nr.ToString(CultureInfo.CurrentCulture));
				sb.Append(' ');

				if (Nr == 1)
					sb.Append(SingularUnit);
				else
					sb.Append(PluralUnit);
			}
		}

		private static void Append(StringBuilder sb, double Nr, ref bool First, string SingularUnit, string PluralUnit)
		{
			if (Nr != 0)
			{
				if (First)
					First = false;
				else
					sb.Append(", ");

				sb.Append(Nr.ToString(CultureInfo.CurrentCulture.NumberFormat));
				sb.Append(' ');

				if (Nr == 1)
					sb.Append(SingularUnit);
				else
					sb.Append(PluralUnit);
			}
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string s && Duration.TryParse(s, out Duration D))
				return D;
			else
				return Duration.Zero;
		}

		/// <inheritdoc/>
		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
