namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// An extensions class for the <see cref="string"/> class.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Returns the part of the string that appears before <paramref name="Delimiter"/>. If <paramref name="Delimiter"/>
		/// does not occur, the entire string is returned.
		/// </summary>
		/// <param name="s">String</param>
		/// <param name="Delimiter">Delimiter</param>
		/// <returns>Part of string before <paramref name="Delimiter"/>.</returns>
		public static string Before(this string s, string Delimiter)
		{
			int i = s.IndexOf(Delimiter, StringComparison.Ordinal);
			if (i < 0)
				return s;
			else
				return s[..i];
		}

		/// <summary>
		/// Returns the part of the string that appears before <paramref name="Delimiter"/>. If <paramref name="Delimiter"/>
		/// does not occur, the entire string is returned.
		/// </summary>
		/// <param name="s">String</param>
		/// <param name="Delimiter">Delimiter</param>
		/// <returns>Part of string before <paramref name="Delimiter"/>.</returns>
		public static string After(this string s, string Delimiter)
		{
			int i = s.IndexOf(Delimiter, StringComparison.Ordinal);
			if (i < 0)
				return s;
			else
				return s[(i + 1)..];
		}

		/// <summary>
		/// Returns the number of Unicode symbols, which may be represented by one or two chars, in a string.
		/// </summary>
		public static int GetUnicodeLength(this string Str)
		{
			ArgumentNullException.ThrowIfNull(Str);

			Str = Str.Normalize();

			int UnicodeCount = 0;
			for (int i = 0; i < Str.Length; i++)
			{
				UnicodeCount++;

				// Jump over the second surrogate char.
				if (char.IsSurrogate(Str, i))
					i++;
			}

			return UnicodeCount;
		}
	}
}
