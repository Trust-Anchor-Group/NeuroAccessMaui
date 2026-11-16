using System.Globalization;
using System.Text;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Turns the code names in a token's state machine, such as "AmountBought" or "OpenForInvestment", into words.
	/// </summary>
	public static class TokenText
	{
		/// <summary>
		/// Splits a code name into words: "AmountBought" becomes "Amount bought", and "TokenId" becomes "Token ID".
		/// </summary>
		/// <param name="Name">The code name.</param>
		/// <returns>The name in words, with only its first letter in capitals, apart from abbreviations.</returns>
		public static string ToWords(string? Name)
		{
			if (string.IsNullOrWhiteSpace(Name))
				return string.Empty;

			List<string> Words = [];
			StringBuilder Word = new();

			for (int i = 0; i < Name.Length; i++)
			{
				char Current = Name[i];

				if (Current == '_' || Current == '-' || Current == ' ' || Current == '.')
				{
					AddWord(Words, Word);
					continue;
				}

				if (Word.Length > 0)
				{
					char Prev = Name[i - 1];
					bool NextIsLower = i + 1 < Name.Length && char.IsLower(Name[i + 1]);

					// A new word starts at a capital after a small letter or a digit, at the last capital of an abbreviation
					// followed by a small letter, and where digits begin or end.
					if ((char.IsUpper(Current) && (char.IsLower(Prev) || char.IsDigit(Prev))) ||
						(char.IsUpper(Current) && char.IsUpper(Prev) && NextIsLower) ||
						(char.IsDigit(Current) != char.IsDigit(Prev) && char.IsLetterOrDigit(Prev)))
					{
						AddWord(Words, Word);
					}
				}

				Word.Append(Current);
			}

			AddWord(Words, Word);

			StringBuilder Result = new();

			for (int i = 0; i < Words.Count; i++)
			{
				string Text = Words[i];

				if (i > 0)
					Result.Append(' ');

				if (string.Equals(Text, "Id", StringComparison.OrdinalIgnoreCase))
					Result.Append("ID");
				else if (IsAbbreviation(Text))
					Result.Append(Text);
				else if (i == 0)
					Result.Append(char.ToUpper(Text[0], CultureInfo.InvariantCulture)).Append(Text[1..].ToLowerInvariant());
				else
					Result.Append(Text.ToLowerInvariant());
			}

			return Result.ToString();
		}

		/// <summary>
		/// Adds the collected word to the list, and starts a new one.
		/// </summary>
		private static void AddWord(List<string> Words, StringBuilder Word)
		{
			if (Word.Length > 0)
			{
				Words.Add(Word.ToString());
				Word.Clear();
			}
		}

		/// <summary>
		/// Checks if a word is an abbreviation, such as "URL", which keeps its capitals.
		/// </summary>
		private static bool IsAbbreviation(string Word)
		{
			return Word.Length > 1 && Word.All(Ch => char.IsUpper(Ch) || char.IsDigit(Ch));
		}
	}
}
