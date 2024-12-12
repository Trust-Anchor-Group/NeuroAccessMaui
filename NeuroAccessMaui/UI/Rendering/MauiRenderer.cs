using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Controls;
using SkiaSharp;
using System.Globalization;
using System.Runtime.CompilerServices;
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
using Waher.Script.Constants;
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

		/// <summary>
		/// The VerticalStackLayout Being created
		/// </summary>
		private VerticalStackLayout verticalStackLayout;

		/// <summary>
		/// The current element being processed
		/// </summary>
		private View currentElement;

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
			this.currentElement = new ContentView();
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
			{
				this.currentElement = new ContentView();

				this.LogType(E.GetType().ToString());
				await E.Render(this);

				this.verticalStackLayout.Add(this.currentElement);
			}

			if (this.NeedsToDisplayFootnotes())
			{
				await this.RenderFootnotes();
				this.verticalStackLayout.Add(this.currentElement);
			}

			this.Document = DocBak;
		}

		private void LogType(string t)
		{
			switch (t)
			{
				case "Waher.Content.Markdown.Model.BlockElements.Paragraph":
					break;
				case "Waher.Content.Markdown.Model.BlockElements.Header":
					break;
				case "Waher.Content.Markdown.Model.BlockElements.Table":
					break;
				case "Waher.Content.Markdown.Model.BlockElements.BulletList":
					break;
				default:
					Console.WriteLine(t);
					break;
			}
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

			this.verticalStackLayout.Add(boxView);

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
						ContentView cv = new ContentView
						{
							Margin = AppStyles.SmallMargins,
							Scale = 0.75,
							TranslationY = -5,
							Content = new Label
							{
								Text = Nr.ToString(CultureInfo.InvariantCulture)
							},
						};

						grid.Add(cv, Row, 0);

						this.currentElement = cv;
						await this.Render(Footnote);

						grid.Add(new ContentView
						{
							Content = this.currentElement
						}, Row, 1);

						Row++;
					}
				}
			}

			this.currentElement = grid;
		}

		/// <summary>
		/// Renders the children of <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element being rendered</param>
		public Task RenderChildren(MarkdownElementChildren Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			VerticalStackLayout vsl = new();
			Bakup.Content = vsl;
			this.currentElement = vsl;

			if (!(Element.Children is null))
			{
				foreach (MarkdownElement E in Element.Children)
				{
					E.Render(this);
					vsl.Add(this.currentElement);
				}
			}
			this.currentElement = Bakup;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders the children of <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element being rendered</param>
		public Task RenderChildren(MarkdownElement Element)
		{
			VerticalStackLayout vsl = new();
			this.currentElement = vsl;

			if (!(Element.Children is null))
			{
				foreach (MarkdownElement E in Element.Children)
				{
					E.Render(this);
					vsl.Add(this.currentElement);
				}
			}

			this.currentElement = vsl;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders the child of <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element being rendered</param>
		public Task RenderChild(MarkdownElementSingleChild Element)
		{
			if (Element.Child is null)
				return Task.CompletedTask;

			Element.Child.Render(this);

			return Task.CompletedTask;
		}

		internal Task RenderSpan(string Text)
		{
			Span span = new Span();

			if (!this.InLabel)
			{
				Label label = new Label
				{
					LineBreakMode = LineBreakMode.WordWrap,
				};

				label.FormattedText.Spans.Add(span);

				ContentView Bakup = (ContentView)this.currentElement;
				Bakup.Content = label;
				this.currentElement = Bakup;
			}
			else
			{
				Label label = (Label)this.currentElement;
				label.FormattedText.Spans.Add(span);
				this.currentElement = label;
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
				span.TextColor = AppColors.AccentForeground;

				span.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async Parameter => await App.OpenUrlAsync(this.Hyperlink))});
			}

			return Task.CompletedTask;
		}

		#region Ovverides

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Footnote Element)
		{
			View Bakup = this.currentElement;

			this.RenderChildren(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Abbreviation Element)
		{
			View Bakup = this.currentElement;

			this.RenderChildren(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Delete Element)
		{
			bool Bak = this.StrikeThrough;
			this.StrikeThrough = true;

			View Bakup = this.currentElement;

			await this.RenderChildren(Element);

			this.currentElement = Bakup;

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

			ContentView Bakup = (ContentView) this.currentElement;

			await this.RenderChildren(Element);

			this.currentElement = Bakup;

			this.Italic = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(UnnumberedItem Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			this.RenderChild(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Underline Element)
		{
			bool Bak = this.Underline;
			this.Underline = true;

			View Bakup = this.currentElement;

			this.RenderChildren(Element);

			this.currentElement = Bakup;

			this.Underline = Bak; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(TaskList Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			int Row = 0;
			bool ParagraphBullet;

			Grid grid = new Grid
			{
				RowSpacing = 0,
				ColumnSpacing = 0,
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star },
				},
			};

			foreach (MarkdownElement _ in Element.Children)
			{
				grid.AddRowDefinition(new RowDefinition { Height = GridLength.Auto });
			}

			foreach (MarkdownElement E in Element.Children)
			{
				if (E is TaskItem TaskItem)
				{
					ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;
					GetMargins(E, out bool TopMargin, out bool BottomMargin);

					Thickness margin = new Thickness(0, 0, 0, 0) + AppStyles.SmallRightMargins;

					if (TopMargin)
						margin += AppStyles.SmallTopMargins;

					if (BottomMargin)
						margin += AppStyles.SmallBottomMargins;

					if (TaskItem.IsChecked)
					{
						this.RenderContentView(margin);

						ContentView cv = (ContentView)this.currentElement;

						cv.Column(0);
						cv.Row(Row);

						cv.Content = (new Label { Text = "✓" });

						grid.Add(cv);
					}

					VerticalStackLayout vsl = new VerticalStackLayout();
					vsl.Column(1);
					vsl.Row(Row);

					this.currentElement = vsl;

					if (ParagraphBullet)
						await E.Render(this);
					else
						await this.RenderLabel(TaskItem, false);

					vsl.Add(this.currentElement);

					grid.Add(vsl);
				}

				Row++;
			}

			ContentView ContentView = new ContentView
			{
				Padding = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins,
				Content = grid
			};

			Bakup.Content = ContentView;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(TaskItem Element)
		{
			View Bakup = this.currentElement;

			this.RenderChild(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Table Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			int Column;
			int Row, NrRows;
			int RowNr = 0;

			Grid grid = new Grid
			{
				RowSpacing = -2,
				ColumnSpacing = -2,
			};

			// TODO: Tooltip/caption

			for (Column = 0; Column < Element.Columns; Column++)
			{
				grid.AddColumnDefinition(new ColumnDefinition { Width = GridLength.Auto });
			}

			for (Row = 0, NrRows = Element.Rows.Length + Element.Headers.Length; Row < NrRows; Row++)
			{
				grid.AddRowDefinition(new RowDefinition { Height = GridLength.Auto });
			}

			ContentView cv = new ContentView
			{
				Padding = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins,
				Content = grid
			};

			ScrollView sv = new ScrollView
			{
				Orientation = ScrollOrientation.Horizontal,
				Content = cv
			};

			for (Row = 0, NrRows = Element.Headers.Length; Row < NrRows; Row++, RowNr++)
			{
				this.currentElement = grid;
				await this.Render(Element.Headers[Row], Element.HeaderCellAlignments[Row], RowNr, true, Element);
			}

			for (Row = 0, NrRows = Element.Rows.Length; Row < NrRows; Row++, RowNr++)
			{
				this.currentElement = grid;
				await this.Render(Element.Rows[Row], Element.RowCellAlignments[Row], RowNr, false, Element);
			}

			Bakup.Content = sv;
			this.currentElement = Bakup;
		}

		private async Task Render(MarkdownElement[] CurrentRow, Waher.Content.Markdown.Model.TextAlignment?[] CellAlignments,
			int RowNr, bool Bold, Table Element)
		{
			MarkdownElement E;
			Waher.Content.Markdown.Model.TextAlignment TextAlignment;
			int Column;
			int NrColumns = Element.Columns;
			int ColSpan;
			StateBackup Bak = this.Backup();

			Grid Bakup = (Grid)this.currentElement;

			this.ClearState();

			for (Column = 0; Column < NrColumns; Column++)
			{
				E = CurrentRow[Column];
				if (E is null)
					continue;

				TextAlignment = CellAlignments[Column] ?? Element.ColumnAlignments[Column];
				ColSpan = Column + 1;
				while (ColSpan < NrColumns && CurrentRow[ColSpan] is null)
					ColSpan++;

				ColSpan -= Column;

				Frame frame = new Frame();

				if ((RowNr & 1) == 0)
					frame.Style = AppStyles.TableCellEven;

				else
					frame.Style = AppStyles.TableCellOdd;

				frame.Column(Column);
				frame.Row(RowNr);

				if (ColSpan > 1)
					frame.ColumnSpan(ColSpan);

				if (E.InlineSpanElement)
				{
					this.RenderContentView(TextAlignment, new Thickness(0, 0, 0, 0), AppStyles.TableCell);

					ContentView cv = (ContentView)this.currentElement;

					this.Bold = Bold;
					await this.RenderLabel(E, true);

					cv.Content = this.currentElement;

					frame.Content = cv;
					this.Bold = false;
				}
				else
				{
					VerticalStackLayout vsl = new VerticalStackLayout();

					this.currentElement = vsl;
					await E.Render(this);

					ContentView cv = new ContentView
					{
						Content = vsl,
						Style = AppStyles.TableCell
					};

					frame.Content = cv;
				}
				Bakup.Add(frame);
			}
			this.Restore(Bak);

			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(SuperScript Element)
		{
			bool Bak = this.Superscript;
			this.Superscript = true;

			View Bakup = this.currentElement;

			await this.RenderChildren(Element);

			this.currentElement = Bakup;

			this.Superscript = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(SubScript Element)
		{
			bool Bak = this.Subscript;
			this.Subscript = true;

			View Bakup = this.currentElement;

			await this.RenderChildren(Element);

			this.currentElement = Bakup;

			this.Subscript = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Strong Element)
		{
			bool Bak = this.Bold;
			this.Bold = true;

			View Bakup = this.currentElement;

			await this.RenderChildren(Element);

			this.currentElement = Bakup;

			this.Bold = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(StrikeThrough Element)
		{
			bool Bak = this.StrikeThrough;
			this.StrikeThrough = true;

			View Bakup = this.currentElement;

			await this.RenderChildren(Element);

			this.currentElement = Bakup;

			this.StrikeThrough = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(SectionSeparator Element)
		{
			BoxView separator = new BoxView
			{
				HeightRequest = 1,
				BackgroundColor = AppColors.NormalEditPlaceholder,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Margin = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins
			};

			this.currentElement.AddLogicalChild(separator);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Sections Element)
		{
			View Bakup = this.currentElement;

			this.RenderChildren(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(RightAligned Element)
		{
			VerticalStackLayout vsl = new VerticalStackLayout();

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Right;

			View Bakup = this.currentElement;
			this.currentElement = vsl;
			await this.RenderChildren(Element);
			Bakup.AddLogicalChild(vsl);
			this.currentElement = Bakup;

			this.Alignment = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Paragraph Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			await this.RenderLabel(Element, false);

			Bakup.Content = this.currentElement;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(NumberedList Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(NumberedItem Element)
		{
			View Bakup = this.currentElement;

			this.RenderChild(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(NestedBlock Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(MultimediaReference Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Waher.Content.Markdown.Model.SpanElements.Multimedia Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(MetaReference Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(MarginAligned Element)
		{
			View Bakup = this.currentElement;

			VerticalStackLayout vsl = new VerticalStackLayout();

			this.currentElement = vsl;

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;

			await this.RenderChildren(Element);

			this.Alignment = Bak; 

			Bakup.AddLogicalChild(vsl);
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(LinkReference Element)
		{
			Waher.Content.Markdown.Model.SpanElements.Multimedia Multimedia = this.Document.GetReference(Element.Label);

			string? Bak = this.Hyperlink;

			if (Multimedia is not null)
				this.Hyperlink = Multimedia.Items[0].Url;

			View Bakup = this.currentElement;

			await this.RenderChildren(Element);

			this.currentElement = Bakup;

			this.Hyperlink = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Link Element)
		{
			View Bakup = this.currentElement;

			this.RenderChildren(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(LineBreak Element)
		{
			View Bakup = this.currentElement;

			this.RenderSpan(Environment.NewLine);

			Bakup.AddLogicalChild(this.currentElement);

			this.currentElement = Bakup;

			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(LeftAligned Element)
		{
			VerticalStackLayout vsl = new VerticalStackLayout();

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;

			View Bakup = this.currentElement;
			this.currentElement = vsl;
			await this.RenderChildren(Element);
			Bakup.AddLogicalChild(vsl);
			this.currentElement = Bakup;

			this.Alignment = Bak; 
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
			// TODO
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Insert Element)
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
		public Task Render(InlineText Element)
		{
			View Bakup = this.currentElement;

			this.RenderSpan(Element.Value);

			Bakup.AddLogicalChild(this.currentElement);
			this.currentElement = Bakup;

			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(InlineScript Element)
		{
			object Result = await Element.EvaluateExpression();
			await this.RenderObject(Result, Element.AloneInParagraph, Element.Variables);
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InlineHTML Element)
		{
			//TODO
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InlineCode Element)
		{
			// TODO or maybe not?
			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HtmlEntityUnicode Element)
		{
			ContentView Bakup = (ContentView)(this.currentElement);

			this.RenderSpan(new string((char)Element.Code, 1));

			Bakup.AddLogicalChild(this.currentElement);
			this.currentElement = Bakup;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HtmlEntity Element)
		{
			string s = Waher.Content.Html.HtmlEntity.EntityToCharacter(Element.Entity);
			if (!string.IsNullOrEmpty(s))
			{
				View Bakup = this.currentElement;
				this.RenderSpan(s);
				Bakup.AddLogicalChild(this.currentElement);
				this.currentElement = Bakup;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HtmlBlock Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HorizontalRule Element)
		{
			BoxView BoxView = new BoxView
			{
				HeightRequest = 1,
				BackgroundColor = AppColors.NormalEditPlaceholder,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Margin = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins
			};

			this.currentElement.AddLogicalChild(BoxView);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(Header Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HashTag Element)
		{
			View Bakup = this.currentElement;

			this.RenderSpan(Element.Tag);

			Bakup.AddLogicalChild(this.currentElement);
			this.currentElement = Bakup;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(FootnoteReference Element)
		{
			View Bakup = this.currentElement;

			if (!(this.Document?.TryGetFootnote(Element.Key, out Footnote? Footnote) ?? false))
				Footnote = null;

			if (Element.AutoExpand && Footnote is not null)
				await this.Render(Footnote);
			else if (this.Document?.TryGetFootnoteNumber(Element.Key, out int Nr) ?? false)
			{
				bool Bak = this.Superscript;
				this.Superscript = true;

				await this.RenderSpan(Nr.ToString(CultureInfo.InvariantCulture));

				this.Superscript = Bak; 

				if (Footnote is not null)
					Footnote.Referenced = true;
			}

			Bakup.AddLogicalChild(this.currentElement);
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(EmojiReference Element)
		{
			View Bakup = this.currentElement;

			if (this.InLabel)
				await this.RenderSpan(Element.Emoji.Unicode);
			else
			{
				IEmojiSource EmojiSource = this.Document.EmojiSource;

				if (EmojiSource is null)
					await this.RenderSpan(Element.Delimiter + Element.Emoji.ShortName + Element.Delimiter);
				else if (!EmojiSource.EmojiSupported(Element.Emoji))
					await this.RenderSpan(Element.Emoji.Unicode);
				else
				{
					Waher.Content.Emoji.IImageSource Source = await this.Document.EmojiSource.GetImageSource(Element.Emoji, Element.Level);
					await this.OutputImage(Source);
				}
			}

			Bakup.AddLogicalChild(this.currentElement);
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DetailsReference Element)
		{
			View Bakup = this.currentElement;

			if (this.Document.Detail is not null)
				this.RenderDocument(this.Document.Detail, false);
			else
				this.Render((MetaReference)Element);

			Bakup.AddLogicalChild(this.currentElement);
			this.currentElement = Bakup;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DeleteBlocks Element)
		{
			// TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(DefinitionTerms Element)
		{
			bool Top = true;

			View Bakup = this.currentElement;

			foreach (MarkdownElement Term in Element.Children)
			{
				Thickness Margins = AppStyles.SmallLeftMargins + AppStyles.SmallRightMargins + AppStyles.SmallBottomMargins;
				if (Top)
					Margins += AppStyles.SmallTopMargins;

				this.RenderContentView(Margins);
				ContentView ContentView = (ContentView)this.currentElement;

				bool BoldBak = this.Bold;
				this.Bold = true;

				await this.RenderLabel(Term, true);
				ContentView.AddLogicalChild(this.currentElement);

				Bakup.AddLogicalChild(ContentView);
				this.Bold = BoldBak;

				Top = false;
			}

			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DefinitionList Element)
		{
			View Bakup = this.currentElement;

			this.RenderChildren(Element);

			Bakup.AddLogicalChild(this.currentElement);
			this.currentElement = Bakup;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DefinitionDescriptions Element)
		{
			// TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(CodeBlock Element)
		{
			// TODO
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
		public async Task Render(BulletList Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			int Row = 0;
			bool ParagraphBullet;

			Grid grid = new Grid
			{
				RowSpacing = 0,
				ColumnSpacing = 0,
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star }
				},
			};

			foreach (MarkdownElement _ in Element.Children)
			{
				grid.AddRowDefinition(new RowDefinition { Height = GridLength.Auto });
			}

			foreach (MarkdownElement E in Element.Children)
			{
				if (E is UnnumberedItem Item)
				{
					ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;
					GetMargins(E, out bool TopMargin, out bool BottomMargin);

					Thickness margin = AppStyles.SmallRightMargins;

					if (TopMargin)
						margin += AppStyles.SmallTopMargins;

					if (BottomMargin)
						margin += AppStyles.SmallBottomMargins;

					this.RenderContentView(margin);

					ContentView cv = (ContentView)this.currentElement;
					cv.Column(0);
					cv.Row(Row);

					Label lbl = new Label { Text = "•" };
					cv.Content = lbl;
					grid.Add(cv);

					ContentView vslContainer = new ContentView();
					vslContainer.Column(1);
					vslContainer.Row(Row);
					this.currentElement = vslContainer;

					if (ParagraphBullet)
						await E.Render(this);
					else
					{
						await this.RenderLabel(Item, false);
						vslContainer.Content = this.currentElement;
					}
					grid.Add(vslContainer);
				}

				Row++;
			}

			Bakup.Padding = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins;
			Bakup.Content = grid;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(CenterAligned Element)
		{
			VerticalStackLayout vsl = new VerticalStackLayout();

			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Center;

			View Bakup = this.currentElement;
			this.currentElement = vsl;
			await this.RenderChildren(Element);
			Bakup.AddLogicalChild(vsl);
			this.currentElement = Bakup;

			this.Alignment = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(BlockQuote Element)
		{
			//TODO
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(AutomaticLinkUrl Element)
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
		public Task Render(AutomaticLinkMail Element)
		{
			string? Bak = this.Hyperlink;
			this.Hyperlink = "mailto:" + Element.EMail;
			this.RenderSpan(this.Hyperlink);
			this.Hyperlink = Bak; 

			return Task.CompletedTask;
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

			Label label = new Label
			{
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = this.LabelAlignment()
			};

			this.currentElement = label;

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

					if (IncludeElement)
						await Element.Render(this);
					else
						await this.RenderChildren(Element);

					this.InLabel = false;
				}
			}
			else
			{
				label.TextType = TextType.Html;

				if (this.Bold)
					label.FontAttributes = FontAttributes.Bold;

				using HtmlRenderer Renderer = new(new HtmlSettings()
				{
					XmlEntitiesOnly = true
				}, this.Document);

				if (IncludeElement)
					await Element.Render(Renderer);
				else
					await Renderer.RenderChildren(Element);

				label.Text = Renderer.ToString();
			}
			this.currentElement = label;
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

		#region Helpers
		/// <summary>
		/// If referenced footnotes need to be rendered.
		/// </summary>
		private bool NeedsToDisplayFootnotes()
		{
			IEnumerable<Footnote> footnotes = this.GetFootnotes(this.Document.Footnotes);

			if (footnotes is null)
				return false;

			foreach (Footnote Footnote in footnotes)
			{
				if (Footnote.Referenced)
				{
					return true;
				}
			}

			return false;
		}

		private IEnumerable<Footnote> GetFootnotes(string[] keys)
		{
			foreach(string key in keys)
			{
				Footnote footnote;
				this.Document.TryGetFootnote(key, out footnote);
				yield return footnote;
			}
		}

		private Task OutputImage(Waher.Content.Emoji.IImageSource source)
		{
			Image image = new Image
			{
				Source = source.Url,
			};

			if (source.Width.HasValue)
				image.WidthRequest = source.Width.Value;

			if (source.Height.HasValue)
				image.HeightRequest = source.Height.Value;

			ScrollView scrollView = new ScrollView
			{
				Orientation = ScrollOrientation.Horizontal,
				Content = image
			};

			this.verticalStackLayout.Children.Add(scrollView);
			return Task.CompletedTask;
		}

		public Microsoft.Maui.TextAlignment LabelAlignment()
		{
			switch (this.Alignment)
			{
				case Waher.Content.Markdown.Model.TextAlignment.Left:
					return Microsoft.Maui.TextAlignment.Start;

				case Waher.Content.Markdown.Model.TextAlignment.Right:
					return Microsoft.Maui.TextAlignment.End;

				case Waher.Content.Markdown.Model.TextAlignment.Center:
					return Microsoft.Maui.TextAlignment.Center;

				default:
					return Microsoft.Maui.TextAlignment.Center;
			}
		}

		internal void RenderContentView(Waher.Content.Markdown.Model.TextAlignment Alignment, Thickness Margins, Style? BoxStyle)
		{
			ContentView contentView = new ContentView();

			if (!Margins.Equals(Thickness.Zero))
				contentView.Padding = Margins;

			if (BoxStyle is not null)
				contentView.Style = BoxStyle;

			switch (Alignment)
			{
				case Waher.Content.Markdown.Model.TextAlignment.Center:
					contentView.HorizontalOptions = LayoutOptions.Center;
					break;

				case Waher.Content.Markdown.Model.TextAlignment.Left:
					contentView.HorizontalOptions = LayoutOptions.Start;
					break;

				case Waher.Content.Markdown.Model.TextAlignment.Right:
					contentView.HorizontalOptions = LayoutOptions.End;
					break;
			}
			
			this.currentElement = contentView;
		}

		internal void RenderContentView(Thickness Margins)
		{
			this.RenderContentView(this.Alignment, Margins, null);
		}

		public async Task RenderObject(object? Result, bool AloneInParagraph, Variables Variables)
		{
			//TODO
			return;
		}

		/// <summary>
		/// Gets margins for content.
		/// </summary>
		/// <param name="Element">Element to render.</param>
		/// <param name="TopMargin">Top margin.</param>
		/// <param name="BottomMargin">Bottom margin.</param>
		private static void GetMargins(MarkdownElement Element, out bool TopMargin, out bool BottomMargin)
		{
			if (Element.InlineSpanElement && !Element.OutsideParagraph)
			{
				TopMargin = false;
				BottomMargin = false;
			}
			else if (Element is NestedBlock NestedBlock)
			{
				bool First = true;

				TopMargin = BottomMargin = false;

				foreach (MarkdownElement E in NestedBlock.Children)
				{
					if (First)
					{
						First = false;
						GetMargins(E, out TopMargin, out BottomMargin);
					}
					else
						GetMargins(E, out bool _, out BottomMargin);
				}
			}
			else if (Element is MarkdownElementSingleChild SingleChild)
				GetMargins(SingleChild.Child, out TopMargin, out BottomMargin);
			else
			{
				TopMargin = true;
				BottomMargin = true;
			}
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

		#endregion
	}
}
