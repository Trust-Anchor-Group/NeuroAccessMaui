using System.Text;

namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// Reads contact cards (vCard) from text, in version 2.1, 3.0 or 4.0. A file can hold several cards.
	/// </summary>
	/// <remarks>
	/// Nothing in a file is trusted: lines that can't be read are skipped, and reading never throws. Callers should limit
	/// the size of the text they read, for example to a few megabytes, as the whole text is held in memory. Each card keeps
	/// at most <see cref="MaxProperties"/> properties.
	/// </remarks>
	public static class VCardReader
	{
		/// <summary>
		/// The most properties a card keeps. Further properties of the card are skipped.
		/// </summary>
		public const int MaxProperties = 1000;

		/// <summary>
		/// Reads the contact cards in a text.
		/// </summary>
		/// <param name="Text">The text, such as the contents of a .vcf file.</param>
		/// <returns>The cards, in order. A card that has no END line at the end of the text is kept.</returns>
		public static IReadOnlyList<VCard> Parse(string? Text)
		{
			List<VCard> Cards = [];

			if (string.IsNullOrEmpty(Text))
				return Cards;

			VCard? Current = null;

			foreach (string Line in Unfold(Text))
			{
				if (string.IsNullOrWhiteSpace(Line) || !TryParseLine(Line, out VCardProperty? Property))
					continue;

				if (Property.Name == "BEGIN")
				{
					if (Property.RawValue.Trim().Equals("VCARD", StringComparison.OrdinalIgnoreCase))
					{
						if (Current is not null && Current.Properties.Count > 0)
							Cards.Add(Current);

						Current = new VCard()
						{
							Version = "2.1"
						};
					}

					continue;
				}

				if (Current is null)
					continue;

				if (Property.Name == "END")
				{
					if (Property.RawValue.Trim().Equals("VCARD", StringComparison.OrdinalIgnoreCase))
					{
						Cards.Add(Current);
						Current = null;
					}

					continue;
				}

				if (Property.Name == "VERSION")
				{
					Current.Version = Property.RawValue.Trim();
					continue;
				}

				if (Current.Properties.Count < MaxProperties)
					Current.Properties.Add(Property);
			}

			if (Current is not null && Current.Properties.Count > 0)
				Cards.Add(Current);

			return Cards;
		}

		/// <summary>
		/// Joins folded lines: a line that starts with a space or a tab continues the line before it. In quoted-printable
		/// values, a line that ends with = continues on the next line.
		/// </summary>
		private static List<string> Unfold(string Text)
		{
			string[] Physical = Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			List<string> Result = [];
			StringBuilder? Current = null;
			bool IsQuotedPrintable = false;

			foreach (string Line in Physical)
			{
				if (Current is not null)
				{
					if (IsQuotedPrintable && Current.Length > 0 && Current[^1] == '=')
					{
						Current.Length--;
						Current.Append(Line);
						continue;
					}

					if (Line.Length > 0 && (Line[0] == ' ' || Line[0] == '\t'))
					{
						Current.Append(Line, 1, Line.Length - 1);
						continue;
					}

					Result.Add(Current.ToString());
				}

				Current = new StringBuilder(Line);
				IsQuotedPrintable = UsesQuotedPrintable(Line);
			}

			if (Current is not null)
				Result.Add(Current.ToString());

			return Result;
		}

		/// <summary>
		/// If a line's parameters say its value is quoted-printable.
		/// </summary>
		private static bool UsesQuotedPrintable(string Line)
		{
			int Colon = IndexOfUnquoted(Line, ':');
			string Head = Colon < 0 ? Line : Line[..Colon];

			return Head.Contains("QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Reads one unfolded line: group, name, parameters and value. Lines that are not properties are rejected.
		/// </summary>
		private static bool TryParseLine(string Line, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out VCardProperty? Property)
		{
			Property = null;

			int Colon = IndexOfUnquoted(Line, ':');
			if (Colon <= 0)
				return false;

			List<string> Head = SplitUnquoted(Line[..Colon], ';');
			string Raw = Line[(Colon + 1)..];
			string Name = Head[0].Trim();
			string? Group = null;

			int Dot = Name.LastIndexOf('.');
			if (Dot >= 0)
			{
				Group = Name[..Dot];
				Name = Name[(Dot + 1)..];
			}

			if (!IsName(Name) || (Group is not null && !IsName(Group)))
				return false;

			Property = new VCardProperty(Name)
			{
				Group = Group
			};

			for (int i = 1; i < Head.Count; i++)
				AddParameter(Property, Head[i]);

			string? Encoding = Property.GetParameter("ENCODING");

			if (Encoding is not null && Encoding.Equals("QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase))
				Raw = DecodeQuotedPrintable(Raw, Property.GetParameter("CHARSET"));
			else if (Property.IsBinary)
				Raw = string.Concat(Raw.Where(ch => !char.IsWhiteSpace(ch)));

			Property.RawValue = Raw;
			return true;
		}

		/// <summary>
		/// Adds a parameter to a property. Version 2.1 parameters without a name, such as CELL or BASE64, become TYPE or
		/// ENCODING parameters.
		/// </summary>
		private static void AddParameter(VCardProperty Property, string Text)
		{
			string Trimmed = Text.Trim();
			if (Trimmed.Length == 0)
				return;

			int Eq = Trimmed.IndexOf('=');

			if (Eq < 0)
			{
				switch (Trimmed.ToUpperInvariant())
				{
					case "BASE64":
					case "B":
					case "QUOTED-PRINTABLE":
					case "8BIT":
					case "7BIT":
						Property.AddParameter("ENCODING", Trimmed);
						break;

					default:
						Property.AddParameter("TYPE", Trimmed);
						break;
				}

				return;
			}

			string Name = Trimmed[..Eq].Trim();
			if (!IsName(Name))
				return;

			List<string> Values = [];

			foreach (string Value in SplitUnquoted(Trimmed[(Eq + 1)..], ','))
			{
				string Clean = Value.Trim();

				if (Clean.Length >= 2 && Clean[0] == '"' && Clean[^1] == '"')
					Clean = Clean[1..^1];
				else
					Clean = Clean.Trim('"');

				Values.Add(VCardText.DecodeParameter(Clean));
			}

			Property.AddParameter(Name, [.. Values]);
		}

		/// <summary>
		/// Decodes a quoted-printable value, such as =C3=85sa, in the character set the card gives, or else UTF-8.
		/// </summary>
		private static string DecodeQuotedPrintable(string Raw, string? Charset)
		{
			List<byte> Bytes = new(Raw.Length);

			for (int i = 0; i < Raw.Length; i++)
			{
				char ch = Raw[i];

				if (ch == '=' && i + 2 < Raw.Length && IsHex(Raw[i + 1]) && IsHex(Raw[i + 2]))
				{
					Bytes.Add((byte)((HexValue(Raw[i + 1]) << 4) | HexValue(Raw[i + 2])));
					i += 2;
				}
				else if (ch == '=' && i == Raw.Length - 1)
				{
					// A soft line break at the end of the value.
				}
				else if (ch < 128)
					Bytes.Add((byte)ch);
				else
					Bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(ch.ToString()));
			}

			return EncodingOf(Charset).GetString([.. Bytes]);
		}

		private static System.Text.Encoding EncodingOf(string? Charset)
		{
			if (string.IsNullOrWhiteSpace(Charset))
				return System.Text.Encoding.UTF8;

			try
			{
				return System.Text.Encoding.GetEncoding(Charset.Trim());
			}
			catch (ArgumentException)
			{
				return Charset.Contains("8859", StringComparison.OrdinalIgnoreCase) || Charset.Contains("1252", StringComparison.Ordinal) ||
					Charset.Contains("latin", StringComparison.OrdinalIgnoreCase) ? System.Text.Encoding.Latin1 : System.Text.Encoding.UTF8;
			}
		}

		private static bool IsHex(char ch)
		{
			return (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
		}

		private static int HexValue(char ch)
		{
			if (ch >= '0' && ch <= '9')
				return ch - '0';

			if (ch >= 'a' && ch <= 'f')
				return ch - 'a' + 10;

			return ch - 'A' + 10;
		}

		/// <summary>
		/// If a text can be a property, group or parameter name: letters, digits and hyphens.
		/// </summary>
		private static bool IsName(string Name)
		{
			if (Name.Length == 0 || Name.Length > 128)
				return false;

			foreach (char ch in Name)
			{
				if (!char.IsAsciiLetterOrDigit(ch) && ch != '-' && ch != '_')
					return false;
			}

			return true;
		}

		/// <summary>
		/// The index of a character that is not inside double quotes, or -1.
		/// </summary>
		private static int IndexOfUnquoted(string Text, char Wanted)
		{
			bool Quoted = false;

			for (int i = 0; i < Text.Length; i++)
			{
				char ch = Text[i];

				if (ch == '"')
					Quoted = !Quoted;
				else if (ch == Wanted && !Quoted)
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Splits a text on a separator that is not inside double quotes.
		/// </summary>
		private static List<string> SplitUnquoted(string Text, char Separator)
		{
			List<string> Parts = [];
			StringBuilder Part = new();
			bool Quoted = false;

			foreach (char ch in Text)
			{
				if (ch == '"')
				{
					Quoted = !Quoted;
					Part.Append(ch);
				}
				else if (ch == Separator && !Quoted)
				{
					Parts.Add(Part.ToString());
					Part.Clear();
				}
				else
					Part.Append(ch);
			}

			Parts.Add(Part.ToString());
			return Parts;
		}
	}
}
