using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Controls;
using SkiaSharp;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Content.Emoji;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Model.BlockElements;
using Waher.Content.Markdown.Model.SpanElements;
using Waher.Content.Markdown.Rendering;
using Waher.Content.Markdown.Model.Multimedia;
using Waher.Events;
using Waher.Script;
using Waher.Script.Constants;
using Waher.Script.Graphs;
using Waher.Script.Operators.Matrices;
using Microsoft.Maui.Graphics.Text;
using Waher.Script.Functions.Analytic;
using ImageSource = Waher.Content.Emoji.ImageSource;
using MarkdownContent = Waher.Content.Markdown.MarkdownContent;
using Svg;
using Microsoft.Maui.Controls.Shapes;

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
			this.verticalStackLayout.Spacing = 10;
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

						grid.Add(cv, 0, Row);

						ContentView cv2 = new ContentView();
						this.currentElement = cv2;
						await this.Render(Footnote);

						grid.Add(cv2, 1, Row);

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
		public async Task RenderChildren(MarkdownElementChildren Element)
		{
			if (this.InLabel && !(Element.Children is null))
			{
				foreach (MarkdownElement E in Element.Children)
				{
					await E.Render(this);
				}
			}
			else
			{
				ContentView Bakup = (ContentView)this.currentElement;

				VerticalStackLayout vsl = new();
				vsl.Spacing = 8; // Same size as small margins
				Bakup.Content = vsl;

				if (!(Element.Children is null))
				{
					foreach (MarkdownElement E in Element.Children)
					{
						ContentView cv = new ContentView();
						this.currentElement = cv;
						await E.Render(this);
						vsl.Add(cv);
					}
				}
				this.currentElement = Bakup; 
			} 
		}

		/// <summary>
		/// Renders the children of <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element being rendered</param>
		public async Task RenderChildren(MarkdownElement Element)
		{
			IEnumerable<MarkdownElement> Children = Element.Children;

			if (this.InLabel && !(Children is null))
			{
				foreach (MarkdownElement E in Children)
				{
					Console.WriteLine(E.GetType().ToString());
					await E.Render(this);
				}
			}
			else
			{
				ContentView Bakup = (ContentView)this.currentElement;

				VerticalStackLayout vsl = new();
				Bakup.Content = vsl;

				if (!(Children is null))
				{
					foreach (MarkdownElement E in Children)
					{
						ContentView cv = new ContentView();
						this.currentElement = cv;
						await E.Render(this);
						vsl.Add(cv);
					}
				}

				this.currentElement = Bakup;
			}
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
			Label label;
			FormattedString fs;

			if (!this.InLabel)
			{
				label = new Label
				{
					LineBreakMode = LineBreakMode.WordWrap,
				};

				ContentView cv = (ContentView)this.currentElement;
				cv.Content = label;
				this.currentElement = cv;
			}
			else
			{
				label = (Label)this.currentElement;
				this.currentElement = label;
			}

			if (label.FormattedText == null)
			{
				fs = new();
			}
			else
			{
				fs = label.FormattedText;
			}

			Span span = new Span();
			fs.Spans.Add(span);
			label.FormattedText = fs;

			if (this.Superscript)
				Text = TextRenderer.ToSuperscript(Text);
			else if (this.Subscript)
				Text = TextRenderer.ToSubscript(Text);

			span.Text = Text;

			if (this.Bold && this.Italic)
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
				span.TextColor = AppColors.BlueLink;

				span.GestureRecognizers.Add(new TapGestureRecognizer { CommandParameter=this.Hyperlink ,Command = new Command(async Parameter => await App.OpenUrlAsync(Parameter as string ?? string.Empty))});
			}

			return Task.CompletedTask;
		}

		#region Ovverides

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Footnote Element)
		{
			await this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Abbreviation Element)
		{
		   await this.RenderChildren(Element);
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
			ContentView Bakup = (ContentView)this.currentElement;

			this.RenderChild(Element);

			this.currentElement = Bakup; 

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Underline Element)
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

					if (TaskItem.IsChecked)
					{
						this.RenderContentView(AppStyles.SmallRightMargins);

						ContentView cv = (ContentView)this.currentElement;

						cv.Column(0);
						cv.Row(Row);

						cv.Content = (new Label { Text = "✓" });

						grid.Add(cv);
					}

					ContentView cv2 = new ContentView();
					cv2.Column(1);
					cv2.Row(Row);

					this.currentElement = cv2;

					if (ParagraphBullet)
						await E.Render(this);
					else
						await this.RenderLabel(TaskItem, false);

					grid.Add(cv2);
				}

				Row++;
			}

			Bakup.Content = grid;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(TaskItem Element)
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
					this.currentElement = cv;

					this.Bold = Bold;
					await this.RenderLabel(E, true);

					frame.Content = cv;
					this.Bold = false;
				}
				else
				{
					ContentView cv = new ContentView
					{
						Style = AppStyles.TableCell
					};
					this.currentElement = cv;

					await E.Render(this);

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

			await this.RenderChildren(Element);

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

			await this.RenderChildren(Element);

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

			await this.RenderChildren(Element);

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

			await this.RenderChildren(Element);

			this.StrikeThrough = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(SectionSeparator Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			BoxView separator = new BoxView
			{
				HeightRequest = 1,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Background = AppColors.NormalEditPlaceholder,
				Margin = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins,
			};

			Bakup.Content = separator;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Sections Element)
		{
			await this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(RightAligned Element)
		{
			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Right;

			await this.RenderChildren(Element);

			this.Alignment = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Paragraph Element)
		{
			await this.RenderLabel(Element, false);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(NumberedList Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			int Expected = 0;
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
				}
			};

			foreach (MarkdownElement _ in Element.Children)
			{
				grid.AddRowDefinition(new RowDefinition { Height = GridLength.Auto });
			}

			foreach (MarkdownElement E in Element.Children)
			{
				if (E is BlockElementSingleChild Item)
				{
					Expected++;

					ParagraphBullet = !E.InlineSpanElement || E.OutsideParagraph;

					this.RenderContentView(AppStyles.SmallRightMargins);
					ContentView innerCv = (ContentView)this.currentElement;
					innerCv.Column(0);
					innerCv.Row(Row);

					Label label = new Label();

					if (Item is NumberedItem NumberedItem)
						label.Text = (Expected = NumberedItem.Number).ToString(CultureInfo.InvariantCulture) + ".";
					else
						label.Text = Expected.ToString(CultureInfo.InvariantCulture) + ".";

					innerCv.Content = label;
					grid.Add(innerCv);

					ContentView vslContainer = new ContentView();
					vslContainer.Column(1);
					vslContainer.Row(Row);
					this.currentElement = vslContainer;

					if (ParagraphBullet)
					{
						await E.Render(this);
					}
					else
					{
						await this.RenderLabel(Item, false);
					}

					grid.Add(vslContainer);
				}

				Row++;
			}

			Bakup.Content = grid;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(NumberedItem Element)
		{
			this.RenderChild(Element);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(NestedBlock Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			if (Element.HasOneChild)
			{
				await Element.FirstChild.Render(this);
			}
			else
			{
				HtmlSettings Settings = new()
				{
					XmlEntitiesOnly = true
				};
				HtmlRenderer? Html = null;

				VerticalStackLayout vsl = new VerticalStackLayout();

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
								Label label = new Label
								{
									LineBreakMode = LineBreakMode.WordWrap,
									HorizontalTextAlignment = this.LabelAlignment(),
									TextType = TextType.Html,
									Text = Html.ToString()
								};

								Html.Dispose();
								Html = null;

								vsl.Add(label);
							}

							ContentView cv = new ContentView();
							this.currentElement = cv;
							await E.Render(this);
							vsl.Add(cv);
						}
					}

					if (Html is not null)
					{
						Label label = new Label
						{
							LineBreakMode = LineBreakMode.WordWrap,
							HorizontalTextAlignment = this.LabelAlignment(),
							TextType = TextType.Html,
							Text = Html.ToString()
						};
						vsl.Add(label);
					}
				}
				finally
				{
					Html?.Dispose();
				}

				Bakup.Content = vsl;
				this.currentElement = Bakup;
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(MultimediaReference Element)
		{
			Waher.Content.Markdown.Model.SpanElements.Multimedia Multimedia = Element.Document.GetReference(Element.Label);

			if (Multimedia is not null)
			{
				// TODO
				IMultimediaMauiXamlRenderer Renderer = Multimedia.MultimediaHandler<IMultimediaMauiXamlRenderer>();
				if (Renderer is not null)
				{
					await this.RenderMaui(Multimedia);
					return;
				}
			} 
			await this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Waher.Content.Markdown.Model.SpanElements.Multimedia Element)
		{
			IMultimediaMauiXamlRenderer Renderer = Element.MultimediaHandler<IMultimediaMauiXamlRenderer>();
			if (Renderer is null)
				await this.RenderChildren(Element);
			else
				await this.RenderMaui(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(MetaReference Element)
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
		public async Task Render(MarginAligned Element)
		{
			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;

			await this.RenderChildren(Element);

			this.Alignment = Bak; 
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

			await this.RenderChildren(Element);

			this.Hyperlink = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Link Element)
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
		public Task Render(LineBreak Element)
		{
			this.RenderSpan(Environment.NewLine);

			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(LeftAligned Element)
		{
			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Left;

			await this.RenderChildren(Element);

			this.Alignment = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InvisibleBreak Element)
		{
			//TODO not?
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(InsertBlocks Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			Border border = new Border
			{
				Padding = AppStyles.SmallMargins,
				Stroke = new SolidColorBrush
				{
					Color = AppColors.InsertedBorder,
				},
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle
				{
					CornerRadius = 2
				}
			};

			Bakup.Content = border;

			ContentView cv = new ContentView();
			this.currentElement = cv;

			await this.RenderChildren(Element);

			border.Content = cv;
			this.currentElement = Bakup;
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
			this.RenderSpan(Element.Value);
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
			if (this.currentElement is Label label)
			{
				label.TextType = TextType.Html;
				label.Text = $"<--- {Element.HTML} --->";
			}
			else
			{
				ContentView cv = (ContentView)this.currentElement;

				Label newLabel = new Label
				{
					TextType = TextType.Html,
					Text = $"<--- {Element.HTML} --->",
				};

				cv.Content = newLabel;
			}

			return Task.CompletedTask;
		}


		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(InlineCode Element)
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
		public Task Render(HtmlEntityUnicode Element)
		{
			this.RenderSpan(new string((char)Element.Code, 1));

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
				this.RenderSpan(s);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(HtmlBlock Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			Thickness margins = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins;
			Bakup.Margin = margins;

			Label label = new Label
			{
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = this.LabelAlignment(),
				TextType = TextType.Html,
			};

			using HtmlRenderer Renderer = new(new HtmlSettings()
			{
				XmlEntitiesOnly = true
			}, this.Document);

			await Renderer.RenderChildren(Element);

			label.Text = Renderer.ToString();

			Bakup.Content = label;
			this.currentElement = Bakup;
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

			ContentView cv = (ContentView)this.currentElement;
			cv.Content = BoxView;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(Header Element)
		{
			int Level = Math.Max(0, Math.Min(9, Element.Level));

			Label label = new Label
			{
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = this.LabelAlignment(),
				TextType = TextType.Html
			};

			using (HtmlRenderer Renderer = new(new HtmlSettings()
			{
				XmlEntitiesOnly = true
			}, this.Document))
			{
				await Renderer.RenderChildren(Element);

				label.Text = Renderer.ToString();
			}

			label.Style = AppStyles.GetHeaderStyle(Level);

			ContentView cv = (ContentView)this.currentElement;
			cv.Content = label;
			this.currentElement = cv;
			
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(HashTag Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			ContentView innerCV = new();
			this.currentElement = innerCV;

			Border border = new Border
			{
				Background = Color.FromArgb("FFFAFAD2"),
				Content = innerCV
			};

			this.RenderSpan(Element.Tag);

			Bakup.Content = border;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(FootnoteReference Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

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

			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(EmojiReference Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

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

			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(DetailsReference Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			//TODO
			if (this.Document.Detail is not null)
				this.RenderDocument(this.Document.Detail, false);
			else
				this.Render((MetaReference)Element);

			this.currentElement = Bakup;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(DeleteBlocks Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			Border border = new Border
			{
				Padding = AppStyles.SmallMargins,
				Stroke = new SolidColorBrush
				{
					Color = AppColors.DeletedBorder,
				},
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle
				{
					CornerRadius = 2
				}
			};

			Bakup.Content = border;

			ContentView cv = new ContentView();
			this.currentElement = cv;

			await this.RenderChildren(Element);

			border.Content = cv;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(DefinitionTerms Element)
		{
			bool Top = true;

			ContentView Bakup = (ContentView)this.currentElement;

			foreach (MarkdownElement Term in Element.Children)
			{
				Thickness Margins = AppStyles.SmallLeftMargins + AppStyles.SmallRightMargins + AppStyles.SmallBottomMargins;
				if (Top)
					Margins += AppStyles.SmallTopMargins;

				this.RenderContentView(Margins);
				ContentView cv = (ContentView)this.currentElement;
				this.currentElement = cv;

				bool BoldBak = this.Bold;
				this.Bold = true;

				await this.RenderLabel(Term, true);
				Bakup.Content = cv;
				this.Bold = BoldBak;

				Top = false;
			}

			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(DefinitionList Element)
		{
			await this.RenderChildren(Element);
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(DefinitionDescriptions Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			MarkdownElement? Last = null;

			foreach (MarkdownElement Description in Element.Children)
				Last = Description;

			foreach (MarkdownElement Description in Element.Children)
			{
				if (Description.InlineSpanElement && !Description.OutsideParagraph)
				{
					Bakup.Margin = AppStyles.SmallTopMargins + AppStyles.SmallBottomMargins;

					Label label = new Label
					{
						LineBreakMode = LineBreakMode.WordWrap,
						HorizontalTextAlignment = this.LabelAlignment(),
						TextType = TextType.Html
					};

					using (HtmlRenderer Renderer = new(new HtmlSettings()
					{
						XmlEntitiesOnly = true
					}, this.Document))
					{
						await Description.Render(Renderer);
						label.Text = Renderer.ToString();
					}

					Bakup.Content = label;
				}
				else
				{
					Bakup.Padding = AppStyles.SmallLeftMargins;

					if (Description == Last)
						Bakup.Padding += AppStyles.SmallBottomMargins;

					ContentView innerCV = new();
					this.currentElement = innerCV;
					await Description.Render(this); //TODO?

					VerticalStackLayout vsl = new VerticalStackLayout();
					vsl.Add(innerCV);
					Bakup.Content = vsl;
				}
			}
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(CodeBlock Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;
			VerticalStackLayout vsl = new();

			StringBuilder Output = new();
			MauiXamlRenderer rend = new(Output, XML.WriterSettings(false, true));
			ICodeContentMauiXamlRenderer Renderer = Element.CodeContentHandler<ICodeContentMauiXamlRenderer>();

			if (Renderer is not null)
			{
				try
				{
					// TODO ?
					if (await Renderer.RenderMauiXaml(rend, Element.Rows, Element.Language, Element.Indent, Element.Document))
						return;
				}
				catch (Exception ex)
				{
					ex = Log.UnnestException(ex);

					if (ex is AggregateException ex2)
					{
						foreach (Exception ex3 in ex2.InnerExceptions)
						{
							Label label = new Label
							{
								LineBreakMode = LineBreakMode.WordWrap,
								TextColor = AppColors.Alert,
								Text = ex3.Message
							};

							vsl.Add(label);
						}
					}
					else
					{
						Label label = new Label
						{
							LineBreakMode = LineBreakMode.WordWrap,
							TextColor = AppColors.Alert,
							Text = ex.Message
						};

						vsl.Add(label);
					}
				}
			}

			for (int i = Element.Start; i <= Element.End; i++)
			{
				Label label = new Label
				{
					LineBreakMode = LineBreakMode.NoWrap,
					HorizontalTextAlignment = this.LabelAlignment(),
					FontFamily = "Courier New",
					Text = Element.Rows[i]
				};

				vsl.Add(label);
			}

			Bakup.Content = vsl;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public Task Render(CommentBlock Element)
		{
			//TODO not?
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

					this.RenderContentView(AppStyles.SmallRightMargins);

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
						await this.RenderLabel(Item, false);

					grid.Add(vslContainer);
				}

				Row++;
			}

			Bakup.Content = grid;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(CenterAligned Element)
		{
			Waher.Content.Markdown.Model.TextAlignment Bak = this.Alignment;
			this.Alignment = Waher.Content.Markdown.Model.TextAlignment.Center;

			await this.RenderChildren(Element);

			this.Alignment = Bak; 
		}

		/// <summary>
		/// Renders <paramref name="Element"/>.
		/// </summary>
		/// <param name="Element">Element to render</param>
		public async Task Render(BlockQuote Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;

			Border border = new Border
			{
				Padding = AppStyles.SmallMargins,
				Stroke = new SolidColorBrush
				{
					Color = AppColors.PrimaryForeground,
				},
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle
				{
					CornerRadius = 2
				}
			};

			Bakup.Content = border;

			ContentView cv = new ContentView();
			this.currentElement = cv;

			await this.RenderChildren(Element);

			border.Content = cv;
			this.currentElement = Bakup;
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
			ContentView Bakup = (ContentView)this.currentElement;

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

			Bakup.Content = label;
			this.currentElement = Bakup;
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

			ContentView cv = (ContentView)this.currentElement;
			cv.Content = scrollView;

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
			ContentView Bakup = (ContentView)this.currentElement;

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
					await this.RenderSpan(Result?.ToString() ?? string.Empty); //TODO

				return;
			}

			if (Result is Graph G)
			{
				PixelInformation Pixels = G.CreatePixels(Variables);
				byte[] Bin = Pixels.EncodeAsPng();

				s = "data:image/png;base64," + Convert.ToBase64String(Bin, 0, Bin.Length);

				await this.OutputMaui(new Waher.Content.Emoji.ImageSource()
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

				await this.OutputMaui(new Waher.Content.Emoji.ImageSource()
				{
					Url = s,
					Width = Img.Width,
					Height = Img.Height
				});
			}
			else if (Result is MarkdownDocument Doc)
			{
				//TODO maybe
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
					VerticalStackLayout vsl = new();

					foreach (Exception ex3 in ex2.InnerExceptions)
					{
						Label label = new Label
						{
							LineBreakMode = LineBreakMode.WordWrap,
							TextColor = AppColors.Alert,
							Text = ex3.Message
						};

						vsl.Add(label);
					}
					Bakup.Content = vsl;
				}
				else
				{
					Label label = new Label
					{
						LineBreakMode = LineBreakMode.WordWrap,
						TextColor = AppColors.Alert,
						Text = ex.Message
					};

					Bakup.Content = label;
				}
			}
			else
			{
				Label label = new Label
				{
					LineBreakMode = LineBreakMode.WordWrap,
					HorizontalTextAlignment = this.LabelAlignment(),
					Text = Result.ToString()
				};

				Bakup.Content = label;
			}
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

		/// <summary>
		/// Outputs an image to Maui object
		/// </summary>
		/// <param name="Source">Image source.</param>
		private async Task OutputMaui(Waher.Content.Emoji.IImageSource Source)
		{
			Source = await CheckDataUri(Source);

			Image image = new Image
			{
				Source = Source.Url,
			};

			ScrollView sv = new ScrollView
			{
				Orientation = ScrollOrientation.Horizontal,
				Content = image
			};

			if (Source.Width.HasValue)
				sv.WidthRequest = Source.Width.Value;

			if (Source.Height.HasValue)
				sv.HeightRequest = Source.Height.Value;

			ContentView cv = (ContentView)this.currentElement;

			cv.Content = sv;
		}

		private async Task RenderMaui(Waher.Content.Markdown.Model.SpanElements.Multimedia Element)
		{
			ContentView Bakup = (ContentView)this.currentElement;
			VerticalStackLayout vsl = new VerticalStackLayout();

			foreach (MultimediaItem item in Element.Items)
			{
				ContentView cv = new();
				this.currentElement = cv;
				await this.OutputMaui(new Waher.Content.Emoji.ImageSource()
				{
					Url = Element.Document.CheckURL(item.Url, null),
					Width = item.Width,
					Height = item.Height,
				});
				vsl.Add(cv);
			}
			Bakup.Content = vsl;
			this.currentElement = Bakup;
		}

		/// <summary>
		/// Checks a Data URI image, that it contains a decodable image.
		/// </summary>
		/// <param name="Source">Image source.</param>
		public static async Task<Waher.Content.Emoji.IImageSource> CheckDataUri(Waher.Content.Emoji.IImageSource Source)
		{
			string Url = Source.Url;
			int i;

			if (Url.StartsWith("data:", StringComparison.CurrentCultureIgnoreCase) && (i = Url.IndexOf("base64,")) > 0)
			{
				int? Width = Source.Width;
				int? Height = Source.Height;
				byte[] Data = Convert.FromBase64String(Url.Substring(i + 7));
				using (SKBitmap Bitmap = SKBitmap.Decode(Data))
				{
					Width = Bitmap.Width;
					Height = Bitmap.Height;
				}

				Url = await ImageContent.GetTemporaryFile(Data);

				return new ImageSource()
				{
					Url = Url,
					Width = Width,
					Height = Height
				};
			}
			else
				return Source;
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
