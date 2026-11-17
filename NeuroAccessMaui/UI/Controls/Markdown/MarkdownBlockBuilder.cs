using System.Globalization;
using System.Text;
using System.Xml;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using SkiaSharp;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.BlockElements;
using Waher.Content.Markdown.Model.SpanElements;
using Waher.Content.Markdown.Rendering;
using Waher.Events;
using Waher.Networking.XMPP.Contracts;
using Waher.Script;
using Waher.Script.Graphs;
using MdMultimedia = Waher.Content.Markdown.Model.SpanElements.Multimedia;
using WaherAlignment = Waher.Content.Markdown.Model.TextAlignment;

namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// Builds the block model of a markdown document. It creates no MAUI objects, so it runs off the UI thread.
	/// </summary>
	/// <remarks>
	/// Text is collected into runs. A run is added to the paragraph that is open; a block element first closes the open
	/// paragraph, and then adds its block to the list of blocks it is inside, such as a list item or a quote.
	/// </remarks>
	internal sealed class MarkdownBlockBuilder : IRenderer
	{
		private readonly MarkdownBuildOptions options;
		private readonly List<MdBlock> blocks = [];
		private MarkdownDocument document;
		private List<MdBlock> target;
		private List<MdRun>? runs;
		private MdAlignment runsAlignment;
		private MdAlignment alignment = MdAlignment.Start;
		private MdTextStyle style;
		private string? link;
		private bool superscript;
		private bool subscript;
		private int depth;
		private int inlineOnly;

		/// <summary>
		/// Builds the block model of a markdown document.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Options">How the model is built.</param>
		public MarkdownBlockBuilder(MarkdownDocument Document, MarkdownBuildOptions Options)
		{
			this.document = Document;
			this.options = Options;
			this.target = this.blocks;
		}

		/// <summary>
		/// The top-level blocks.
		/// </summary>
		public List<MdBlock> Blocks => this.blocks;

		/// <inheritdoc/>
		public void Dispose()
		{
		}

		#region Document

		/// <inheritdoc/>
		public async Task RenderDocument(MarkdownDocument Document, bool Inclusion)
		{
			MarkdownDocument Bak = this.document;
			this.document = Document;

			try
			{
				await this.RenderBlocks(Document.Elements);
				this.FlushRuns();

				if (!Inclusion)
					await this.RenderFootnotes();
			}
			finally
			{
				this.document = Bak;
			}
		}

		private async Task RenderFootnotes()
		{
			// A text without footnotes has no footnote order.
			IEnumerable<string>? Order = this.document.FootnoteOrder;
			if (Order is null)
				return;

			List<MdFootnote> Items = [];

			foreach (string Key in Order)
			{
				if (!this.document.TryGetFootnoteNumber(Key, out int Number) ||
					!this.document.TryGetFootnote(Key, out Footnote Note) ||
					Note is null ||
					!Note.Referenced)
				{
					continue;
				}

				MdTextStyle StyleBak = this.style;
				this.style = MdTextStyle.None;

				List<MdBlock> Blocks = await this.CaptureBlocks(() => this.RenderBlocks(Note.Children));

				this.style = StyleBak;
				Items.Add(new MdFootnote(Number, Blocks));
			}

			if (Items.Count > 0)
				this.Add(new MdFootnotes { Items = Items });
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Renders child elements that may be blocks or text. Text goes into the open paragraph.
		/// </summary>
		private async Task RenderBlocks(IEnumerable<MarkdownElement>? Children)
		{
			if (Children is null)
				return;

			foreach (MarkdownElement E in Children)
			{
				if (E.InlineSpanElement && !E.OutsideParagraph)
					this.EnsureRuns();

				await E.Render(this);
			}
		}

		/// <summary>
		/// Renders child elements as text.
		/// </summary>
		private async Task RenderInline(IEnumerable<MarkdownElement>? Children)
		{
			if (Children is null)
				return;

			foreach (MarkdownElement E in Children)
				await E.Render(this);
		}

		/// <summary>
		/// Renders the blocks <paramref name="Render"/> makes into a list of their own, such as a list item's blocks.
		/// </summary>
		private async Task<List<MdBlock>> CaptureBlocks(Func<Task> Render)
		{
			this.FlushRuns();

			List<MdBlock> Bak = this.target;
			List<MdBlock> Result = [];

			this.target = Result;
			this.depth++;

			try
			{
				await Render();
				this.FlushRuns();
			}
			finally
			{
				this.target = Bak;
				this.depth--;
			}

			return Result;
		}

		/// <summary>
		/// Renders elements as text into runs of their own, without closing the open paragraph.
		/// </summary>
		private async Task<List<MdRun>> CaptureRuns(IEnumerable<MarkdownElement>? Children)
		{
			List<MdRun>? Bak = this.runs;
			MdAlignment AlignmentBak = this.runsAlignment;
			List<MdRun> Result = [];

			this.runs = Result;
			this.inlineOnly++;

			try
			{
				await this.RenderInline(Children);
			}
			finally
			{
				this.runs = Bak;
				this.runsAlignment = AlignmentBak;
				this.inlineOnly--;
			}

			return Result;
		}

		private static string PlainText(IEnumerable<MdRun> Runs)
		{
			StringBuilder Sb = new();

			foreach (MdRun Run in Runs)
				Sb.Append(Run.Text);

			return Sb.ToString().Trim();
		}

		private void EnsureRuns()
		{
			if (this.runs is null)
			{
				this.runs = [];
				this.runsAlignment = this.alignment;
			}
		}

		private void AddRun(string? Text, MdTextStyle Extra = MdTextStyle.None, string? Link = null)
		{
			if (string.IsNullOrEmpty(Text))
				return;

			if (this.superscript)
				Text = TextRenderer.ToSuperscript(Text);
			else if (this.subscript)
				Text = TextRenderer.ToSubscript(Text);

			this.EnsureRuns();

			MdTextStyle Style = this.style | Extra;
			string? RunLink = Link ?? this.link;
			List<MdRun> Runs = this.runs!;

			// Neighbouring text with the same formatting becomes one span.
			if (Runs.Count > 0)
			{
				MdRun Last = Runs[^1];
				if (Last.Style == Style && Last.Link == RunLink)
				{
					Runs[^1] = Last with { Text = Last.Text + Text };
					return;
				}
			}

			Runs.Add(new MdRun(Text, Style, RunLink));
		}

		/// <summary>
		/// Closes the open paragraph, if it has any text.
		/// </summary>
		private void FlushRuns()
		{
			List<MdRun>? Runs = this.runs;
			this.runs = null;

			if (Runs is null || Runs.Count == 0)
				return;

			bool HasText = false;
			foreach (MdRun Run in Runs)
			{
				if (!string.IsNullOrWhiteSpace(Run.Text))
				{
					HasText = true;
					break;
				}
			}

			if (!HasText)
				return;

			// A paragraph does not start or end with a line break.
			if (Runs[0].Text.StartsWith('\n') || Runs[^1].Text.EndsWith('\n'))
			{
				Runs[0] = Runs[0] with { Text = Runs[0].Text.TrimStart('\n') };
				Runs[^1] = Runs[^1] with { Text = Runs[^1].Text.TrimEnd('\n') };
			}

			this.target.Add(new MdParagraph { Runs = Runs, Alignment = this.runsAlignment });
		}

		private void Add(MdBlock Block)
		{
			this.FlushRuns();
			this.target.Add(Block);
		}

		private async Task WithStyle(MdTextStyle Style, IEnumerable<MarkdownElement>? Children)
		{
			MdTextStyle Bak = this.style;
			this.style |= Style;

			try
			{
				await this.RenderInline(Children);
			}
			finally
			{
				this.style = Bak;
			}
		}

		private async Task WithAlignment(MdAlignment Alignment, IEnumerable<MarkdownElement>? Children)
		{
			this.FlushRuns();

			MdAlignment Bak = this.alignment;
			this.alignment = Alignment;

			try
			{
				await this.RenderBlocks(Children);
				this.FlushRuns();
			}
			finally
			{
				this.alignment = Bak;
			}
		}

		private async Task WithLink(string? Url, IEnumerable<MarkdownElement>? Children)
		{
			string? Bak = this.link;
			this.link = Url;

			try
			{
				await this.RenderInline(Children);
			}
			finally
			{
				this.link = Bak;
			}
		}

		private async Task RenderSingleChild(MarkdownElementSingleChild Element)
		{
			MarkdownElement Child = Element.Child;
			if (Child is null)
				return;

			if (Child.InlineSpanElement && !Child.OutsideParagraph)
				this.EnsureRuns();

			await Child.Render(this);
		}

		private static MdAlignment ToAlignment(WaherAlignment Alignment)
		{
			return Alignment switch
			{
				WaherAlignment.Center => MdAlignment.Center,
				WaherAlignment.Right => MdAlignment.End,
				_ => MdAlignment.Start
			};
		}

		/// <summary>
		/// Makes the anchor of a heading, as in [see](#payment-terms).
		/// </summary>
		/// <param name="Text">Heading text.</param>
		/// <returns>Anchor.</returns>
		internal static string ToAnchor(string Text)
		{
			StringBuilder Sb = new();
			bool Dash = false;

			foreach (char ch in Text.Trim().ToLowerInvariant())
			{
				if (char.IsLetterOrDigit(ch))
				{
					if (Dash && Sb.Length > 0)
						Sb.Append('-');

					Sb.Append(ch);
					Dash = false;
				}
				else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
					Dash = true;
			}

			return Sb.ToString();
		}

		#endregion

		#region Links and embeds

		/// <summary>
		/// What a link points to, from its scheme.
		/// </summary>
		/// <param name="Url">Link.</param>
		/// <returns>Kind of card.</returns>
		internal static MdEmbedKind KindOf(string Url)
		{
			return Constants.UriSchemes.GetScheme(Url) switch
			{
				Constants.UriSchemes.NeuroFeature => MdEmbedKind.Token,
				Constants.UriSchemes.IotId => MdEmbedKind.Identity,
				Constants.UriSchemes.IotSc => MdEmbedKind.Contract,
				Constants.UriSchemes.EDaler => MdEmbedKind.Payment,
				Constants.UriSchemes.Xmpp => MdEmbedKind.Chat,
				null => MdEmbedKind.Web,
				_ => MdEmbedKind.App
			};
		}

		/// <summary>
		/// The localized name of what a card points to, shown as its second line.
		/// </summary>
		internal static string? KindName(MdEmbedKind Kind)
		{
			return Kind switch
			{
				MdEmbedKind.Token => ServiceRef.Localizer[nameof(AppResources.MarkdownToken)],
				MdEmbedKind.Identity => ServiceRef.Localizer[nameof(AppResources.LegalId)],
				MdEmbedKind.Contract => ServiceRef.Localizer[nameof(AppResources.MarkdownContract)],
				MdEmbedKind.Payment => ServiceRef.Localizer[nameof(AppResources.Payment)],
				MdEmbedKind.Chat => ServiceRef.Localizer[nameof(AppResources.Chat)],
				_ => (string?)null
			};
		}

		/// <summary>
		/// The second line of a card for a web link: the site, or the mail address.
		/// </summary>
		private static string? WebSubtitle(string Url)
		{
			if (Url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
				return Url[7..];

			if (Uri.TryCreate(Url, UriKind.Absolute, out Uri? Parsed) && !string.IsNullOrEmpty(Parsed.Host))
			{
				string Host = Parsed.Host;
				return Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? Host[4..] : Host;
			}

			return null;
		}

		/// <summary>
		/// The second line of a card for a link to something else in the app, such as a thing.
		/// </summary>
		private static string? AppSubtitle(string Url)
		{
			if (Constants.UriSchemes.GetScheme(Url) == Constants.UriSchemes.IotDisco)
				return ServiceRef.Localizer[nameof(AppResources.Thing)];

			return null;
		}

		private static MdEmbed CreateLinkCard(string Url, string? Text, MdAlignment Alignment)
		{
			MdEmbedKind Kind = KindOf(Url);
			string Title = string.IsNullOrWhiteSpace(Text) ? Url : Text;
			string? Subtitle = Kind switch
			{
				MdEmbedKind.Web => WebSubtitle(Url),
				MdEmbedKind.App => AppSubtitle(Url),
				_ => KindName(Kind)
			};

			if (string.Equals(Subtitle, Title, StringComparison.OrdinalIgnoreCase))
				Subtitle = null;

			return new MdEmbed
			{
				Kind = Kind,
				Title = Title,
				Subtitle = Subtitle,
				Uri = Url,
				Alignment = Alignment
			};
		}

		/// <summary>
		/// If a paragraph is nothing but a link.
		/// </summary>
		private async Task<(string Url, string Text)?> GetLinkOnly(Paragraph Element)
		{
			if (!Element.HasOneChild)
				return null;

			switch (Element.FirstChild)
			{
				case Link L when !string.IsNullOrEmpty(L.Url):
					return (L.Url, PlainText(await this.CaptureRuns(L.Children)));

				case AutomaticLinkUrl Auto:
					return (Auto.URL, Auto.URL);

				case LinkReference Reference:
					MdMultimedia? Target = this.document.GetReference(Reference.Label);
					if (Target is null || Target.Items.Length == 0)
						return null;

					return (Target.Items[0].Url, PlainText(await this.CaptureRuns(Reference.Children)));

				default:
					return null;
			}
		}

		/// <summary>
		/// Makes a card for a token, an identity or a contract sent as XML in a code block, as chat messages do.
		/// </summary>
		private static async Task<MdBlock?> TryCreateObjectCard(string Language, string Xml)
		{
			string Scheme = Language.ToLowerInvariant();

			if (Scheme != Constants.UriSchemes.NeuroFeature &&
				Scheme != Constants.UriSchemes.IotId &&
				Scheme != Constants.UriSchemes.IotSc)
			{
				return null;
			}

			try
			{
				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};
				Doc.LoadXml(Xml);

				string? Title;
				MdEmbedKind Kind;

				switch (Scheme)
				{
					case Constants.UriSchemes.NeuroFeature:
						NeuroFeatures.Token? Token = await NeuroFeatures.Token.TryParse(Doc.DocumentElement);
						if (Token is null)
							throw new Exception(ServiceRef.Localizer[nameof(AppResources.InvalidNeuroFeatureToken)]);

						Title = Token.FriendlyName;
						Kind = MdEmbedKind.Token;
						break;

					case Constants.UriSchemes.IotId:
						LegalIdentity Identity = LegalIdentity.Parse(Doc.DocumentElement);
						Title = ContactInfo.GetFriendlyName(Identity);
						Kind = MdEmbedKind.Identity;
						break;

					default:
						ParsedContract Parsed = await Contract.Parse(Doc.DocumentElement, ServiceRef.XmppService.ContractsClient, false);
						if (Parsed?.Contract is null)
							throw new Exception(ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeOpened)]);

						Title = await ContractModel.GetCategory(Parsed.Contract) ?? Parsed.Contract.ContractId;
						Kind = MdEmbedKind.Contract;
						break;
				}

				return new MdEmbed
				{
					Kind = Kind,
					Title = string.IsNullOrWhiteSpace(Title) ? KindName(Kind) ?? Scheme : Title,
					Subtitle = KindName(Kind),
					Uri = Scheme + ":" + Xml
				};
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return new MdNotice { Text = Ex.Message };
			}
		}

		#endregion

		#region Block elements

		/// <inheritdoc/>
		public async Task Render(BlockQuote Element)
		{
			List<MdBlock> Blocks = await this.CaptureBlocks(() => this.RenderBlocks(Element.Children));
			this.Add(new MdFrame { Kind = MdFrameKind.Quote, Blocks = Blocks, Alignment = this.alignment });
		}

		/// <inheritdoc/>
		public async Task Render(BulletList Element)
		{
			List<MdListItem> Items = [];

			foreach (MarkdownElement E in Element.Children)
				Items.Add(await this.RenderListItem(E, "•", null));

			this.Add(new MdList { Kind = MdListKind.Bullet, Items = Items, Alignment = this.alignment });
		}

		/// <inheritdoc/>
		public async Task Render(NumberedList Element)
		{
			List<MdListItem> Items = [];
			int Expected = 0;

			foreach (MarkdownElement E in Element.Children)
			{
				Expected++;

				if (E is NumberedItem Item)
					Expected = Item.Number;

				Items.Add(await this.RenderListItem(E, Expected.ToString(CultureInfo.CurrentCulture) + ".", null));
			}

			this.Add(new MdList { Kind = MdListKind.Numbered, Items = Items, Alignment = this.alignment });
		}

		/// <inheritdoc/>
		public async Task Render(TaskList Element)
		{
			List<MdListItem> Items = [];

			foreach (MarkdownElement E in Element.Children)
				Items.Add(await this.RenderListItem(E, string.Empty, E is TaskItem Item && Item.IsChecked));

			this.Add(new MdList { Kind = MdListKind.Task, Items = Items, Alignment = this.alignment });
		}

		private async Task<MdListItem> RenderListItem(MarkdownElement Item, string Marker, bool? Checked)
		{
			List<MdBlock> Blocks = await this.CaptureBlocks(async () =>
			{
				if (Item.InlineSpanElement && !Item.OutsideParagraph)
					this.EnsureRuns();

				await Item.Render(this);
			});

			return new MdListItem
			{
				Marker = Marker,
				Checked = Checked,
				Blocks = Blocks
			};
		}

		/// <inheritdoc/>
		public Task Render(UnnumberedItem Element) => this.RenderSingleChild(Element);

		/// <inheritdoc/>
		public Task Render(NumberedItem Element) => this.RenderSingleChild(Element);

		/// <inheritdoc/>
		public Task Render(TaskItem Element) => this.RenderSingleChild(Element);

		/// <inheritdoc/>
		public Task Render(CenterAligned Element) => this.WithAlignment(MdAlignment.Center, Element.Children);

		/// <inheritdoc/>
		public Task Render(LeftAligned Element) => this.WithAlignment(MdAlignment.Start, Element.Children);

		/// <inheritdoc/>
		public Task Render(RightAligned Element) => this.WithAlignment(MdAlignment.End, Element.Children);

		/// <inheritdoc/>
		public Task Render(MarginAligned Element) => this.WithAlignment(MdAlignment.Start, Element.Children);

		/// <inheritdoc/>
		public async Task Render(CodeBlock Element)
		{
			StringBuilder Sb = new();
			int Start = Math.Max(0, Element.Start);
			int End = Math.Min(Element.Rows.Length - 1, Element.End);

			for (int i = Start; i <= End; i++)
			{
				if (i > Start)
					Sb.Append('\n');

				Sb.Append(Element.Rows[i]);
			}

			string Text = Sb.ToString();
			string Language = Element.Language ?? string.Empty;

			// A chart sent as graph XML, as in a token's history report, is drawn when it is first shown.
			if (string.Equals(Language, "graph", StringComparison.OrdinalIgnoreCase))
			{
				string[] Rows = Element.Rows;
				MarkdownDocument Doc = Element.Document;

				this.Add(new MdImage
				{
					Source = MdImageSource.FromPng(async () =>
					{
						PixelInformation Pixels = await new Waher.Content.Markdown.Model.CodeContent.GraphContent().GenerateImage(Rows, "graph", Doc);
						return (Pixels.EncodeAsPng(), Pixels.Width, Pixels.Height);
					}),
					IsChart = true,
					Alignment = this.alignment
				});

				return;
			}

			if (Language.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			{
				string Base64 = string.Concat(Element.Rows);

				this.Add(new MdImage
				{
					Source = new MdImageSource(() => MdImageSource.FromBytesAsync(Convert.FromBase64String(Base64)), null, null),
					Alignment = this.alignment
				});

				return;
			}

			MdBlock? Card = await TryCreateObjectCard(Language, Text);
			if (Card is not null)
			{
				this.Add(Card);
				return;
			}

			this.Add(new MdCode
			{
				Language = string.IsNullOrWhiteSpace(Language) ? null : Language,
				Text = Text
			});
		}

		/// <inheritdoc/>
		public Task Render(CommentBlock Element)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(DefinitionList Element) => this.RenderBlocks(Element.Children);

		/// <inheritdoc/>
		public async Task Render(DefinitionTerms Element)
		{
			foreach (MarkdownElement Term in Element.Children)
			{
				this.FlushRuns();
				this.EnsureRuns();
				await this.WithStyle(MdTextStyle.Bold, [Term]);
				this.FlushRuns();
			}
		}

		/// <inheritdoc/>
		public async Task Render(DefinitionDescriptions Element)
		{
			List<MdBlock> Blocks = await this.CaptureBlocks(() => this.RenderBlocks(Element.Children));
			this.Add(new MdFrame { Kind = MdFrameKind.Indented, Blocks = Blocks, Alignment = this.alignment });
		}

		/// <inheritdoc/>
		public async Task Render(DeleteBlocks Element)
		{
			List<MdBlock> Blocks = await this.CaptureBlocks(() => this.RenderBlocks(Element.Children));
			this.Add(new MdFrame { Kind = MdFrameKind.Deleted, Blocks = Blocks, Alignment = this.alignment });
		}

		/// <inheritdoc/>
		public async Task Render(InsertBlocks Element)
		{
			List<MdBlock> Blocks = await this.CaptureBlocks(() => this.RenderBlocks(Element.Children));
			this.Add(new MdFrame { Kind = MdFrameKind.Inserted, Blocks = Blocks, Alignment = this.alignment });
		}

		/// <inheritdoc/>
		public Task Render(Footnote Element) => this.RenderBlocks(Element.Children);

		/// <inheritdoc/>
		public async Task Render(Header Element)
		{
			this.FlushRuns();

			List<MdRun> Runs = await this.CaptureRuns(Element.Children);
			string Text = PlainText(Runs);

			if (Text.Length == 0)
				return;

			this.Add(new MdHeading
			{
				Level = Math.Clamp(Element.Level, 1, 9),
				Runs = Runs,
				Text = Text,
				Anchor = ToAnchor(Text),
				Alignment = this.alignment
			});
		}

		/// <inheritdoc/>
		public Task Render(HorizontalRule Element)
		{
			this.Add(new MdRule());
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task Render(HtmlBlock Element)
		{
			using HtmlRenderer Html = new(new HtmlSettings()
			{
				XmlEntitiesOnly = true
			}, this.document);

			await Html.RenderChildren(Element);

			string Text = Html.ToString();
			if (!string.IsNullOrWhiteSpace(Text))
				this.Add(new MdHtml { Html = Text, Alignment = this.alignment });
		}

		/// <inheritdoc/>
		public Task Render(InvisibleBreak Element)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(NestedBlock Element) => this.RenderBlocks(Element.Children);

		/// <inheritdoc/>
		public async Task Render(Paragraph Element)
		{
			this.FlushRuns();

			if (this.options.LinkCards && this.depth == 0 && this.inlineOnly == 0)
			{
				(string Url, string Text)? LinkOnly = await this.GetLinkOnly(Element);
				if (LinkOnly.HasValue)
				{
					this.Add(CreateLinkCard(LinkOnly.Value.Url, LinkOnly.Value.Text, this.alignment));
					return;
				}
			}

			this.EnsureRuns();
			await this.RenderInline(Element.Children);
			this.FlushRuns();
		}

		/// <inheritdoc/>
		public Task Render(Sections Element) => this.RenderBlocks(Element.Children);

		/// <inheritdoc/>
		public Task Render(SectionSeparator Element)
		{
			this.Add(new MdRule());
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task Render(Table Element)
		{
			this.FlushRuns();

			List<MdTableRow> Rows = [];

			for (int i = 0; i < Element.Headers.Length; i++)
				Rows.Add(await this.RenderTableRow(Element.Headers[i], Element.HeaderCellAlignments[i], true, Element));

			for (int i = 0; i < Element.Rows.Length; i++)
				Rows.Add(await this.RenderTableRow(Element.Rows[i], Element.RowCellAlignments[i], false, Element));

			this.Add(new MdTable
			{
				Columns = Element.Columns,
				Rows = Rows,
				Caption = string.IsNullOrWhiteSpace(Element.Caption) ? null : Element.Caption
			});
		}

		private async Task<MdTableRow> RenderTableRow(MarkdownElement[] Row, WaherAlignment?[] CellAlignments, bool IsHeader, Table Element)
		{
			List<MdTableCell> Cells = [];
			int NrColumns = Element.Columns;
			MdTextStyle StyleBak = this.style;
			string? LinkBak = this.link;
			MdAlignment AlignmentBak = this.alignment;

			try
			{
				for (int Column = 0; Column < NrColumns; Column++)
				{
					MarkdownElement E = Row[Column];
					if (E is null)
						continue;

					int ColumnSpan = Column + 1;
					while (ColumnSpan < NrColumns && Row[ColumnSpan] is null)
						ColumnSpan++;

					ColumnSpan -= Column;

					MdAlignment Alignment = ToAlignment(CellAlignments[Column] ?? Element.ColumnAlignments[Column]);

					this.style = IsHeader ? MdTextStyle.Bold : MdTextStyle.None;
					this.link = null;
					this.alignment = Alignment;

					if (E.InlineSpanElement)
					{
						Cells.Add(new MdTableCell
						{
							Column = Column,
							ColumnSpan = ColumnSpan,
							Alignment = Alignment,
							Runs = await this.CaptureRuns([E])
						});
					}
					else
					{
						Cells.Add(new MdTableCell
						{
							Column = Column,
							ColumnSpan = ColumnSpan,
							Alignment = Alignment,
							Blocks = await this.CaptureBlocks(() => E.Render(this))
						});
					}
				}
			}
			finally
			{
				this.style = StyleBak;
				this.link = LinkBak;
				this.alignment = AlignmentBak;
			}

			return new MdTableRow(IsHeader, Cells);
		}

		#endregion

		#region Span elements

		/// <inheritdoc/>
		public Task Render(Abbreviation Element) => this.RenderInline(Element.Children);

		/// <inheritdoc/>
		public Task Render(AutomaticLinkMail Element)
		{
			this.AddRun(Element.EMail, MdTextStyle.None, "mailto:" + Element.EMail);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(AutomaticLinkUrl Element)
		{
			this.AddRun(Element.URL, MdTextStyle.None, Element.URL);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(Delete Element) => this.WithStyle(MdTextStyle.Strikethrough, Element.Children);

		/// <inheritdoc/>
		public async Task Render(DetailsReference Element)
		{
			if (this.document.Detail is not null)
				await this.RenderDocument(this.document.Detail, true);
			else
				await this.Render((MetaReference)Element);
		}

		/// <inheritdoc/>
		public Task Render(EmojiReference Element)
		{
			this.AddRun(Element.Emoji.Unicode);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(Emphasize Element) => this.WithStyle(MdTextStyle.Italic, Element.Children);

		/// <inheritdoc/>
		public async Task Render(FootnoteReference Element)
		{
			if (!this.document.TryGetFootnote(Element.Key, out Footnote Note))
				Note = null!;

			if (Element.AutoExpand && Note is not null)
			{
				await this.RenderInline(Note.Children);
				return;
			}

			if (this.document.TryGetFootnoteNumber(Element.Key, out int Number))
			{
				bool Bak = this.superscript;
				this.superscript = true;
				this.AddRun(Number.ToString(CultureInfo.InvariantCulture), MdTextStyle.FootnoteReference, "#fn:" + Number.ToString(CultureInfo.InvariantCulture));
				this.superscript = Bak;

				if (Note is not null)
					Note.Referenced = true;
			}
		}

		/// <inheritdoc/>
		public Task Render(HashTag Element)
		{
			this.AddRun("#" + Element.Tag, MdTextStyle.Tag);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(HtmlEntity Element)
		{
			this.AddRun(Waher.Content.Html.HtmlEntity.EntityToCharacter(Element.Entity));
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(HtmlEntityUnicode Element)
		{
			try
			{
				this.AddRun(char.ConvertFromUtf32(Element.Code));
			}
			catch (ArgumentOutOfRangeException)
			{
				// Not a character. Nothing is shown.
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(InlineCode Element)
		{
			this.AddRun(Element.Code, this.options.InlineCodeIsPlaceholder ? MdTextStyle.Placeholder : MdTextStyle.Code);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(InlineHTML Element)
		{
			// Inline tags are left out, apart from line breaks.
			if (Element.HTML.StartsWith("<br", StringComparison.OrdinalIgnoreCase))
				this.AddRun("\n");

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task Render(InlineScript Element)
		{
			object Result = await Element.EvaluateExpression();
			await this.RenderObject(Result, Element.AloneInParagraph, Element.Variables);
		}

		/// <inheritdoc/>
		public Task Render(InlineText Element)
		{
			this.AddRun(Element.Value);
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(Insert Element) => this.WithStyle(MdTextStyle.Underline, Element.Children);

		/// <inheritdoc/>
		public Task Render(LineBreak Element)
		{
			this.AddRun("\n");
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(Link Element) => this.WithLink(Element.Url, Element.Children);

		/// <inheritdoc/>
		public Task Render(LinkReference Element)
		{
			MdMultimedia? Target = this.document.GetReference(Element.Label);
			string? Url = Target is not null && Target.Items.Length > 0 ? Target.Items[0].Url : this.link;

			return this.WithLink(Url, Element.Children);
		}

		/// <inheritdoc/>
		public Task Render(MetaReference Element)
		{
			StringBuilder Sb = new();
			bool FirstOnRow = true;

			if (Element.TryGetMetaData(out KeyValuePair<string, bool>[] Values))
			{
				foreach (KeyValuePair<string, bool> P in Values)
				{
					if (FirstOnRow)
						FirstOnRow = false;
					else
						Sb.Append(' ');

					Sb.Append(P.Key);

					if (P.Value)
					{
						Sb.Append('\n');
						FirstOnRow = true;
					}
				}
			}

			this.AddRun(Sb.ToString());
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public Task Render(MdMultimedia Element)
		{
			return this.RenderMultimedia(Element, Element.AloneInParagraph, Element.Children);
		}

		/// <inheritdoc/>
		public Task Render(MultimediaReference Element)
		{
			MdMultimedia? Target = this.document.GetReference(Element.Label);
			if (Target is null)
				return this.RenderInline(Element.Children);

			return this.RenderMultimedia(Target, Element.AloneInParagraph, Element.Children);
		}

		private async Task RenderMultimedia(MdMultimedia Element, bool AloneInParagraph, IEnumerable<MarkdownElement>? AltText)
		{
			string Description = PlainText(await this.CaptureRuns(AltText));

			foreach (MultimediaItem Item in Element.Items)
			{
				string Url = this.document.CheckURL(Item.Url, null);
				string? Title = string.IsNullOrWhiteSpace(Item.Title) ? null : Item.Title;

				bool IsImage =
					Url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
					MdImageSource.HasSchemeLoader(Url) ||
					(Item.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false);

				if (this.inlineOnly > 0)
				{
					this.AddRun(Description.Length > 0 ? Description : Title ?? Url, MdTextStyle.None, Url);
				}
				else if (IsImage)
				{
					this.Add(new MdImage
					{
						Source = MdImageSource.FromUrl(Url, Item.Width, Item.Height),
						Caption = Title,
						Description = Description.Length > 0 ? Description : Title,
						Alignment = this.alignment
					});
				}
				else if (AloneInParagraph || KindOf(Url) != MdEmbedKind.Web)
				{
					this.Add(CreateLinkCard(Url, Description.Length > 0 ? Description : Title, this.alignment));
				}
				else
				{
					this.AddRun(Description.Length > 0 ? Description : Title ?? Url, MdTextStyle.None, Url);
				}
			}
		}

		/// <inheritdoc/>
		public Task Render(StrikeThrough Element) => this.WithStyle(MdTextStyle.Strikethrough, Element.Children);

		/// <inheritdoc/>
		public Task Render(Strong Element) => this.WithStyle(MdTextStyle.Bold, Element.Children);

		/// <inheritdoc/>
		public async Task Render(SubScript Element)
		{
			bool Bak = this.subscript;
			this.subscript = true;

			try
			{
				await this.RenderInline(Element.Children);
			}
			finally
			{
				this.subscript = Bak;
			}
		}

		/// <inheritdoc/>
		public async Task Render(SuperScript Element)
		{
			bool Bak = this.superscript;
			this.superscript = true;

			try
			{
				await this.RenderInline(Element.Children);
			}
			finally
			{
				this.superscript = Bak;
			}
		}

		/// <inheritdoc/>
		public Task Render(Underline Element) => this.WithStyle(MdTextStyle.Underline, Element.Children);

		#endregion

		#region Script results

		/// <summary>
		/// Adds the result of a script: a chart or an image as an image, a document as its blocks, an error as a notice, and
		/// anything else as text.
		/// </summary>
		private async Task RenderObject(object? Result, bool AloneInParagraph, Variables Variables)
		{
			if (Result is null)
				return;

			if (Result is XmlDocument Xml)
				Result = await MarkdownDocument.TransformXml(Xml, Variables);

			if (this.inlineOnly > 0 && Result is Graph or SKImage or MarkdownDocument or MarkdownContent)
			{
				this.AddRun(Result.ToString());
				return;
			}

			switch (Result)
			{
				case Graph G:
					string? Title = (G as Graph2D)?.Title;
					this.Add(new MdImage
					{
						Source = MdImageSource.FromPng(() =>
						{
							PixelInformation Pixels = G.CreatePixels(Variables);
							return Task.FromResult((Pixels.EncodeAsPng(), Pixels.Width, Pixels.Height));
						}),
						Caption = string.IsNullOrWhiteSpace(Title) ? null : Title,
						Description = Title,
						IsChart = true,
						Alignment = this.alignment
					});
					break;

				case SKImage Bitmap:
					byte[] Png;
					using (SKData Data = Bitmap.Encode(SKEncodedImageFormat.Png, 100))
					{
						Png = Data.ToArray();
					}

					int Width = Bitmap.Width;
					int Height = Bitmap.Height;

					this.Add(new MdImage
					{
						Source = MdImageSource.FromPng(() => Task.FromResult((Png, Width, Height))),
						Alignment = this.alignment
					});
					break;

				case MarkdownDocument Doc:
					await this.RenderDocument(Doc, true);
					Doc.ProcessAsyncTasks();
					break;

				case MarkdownContent Content:
					MarkdownDocument Parsed = await MarkdownDocument.CreateAsync(Content.Markdown);
					await this.RenderDocument(Parsed, true);
					Parsed.ProcessAsyncTasks();
					break;

				case Exception Ex:
					Ex = Log.UnnestException(Ex);

					if (Ex is AggregateException Aggregate)
					{
						foreach (Exception Inner in Aggregate.InnerExceptions)
							this.Add(new MdNotice { Text = Inner.Message });
					}
					else
						this.Add(new MdNotice { Text = Ex.Message });
					break;

				default:
					if (AloneInParagraph)
						this.FlushRuns();

					this.AddRun(Result.ToString());

					if (AloneInParagraph)
						this.FlushRuns();
					break;
			}
		}

		#endregion
	}
}
