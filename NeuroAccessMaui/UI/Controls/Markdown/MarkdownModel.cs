using Waher.Content.Markdown;

namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// How a <see cref="MarkdownView"/> lays out its text.
	/// </summary>
	public enum MarkdownMode
	{
		/// <summary>
		/// The text sits in the page's own scrolling, such as a token description or a chat message. All blocks are built,
		/// a few at a time.
		/// </summary>
		Inline,

		/// <summary>
		/// The text owns its scrolling, such as a contract text or a report. Only the blocks on screen are built, and a
		/// Contents button jumps to any heading.
		/// </summary>
		Document
	}

	/// <summary>
	/// How much room the text takes.
	/// </summary>
	public enum MarkdownDensity
	{
		/// <summary>
		/// For reading: 16 px text and room between blocks.
		/// </summary>
		Comfortable,

		/// <summary>
		/// For small places such as chat bubbles: 15 px text, tighter gaps and smaller headings.
		/// </summary>
		Compact
	}

	/// <summary>
	/// Horizontal alignment of a block.
	/// </summary>
	public enum MdAlignment
	{
		/// <summary>Start of the line.</summary>
		Start,
		/// <summary>Centered.</summary>
		Center,
		/// <summary>End of the line.</summary>
		End
	}

	/// <summary>
	/// Formatting of a run of text.
	/// </summary>
	[Flags]
	public enum MdTextStyle
	{
		/// <summary>Plain text.</summary>
		None = 0,
		/// <summary>Bold.</summary>
		Bold = 1,
		/// <summary>Italic.</summary>
		Italic = 2,
		/// <summary>Struck through.</summary>
		Strikethrough = 4,
		/// <summary>Underlined.</summary>
		Underline = 8,
		/// <summary>Inline code.</summary>
		Code = 16,
		/// <summary>A value that is still missing, such as a contract parameter without a value. Shows its guide text on a
		/// warning tint.</summary>
		Placeholder = 32,
		/// <summary>A hash tag.</summary>
		Tag = 64,
		/// <summary>A footnote reference.</summary>
		FootnoteReference = 128
	}

	/// <summary>
	/// What an embed card points to.
	/// </summary>
	public enum MdEmbedKind
	{
		/// <summary>A web page or other link that leaves the app.</summary>
		Web,
		/// <summary>A token.</summary>
		Token,
		/// <summary>A legal identity.</summary>
		Identity,
		/// <summary>A smart contract.</summary>
		Contract,
		/// <summary>A payment.</summary>
		Payment,
		/// <summary>A chat address.</summary>
		Chat,
		/// <summary>Another link the app opens itself.</summary>
		App
	}

	/// <summary>
	/// Kinds of lists.
	/// </summary>
	public enum MdListKind
	{
		/// <summary>Bullet list.</summary>
		Bullet,
		/// <summary>Numbered list.</summary>
		Numbered,
		/// <summary>Task list, with check boxes.</summary>
		Task
	}

	/// <summary>
	/// Kinds of framed blocks.
	/// </summary>
	public enum MdFrameKind
	{
		/// <summary>A quote.</summary>
		Quote,
		/// <summary>Inserted text, when two versions are compared.</summary>
		Inserted,
		/// <summary>Deleted text, when two versions are compared.</summary>
		Deleted,
		/// <summary>Indented text without a frame, such as definitions.</summary>
		Indented
	}

	/// <summary>
	/// A run of text with one formatting.
	/// </summary>
	/// <param name="Text">The text.</param>
	/// <param name="Style">Its formatting.</param>
	/// <param name="Link">Where it links to, if anywhere.</param>
	public sealed record MdRun(string Text, MdTextStyle Style, string? Link);

	/// <summary>
	/// A block of the text model. The model holds no MAUI types, so it can be built off the UI thread.
	/// </summary>
	public abstract class MdBlock
	{
		/// <summary>
		/// Horizontal alignment.
		/// </summary>
		public MdAlignment Alignment { get; init; }
	}

	/// <summary>
	/// A paragraph: one label, with its formatting as spans.
	/// </summary>
	public sealed class MdParagraph : MdBlock
	{
		/// <summary>
		/// The runs of text.
		/// </summary>
		public required IReadOnlyList<MdRun> Runs { get; init; }
	}

	/// <summary>
	/// A heading.
	/// </summary>
	public sealed class MdHeading : MdBlock
	{
		/// <summary>
		/// Heading level, from 1.
		/// </summary>
		public required int Level { get; init; }

		/// <summary>
		/// The runs of text.
		/// </summary>
		public required IReadOnlyList<MdRun> Runs { get; init; }

		/// <summary>
		/// The heading as plain text, as shown in Contents.
		/// </summary>
		public required string Text { get; init; }

		/// <summary>
		/// The heading's anchor, for links such as [see](#terms).
		/// </summary>
		public required string Anchor { get; init; }
	}

	/// <summary>
	/// A list.
	/// </summary>
	public sealed class MdList : MdBlock
	{
		/// <summary>
		/// Kind of list.
		/// </summary>
		public required MdListKind Kind { get; init; }

		/// <summary>
		/// The items.
		/// </summary>
		public required IReadOnlyList<MdListItem> Items { get; init; }
	}

	/// <summary>
	/// An item in a list.
	/// </summary>
	public sealed class MdListItem
	{
		/// <summary>
		/// The marker, such as a bullet or a number. Empty for task items.
		/// </summary>
		public required string Marker { get; init; }

		/// <summary>
		/// For task items, if the task is done.
		/// </summary>
		public bool? Checked { get; init; }

		/// <summary>
		/// The item's blocks.
		/// </summary>
		public required List<MdBlock> Blocks { get; init; }
	}

	/// <summary>
	/// A framed or indented group of blocks, such as a quote.
	/// </summary>
	public sealed class MdFrame : MdBlock
	{
		/// <summary>
		/// Kind of frame.
		/// </summary>
		public required MdFrameKind Kind { get; init; }

		/// <summary>
		/// The blocks in the frame.
		/// </summary>
		public required List<MdBlock> Blocks { get; init; }
	}

	/// <summary>
	/// A code block.
	/// </summary>
	public sealed class MdCode : MdBlock
	{
		/// <summary>
		/// The language, if given.
		/// </summary>
		public string? Language { get; init; }

		/// <summary>
		/// The code.
		/// </summary>
		public required string Text { get; init; }
	}

	/// <summary>
	/// A table.
	/// </summary>
	public sealed class MdTable : MdBlock
	{
		/// <summary>
		/// Number of columns.
		/// </summary>
		public required int Columns { get; init; }

		/// <summary>
		/// Rows, header rows first.
		/// </summary>
		public required IReadOnlyList<MdTableRow> Rows { get; init; }

		/// <summary>
		/// Caption, if any.
		/// </summary>
		public string? Caption { get; init; }
	}

	/// <summary>
	/// A table row.
	/// </summary>
	/// <param name="IsHeader">If the row is a header row.</param>
	/// <param name="Cells">The cells of the row.</param>
	public sealed record MdTableRow(bool IsHeader, IReadOnlyList<MdTableCell> Cells);

	/// <summary>
	/// A table cell.
	/// </summary>
	public sealed class MdTableCell
	{
		/// <summary>
		/// Column of the cell.
		/// </summary>
		public required int Column { get; init; }

		/// <summary>
		/// Number of columns the cell spans.
		/// </summary>
		public required int ColumnSpan { get; init; }

		/// <summary>
		/// Alignment of the cell's text.
		/// </summary>
		public required MdAlignment Alignment { get; init; }

		/// <summary>
		/// The cell's text, when it is one line of text.
		/// </summary>
		public IReadOnlyList<MdRun>? Runs { get; init; }

		/// <summary>
		/// The cell's blocks, when it holds more than text.
		/// </summary>
		public IReadOnlyList<MdBlock>? Blocks { get; init; }
	}

	/// <summary>
	/// A horizontal line.
	/// </summary>
	public sealed class MdRule : MdBlock
	{
	}

	/// <summary>
	/// An image or a chart. It is loaded when it is first shown.
	/// </summary>
	public sealed class MdImage : MdBlock
	{
		/// <summary>
		/// Where the image comes from.
		/// </summary>
		public required MdImageSource Source { get; init; }

		/// <summary>
		/// Caption shown below the image, if any.
		/// </summary>
		public string? Caption { get; init; }

		/// <summary>
		/// What the image shows, for screen readers and the image viewer.
		/// </summary>
		public string? Description { get; init; }

		/// <summary>
		/// If the image is a chart drawn from data, which takes a moment to draw.
		/// </summary>
		public bool IsChart { get; init; }
	}

	/// <summary>
	/// A card for something the app can open, such as a token, an identity, a contract or a web page.
	/// </summary>
	public sealed class MdEmbed : MdBlock
	{
		/// <summary>
		/// What the card points to.
		/// </summary>
		public required MdEmbedKind Kind { get; init; }

		/// <summary>
		/// Title.
		/// </summary>
		public required string Title { get; init; }

		/// <summary>
		/// Second line, such as the kind of object or the web site.
		/// </summary>
		public string? Subtitle { get; init; }

		/// <summary>
		/// The link that is opened when the card is tapped.
		/// </summary>
		public required string Uri { get; init; }
	}

	/// <summary>
	/// HTML that markdown passed through as is.
	/// </summary>
	public sealed class MdHtml : MdBlock
	{
		/// <summary>
		/// The HTML.
		/// </summary>
		public required string Html { get; init; }
	}

	/// <summary>
	/// A short message in place of content that could not be shown, such as an error.
	/// </summary>
	public sealed class MdNotice : MdBlock
	{
		/// <summary>
		/// The message.
		/// </summary>
		public required string Text { get; init; }
	}

	/// <summary>
	/// The footnotes at the end of the text.
	/// </summary>
	public sealed class MdFootnotes : MdBlock
	{
		/// <summary>
		/// The footnotes, in order.
		/// </summary>
		public required IReadOnlyList<MdFootnote> Items { get; init; }
	}

	/// <summary>
	/// A footnote.
	/// </summary>
	/// <param name="Number">Its number.</param>
	/// <param name="Blocks">Its text.</param>
	public sealed record MdFootnote(int Number, IReadOnlyList<MdBlock> Blocks);

	/// <summary>
	/// A text, built from markdown into blocks that a <see cref="MarkdownView"/> shows. The model holds no MAUI types, so
	/// it is built off the UI thread.
	/// </summary>
	public sealed class MarkdownModel
	{
		/// <summary>
		/// An empty text.
		/// </summary>
		public static readonly MarkdownModel Empty = new([]);

		/// <summary>
		/// Creates a text model.
		/// </summary>
		/// <param name="Blocks">The top-level blocks.</param>
		public MarkdownModel(IReadOnlyList<MdBlock> Blocks)
		{
			this.Blocks = Blocks;

			List<MdHeading> Headings = [];
			List<MdImage> Images = [];

			foreach (MdBlock Block in Blocks)
			{
				if (Block is MdHeading Heading)
					Headings.Add(Heading);
				else if (Block is MdImage Image)
					Images.Add(Image);
			}

			this.Headings = Headings;
			this.Images = Images;
		}

		/// <summary>
		/// The top-level blocks. In Document mode, each is one item of the list.
		/// </summary>
		public IReadOnlyList<MdBlock> Blocks { get; }

		/// <summary>
		/// The top-level headings.
		/// </summary>
		public IReadOnlyList<MdHeading> Headings { get; }

		/// <summary>
		/// The top-level images and charts.
		/// </summary>
		public IReadOnlyList<MdImage> Images { get; }

		/// <summary>
		/// If the text has no blocks.
		/// </summary>
		public bool IsEmpty => this.Blocks.Count == 0;

		/// <summary>
		/// The image of a text that has exactly one image or chart, such as a state diagram report. Such a report is shown as
		/// the image, where it can be zoomed.
		/// </summary>
		public MdImage? SingleImage => this.Images.Count == 1 ? this.Images[0] : null;

		/// <summary>
		/// Parses markdown and builds its text model, off the UI thread.
		/// </summary>
		/// <param name="Markdown">Markdown text.</param>
		/// <param name="Options">How the model is built.</param>
		/// <returns>The text model.</returns>
		public static Task<MarkdownModel> FromMarkdownAsync(string? Markdown, MarkdownBuildOptions? Options = null)
		{
			if (string.IsNullOrWhiteSpace(Markdown))
				return Task.FromResult(Empty);

			return Task.Run(async () =>
			{
				MarkdownDocument Doc = await ParseAsync(Markdown);
				return await BuildAsync(Doc, Options);
			});
		}

		/// <summary>
		/// Builds the text model of a parsed markdown document, off the UI thread.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Options">How the model is built.</param>
		/// <returns>The text model.</returns>
		public static Task<MarkdownModel> FromDocumentAsync(MarkdownDocument? Document, MarkdownBuildOptions? Options = null)
		{
			if (Document is null)
				return Task.FromResult(Empty);

			return Task.Run(() => BuildAsync(Document, Options));
		}

		/// <summary>
		/// Parses markdown with the settings the app uses for text from others: no scripts, no autoplay, no meta data.
		/// </summary>
		/// <param name="Markdown">Markdown text.</param>
		/// <returns>Parsed document.</returns>
		public static Task<MarkdownDocument> ParseAsync(string Markdown)
		{
			MarkdownSettings Settings = new()
			{
				AllowScriptTag = false,
				EmbedEmojis = false,
				AudioAutoplay = false,
				AudioControls = false,
				ParseMetaData = false,
				VideoAutoplay = false,
				VideoControls = false
			};

			return MarkdownDocument.CreateAsync(Markdown, Settings);
		}

		private static async Task<MarkdownModel> BuildAsync(MarkdownDocument Document, MarkdownBuildOptions? Options)
		{
			using MarkdownBlockBuilder Builder = new(Document, Options ?? MarkdownBuildOptions.Default);
			await Document.RenderDocument(Builder);
			return new MarkdownModel(Builder.Blocks);
		}
	}

	/// <summary>
	/// How a text model is built.
	/// </summary>
	public sealed record MarkdownBuildOptions
	{
		/// <summary>
		/// The options used when none are given.
		/// </summary>
		public static readonly MarkdownBuildOptions Default = new();

		/// <summary>
		/// The options for contract texts, where inline code is a parameter that has no value yet.
		/// </summary>
		public static readonly MarkdownBuildOptions Contract = new() { InlineCodeIsPlaceholder = true };

		/// <summary>
		/// If inline code is a missing value, such as a contract parameter without a value, shown on a warning tint.
		/// </summary>
		public bool InlineCodeIsPlaceholder { get; init; }

		/// <summary>
		/// If a top-level paragraph that is nothing but a link becomes a card, at least 48 high.
		/// </summary>
		public bool LinkCards { get; init; } = true;
	}
}
