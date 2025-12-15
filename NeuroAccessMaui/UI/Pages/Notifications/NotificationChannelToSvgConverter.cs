using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using NeuroAccessMaui;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	/// <summary>
	/// Converts a notification channel label into the corresponding SVG resource name.
	/// </summary>
	public class NotificationChannelToSvgConverter : IValueConverter
	{
		/// <summary>
		/// Converts a notification channel label into an SVG resource name, defaulting to the general notifications icon when the channel is unknown.
		/// </summary>
		/// <param name="value">The channel label.</param>
		/// <param name="targetType">The target binding type.</param>
		/// <param name="parameter">An optional converter parameter.</param>
		/// <param name="culture">The culture for the conversion.</param>
		/// <returns>The SVG resource name that corresponds to the channel label.</returns>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			string Channel = (value as string)?.Trim() ?? string.Empty;

			switch (Channel)
			{
				case Constants.PushChannels.Messages:
					return "message.svg";
				case Constants.PushChannels.Petitions:
					return "notifications.svg";
				case Constants.PushChannels.Identities:
					return "person.svg";
				case Constants.PushChannels.Contracts:
					return "contract.svg";
				case Constants.PushChannels.EDaler:
					return "wallet.svg";
				case Constants.PushChannels.Tokens:
					return "token.svg";
				case Constants.PushChannels.Provisioning:
					return "thing.svg";
				default:
					return "notifications.svg";
			}
		}

		/// <summary>
		/// Not implemented because conversion from SVG name back to channel label is not supported.
		/// </summary>
		/// <param name="value">The SVG resource name.</param>
		/// <param name="targetType">The target binding type.</param>
		/// <param name="parameter">An optional converter parameter.</param>
		/// <param name="culture">The culture for the conversion.</param>
		/// <returns>An exception is always thrown because this method is not implemented.</returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("Converting SVG resource names back to channel labels is not supported.");
		}
	}
}
