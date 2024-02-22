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
	/// Renders XAML (Xamarin.Forms flavour) from a Markdown document.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class XamarinFormsXamlRenderer : Renderer
	{
		/// <summary>
		/// XML output
		/// </summary>
		public readonly XmlWriter XmlOutput;

		/// <summary>
		/// XAML settings.
		/// </summary>
		public readonly XamlSettings XamlSettings;

		/// <summary>
		/// Current text-alignment.
		/// </summary>
		public Waher.Content.Markdown.Model.TextAlignment Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;

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
		/// Renders XAML (Xamarin.Forms flavour) from a Markdown document.
		/// </summary>
		/// <param name="XmlSettings">XML-specific settings.</param>
		/// <param name="XamlSettings">XAML-specific settings.</param>
		public XamarinFormsXamlRenderer(XmlWriterSettings XmlSettings, XamlSettings XamlSettings)
			: base()
		{
			this.XamlSettings = XamlSettings;
			this.XmlOutput = XmlWriter.Create(this.Output, XmlSettings);
		}

		/// <summary>
		/// Renders XAML (Xamarin.Forms flavour) from a Markdown document.
		/// </summary>
		/// <param name="Output">XAML output.</param>
		/// <param name="XmlSettings">XML-specific settings.</param>
		/// <param name="XamlSettings">XAML-specific settings.</param>
		public XamarinFormsXamlRenderer(StringBuilder Output, XmlWriterSettings XmlSettings, XamlSettings XamlSettings)
			: base(Output)
		{
			this.XamlSettings = XamlSettings;
			this.XmlOutput = XmlWriter.Create(this.Output, XmlSettings);
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			base.Dispose();

			this.XmlOutput.Dispose();
		}

		/// <summary>
		/// Renders a document.
		/// </summary>
		/// <param name="Document">Document to render.</param>
		/// <param name="Inclusion">If the rendered output is to be included in another document (true), or if it is a standalone document (false).</param>
		public override Task RenderDocument(MarkdownDocument Document, bool Inclusion)
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

			return base.RenderDocument(Document, Inclusion);
		}

		/// <summary>
		/// Renders the document header.
		/// </summary>
		public override Task RenderDocumentHeader()
		{
			this.XmlOutput.WriteStartElement("StackLayout", "http://xamarin.com/schemas/2014/forms");
			this.XmlOutput.WriteAttributeString("xmlns", "x", null, "http://schemas.microsoft.com/winfx/2009/xaml");
			this.XmlOutput.WriteAttributeString("Spacing", "0");

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders footnotes.
		/// </summary>
		public override async Task RenderFootnotes()
		{
			Footnote Footnote;
			string FootnoteMargin = "0," + this.XamlSettings.ParagraphMarginTop.ToString(CultureInfo.InvariantCulture) + "," +
				this.XamlSettings.FootnoteSeparator.ToString(CultureInfo.InvariantCulture) + "," +
				this.XamlSettings.ParagraphMarginBottom.ToString(CultureInfo.InvariantCulture);
			string Scale = CommonTypes.Encode(this.XamlSettings.SuperscriptScale);
			string Offset = this.XamlSettings.SuperscriptOffset.ToString(CultureInfo.InvariantCulture);
			int Nr;
			int Row = 0;

			this.XmlOutput.WriteStartElement("BoxView");
			this.XmlOutput.WriteAttributeString("HeightRequest", "1");
			this.XmlOutput.WriteAttributeString("BackgroundColor", this.XamlSettings.TableCellBorderColor);
			this.XmlOutput.WriteAttributeString("HorizontalOptions", "FillAndExpand");
			this.XmlOutput.WriteAttributeString("Margin", this.XamlSettings.ParagraphMargins);
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteStartElement("Grid");
			this.XmlOutput.WriteAttributeString("RowSpacing", "0");
			this.XmlOutput.WriteAttributeString("ColumnSpacing", "0");

			this.XmlOutput.WriteStartElement("Grid.ColumnDefinitions");

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "Auto");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "*");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteStartElement("Grid.RowDefinitions");

			foreach (string Key in this.Document.FootnoteOrder)
			{
				if ((this.Document?.TryGetFootnoteNumber(Key, out Nr) ?? false) &&
					(this.Document?.TryGetFootnote(Key, out Footnote) ?? false) &&
					Footnote.Referenced)
				{
					this.XmlOutput.WriteStartElement("RowDefinition");
					this.XmlOutput.WriteAttributeString("Height", "Auto");
					this.XmlOutput.WriteEndElement();
				}
			}

			this.XmlOutput.WriteEndElement();

			if (this.Document is not null)
			{
				foreach (string Key in this.Document.FootnoteOrder)
				{
					if ((this.Document?.TryGetFootnoteNumber(Key, out Nr) ?? false) &&
						(this.Document?.TryGetFootnote(Key, out Footnote) ?? false) &&
						Footnote.Referenced)
					{
						this.XmlOutput.WriteStartElement("ContentView");
						this.XmlOutput.WriteAttributeString("Margin", FootnoteMargin);
						this.XmlOutput.WriteAttributeString("Grid.Column", "0");
						this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));
						this.XmlOutput.WriteAttributeString("Scale", Scale);
						this.XmlOutput.WriteAttributeString("TranslationY", Offset);

						this.XmlOutput.WriteStartElement("Label");
						this.XmlOutput.WriteAttributeString("Text", Nr.ToString(CultureInfo.InvariantCulture));
						this.XmlOutput.WriteEndElement();
						this.XmlOutput.WriteEndElement();

						this.XmlOutput.WriteStartElement("ContentView");
						this.XmlOutput.WriteAttributeString("Grid.Column", "1");
						this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));
						await Footnote.Render(this);
						this.XmlOutput.WriteEndElement();

						Row++;
					}
				}
			}

			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders the document header.
		/// </summary>
		public override Task RenderDocumentFooter()
		{
			this.XmlOutput.WriteEndElement();
			this.XmlOutput.Flush();

			return Task.CompletedTask;
		}

		#region Span Elements

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(Abbreviation Element)
		{
			return this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(AutomaticLinkMail Element)
		{
			string? Bak = this.Hyperlink;
			this.Hyperlink = "mailto:" + Element.EMail;
			this.RenderSpan(this.Hyperlink);
			this.Hyperlink = Bak;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(AutomaticLinkUrl Element)
		{
			string? Bak = this.Hyperlink;
			this.Hyperlink = Element.URL;
			this.RenderSpan(Element.URL);
			this.Hyperlink = Bak;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Delete Element)
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
		public override Task Render(DetailsReference Element)
		{
			if (this.Document.Detail is not null)
				return this.RenderDocument(this.Document.Detail, false);
			else
				return this.Render((MetaReference)Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(EmojiReference Element)
		{
			if (this.InLabel)
				this.RenderSpan(Element.Emoji.Unicode);
			else
			{
				IEmojiSource EmojiSource = this.Document.EmojiSource;

				if (EmojiSource is null)
					this.RenderSpan(Element.Delimiter + Element.Emoji.ShortName + Element.Delimiter);
				else if (!EmojiSource.EmojiSupported(Element.Emoji))
					this.RenderSpan(Element.Emoji.Unicode);
				else
				{
					Waher.Content.Emoji.IImageSource Source = await this.Document.EmojiSource.GetImageSource(Element.Emoji, Element.Level);
					await Multimedia.ImageContent.OutputXamarinForms(this.XmlOutput, Source);
				}
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Emphasize Element)
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
		public override async Task Render(FootnoteReference Element)
		{
			if (!(this.Document?.TryGetFootnote(Element.Key, out Footnote? Footnote) ?? false))
				Footnote = null;

			if (Element.AutoExpand && Footnote is not null)
				await this.Render(Footnote);
			else if (this.Document?.TryGetFootnoteNumber(Element.Key, out int Nr) ?? false)
			{
				bool Bak = this.Superscript;
				this.Superscript = true;

				this.RenderSpan(Nr.ToString(CultureInfo.InvariantCulture));

				this.Superscript = Bak;

				if (Footnote is not null)
					Footnote.Referenced = true;
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(HashTag Element)
		{
			this.RenderSpan(Element.Tag);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(HtmlEntity Element)
		{
			string s = Waher.Content.Html.HtmlEntity.EntityToCharacter(Element.Entity);
			if (!string.IsNullOrEmpty(s))
				this.RenderSpan(s);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(HtmlEntityUnicode Element)
		{
			this.RenderSpan(new string((char)Element.Code, 1));
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(InlineCode Element)
		{
			bool Bak = this.Code;
			this.Code = true;

			this.RenderSpan(Element.Code);

			this.Code = Bak;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(InlineHTML Element)
		{
			this.XmlOutput.WriteComment(Element.HTML);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(InlineScript Element)
		{
			object Result = await Element.EvaluateExpression();
			await this.RenderObject(Result, Element.AloneInParagraph, Element.Variables);
		}

		/// <summary>
		/// Generates WPF XAML from Script output.
		/// </summary>
		/// <param name="Result">Script output.</param>
		/// <param name="AloneInParagraph">If the script output is to be presented alone in a paragraph.</param>
		/// <param name="Variables">Current variables.</param>
		public async Task RenderObject(object? Result, bool AloneInParagraph, Variables Variables)
		{
			if (Result is null)
				return;

			string? s;

			if (Result is XmlDocument Xml)
				Result = await MarkdownDocument.TransformXml(Xml, Variables);
			else if (Result is IToMatrix ToMatrix)
				Result = ToMatrix.ToMatrix();

			if (this.InLabel)
			{
				s = Result?.ToString();
				if (!string.IsNullOrEmpty(s))
					this.RenderSpan(Result?.ToString() ?? string.Empty);

				return;
			}

			if (Result is Graph G)
			{
				PixelInformation Pixels = G.CreatePixels(Variables);
				byte[] Bin = Pixels.EncodeAsPng();

				s = "data:image/png;base64," + Convert.ToBase64String(Bin, 0, Bin.Length);

				await Multimedia.ImageContent.OutputXamarinForms(this.XmlOutput, new Waher.Content.Emoji.ImageSource()
				{
					Url = s,
					Width = Pixels.Width,
					Height = Pixels.Height
				});
			}
			else if (Result is SKImage Img)
			{
				using SKData Data = Img.Encode(SKEncodedImageFormat.Png, 100);
				byte[] Bin = Data.ToArray();

				s = "data:image/png;base64," + Convert.ToBase64String(Bin, 0, Bin.Length);

				await Multimedia.ImageContent.OutputXamarinForms(this.XmlOutput, new Waher.Content.Emoji.ImageSource()
				{
					Url = s,
					Width = Img.Width,
					Height = Img.Height
				});
			}
			else if (Result is MarkdownDocument Doc)
			{
				await this.RenderDocument(Doc, true);   // Does not call ProcessAsyncTasks()
				Doc.ProcessAsyncTasks();
			}
			else if (Result is MarkdownContent Markdown)
			{
				Doc = await MarkdownDocument.CreateAsync(Markdown.Markdown);
				await this.RenderDocument(Doc, true);   // Does not call ProcessAsyncTasks()
				Doc.ProcessAsyncTasks();
			}
			else if (Result is Exception ex)
			{
				ex = Log.UnnestException(ex);

				if (ex is AggregateException ex2)
				{
					foreach (Exception ex3 in ex2.InnerExceptions)
					{
						this.RenderContentView();
						this.XmlOutput.WriteStartElement("Label");
						this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
						this.XmlOutput.WriteAttributeString("TextColor", "Red");
						this.XmlOutput.WriteValue(ex3.Message);
						this.XmlOutput.WriteEndElement();
						this.XmlOutput.WriteEndElement();
					}
				}
				else
				{
					if (AloneInParagraph)
						this.RenderContentView();

					this.XmlOutput.WriteStartElement("Label");
					this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
					this.XmlOutput.WriteAttributeString("TextColor", "Red");
					this.XmlOutput.WriteValue(ex.Message);
					this.XmlOutput.WriteEndElement();

					if (AloneInParagraph)
						this.XmlOutput.WriteEndElement();
				}
			}
			else
			{
				if (AloneInParagraph)
					this.RenderContentView();

				this.XmlOutput.WriteStartElement("Label");
				this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");

				this.RenderLabelAlignment();
				this.XmlOutput.WriteValue(Result.ToString());
				this.XmlOutput.WriteEndElement();

				if (AloneInParagraph)
					this.XmlOutput.WriteEndElement();
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(InlineText Element)
		{
			this.RenderSpan(Element.Value);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Insert Element)
		{
			bool Bak = this.Underline;
			this.Underline = true;

			await this.RenderChildren(Element);

			this.Underline = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(LineBreak Element)
		{
			this.RenderSpan(Environment.NewLine);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Link Element)
		{
			string? Bak = this.Hyperlink;
			this.Hyperlink = Element.Url;

			await this.RenderChildren(Element);

			this.Hyperlink = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(LinkReference Element)
		{
			Waher.Content.Markdown.Model.SpanElements.Multimedia Multimedia = this.Document.GetReference(Element.Label);

			string? Bak = this.Hyperlink;

			if (Multimedia is not null)
				this.Hyperlink = Multimedia.Items[0].Url;

			await this.RenderChildren(Element);

			this.Hyperlink = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(MetaReference Element)
		{
			StringBuilder sb = new();
			bool FirstOnRow = true;

			if (Element.TryGetMetaData(out KeyValuePair<string, bool>[] Values))
			{
				foreach (KeyValuePair<string, bool> P in Values)
				{
					if (FirstOnRow)
						FirstOnRow = false;
					else
						sb.Append(' ');

					sb.Append(P.Key);
					if (P.Value)
					{
						sb.Append(Environment.NewLine);
						FirstOnRow = true;
					}
				}
			}

			this.RenderSpan(sb.ToString());

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(Waher.Content.Markdown.Model.SpanElements.Multimedia Element)
		{
			IMultimediaXamarinFormsXamlRenderer Renderer = Element.MultimediaHandler<IMultimediaXamarinFormsXamlRenderer>();
			if (Renderer is null)
				return this.RenderChildren(Element);
			else
				return Renderer.RenderXamarinFormsXaml(this, Element.Items, Element.Children, Element.AloneInParagraph, Element.Document);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(MultimediaReference Element)
		{
			Waher.Content.Markdown.Model.SpanElements.Multimedia Multimedia = Element.Document.GetReference(Element.Label);

			if (Multimedia is not null)
			{
				IMultimediaXamarinFormsXamlRenderer Renderer = Multimedia.MultimediaHandler<IMultimediaXamarinFormsXamlRenderer>();
				if (Renderer is not null)
					return Renderer.RenderXamarinFormsXaml(this, Multimedia.Items, Element.Children, Element.AloneInParagraph, Element.Document);
			}

			return this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(StrikeThrough Element)
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
		public override async Task Render(Strong Element)
		{
			bool Bak = this.Bold;
			this.Bold = true;

			await this.RenderChildren(Element);

			this.Bold = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(SubScript Element)
		{
			bool Bak = this.Subscript;
			this.Subscript = true;

			await this.RenderChildren(Element);

			this.Subscript = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(SuperScript Element)
		{
			bool Bak = this.Superscript;
			this.Superscript = true;

			await this.RenderChildren(Element);

			this.Superscript = Bak;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Underline Element)
		{
			bool Bak = this.Underline;
			this.Underline = true;

			await this.RenderChildren(Element);

			this.Underline = Bak;
		}

		#endregion

		#region Block elements

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(BlockQuote Element)
		{
			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.BlockQuoteMargin.ToString(CultureInfo.InvariantCulture) + "," +
				this.XamlSettings.ParagraphMarginTop.ToString(CultureInfo.InvariantCulture) + ",0," + this.XamlSettings.ParagraphMarginBottom.ToString(CultureInfo.InvariantCulture));

			this.XmlOutput.WriteStartElement("Frame");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.BlockQuotePadding.ToString(CultureInfo.InvariantCulture) +
				",0," + this.XamlSettings.BlockQuotePadding.ToString(CultureInfo.InvariantCulture) + ",0");
			this.XmlOutput.WriteAttributeString("BorderColor", this.XamlSettings.BlockQuoteBorderColor);
			// TODO: Border thickness

			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			await this.RenderChildren(Element);

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(BulletList Element)
		{
			int Row = 0;
			bool ParagraphBullet;

			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.ParagraphMargins);

			this.XmlOutput.WriteStartElement("Grid");
			this.XmlOutput.WriteAttributeString("RowSpacing", "0");
			this.XmlOutput.WriteAttributeString("ColumnSpacing", "0");

			this.XmlOutput.WriteStartElement("Grid.ColumnDefinitions");

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "Auto");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "*");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteStartElement("Grid.RowDefinitions");

			foreach (MarkdownElement _ in Element.Children)
			{
				this.XmlOutput.WriteStartElement("RowDefinition");
				this.XmlOutput.WriteAttributeString("Height", "Auto");
				this.XmlOutput.WriteEndElement();
			}

			this.XmlOutput.WriteEndElement();

			foreach (MarkdownElement E in Element.Children)
			{
				if (E is UnnumberedItem Item)
				{
					ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;
					this.GetMargins(E, out int TopMargin, out int BottomMargin);

					this.RenderContentView("0," + TopMargin.ToString(CultureInfo.InvariantCulture) + "," +
						this.XamlSettings.ListContentMargin.ToString(CultureInfo.InvariantCulture) + "," +
						BottomMargin.ToString(CultureInfo.InvariantCulture));
					this.XmlOutput.WriteAttributeString("Grid.Column", "0");
					this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));

					this.XmlOutput.WriteElementString("Label", "•");
					this.XmlOutput.WriteEndElement();

					this.XmlOutput.WriteStartElement("StackLayout");
					this.XmlOutput.WriteAttributeString("Grid.Column", "1");
					this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));
					this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

					if (ParagraphBullet)
						await E.Render(this);
					else
						await this.RenderLabel(Item, false);

					this.XmlOutput.WriteEndElement();
				}

				Row++;
			}

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Gets margins for content.
		/// </summary>
		/// <param name="Element">Element to render.</param>
		/// <param name="TopMargin">Top margin.</param>
		/// <param name="BottomMargin">Bottom margin.</param>
		private void GetMargins(MarkdownElement Element, out int TopMargin, out int BottomMargin)
		{
			if (Element.InlineSpanElement && !Element.OutsideParagraph)
			{
				TopMargin = 0;
				BottomMargin = 0;
			}
			else if (Element is NestedBlock NestedBlock)
			{
				bool First = true;

				TopMargin = BottomMargin = 0;

				foreach (MarkdownElement E in NestedBlock.Children)
				{
					if (First)
					{
						First = false;
						this.GetMargins(E, out TopMargin, out BottomMargin);
					}
					else
						this.GetMargins(E, out int _, out BottomMargin);
				}
			}
			else if (Element is MarkdownElementSingleChild SingleChild)
				this.GetMargins(SingleChild.Child, out TopMargin, out BottomMargin);
			else
			{
				TopMargin = this.XamlSettings.ParagraphMarginTop;
				BottomMargin = this.XamlSettings.ParagraphMarginBottom;
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(CenterAligned Element)
		{
			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Center;

			await this.RenderChildren(Element);

			this.Alignment = Bak;
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(CodeBlock Element)
		{
			ICodeContentXamarinFormsXamlRenderer Renderer = Element.CodeContentHandler<ICodeContentXamarinFormsXamlRenderer>();

			if (Renderer is not null)
			{
				try
				{
					if (await Renderer.RenderXamarinFormsXaml(this, Element.Rows, Element.Language, Element.Indent, Element.Document))
						return;
				}
				catch (Exception ex)
				{
					ex = Log.UnnestException(ex);

					if (ex is AggregateException ex2)
					{
						foreach (Exception ex3 in ex2.InnerExceptions)
						{
							this.RenderContentView();
							this.XmlOutput.WriteStartElement("Label");
							this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
							this.XmlOutput.WriteAttributeString("TextColor", "Red");
							this.XmlOutput.WriteValue(ex3.Message);
							this.XmlOutput.WriteEndElement();
							this.XmlOutput.WriteEndElement();
						}
					}
					else
					{
						this.RenderContentView();
						this.XmlOutput.WriteStartElement("Label");
						this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
						this.XmlOutput.WriteAttributeString("TextColor", "Red");
						this.XmlOutput.WriteValue(ex.Message);
						this.XmlOutput.WriteEndElement();
						this.XmlOutput.WriteEndElement();
					}
				}
			}

			this.RenderContentView();
			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			int i;

			for (i = Element.Start; i <= Element.End; i++)
			{
				this.XmlOutput.WriteStartElement("Label");
				this.XmlOutput.WriteAttributeString("LineBreakMode", "NoWrap");
				this.RenderLabelAlignment();
				this.XmlOutput.WriteAttributeString("FontFamily", "Courier New");
				this.XmlOutput.WriteAttributeString("Text", Element.Rows[i]);
				this.XmlOutput.WriteEndElement();
			}

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(CommentBlock Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(DefinitionDescriptions Element)
		{
			MarkdownElement? Last = null;

			foreach (MarkdownElement Description in Element.Children)
				Last = Description;

			foreach (MarkdownElement Description in Element.Children)
			{
				if (Description.InlineSpanElement && !Description.OutsideParagraph)
				{
					this.RenderContentView();

					this.XmlOutput.WriteStartElement("Label");
					this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
					this.RenderLabelAlignment();
					this.XmlOutput.WriteAttributeString("TextType", "Html");

					using (HtmlRenderer Renderer = new(new HtmlSettings()
					{
						XmlEntitiesOnly = true
					}, this.Document))
					{
						await Description.Render(Renderer);
						this.XmlOutput.WriteCData(Renderer.ToString());
					}

					this.XmlOutput.WriteEndElement();
					this.XmlOutput.WriteEndElement();
				}
				else
				{
					this.XmlOutput.WriteStartElement("ContentView");
					this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.DefinitionMargin.ToString(CultureInfo.InvariantCulture) + ",0,0," +
						(Description == Last ? this.XamlSettings.DefinitionSeparator : 0).ToString(CultureInfo.InvariantCulture));

					this.XmlOutput.WriteStartElement("StackLayout");
					await Description.Render(this);
					this.XmlOutput.WriteEndElement();

					this.XmlOutput.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(DefinitionList Element)
		{
			return this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(DefinitionTerms Element)
		{
			int TopMargin = this.XamlSettings.ParagraphMarginTop;

			foreach (MarkdownElement Term in Element.Children)
			{
				this.RenderContentView(this.XamlSettings.ParagraphMarginLeft.ToString(CultureInfo.InvariantCulture) + "," +
					TopMargin.ToString(CultureInfo.InvariantCulture) + "," +
					this.XamlSettings.ParagraphMarginRight.ToString(CultureInfo.InvariantCulture) + ",0");

				bool BoldBak = this.Bold;
				this.Bold = true;

				await this.RenderLabel(Term, true);

				this.Bold = BoldBak;
				this.XmlOutput.WriteEndElement();

				TopMargin = 0;
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(DeleteBlocks Element)
		{
			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.BlockQuoteMargin.ToString(CultureInfo.InvariantCulture) + "," +
				this.XamlSettings.ParagraphMarginTop.ToString(CultureInfo.InvariantCulture) + ",0," +
				this.XamlSettings.ParagraphMarginBottom.ToString(CultureInfo.InvariantCulture));

			this.XmlOutput.WriteStartElement("Frame");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.BlockQuotePadding.ToString(CultureInfo.InvariantCulture) +
				",0," + this.XamlSettings.BlockQuotePadding.ToString(CultureInfo.InvariantCulture) + ",0");
			this.XmlOutput.WriteAttributeString("BorderColor", this.XamlSettings.DeletedBlockQuoteBorderColor);
			// TODO: Border thickness

			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			foreach (MarkdownElement E in Element.Children)
				await E.Render(this);

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(Footnote Element)
		{
			return this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Header Element)
		{
			this.RenderContentView();

			this.XmlOutput.WriteStartElement("Label");
			this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
			this.RenderLabelAlignment();

			if (Element.Level > 0 && Element.Level <= this.XamlSettings.HeaderFontSize.Length)
			{
				this.XmlOutput.WriteAttributeString("FontSize", this.XamlSettings.HeaderFontSize[Element.Level - 1].ToString(CultureInfo.InvariantCulture));
				this.XmlOutput.WriteAttributeString("TextColor", this.XamlSettings.HeaderForegroundColor[Element.Level - 1].ToString());
			}

			this.XmlOutput.WriteAttributeString("TextType", "Html");

			using (HtmlRenderer Renderer = new(new HtmlSettings()
			{
				XmlEntitiesOnly = true
			}, this.Document))
			{
				await Renderer.RenderChildren(Element);

				this.XmlOutput.WriteCData(Renderer.ToString());
			}

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Writes a text-alignment attribute to a Xamarin.Forms label element.
		/// </summary>
		public void RenderLabelAlignment()
		{
			switch (this.Alignment)
			{
				case Waher.Content.Markdown.Model.TextAlignment.Left:
					this.XmlOutput.WriteAttributeString("HorizontalTextAlignment", "Start");
					break;

				case Waher.Content.Markdown.Model.TextAlignment.Right:
					this.XmlOutput.WriteAttributeString("HorizontalTextAlignment", "End");
					break;

				case Waher.Content.Markdown.Model.TextAlignment.Center:
					this.XmlOutput.WriteAttributeString("HorizontalTextAlignment", "Center");
					break;
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(HorizontalRule Element)
		{
			this.XmlOutput.WriteStartElement("BoxView");
			this.XmlOutput.WriteAttributeString("HeightRequest", "1");
			this.XmlOutput.WriteAttributeString("BackgroundColor", this.XamlSettings.TableCellBorderColor);
			this.XmlOutput.WriteAttributeString("HorizontalOptions", "FillAndExpand");
			this.XmlOutput.WriteAttributeString("Margin", this.XamlSettings.ParagraphMargins);
			this.XmlOutput.WriteEndElement();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(HtmlBlock Element)
		{
			this.RenderContentView();

			this.XmlOutput.WriteStartElement("Label");
			this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
			this.RenderLabelAlignment();
			this.XmlOutput.WriteAttributeString("TextType", "Html");

			using HtmlRenderer Renderer = new(new HtmlSettings()
			{
				XmlEntitiesOnly = true
			}, this.Document);

			await Renderer.RenderChildren(Element);

			this.XmlOutput.WriteCData(Renderer.ToString());

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(InsertBlocks Element)
		{
			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.BlockQuoteMargin.ToString(CultureInfo.InvariantCulture) + "," +
				this.XamlSettings.ParagraphMarginTop.ToString(CultureInfo.InvariantCulture) + ",0," +
				this.XamlSettings.ParagraphMarginBottom.ToString(CultureInfo.InvariantCulture));

			this.XmlOutput.WriteStartElement("Frame");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.BlockQuotePadding.ToString(CultureInfo.InvariantCulture) +
				",0," + this.XamlSettings.BlockQuotePadding.ToString(CultureInfo.InvariantCulture) + ",0");
			this.XmlOutput.WriteAttributeString("BorderColor", this.XamlSettings.InsertedBlockQuoteBorderColor);
			// TODO: Border thickness

			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			await this.RenderChildren(Element);

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(InvisibleBreak Element)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(LeftAligned Element)
		{
			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;

			await this.RenderChildren(Element);

			this.Alignment = Bak;
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(MarginAligned Element)
		{
			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;

			await this.RenderChildren(Element);

			this.Alignment = Bak;
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(NestedBlock Element)
		{
			if (Element.HasOneChild)
				await Element.FirstChild.Render(this);
			else
			{
				HtmlSettings Settings = new()
				{
					XmlEntitiesOnly = true
				};
				HtmlRenderer? Html = null;

				try
				{
					foreach (MarkdownElement E in Element.Children)
					{
						if (E.InlineSpanElement)
						{
							Html ??= new HtmlRenderer(Settings, this.Document);
							await E.Render(Html);
						}
						else
						{
							if (Html is not null)
							{
								this.XmlOutput.WriteStartElement("Label");
								this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
								this.RenderLabelAlignment();
								this.XmlOutput.WriteAttributeString("TextType", "Html");
								this.XmlOutput.WriteCData(Html.ToString());
								this.XmlOutput.WriteEndElement();

								Html.Dispose();
								Html = null;
							}

							await E.Render(this);
						}
					}

					if (Html is not null)
					{
						this.XmlOutput.WriteStartElement("Label");
						this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
						this.RenderLabelAlignment();
						this.XmlOutput.WriteAttributeString("TextType", "Html");
						this.XmlOutput.WriteCData(Html.ToString());
						this.XmlOutput.WriteEndElement();
					}
				}
				finally
				{
					Html?.Dispose();
				}
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(NumberedItem Element)
		{
			return this.RenderChild(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(NumberedList Element)
		{
			int Expected = 0;
			int Row = 0;
			bool ParagraphBullet;

			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.ParagraphMargins);

			this.XmlOutput.WriteStartElement("Grid");
			this.XmlOutput.WriteAttributeString("RowSpacing", "0");
			this.XmlOutput.WriteAttributeString("ColumnSpacing", "0");

			this.XmlOutput.WriteStartElement("Grid.ColumnDefinitions");

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "Auto");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "*");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteStartElement("Grid.RowDefinitions");

			foreach (MarkdownElement _ in Element.Children)
			{
				this.XmlOutput.WriteStartElement("RowDefinition");
				this.XmlOutput.WriteAttributeString("Height", "Auto");
				this.XmlOutput.WriteEndElement();
			}

			this.XmlOutput.WriteEndElement();

			foreach (MarkdownElement E in Element.Children)
			{
				if (E is BlockElementSingleChild Item)
				{
					Expected++;

					ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;
					this.GetMargins(E, out int TopMargin, out int BottomMargin);

					this.RenderContentView("0," + TopMargin.ToString(CultureInfo.InvariantCulture) + "," +
						this.XamlSettings.ListContentMargin.ToString(CultureInfo.InvariantCulture) + "," +
						BottomMargin.ToString(CultureInfo.InvariantCulture));
					this.XmlOutput.WriteAttributeString("Grid.Column", "0");
					this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));

					this.XmlOutput.WriteStartElement("Label");

					if (Item is NumberedItem NumberedItem)
						this.XmlOutput.WriteValue((Expected = NumberedItem.Number).ToString(CultureInfo.InvariantCulture));
					else
						this.XmlOutput.WriteValue(Expected.ToString(CultureInfo.InvariantCulture));

					this.XmlOutput.WriteValue(".");
					this.XmlOutput.WriteEndElement();
					this.XmlOutput.WriteEndElement();

					this.XmlOutput.WriteStartElement("StackLayout");
					this.XmlOutput.WriteAttributeString("Grid.Column", "1");
					this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));
					this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

					if (ParagraphBullet)
						await E.Render(this);
					else
						await this.RenderLabel(Item, false);

					this.XmlOutput.WriteEndElement();
				}

				Row++;
			}

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Paragraph Element)
		{
			this.RenderContentView();
			await this.RenderLabel(Element, false);
			this.XmlOutput.WriteEndElement();
		}

		internal async Task RenderLabel(MarkdownElement Element, bool IncludeElement)
		{
			bool HasLink = !Element.ForEach((E, _) =>
			{
				return !(
					E is AutomaticLinkMail ||
					E is AutomaticLinkUrl ||
					E is Link ||
					E is LinkReference);
			}, null);

			this.XmlOutput.WriteStartElement("Label");
			this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
			this.RenderLabelAlignment();

			if (HasLink)
			{
				if (this.InLabel)
				{
					if (IncludeElement)
						await Element.Render(this);
					else
						await this.RenderChildren(Element);
				}
				else
				{
					this.InLabel = true;

					this.XmlOutput.WriteStartElement("Label.FormattedText");
					this.XmlOutput.WriteStartElement("FormattedString");

					if (IncludeElement)
						await Element.Render(this);
					else
						await this.RenderChildren(Element);

					this.XmlOutput.WriteEndElement();
					this.XmlOutput.WriteEndElement();

					this.InLabel = false;
				}
			}
			else
			{
				this.XmlOutput.WriteAttributeString("TextType", "Html");

				if (this.Bold)
					this.XmlOutput.WriteAttributeString("FontAttributes", "Bold");

				using HtmlRenderer Renderer = new(new HtmlSettings()
				{
					XmlEntitiesOnly = true
				}, this.Document);

				if (IncludeElement)
					await Element.Render(Renderer);
				else
					await Renderer.RenderChildren(Element);

				this.XmlOutput.WriteCData(Renderer.ToString());
			}

			this.XmlOutput.WriteEndElement();
		}

		internal void RenderSpan(string Text)
		{
			if (!this.InLabel)
			{
				this.XmlOutput.WriteStartElement("Label");
				this.XmlOutput.WriteAttributeString("LineBreakMode", "WordWrap");
				this.RenderLabelAlignment();
				this.XmlOutput.WriteStartElement("Label.FormattedText");
				this.XmlOutput.WriteStartElement("FormattedString");
			}

			this.XmlOutput.WriteStartElement("Span");

			if (this.Superscript)
				Text = TextRenderer.ToSuperscript(Text);
			else if (this.Subscript)
				Text = TextRenderer.ToSubscript(Text);

			this.XmlOutput.WriteAttributeString("Text", Text);

			if (this.Bold && this.Italic)
				this.XmlOutput.WriteAttributeString("FontAttributes", "Italic, Bold");
			else if (this.Bold)
				this.XmlOutput.WriteAttributeString("FontAttributes", "Bold");
			else if (this.Italic)
				this.XmlOutput.WriteAttributeString("FontAttributes", "Italic");

			if (this.StrikeThrough && this.Underline)
				this.XmlOutput.WriteAttributeString("TextDecorations", "Strikethrough, Underline");
			else if (this.StrikeThrough)
				this.XmlOutput.WriteAttributeString("TextDecorations", "Strikethrough");
			else if (this.Underline)
				this.XmlOutput.WriteAttributeString("TextDecorations", "Underline");

			if (this.Code)
				this.XmlOutput.WriteAttributeString("FontFamily", "Courier New");

			if (this.Hyperlink is not null)
			{
				this.XmlOutput.WriteAttributeString("TextColor", "{Binding HyperlinkColor}");

				this.XmlOutput.WriteStartElement("Span.GestureRecognizers");
				this.XmlOutput.WriteStartElement("TapGestureRecognizer");
				this.XmlOutput.WriteAttributeString("Command", "{Binding HyperlinkClicked}");
				this.XmlOutput.WriteAttributeString("CommandParameter", this.Hyperlink);
				this.XmlOutput.WriteEndElement();
				this.XmlOutput.WriteEndElement();
			}

			if (!this.InLabel)
			{
				this.XmlOutput.WriteEndElement();
				this.XmlOutput.WriteEndElement();
				this.XmlOutput.WriteEndElement();
			}

			this.XmlOutput.WriteEndElement();
		}

		internal void RenderContentView()
		{
			this.RenderContentView(this.Alignment, this.XamlSettings.ParagraphMargins);
		}

		internal void RenderContentView(string Margins)
		{
			this.RenderContentView(this.Alignment, Margins);
		}

		internal void RenderContentView(Waher.Content.Markdown.Model.TextAlignment Alignment, string Margins)
		{
			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", Margins);

			switch (Alignment)
			{
				case Waher.Content.Markdown.Model.TextAlignment.Center:
					this.XmlOutput.WriteAttributeString("HorizontalOptions", "Center");
					break;

				case Waher.Content.Markdown.Model.TextAlignment.Left:
					this.XmlOutput.WriteAttributeString("HorizontalOptions", "Start");
					break;

				case Waher.Content.Markdown.Model.TextAlignment.Right:
					this.XmlOutput.WriteAttributeString("HorizontalOptions", "End");
					break;
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(RightAligned Element)
		{
			this.XmlOutput.WriteStartElement("StackLayout");
			this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Right;

			await this.RenderChildren(Element);

			this.Alignment = Bak;
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(Sections Element)
		{
			return this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(SectionSeparator Element)
		{
			this.XmlOutput.WriteStartElement("BoxView");
			this.XmlOutput.WriteAttributeString("HeightRequest", "1");
			this.XmlOutput.WriteAttributeString("BackgroundColor", this.XamlSettings.TableCellBorderColor);
			this.XmlOutput.WriteAttributeString("HorizontalOptions", "FillAndExpand");
			this.XmlOutput.WriteAttributeString("Margin", this.XamlSettings.ParagraphMargins);
			this.XmlOutput.WriteEndElement();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(Table Element)
		{
			int Column;
			int Row, NrRows;
			int RowNr = 0;

			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.ParagraphMargins);

			this.XmlOutput.WriteStartElement("Grid");
			this.XmlOutput.WriteAttributeString("RowSpacing", "-2");
			this.XmlOutput.WriteAttributeString("ColumnSpacing", "-2");

			// TODO: Tooltip/caption

			this.XmlOutput.WriteStartElement("Grid.ColumnDefinitions");

			for (Column = 0; Column < Element.Columns; Column++)
			{
				this.XmlOutput.WriteStartElement("ColumnDefinition");
				this.XmlOutput.WriteAttributeString("Width", "Auto");
				this.XmlOutput.WriteEndElement();
			}

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteStartElement("Grid.RowDefinitions");

			for (Row = 0, NrRows = Element.Rows.Length + Element.Headers.Length; Row < NrRows; Row++)
			{
				this.XmlOutput.WriteStartElement("RowDefinition");
				this.XmlOutput.WriteAttributeString("Height", "Auto");
				this.XmlOutput.WriteEndElement();
			}

			this.XmlOutput.WriteEndElement();

			for (Row = 0, NrRows = Element.Headers.Length; Row < NrRows; Row++, RowNr++)
				await this.Render(Element.Headers[Row], RowNr, true, Element);

			for (Row = 0, NrRows = Element.Rows.Length; Row < NrRows; Row++, RowNr++)
				await this.Render(Element.Rows[Row], RowNr, false, Element);

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		private void ClearState()
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
		}

		private StateBackup Backup()
		{
			return new StateBackup()
			{
				Alignment = this.Alignment,
				Bold = this.Bold,
				Italic = this.Italic,
				StrikeThrough = this.StrikeThrough,
				Underline = this.Underline,
				Superscript = this.Superscript,
				Subscript = this.Subscript,
				Code = this.Code,
				InLabel = this.InLabel,
				Hyperlink = this.Hyperlink
			};
		}

		private void Restore(StateBackup Backup)
		{
			this.Alignment = Backup.Alignment;
			this.Bold = Backup.Bold;
			this.Italic = Backup.Italic;
			this.StrikeThrough = Backup.StrikeThrough;
			this.Underline = Backup.Underline;
			this.Superscript = Backup.Superscript;
			this.Subscript = Backup.Subscript;
			this.Code = Backup.Code;
			this.InLabel = Backup.InLabel;
			this.Hyperlink = Backup.Hyperlink;
		}

		private class StateBackup
		{
			public Waher.Content.Markdown.Model.TextAlignment Alignment;
			public bool Bold;
			public bool Italic;
			public bool StrikeThrough;
			public bool Underline;
			public bool Superscript;
			public bool Subscript;
			public bool Code;
			public bool InLabel;
			public string? Hyperlink;
		}

		private async Task Render(MarkdownElement[] CurrentRow, int RowNr, bool Bold, Table Element)
		{
			MarkdownElement E;
			Waher.Content.Markdown.Model.TextAlignment TextAlignment;
			int Column;
			int NrColumns;
			int ColSpan;
			StateBackup Bak = this.Backup();

			this.ClearState();

			for (Column = 0, NrColumns = CurrentRow.Length; Column < NrColumns; Column++)
			{
				E = CurrentRow[Column];
				if (E is null)
					continue;

				TextAlignment = Element.Alignments[Column];
				ColSpan = Column + 1;
				while (ColSpan < NrColumns && CurrentRow[ColSpan] is null)
					ColSpan++;

				ColSpan -= Column;

				this.XmlOutput.WriteStartElement("Frame");
				this.XmlOutput.WriteAttributeString("Padding", "0,0,0,0");
				this.XmlOutput.WriteAttributeString("BorderColor", this.XamlSettings.TableCellBorderColor);
				// TODO: Table-cell border thickness

				if ((RowNr & 1) == 0)
				{
					if (!string.IsNullOrEmpty(this.XamlSettings.TableCellRowBackgroundColor1))
						this.XmlOutput.WriteAttributeString("BackgroundColor", this.XamlSettings.TableCellRowBackgroundColor1);
				}
				else
				{
					if (!string.IsNullOrEmpty(this.XamlSettings.TableCellRowBackgroundColor2))
						this.XmlOutput.WriteAttributeString("BackgroundColor", this.XamlSettings.TableCellRowBackgroundColor2);
				}

				this.XmlOutput.WriteAttributeString("Grid.Column", Column.ToString(CultureInfo.InvariantCulture));
				this.XmlOutput.WriteAttributeString("Grid.Row", RowNr.ToString(CultureInfo.InvariantCulture));

				if (ColSpan > 1)
					this.XmlOutput.WriteAttributeString("Grid.ColumnSpan", ColSpan.ToString(CultureInfo.InvariantCulture));

				if (E.InlineSpanElement)
				{
					this.RenderContentView(TextAlignment, this.XamlSettings.TableCellPadding);

					this.Bold = Bold;
					await this.RenderLabel(E, true);
					this.Bold = false;

					this.XmlOutput.WriteEndElement();   // Paragraph
				}
				else
				{
					this.XmlOutput.WriteStartElement("ContentView");
					this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.TableCellPadding);

					this.XmlOutput.WriteStartElement("StackLayout");
					await E.Render(this);
					this.XmlOutput.WriteEndElement();   // StackLayout

					this.XmlOutput.WriteEndElement();   // ContentView
				}

				this.XmlOutput.WriteEndElement();   // Frame
			}

			this.Restore(Bak);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(TaskItem Element)
		{
			return this.RenderChild(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override async Task Render(TaskList Element)
		{
			int Row = 0;
			bool ParagraphBullet;

			this.XmlOutput.WriteStartElement("ContentView");
			this.XmlOutput.WriteAttributeString("Padding", this.XamlSettings.ParagraphMargins);

			this.XmlOutput.WriteStartElement("Grid");
			this.XmlOutput.WriteAttributeString("RowSpacing", "0");
			this.XmlOutput.WriteAttributeString("ColumnSpacing", "0");

			this.XmlOutput.WriteStartElement("Grid.ColumnDefinitions");

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "Auto");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteStartElement("ColumnDefinition");
			this.XmlOutput.WriteAttributeString("Width", "*");
			this.XmlOutput.WriteEndElement();

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteStartElement("Grid.RowDefinitions");

			foreach (MarkdownElement _ in Element.Children)
			{
				this.XmlOutput.WriteStartElement("RowDefinition");
				this.XmlOutput.WriteAttributeString("Height", "Auto");
				this.XmlOutput.WriteEndElement();
			}

			this.XmlOutput.WriteEndElement();

			foreach (MarkdownElement E in Element.Children)
			{
				if (E is TaskItem TaskItem)
				{
					ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;
					this.GetMargins(E, out int TopMargin, out int BottomMargin);

					if (TaskItem.IsChecked)
					{
						this.RenderContentView("0," + TopMargin.ToString(CultureInfo.InvariantCulture) + "," +
							this.XamlSettings.ListContentMargin.ToString(CultureInfo.InvariantCulture) + "," +
							BottomMargin.ToString(CultureInfo.InvariantCulture));
						this.XmlOutput.WriteAttributeString("Grid.Column", "0");
						this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));

						this.XmlOutput.WriteElementString("Label", "✓");
						this.XmlOutput.WriteEndElement();
					}

					this.XmlOutput.WriteStartElement("StackLayout");
					this.XmlOutput.WriteAttributeString("Grid.Column", "1");
					this.XmlOutput.WriteAttributeString("Grid.Row", Row.ToString(CultureInfo.InvariantCulture));
					this.XmlOutput.WriteAttributeString("Orientation", "Vertical");

					if (ParagraphBullet)
						await E.Render(this);
					else
						await this.RenderLabel(TaskItem, false);

					this.XmlOutput.WriteEndElement();
				}

				Row++;
			}

			this.XmlOutput.WriteEndElement();
			this.XmlOutput.WriteEndElement();
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public override Task Render(UnnumberedItem Element)
		{
			return this.RenderChild(Element);
		}

		#endregion

	}
}
