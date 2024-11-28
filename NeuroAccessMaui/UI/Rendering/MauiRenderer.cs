using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Controls;
using SkiaSharp;
using System.Globalization;
using System.Text;
using System.Xml;
using Waher.Content;
using Waher.Content.Emoji;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.BlockElements;
using Waher.Content.Markdown.Model.SpanElements;
using Waher.Content.Markdown.Rendering;
using Waher.Events;
using Waher.Script;
using Waher.Script.Graphs;
using Waher.Script.Operators.Matrices;

namespace NeuroAccessMaui.UI.Rendering
{
	/// <summary>
	/// Renders MAUI objects in a verticalStackLayout from a Markdown document.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class MauiRenderer : IRenderer
	{
		/// <summary>
		/// Reference to Markdown document being processed.
		/// </summary>
		public MarkdownDocument Document;

		/// <summary
		/// The VerticalStackLayout Being created
		/// </summary>
		private VerticalStackLayout verticalStackLayout;

		/// <summary>
		/// If text is bold
		/// </summary>
		public bool Bold = false;

		/// <summary>
		/// If text is italic
		/// </summary>
		public bool Italic = false;

		/// <summary>
		/// If text is stricken through
		/// </summary>
		public bool StrikeThrough = false;

		/// <summary>
		/// If text is underlined
		/// </summary>
		public bool Underline = false;

		/// <summary>
		/// If text is superscript
		/// </summary>
		public bool Superscript = false;

		/// <summary>
		/// If text is subscript
		/// </summary>
		public bool Subscript = false;

		/// <summary>
		/// If text is inline code.
		/// </summary>
		public bool Code = false;

		/// <summary>
		/// If rendering is inside a label.
		/// </summary>
		public bool InLabel = false;

		/// <summary>
		/// Link, if rendering a hyperlink, null otherwise.
		/// </summary>
		public string? Hyperlink = null;

		/// <summary>
		/// Current text-alignment.
		/// </summary>
		public Waher.Content.Markdown.Model.TextAlignment Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;


		public MauiRenderer(MarkdownDocument document)
		{
			this.Document = document;

			this.verticalStackLayout = new VerticalStackLayout();
		}

		public VerticalStackLayout? Output()
		{
			if (this.verticalStackLayout.Children.Count == 0) return null;
			return this.verticalStackLayout;
		}
		

		/// <summary>
		/// Renders a document.
		/// </summary>
		/// <param name="Document">Document to render.</param>
		/// <param name="Inclusion">If the rendered output is to be included in another document (true), or if it is a standalone document (false).</param>
		public Task RenderDocument(MarkdownDocument Document, bool Inclusion)
		{
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;
			this.Bold = false;
			this.Italic = false;
			this.StrikeThrough = false;
			this.Underline = false;
			this.Superscript = false;
			this.Subscript = false;
			this.Code = false;
			this.InLabel = false;
			this.Hyperlink = null;

			return this.RenderDocumentEntry(Document, Inclusion);
		}

		/// <summary>
		/// Renders a document.
		/// </summary>
		/// <param name="Document">Document to render.</param>
		/// <param name="Inclusion">If the rendered output is to be included in another document (true), or if it is a standalone document (false).</param>
		public virtual async Task RenderDocumentEntry(MarkdownDocument Document, bool Inclusion)
		{
			MarkdownDocument DocBak = this.Document;

			this.Document = Document;

			if (!Inclusion && this.Document.TryGetMetaData("BODYONLY", out KeyValuePair<string, bool>[] Values))
			{
				if (CommonTypes.TryParse(Values[0].Key, out bool b) && b)
					Inclusion = true;
			}

			if (!Inclusion)
				await this.RenderDocumentHeader();

			foreach (MarkdownElement E in this.Document.Elements)
				await E.Render(this);

			//if (this.Document.NeedsToDisplayFootnotes)
				//await this.RenderFootnotes();

			this.Document = DocBak;
		}

		/// <summary>
		/// Renders the document header.
		/// </summary>
		public Task RenderDocumentHeader()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders footnotes.
		/// </summary>
		public async Task RenderFootnotes()
		{
			Footnote Footnote;
			int Nr;
			int Row = 0;

			BoxView boxView = new BoxView
			{
				HeightRequest = 1,
				BackgroundColor = AppColors.NormalEditPlaceholder,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Margin = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins
			};

			Grid grid = new Grid
			{
				RowSpacing = 0,
				ColumnSpacing = 0,
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Auto),
					new ColumnDefinition(GridLength.Star)
				} 
			};

			foreach (string Key in this.Document.FootnoteOrder)
			{
				if ((this.Document?.TryGetFootnoteNumber(Key, out Nr) ?? false) &&
					(this.Document?.TryGetFootnote(Key, out Footnote) ?? false) &&
					Footnote.Referenced)
				{
					grid.AddRowDefinition(new RowDefinition { Height = GridLength.Auto });
				}
			}

