using System.Text;
using NeuroAccessMaui.Services;
using Waher.Events;

namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Static class that gives access to app-specific styles
	/// </summary>
	public static class AppStyles
	{
		private static readonly SortedDictionary<string, bool> missingStyles = [];
		private static Timer? timer = null;

		private static Thickness? smallBottomOnlyMargins;
		private static Style? sectionTitleLabelStyle;
		private static Style? keyLabel;
		private static Style? valueLabel;
		private static Style? formattedValueLabel;
		private static Style? clickableValueLabel;
		private static Style? infoLabelStyle;

		static AppStyles()
		{
			Log.Terminating += Log_Terminating;
		}

		private static void Log_Terminating(object? sender, EventArgs e)
		{
			timer?.Dispose();
			timer = null;
		}

		/// <summary>
		/// Tries to get an embedded resource.
		/// </summary>
		/// <typeparam name="T">Type of resource.</typeparam>
		/// <param name="Name">Name of resource.</param>
		/// <returns>Typed resource, if found.</returns>
		public static T? TryGetResource<T>(string Name)
		{
			if ((Application.Current?.Resources.TryGetValue(Name, out object Value) ?? false) && Value is T TypedValue)
				return TypedValue;
			else
			{
				lock (missingStyles)
				{
					timer?.Dispose();
					timer = null;

					missingStyles[Name] = true;

					timer = new Timer(LogAlert, null, 1000, Timeout.Infinite);
				}

				return default;
			}
		}

		private static void LogAlert(object? _)
		{
			StringBuilder sb = new();

			sb.AppendLine("Missing styles:");
			sb.AppendLine();

			lock (missingStyles)
			{
				foreach (string Key in missingStyles.Keys)
				{
					sb.Append("* ");
					sb.AppendLine(Key);
				}

				missingStyles.Clear();
			}

			ServiceRef.LogService.LogAlert(sb.ToString());
		}

		/// <summary>
		/// Bottom-only small margins
		/// </summary>
		public static Thickness SmallBottomOnlyMargins
		{
			get
			{
				smallBottomOnlyMargins ??= TryGetResource<Thickness>("SmallBottomOnlyMargins");
				return smallBottomOnlyMargins.Value;
			}
		}

		/// <summary>
		/// Style of section title labels
		/// </summary>
		public static Style SectionTitleLabelStyle
		{
			get
			{
				sectionTitleLabelStyle ??= TryGetResource<Style>("SectionTitleLabelStyle");
				return sectionTitleLabelStyle!;
			}
		}

		/// <summary>
		/// Style of key labels
		/// </summary>
		public static Style KeyLabel
		{
			get
			{
				keyLabel ??= TryGetResource<Style>("KeyLabel");
				return keyLabel!;
			}
		}

		/// <summary>
		/// Style of value labels
		/// </summary>
		public static Style ValueLabel
		{
			get
			{
				valueLabel ??= TryGetResource<Style>("ValueLabel");
				return valueLabel!;
			}
		}

		/// <summary>
		/// Style of formatted value labels
		/// </summary>
		public static Style FormattedValueLabel
		{
			get
			{
				formattedValueLabel ??= TryGetResource<Style>("FormattedValueLabel");
				return formattedValueLabel!;
			}
		}

		/// <summary>
		/// Style of clickable value labels
		/// </summary>
		public static Style ClickableValueLabel
		{
			get
			{
				clickableValueLabel ??= TryGetResource<Style>("ClickableValueLabel");
				return clickableValueLabel!;
			}
		}

		/// <summary>
		/// Style of information labels
		/// </summary>
		public static Style InfoLabelStyle
		{
			get
			{
				infoLabelStyle ??= TryGetResource<Style>("InfoLabelStyle");
				return infoLabelStyle!;
			}
		}

	}
}
