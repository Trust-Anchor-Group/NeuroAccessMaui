using System.Text;
using System.Text.RegularExpressions;

namespace NeuroAccessMaui.Services.Chat
{
	/// <summary>
	/// Prepares the markdown of chat messages.
	/// </summary>
	public static partial class ChatMarkdown
	{
		/// <summary>
		/// What markdown reads as a line break at the end of a line.
		/// </summary>
		private const string hardBreak = "  ";

		/// <summary>
		/// Makes each new line in a message a line break, as people expect in a chat. In markdown a single new line only
		/// joins two lines, so two spaces are added at the end of a line that the next line of the same paragraph follows.
		/// </summary>
		/// <remarks>
		/// Lines in code blocks, table rows, headings, and lines followed by a blank line or by the start of another block,
		/// such as a list item, are left as they are, so those render as before. Lines that already end in a line break are
		/// not changed, so the text can be prepared more than once.
		/// </remarks>
		/// <param name="Markdown">Markdown, as written.</param>
		/// <returns>Markdown that keeps its line breaks.</returns>
		public static string KeepLineBreaks(string Markdown)
		{
			if (string.IsNullOrEmpty(Markdown) || Markdown.IndexOf('\n') < 0)
				return Markdown;

			string[] Lines = Markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			StringBuilder Result = new(Markdown.Length + 16);
			string? Fence = null;

			for (int i = 0; i < Lines.Length; i++)
			{
				string Line = Lines[i];
				string Trimmed = Line.TrimStart();

				if (i > 0)
					Result.Append('\n');

				Result.Append(Line);

				if (Fence is not null)
				{
					if (Trimmed.StartsWith(Fence, StringComparison.Ordinal))
						Fence = null;

					continue;
				}

				string? Opened = FenceOf(Trimmed);
				if (Opened is not null)
				{
					Fence = Opened;
					continue;
				}

				if (i + 1 < Lines.Length && NeedsBreak(Line, Lines[i + 1]))
					Result.Append(hardBreak);
			}

			return Result.ToString();
		}

		/// <summary>
		/// If a line, followed by the next one, is in the middle of a paragraph and does not yet end in a line break.
		/// </summary>
		private static bool NeedsBreak(string Line, string Next)
		{
			if (string.IsNullOrWhiteSpace(Line) || string.IsNullOrWhiteSpace(Next))
				return false;

			if (Line.EndsWith(hardBreak, StringComparison.Ordinal) || Line.EndsWith('\\'))
				return false;

			if (IsIndentedCode(Line) || IsTableRow(Line) || IsHeading(Line))
				return false;

			string NextTrimmed = Next.TrimStart();

			// Lines of the same quote get line breaks too; a quote that starts after a line is a block of its own.
			if (NextTrimmed.StartsWith('>'))
				return Line.TrimStart().StartsWith('>');

			return !StartsBlock(NextTrimmed) && !IsTableRow(Next);
		}

		/// <summary>
		/// If a line, without its indentation, starts a block of its own: a list item, a heading, a code block, a rule or the
		/// line under a heading.
		/// </summary>
		private static bool StartsBlock(string Trimmed)
		{
			return ListItem().IsMatch(Trimmed) ||
				IsHeading(Trimmed) ||
				FenceOf(Trimmed) is not null ||
				RuleOrUnderline().IsMatch(Trimmed);
		}

		/// <summary>
		/// The fence that a line opens a code block with, if any.
		/// </summary>
		private static string? FenceOf(string Trimmed)
		{
			if (Trimmed.StartsWith("```", StringComparison.Ordinal))
				return "```";

			if (Trimmed.StartsWith("~~~", StringComparison.Ordinal))
				return "~~~";

			return null;
		}

		private static bool IsIndentedCode(string Line)
		{
			return Line.StartsWith('\t') || Line.StartsWith("    ", StringComparison.Ordinal);
		}

		private static bool IsTableRow(string Line)
		{
			string Trimmed = Line.Trim();
			return Trimmed.Length > 1 && (Trimmed.StartsWith('|') || Trimmed.EndsWith('|'));
		}

		private static bool IsHeading(string Line)
		{
			return Heading().IsMatch(Line.TrimStart());
		}

		[GeneratedRegex(@"^([-*+]|\d{1,9}[.)])\s")]
		private static partial Regex ListItem();

		[GeneratedRegex(@"^#{1,6}(\s|$)")]
		private static partial Regex Heading();

		[GeneratedRegex(@"^([-=*_])(\s*\1){2,}\s*$")]
		private static partial Regex RuleOrUnderline();
	}
}
