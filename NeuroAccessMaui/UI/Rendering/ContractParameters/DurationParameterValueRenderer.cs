using NeuroAccessMaui.UI.Converters;
using System.Globalization;
using System.Text;
using Waher.Content;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements.ValueRendering;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;
using NeuroAccessMaui.Services; // Added for localization
using NeuroAccessMaui.Resources.Languages; // Added for resource keys

namespace NeuroAccessMaui.UI.Rendering.ContractParameters
{
	/// <summary>
	/// Converts a <see cref="Duration"/> parameter value to a localized human-readable string.
	/// </summary>
	public class DurationParameterValueRenderer : ParameterValueRenderer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DurationParameterValueRenderer"/> class.
		/// </summary>
		public DurationParameterValueRenderer() : base() { }

		/// <summary>
		/// Determines how well a parameter value is supported.
		/// </summary>
		/// <param name="Value">Parameter value.</param>
		/// <returns>Support grade.</returns>
		public override Grade Supports(object Value)
		{
			return Value is Duration ? Grade.Excellent : Grade.NotAtAll; // Higher than Grade.Ok overrides default behaviour.
		}

		/// <summary>
		/// Generates a localized Markdown string from the parameter value.
		/// </summary>
		/// <param name="Value">Parameter value.</param>
		/// <param name="Language">Desired language.</param>
		/// <param name="Settings">Markdown settings.</param>
		/// <returns>Localized Markdown string.</returns>
		public override Task<string> ToString(object Value, string Language, MarkdownSettings Settings)
		{
			if (Value is not Duration Duration)
				return base.ToString(Value, Language, Settings);

			ChunkedList<string> Parts = [];

			if (Duration.Years != 0)
				Parts.Add(UnitToString(Duration.Years, nameof(AppResources.Year), nameof(AppResources.Years)));
			if (Duration.Months != 0)
				Parts.Add(UnitToString(Duration.Months, nameof(AppResources.Month), nameof(AppResources.Months)));
			if (Duration.Days != 0)
				Parts.Add(UnitToString(Duration.Days, nameof(AppResources.Day), nameof(AppResources.Days)));
			if (Duration.Hours != 0)
				Parts.Add(UnitToString(Duration.Hours, nameof(AppResources.Hour), nameof(AppResources.Hours)));
			if (Duration.Minutes != 0)
				Parts.Add(UnitToString(Duration.Minutes, nameof(AppResources.Minute), nameof(AppResources.Minutes)));
			if (Duration.Seconds != 0)
				Parts.Add(UnitToString(Duration.Seconds, nameof(AppResources.Second), nameof(AppResources.Seconds)));

			if (Parts.Count == 0)
				Parts.Add(UnitToString(0, nameof(AppResources.Second), nameof(AppResources.Seconds)));

			StringBuilder Builder = new StringBuilder();
			if (Duration.Negation)
				Builder.Append('-');

			string AndSep = ServiceRef.Localizer[nameof(AppResources.DurationAndSeparator)].ResourceNotFound ? " and " : ServiceRef.Localizer[nameof(AppResources.DurationAndSeparator)].Value;
			string CommaSep = ServiceRef.Localizer[nameof(AppResources.DurationCommaSeparator)].ResourceNotFound ? ", " : ServiceRef.Localizer[nameof(AppResources.DurationCommaSeparator)].Value;

			for (int i = 0; i < Parts.Count; i++)
			{
				if (i > 0)
					Builder.Append(i == Parts.Count -1 ? AndSep : CommaSep);

				Builder.Append(Parts[i]);
			}

			return Task.FromResult(Builder.ToString());
		}

		private static string UnitToString(int Value, string SingularKey, string PluralKey)
		{
			string Unit = ServiceRef.Localizer[Value == 1 ? SingularKey : PluralKey].ResourceNotFound ? (Value == 1 ? SingularKey.ToLowerInvariant() : PluralKey.ToLowerInvariant()) : ServiceRef.Localizer[Value == 1 ? SingularKey : PluralKey].Value;
			return Value.ToString(CultureInfo.InvariantCulture) + " " + Unit;
		}

		private static string UnitToString(double Value, string SingularKey, string PluralKey)
		{
			string Unit = ServiceRef.Localizer[Value == 1 ? SingularKey : PluralKey].ResourceNotFound ? (Value == 1 ? SingularKey.ToLowerInvariant() : PluralKey.ToLowerInvariant()) : ServiceRef.Localizer[Value == 1 ? SingularKey : PluralKey].Value;
			return MoneyToString.ToString((decimal)Value) + " " + Unit;
		}
	}
}
