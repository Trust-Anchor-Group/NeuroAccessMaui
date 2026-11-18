using System.Text;

namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// Escapes and unescapes the text of contact card values, and splits structured and list values.
	/// </summary>
	internal static class VCardText
	{
		/// <summary>
		/// Escapes a text value: backslashes, commas, semicolons and new lines.
		/// </summary>
		/// <param name="Text">Text, as shown.</param>
		/// <returns>Text, as written in a contact card.</returns>
		public static string Escape(string Text)
		{
			if (string.IsNullOrEmpty(Text))
				return string.Empty;

			StringBuilder Result = new(Text.Length + 8);
			string Normalized = Text.Replace("\r\n", "\n").Replace('\r', '\n');

			foreach (char ch in Normalized)
			{
				switch (ch)
				{
					case '\\':
						Result.Append("\\\\");
						break;

					case ',':
						Result.Append("\\,");
						break;

					case ';':
						Result.Append("\\;");
						break;

					case '\n':
						Result.Append("\\n");
						break;

					default:
						Result.Append(ch);
						break;
				}
			}

			return Result.ToString();
		}

		/// <summary>
		/// Unescapes a text value. Escapes that are not known are kept as they are, so no text is lost.
		/// </summary>
		/// <param name="Raw">Text, as written in a contact card.</param>
		/// <returns>Text, as shown.</returns>
		public static string Unescape(string Raw)
		{
			if (string.IsNullOrEmpty(Raw) || Raw.IndexOf('\\') < 0)
				return Raw ?? string.Empty;

			StringBuilder Result = new(Raw.Length);

			for (int i = 0; i < Raw.Length; i++)
			{
				char ch = Raw[i];

				if (ch != '\\' || i + 1 >= Raw.Length)
				{
					Result.Append(ch);
					continue;
				}

				char Next = Raw[++i];

				switch (Next)
				{
					case 'n':
					case 'N':
						Result.Append('\n');
						break;

					case '\\':
					case ',':
					case ';':
					case ':':
						Result.Append(Next);
						break;

					default:
						Result.Append('\\');
						Result.Append(Next);
						break;
				}
			}

			return Result.ToString();
		}

		/// <summary>
		/// Splits an escaped value on a separator that is not escaped, and unescapes each part.
		/// </summary>
		/// <param name="Raw">Value, as written in a contact card.</param>
		/// <param name="Separator">The separator: ';' for the parts of a name or an address, ',' for a list.</param>
		/// <returns>The parts, unescaped.</returns>
		public static List<string> Split(string Raw, char Separator)
		{
			List<string> Parts = [];
			StringBuilder Part = new();

			if (string.IsNullOrEmpty(Raw))
			{
				Parts.Add(string.Empty);
				return Parts;
			}

			for (int i = 0; i < Raw.Length; i++)
			{
				char ch = Raw[i];

				if (ch == '\\' && i + 1 < Raw.Length)
				{
					Part.Append(ch);
					Part.Append(Raw[++i]);
				}
				else if (ch == Separator)
				{
					Parts.Add(Unescape(Part.ToString()));
					Part.Clear();
				}
				else
					Part.Append(ch);
			}

			Parts.Add(Unescape(Part.ToString()));
			return Parts;
		}

		/// <summary>
		/// Decodes a parameter value written with the caret escapes of RFC 6868: ^n, ^' and ^^.
		/// </summary>
		/// <param name="Raw">Parameter value, as written.</param>
		/// <returns>Parameter value.</returns>
		public static string DecodeParameter(string Raw)
		{
			if (Raw.IndexOf('^') < 0)
				return Raw;

			StringBuilder Result = new(Raw.Length);

			for (int i = 0; i < Raw.Length; i++)
			{
				char ch = Raw[i];

				if (ch == '^' && i + 1 < Raw.Length)
				{
					char Next = Raw[i + 1];

					if (Next == 'n' || Next == 'N')
					{
						Result.Append('\n');
						i++;
						continue;
					}

					if (Next == '\'')
					{
						Result.Append('"');
						i++;
						continue;
					}

					if (Next == '^')
					{
						Result.Append('^');
						i++;
						continue;
					}
				}

				Result.Append(ch);
			}

			return Result.ToString();
		}

		/// <summary>
		/// Encodes a parameter value with the caret escapes of RFC 6868, and quotes it when it holds a colon, semicolon or
		/// comma.
		/// </summary>
		/// <param name="Value">Parameter value.</param>
		/// <returns>Parameter value, as written.</returns>
		public static string EncodeParameter(string Value)
		{
			StringBuilder Result = new(Value.Length + 4);
			bool Quote = false;

			foreach (char ch in Value.Replace("\r\n", "\n").Replace('\r', '\n'))
			{
				switch (ch)
				{
					case '^':
						Result.Append("^^");
						break;

					case '"':
						Result.Append("^'");
						break;

					case '\n':
						Result.Append("^n");
						break;

					case ':':
					case ';':
					case ',':
						Quote = true;
						Result.Append(ch);
						break;

					default:
						Result.Append(ch);
						break;
				}
			}

			return Quote ? "\"" + Result.ToString() + "\"" : Result.ToString();
		}
	}
}
