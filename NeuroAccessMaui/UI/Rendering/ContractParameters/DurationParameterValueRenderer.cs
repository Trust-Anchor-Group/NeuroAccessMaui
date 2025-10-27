using NeuroAccessMaui.UI.Converters;
using System.Globalization;
using System.Text;
using Waher.Content;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.InlineElements.ValueRendering;
using Waher.Runtime.Collections;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Rendering.ContractParameters
{
	/// <summary>
	/// Converts a Duration parameter value to a human-readable string.
	/// </summary>
	public class DurationParameterValueRenderer : ParameterValueRenderer
	{
		/// <summary>
		/// Converts a Duration parameter value to a human-readable string.
		/// </summary>
		public DurationParameterValueRenderer()
			: base()
		{
		}

		/// <summary>
		/// How well a parameter value is supported.
		/// </summary>
		/// <param name="Value">Parameter value.</param>
		/// <returns>How well values of this type are supported.</returns>
		public override Grade Supports(object Value)
		{
			return Value is Duration ? Grade.Excellent : Grade.NotAtAll;    // Higher than Grade.Ok overrides default behaviour.
		}

		/// <summary>
		/// Generates a Markdown string from the parameter value.
		/// </summary>
		/// <param name="Value">Parameter value.</param>
		/// <param name="Language">Desired language.</param>
		/// <param name="Settings">Markdown settings.</param>
		/// <returns>Markdown string.</returns>
		public override Task<string> ToString(object Value, string Language,
			MarkdownSettings Settings)
		{
			if (Value is not Duration D)
				return base.ToString(Value, Language, Settings);

			ChunkedList<string> Parts = [];

			if (D.Years != 0)
				Parts.Add(ToString(D.Years, "year", "years"));

			if (D.Months != 0)
				Parts.Add(ToString(D.Months, "month", "months"));

			if (D.Days != 0)
				Parts.Add(ToString(D.Days, "day", "days"));

			if (D.Hours != 0)
				Parts.Add(ToString(D.Hours, "hour", "hours"));

			if (D.Minutes != 0)
				Parts.Add(ToString(D.Minutes, "minute", "minutes"));

			if (D.Seconds != 0)
				Parts.Add(ToString(D.Seconds, "second", "seconds"));

			if (Parts.Count == 0)
				Parts.Add(ToString(0, "second", "seconds"));

			int i, c = Parts.Count;
			StringBuilder sb = new();

			if (D.Negation)
				sb.Append('-');

			for (i = 0; i < c; i++)
			{
				if (i > 0)
				{
					if (i == c - 1)
						sb.Append(" and ");
					else
						sb.Append(", ");
				}

				sb.Append(Parts[i]);
			}

			return Task.FromResult(sb.ToString());
		}

		private static string ToString(int Value, string SingularUnit, string PluralUnit)
		{
			if (Value == 1)
				return "1 " + SingularUnit;
			else
				return Value.ToString(CultureInfo.InvariantCulture) + " " + PluralUnit;
		}

		private static string ToString(double Value, string SingularUnit, string PluralUnit)
		{
			if (Value == 1)
				return "1 " + SingularUnit;
			else
				return MoneyToString.ToString((decimal)Value) + " " + PluralUnit;
		}
	}
}
