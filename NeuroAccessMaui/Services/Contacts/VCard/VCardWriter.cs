using System.Text;

namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// Writes contact cards (vCard) as version 4.0 text, with CRLF line endings and lines folded at 75 octets.
	/// </summary>
	/// <remarks>
	/// Cards read from version 2.1 or 3.0 are converted: types are kept, ENCODING and CHARSET are dropped, embedded data
	/// such as a photo becomes a data: link, and a "pref" type becomes PREF=1. Text values are escaped again, so a card
	/// written by this class reads back the same.
	/// </remarks>
	public static class VCardWriter
	{
		private const int maxLineOctets = 75;

		private static readonly HashSet<string> structuredNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"N", "ADR", "ORG", "GENDER", "CLIENTPIDMAP"
		};

		private static readonly HashSet<string> listNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"NICKNAME", "CATEGORIES"
		};

		/// <summary>
		/// Writes one card.
		/// </summary>
		/// <param name="Card">The card.</param>
		/// <returns>The card as version 4.0 text.</returns>
		public static string Write(VCard Card)
		{
			return Write([Card]);
		}

		/// <summary>
		/// Writes several cards, one after another, as in a .vcf file with many contacts.
		/// </summary>
		/// <param name="Cards">The cards.</param>
		/// <returns>The cards as version 4.0 text.</returns>
		public static string Write(IEnumerable<VCard> Cards)
		{
			StringBuilder Result = new();

			foreach (VCard Card in Cards)
			{
				Result.Append("BEGIN:VCARD\r\n");
				Result.Append("VERSION:4.0\r\n");

				foreach (VCardProperty Property in Card.Properties)
				{
					if (Property.Name is "BEGIN" or "END" or "VERSION")
						continue;

					AppendFolded(Result, FormatLine(Property));
				}

				Result.Append("END:VCARD\r\n");
			}

			return Result.ToString();
		}

		/// <summary>
		/// Formats a property as one unfolded line of a version 4.0 card.
		/// </summary>
		private static string FormatLine(VCardProperty Property)
		{
			StringBuilder Line = new();

			if (!string.IsNullOrEmpty(Property.Group))
			{
				Line.Append(Property.Group);
				Line.Append('.');
			}

			Line.Append(Property.Name);

			string Formatted = FormatValue(Property);
			bool ToDataUri = Property.IsBinary;

			foreach (VCardParameter Parameter in Property.Parameters)
			{
				if (Parameter.Name is "ENCODING" or "CHARSET")
					continue;

				List<string> Values = [.. Parameter.Values.Where(Value => !string.IsNullOrWhiteSpace(Value))];

				if (Parameter.Name == "TYPE")
				{
					// In version 3.0 a photo's type, such as JPEG, is its media type, which the data: link now holds.
					if (ToDataUri)
						continue;

					Values = [.. Values.SelectMany(Value => Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
						.Select(Value => Value.ToLowerInvariant())
						.Distinct()];

					if (Values.Remove("pref") && Property.GetParameter("PREF") is null)
						Line.Append(";PREF=1");
				}

				if (Parameter.Name == "VALUE" && ToDataUri)
					continue;

				if (Values.Count == 0)
					continue;

				Line.Append(';');
				Line.Append(Parameter.Name);
				Line.Append('=');
				Line.Append(string.Join(',', Values.Select(VCardText.EncodeParameter)));
			}

			Line.Append(':');
			Line.Append(Formatted);

			return Line.ToString();
		}

		/// <summary>
		/// The value as written in a version 4.0 card: text escaped again, embedded data as a data: link, and links as
		/// they are.
		/// </summary>
		private static string FormatValue(VCardProperty Property)
		{
			if (Property.IsBinary)
			{
				string MediaType = VCard.MediaTypeOf(Property) ?? "application/octet-stream";
				return "data:" + MediaType + ";base64," + Property.RawValue;
			}

			if (!Property.IsText)
				return Property.RawValue.Replace("\r", string.Empty).Replace("\n", string.Empty);

			if (structuredNames.Contains(Property.Name))
				return string.Join(';', Property.Components.Select(VCardText.Escape));

			if (listNames.Contains(Property.Name))
				return string.Join(',', Property.ListValues.Select(VCardText.Escape));

			return VCardText.Escape(Property.Value);
		}

		/// <summary>
		/// Appends a line, folded so that no line is longer than 75 octets in UTF-8. A character is never split, and each
		/// continuation line starts with a space.
		/// </summary>
		private static void AppendFolded(StringBuilder Result, string Line)
		{
			int Octets = 0;

			for (int i = 0; i < Line.Length; i++)
			{
				int Length = char.IsHighSurrogate(Line[i]) && i + 1 < Line.Length && char.IsLowSurrogate(Line[i + 1]) ? 2 : 1;
				int Size = Encoding.UTF8.GetByteCount(Line.AsSpan(i, Length));

				if (Octets + Size > maxLineOctets)
				{
					Result.Append("\r\n ");
					Octets = 1;
				}

				Result.Append(Line, i, Length);
				Octets += Size;
				i += Length - 1;
			}

			Result.Append("\r\n");
		}
	}
}