			if (this.Document is not null)
			{
				foreach (string Key in this.Document.FootnoteOrder)
				{
					if ((this.Document?.TryGetFootnoteNumber(Key, out Nr) ?? false) &&
						(this.Document?.TryGetFootnote(Key, out Footnote) ?? false) &&
						Footnote.Referenced)
					{
						grid.Add(new ContentView
						{
							Margin = AppStyles.SmallMargins,
							Scale = 0.75,
							TranslationY = -5,
							Content = new Label
							{
								Text = Nr.ToString(CultureInfo.InvariantCulture)
							},
						}, Row, 0);

						grid.Add(new ContentView
						{
							//Content = Render(Footnote)
						}, Row, 1);

						Row++;
					}
				}
			}
		}

		/// <summary>
		/// Renders the children of <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element being rendered</param>
		public async Task RenderChildren(MarkdownElementChildren Element)
		{
			if (!(Element.Children is null))
			{
				foreach (MarkdownElement E in Element.Children)
					await E.Render(this);
			}
		}

		/// <summary>
		/// Renders the children of <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element being rendered</param>
		public async Task RenderChildren(MarkdownElement Element)
		{
			IEnumerable<MarkdownElement> Children = Element.Children;

			if (!(Children is null))
			{
				foreach (MarkdownElement E in Children)
					await E.Render(this);
			}
		}

		internal Span RenderSpan(string Text)
		{
			Span span = new Span();

			if (!this.InLabel)
			{
				//this.XmlOutput.WriteStartElement("Label");
				//this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
				//this.RenderLabelAlignment();
				//this.XmlOutput.WriteStartElement("Label.FormattedText");
				//this.XmlOutput.WriteStartElement("FormattedString");

				//FormattedString FormattedString = new FormattedString();
				//FormattedString.AddLogicalChild(span);

				//Label label = new Label
				//{
				//	LineBreakMode = LineBreakMode.WordWrap,
				//	FormattedText = FormattedString
				//};
			}

			if (this.Superscript)
				span.Text = TextRenderer.ToSuperscript(Text);
			else if (this.Subscript)
				span.Text = TextRenderer.ToSubscript(Text);

			if (this.Bold && this.Italic)
				//this.XmlOutput.WriteAttributeString("FontAttributes", "Italic, Bold");
				span.FontAttributes = FontAttributes.Bold | FontAttributes.Italic;
			else if (this.Bold)
				span.FontAttributes = FontAttributes.Bold;
			else if (this.Italic)
				span.FontAttributes = FontAttributes.Italic;

			if (this.StrikeThrough && this.Underline)
				span.TextDecorations = TextDecorations.Underline | TextDecorations.Strikethrough;
			else if (this.StrikeThrough)
				span.TextDecorations = TextDecorations.Strikethrough;
			else if (this.Underline)
				span.TextDecorations = TextDecorations.Underline;

			if (this.Code)
				span.FontFamily = "Courier New";

			if (this.Hyperlink is not null)
			{
				//this.XmlOutput.WriteAttributeString("TextColor", "{AppThemeBinding Light={StaticResource AccentForegroundLight}, Dark={StaticResource AccentForegroundDark}}");

				//this.XmlOutput.WriteStartElement("Span.GestureRecognizers");
				//this.XmlOutput.WriteStartElement("TapGestureRecognizer");
				//this.XmlOutput.WriteAttributeString("Command", "{Binding HyperlinkClicked}");
				//this.XmlOutput.WriteAttributeString("CommandParameter", this.Hyperlink);
				//this.XmlOutput.WriteEndElement();
				//this.XmlOutput.WriteEndElement();

				span.TextColor = AppColors.AccentForeground;

				span.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async Parameter => await App.OpenUrlAsync(this.Hyperlink))});
			}

			return span;
		}

		#region Ovverides

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Footnote Element)
		{
			return this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Abbreviation Element)
		{
			return this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Delete Element)
		{
			bool Bak = this.StrikeThrough;
			this.StrikeThrough = true;

			await this.RenderChildren(Element);

			this.StrikeThrough = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Emphasize Element)
		{
			bool Bak = this.Italic;
			this.Italic = true;

			await this.RenderChildren(Element);

			this.Italic = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(UnnumberedItem Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Underline Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(TaskList Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(TaskItem Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Table Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(SuperScript Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(SubScript Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Strong Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(StrikeThrough Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(SectionSeparator Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Sections Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(RightAligned Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Paragraph Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(NumberedList Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(NumberedItem Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(NestedBlock Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(MultimediaReference Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Waher.Content.Markdown.Model.SpanElements.Multimedia Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(MetaReference Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(MarginAligned Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(LinkReference Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Link Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(LineBreak Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(LeftAligned Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InvisibleBreak Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InsertBlocks Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Insert Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InlineText Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InlineScript Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InlineHTML Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InlineCode Element)
		{
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HtmlEntityUnicode Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HtmlEntity Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HtmlBlock Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HorizontalRule Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Header Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HashTag Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(FootnoteReference Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(EmojiReference Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DetailsReference Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DeleteBlocks Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DefinitionTerms Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DefinitionList Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DefinitionDescriptions Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(CodeBlock Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(CommentBlock Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(BulletList Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(CenterAligned Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(BlockQuote Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(AutomaticLinkUrl Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(AutomaticLinkMail Element)
		{
			return Task.CompletedTask;
		}
		#endregion

		#region Dispose
		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private bool isDisposed;

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			if (disposing)
			{

			}
			this.isDisposed = true;
		}

		#endregion
	}
}
