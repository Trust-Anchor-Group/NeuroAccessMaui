using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using NeuroAccessMaui.UI.Rendering.ContractParameters;
using Waher.Content;
using Waher.Networking.XMPP.Contracts.HumanReadable;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts <see cref="Duration"/> values to localized strings using <see cref="DurationParameterValueRenderer"/>.
	/// </summary>
	public class DurationToStringConverter : IValueConverter
	{
		private static readonly DurationParameterValueRenderer renderer = new DurationParameterValueRenderer();
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			Duration Duration;

			if (value is Duration Direct)
			{
				Duration = Direct;
			}
			else if (value is string Str && Duration.TryParse(Str, out Duration Parsed))
			{
				Duration = Parsed;
			}
			else
			{
				return string.Empty;
			}

			string Language = culture?.Name ?? CultureInfo.CurrentUICulture.Name;
			MarkdownSettings MarkdownSettings = new MarkdownSettings(null, MarkdownType.ForRendering);

			try
			{
				return renderer.ToString(Duration, Language, MarkdownSettings).GetAwaiter().GetResult();
			}
			catch
			{
				return Duration.ToString();
			}
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string Str && Duration.TryParse(Str, out Duration Parsed))
				return Parsed;

			return Duration.Zero;
		}
	}
}
