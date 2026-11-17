using System.Text;
using System.Text.RegularExpressions;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Applies the markdown formats of a chat's formatting bar to the text being written, and tells if a text has any
	/// formatting.
	/// </summary>
	public static partial class ChatFormatting
	{
		private const string linkAddress = "https://";
		private const string fence = "```";

		/// <summary>
		/// A text, with what is selected in it.
		/// </summary>
		/// <param name="Text">The text.</param>
		/// <param name="Start">Where the selection, or the cursor, starts.</param>
		/// <param name="Length">Length of the selection, or 0 if nothing is selected.</param>
		public readonly record struct Selection(string Text, int Start, int Length);

		/// <summary>
		/// Applies a format to the selected text, or at the cursor. Applying the same format again removes it.
		/// </summary>
		/// <remarks>
		/// Bold, italic, struck-through text and code wrap the selection in their marks, or put the marks at the cursor
		/// with the cursor between them. A link wraps the selection as its text and selects the address. Lists and quotes
		/// start each selected line with their mark. A code block puts ``` lines around the selected lines.
		/// </remarks>
		/// <param name="Format">The format.</param>
		/// <param name="Current">The text, and what is selected in it.</param>
		/// <returns>The new text, and what to select in it.</returns>
		public static Selection Apply(ChatFormat Format, Selection Current)
		{
			Selection Clamped = Clamp(Current);

			return Format switch
			{
				ChatFormat.Bold => Wrap(Clamped, "**"),
				ChatFormat.Italic => Wrap(Clamped, "*"),
				ChatFormat.Strikethrough => Wrap(Clamped, "~~"),
				ChatFormat.Code => Wrap(Clamped, "`"),
				ChatFormat.Link => Link(Clamped),
				ChatFormat.BulletList => PrefixLines(Clamped, _ => "- ", BulletPrefix()),
				ChatFormat.NumberedList => PrefixLines(Clamped, Number => Number.ToString(System.Globalization.CultureInfo.InvariantCulture) + ". ", NumberPrefix()),
				ChatFormat.Quote => PrefixLines(Clamped, _ => "> ", QuotePrefix()),
				ChatFormat.CodeBlock => CodeBlock(Clamped),
				_ => Clamped
			};
		}

		/// <summary>
		/// If a text uses markdown formatting, so that a preview of it shows something the plain text does not.
		/// </summary>
		/// <param name="Text">The text.</param>
		/// <returns>If the text has formatting.</returns>
		public static bool HasFormatting(string? Text)
		{
			return !string.IsNullOrWhiteSpace(Text) && (InlineFormatting().IsMatch(Text) || BlockFormatting().IsMatch(Text));
		}

		private static Selection Clamp(Selection Current)
		{
			string Text = Current.Text ?? string.Empty;
			int Start = Math.Clamp(Current.Start, 0, Text.Length);
			int Length = Math.Clamp(Current.Length, 0, Text.Length - Start);

			return new Selection(Text, Start, Length);
		}

		/// <summary>
		/// Wraps the selection in a mark, or removes the mark if it is already around the selection or the cursor.
		/// </summary>
		private static Selection Wrap(Selection Current, string Mark)
		{
			string Text = Current.Text;
			int Start = Current.Start;
			int End = Start + Current.Length;
			int m = Mark.Length;

			// The marks are inside the selection: remove them.
			if (Current.Length >= 2 * m &&
				string.CompareOrdinal(Text, Start, Mark, 0, m) == 0 &&
				string.CompareOrdinal(Text, End - m, Mark, 0, m) == 0)
			{
				string Inner = Text.Substring(Start + m, Current.Length - 2 * m);
				return new Selection(Text[..Start] + Inner + Text[End..], Start, Inner.Length);
			}

			// The marks are just outside the selection, or around the cursor: remove them.
			if (Start >= m && End + m <= Text.Length &&
				string.CompareOrdinal(Text, Start - m, Mark, 0, m) == 0 &&
				string.CompareOrdinal(Text, End, Mark, 0, m) == 0)
			{
				return new Selection(Text[..(Start - m)] + Text[Start..End] + Text[(End + m)..], Start - m, Current.Length);
			}

			return new Selection(Text[..Start] + Mark + Text[Start..End] + Mark + Text[End..], Start + m, Current.Length);
		}

		private static Selection Link(Selection Current)
		{
			string Text = Current.Text;
			int Start = Current.Start;
			int End = Start + Current.Length;
			string Label = Text[Start..End];
			string Inserted = "[" + Label + "](" + linkAddress + ")";
			string Result = Text[..Start] + Inserted + Text[End..];

			// With a text, the address is selected so it can be written over. Without, the cursor goes where the text goes.
			if (Label.Length > 0)
				return new Selection(Result, Start + Label.Length + 3, linkAddress.Length);

			return new Selection(Result, Start + 1, 0);
		}

		/// <summary>
		/// Starts each line the selection touches with a prefix, or removes the prefixes if all the lines have one.
		/// </summary>
		private static Selection PrefixLines(Selection Current, Func<int, string> Prefix, Regex Existing)
		{
			string Text = Current.Text;
			int First = LineStart(Text, Current.Start);
			int Last = LineEnd(Text, Current.Start + Current.Length);
			string[] Lines = Text[First..Last].Split('\n');
			bool AllHave = Lines.All(Line => Existing.IsMatch(Line));
			StringBuilder Block = new();

			for (int i = 0; i < Lines.Length; i++)
			{
				if (i > 0)
					Block.Append('\n');

				string Line = Lines[i];

				if (AllHave)
					Block.Append(Existing.Replace(Line, string.Empty, 1));
				else
				{
					Block.Append(Prefix(i + 1));
					Block.Append(Existing.Replace(Line, string.Empty, 1));
				}
			}

			string Result = Text[..First] + Block.ToString() + Text[Last..];

			// A single empty line gets its mark, and the cursor goes after it.
			if (Current.Length == 0 && Lines.Length == 1 && Lines[0].Length == 0)
				return new Selection(Result, First + Block.Length, 0);

			return new Selection(Result, First, Block.Length);
		}

		private static Selection CodeBlock(Selection Current)
		{
			string Text = Current.Text;
			int First = LineStart(Text, Current.Start);
			int Last = LineEnd(Text, Current.Start + Current.Length);
			string Lines = Text[First..Last];

			// The lines are a code block already, with its fences selected too: remove the fences.
			if (Lines.StartsWith(fence + "\n", StringComparison.Ordinal) && Lines.EndsWith("\n" + fence, StringComparison.Ordinal) &&
				Lines.Length >= 2 * fence.Length + 2)
			{
				string Inner = Lines[(fence.Length + 1)..^(fence.Length + 1)];
				return new Selection(Text[..First] + Inner + Text[Last..], First, Inner.Length);
			}

			// The lines are inside a code block, as after the button was pressed: remove the fences around them.
			if (Text[..First].EndsWith(fence + "\n", StringComparison.Ordinal) &&
				Text[Last..].StartsWith("\n" + fence, StringComparison.Ordinal))
			{
				int OpenAt = First - fence.Length - 1;
				int CloseEnd = Last + fence.Length + 1;
				return new Selection(Text[..OpenAt] + Lines + Text[CloseEnd..], OpenAt, Lines.Length);
			}

			// A code block needs a blank line before it, unless it starts the text.
			string Before = First > 0 && !Text[..First].EndsWith("\n\n", StringComparison.Ordinal) ? "\n" : string.Empty;
			string Block = Before + fence + "\n" + Lines + "\n" + fence;
			string Result = Text[..First] + Block + Text[Last..];
			int ContentStart = First + Before.Length + fence.Length + 1;

			return new Selection(Result, ContentStart, Lines.Length);
		}

		private static int LineStart(string Text, int Position)
		{
			if (Position <= 0)
				return 0;

			int i = Text.LastIndexOf('\n', Position - 1);
			return i < 0 ? 0 : i + 1;
		}

		private static int LineEnd(string Text, int Position)
		{
			int i = Text.IndexOf('\n', Math.Min(Position, Text.Length));
			return i < 0 ? Text.Length : i;
		}

		[GeneratedRegex(@"^\s*[-*+]\s")]
		private static partial Regex BulletPrefix();

		[GeneratedRegex(@"^\s*\d{1,9}[.)]\s")]
		private static partial Regex NumberPrefix();

		[GeneratedRegex(@"^\s*>\s?")]
		private static partial Regex QuotePrefix();

		[GeneratedRegex(@"\*\*\S|\S\*\*|(^|[^*\w])\*[^*\s]|(^|[^\w])_[^_\s][^_]*_($|[^\w])|~~\S|`[^`]+`|\[[^\]\n]*\]\([^)\s]+\)|```")]
		private static partial Regex InlineFormatting();

		[GeneratedRegex(@"(^|\n)\s*([-*+]\s|\d{1,9}[.)]\s|>\s?|#{1,6}\s)", RegexOptions.None)]
		private static partial Regex BlockFormatting();
	}
}
