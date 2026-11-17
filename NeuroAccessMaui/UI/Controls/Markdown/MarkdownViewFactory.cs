using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// Creates the views of the blocks of a text model. Runs on the UI thread.
	/// </summary>
	internal sealed class MarkdownViewFactory
	{
		private readonly MarkdownMetrics metrics;
		private readonly ICommand linkCommand;

		/// <summary>
		/// How a block is shown where it is.
		/// </summary>
		/// <param name="Muted">If text is in the quiet color, as in quotes.</param>
		/// <param name="Small">If text is small, as in footnotes.</param>
		private readonly record struct Context(bool Muted, bool Small);

		/// <summary>
		/// Creates the views of the blocks of a text model.
		/// </summary>
		/// <param name="Metrics">Sizes and gaps.</param>
		/// <param name="OnLink">Called with the link when a link or a card is tapped.</param>
		public MarkdownViewFactory(MarkdownMetrics Metrics, Action<string> OnLink)
		{
			this.metrics = Metrics;
			this.linkCommand = new Command<string>(Url =>
			{
				if (!string.IsNullOrEmpty(Url))
					OnLink(Url);
			});
		}

		/// <summary>
		/// Sizes and gaps.
		/// </summary>
		public MarkdownMetrics Metrics => this.metrics;

		/// <summary>
		/// Creates the view of a top-level block.
		/// </summary>
		/// <param name="Block">The block.</param>
		/// <param name="First">If it is the first block, which gets no extra room above it.</param>
		/// <returns>The view.</returns>
		public View Create(MdBlock Block, bool First)
		{
			View View;

			try
			{
				View = this.Create(Block, new Context(false, false));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				View = this.CreateNotice(Ex.Message, new Context(false, false));
			}

			if (!First && Block is MdHeading Heading)
				View.Margin = new Thickness(0, this.metrics.HeadingTopMargins[Heading.Level - 1], 0, 0);

			return View;
		}

		private View Create(MdBlock Block, Context Context)
		{
			return Block switch
			{
				MdParagraph Paragraph => this.CreateLabel(Paragraph.Runs, Paragraph.Alignment, Context),
				MdHeading Heading => this.CreateHeading(Heading),
				MdList List => this.CreateList(List, Context),
				MdFrame Frame => this.CreateFrame(Frame, Context),
				MdCode Code => this.CreateCode(Code),
				MdTable Table => this.CreateTable(Table, Context),
				MdRule => this.CreateRule(),
				MdImage Image => new MarkdownImageView(Image, this.metrics),
				MdEmbed Embed => this.CreateEmbed(Embed),
				MdHtml Html => this.CreateHtml(Html, Context),
				MdNotice Notice => this.CreateNotice(Notice.Text, Context),
				MdFootnotes Footnotes => this.CreateFootnotes(Footnotes),
				_ => new ContentView()
			};
		}

		private View CreateBlocks(IReadOnlyList<MdBlock> Blocks, double Spacing, Context Context)
		{
			if (Blocks.Count == 1)
				return this.Create(Blocks[0], Context);

			VerticalStackLayout Stack = new() { Spacing = Spacing };

			foreach (MdBlock Block in Blocks)
				Stack.Add(this.Create(Block, Context));

			return Stack;
		}

		#region Text

		private static Microsoft.Maui.TextAlignment ToTextAlignment(MdAlignment Alignment)
		{
			return Alignment switch
			{
				MdAlignment.Center => Microsoft.Maui.TextAlignment.Center,
				MdAlignment.End => Microsoft.Maui.TextAlignment.End,
				_ => Microsoft.Maui.TextAlignment.Start
			};
		}

		private double TextSize(Context Context) => Context.Small ? this.metrics.SmallSize + 1 : this.metrics.BodySize;

		/// <summary>
		/// Creates a label for runs of text. Formatting and links are spans in the one label.
		/// </summary>
		private Label CreateLabel(IReadOnlyList<MdRun> Runs, MdAlignment Alignment, Context Context)
		{
			double Size = this.TextSize(Context);

			Label Label = new()
			{
				FontFamily = MarkdownFonts.Body,
				FontSize = Size,
				LineHeight = this.metrics.LineHeight,
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = ToTextAlignment(Alignment)
			};
			Label.SetDynamicResource(Label.TextColorProperty, Context.Muted ? MarkdownColors.Muted : MarkdownColors.Text);

			this.SetText(Label, Runs, Size, false);

			return Label;
		}

		private void SetText(Label Label, IReadOnlyList<MdRun> Runs, double Size, bool IsHeading)
		{
			if (Runs.Count == 1 && Runs[0].Link is null && (Runs[0].Style == MdTextStyle.None || (IsHeading && Runs[0].Style == MdTextStyle.Bold)))
			{
				Label.Text = Runs[0].Text;
				return;
			}

			FormattedString Text = new();

			foreach (MdRun Run in Runs)
				Text.Spans.Add(this.CreateSpan(Run, Size, IsHeading));

			Label.FormattedText = Text;
		}

		private Span CreateSpan(MdRun Run, double Size, bool IsHeading)
		{
			Span Span = new() { Text = Run.Text };
			MdTextStyle Style = Run.Style;
			TextDecorations Decorations = TextDecorations.None;

			if (Style.HasFlag(MdTextStyle.Bold) && !IsHeading)
				Span.FontFamily = MarkdownFonts.Bold;

			if (Style.HasFlag(MdTextStyle.Italic))
				Span.FontAttributes = FontAttributes.Italic;

			if (Style.HasFlag(MdTextStyle.Underline))
				Decorations |= TextDecorations.Underline;

			if (Style.HasFlag(MdTextStyle.Strikethrough))
				Decorations |= TextDecorations.Strikethrough;

			if (Style.HasFlag(MdTextStyle.Placeholder))
			{
				// A value that is still missing shows its guide on a warning tint.
				Span.Text = " " + Run.Text + " ";
				Span.FontFamily = MarkdownFonts.Medium;
				Span.SetDynamicResource(Span.TextColorProperty, MarkdownColors.Warning);
				Span.SetDynamicResource(Span.BackgroundColorProperty, MarkdownColors.WarningBackground);
			}
			else if (Style.HasFlag(MdTextStyle.Code))
			{
				Span.Text = " " + Run.Text + " ";
				Span.FontFamily = MarkdownFonts.InlineCode;
				Span.SetDynamicResource(Span.BackgroundColorProperty, MarkdownColors.Chip);
			}
			else if (Style.HasFlag(MdTextStyle.Tag))
			{
				Span.Text = " " + Run.Text + " ";
				Span.FontFamily = MarkdownFonts.Medium;
				Span.FontSize = Size - 2;
				Span.SetDynamicResource(Span.BackgroundColorProperty, MarkdownColors.Chip);
			}

			if (Run.Link is not null)
			{
				if (Style.HasFlag(MdTextStyle.FootnoteReference))
					Span.FontFamily = MarkdownFonts.Bold;
				else
					Decorations |= TextDecorations.Underline;

				if (!Style.HasFlag(MdTextStyle.Placeholder))
					Span.SetDynamicResource(Span.TextColorProperty, MarkdownColors.Accent);

				Span.GestureRecognizers.Add(new TapGestureRecognizer
				{
					Command = this.linkCommand,
					CommandParameter = Run.Link
				});
			}

			if (Decorations != TextDecorations.None)
				Span.TextDecorations = Decorations;

			return Span;
		}

		private Label CreateHeading(MdHeading Heading)
		{
			double Size = this.metrics.HeadingSizes[Heading.Level - 1];

			Label Label = new()
			{
				FontFamily = MarkdownFonts.Heading,
				FontSize = Size,
				LineHeight = 1.1,
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = ToTextAlignment(Heading.Alignment)
			};
			Label.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Text);
			SemanticProperties.SetHeadingLevel(Label, (SemanticHeadingLevel)Heading.Level);

			this.SetText(Label, Heading.Runs, Size, true);

			return Label;
		}

		private Label CreateNotice(string Text, Context Context)
		{
			Label Label = new()
			{
				Text = Text,
				FontFamily = MarkdownFonts.Body,
				FontSize = this.TextSize(Context),
				LineBreakMode = LineBreakMode.WordWrap
			};
			Label.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Danger);

			return Label;
		}

		private Label CreateHtml(MdHtml Html, Context Context)
		{
			Label Label = new()
			{
				Text = Html.Html,
				TextType = TextType.Html,
				FontFamily = MarkdownFonts.Body,
				FontSize = this.TextSize(Context),
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = ToTextAlignment(Html.Alignment)
			};
			Label.SetDynamicResource(Label.TextColorProperty, Context.Muted ? MarkdownColors.Muted : MarkdownColors.Text);

			return Label;
		}

		#endregion

		#region Lists

		private View CreateList(MdList List, Context Context)
		{
			VerticalStackLayout Stack = new() { Spacing = this.metrics.ListSpacing };
			double Size = this.TextSize(Context);

			foreach (MdListItem Item in List.Items)
			{
				Grid Row = new()
				{
					ColumnDefinitions =
					{
						new ColumnDefinition(GridLength.Auto),
						new ColumnDefinition(GridLength.Star)
					},
					ColumnSpacing = this.metrics.MarkerGap
				};

				View Marker = List.Kind == MdListKind.Task ?
					CreateCheckBox(Item.Checked ?? false) :
					this.CreateMarker(Item.Marker, List.Kind == MdListKind.Numbered, Size);

				Row.Add(Marker, 0, 0);

				if (Item.Blocks.Count > 0)
					Row.Add(this.CreateBlocks(Item.Blocks, this.metrics.ListSpacing, Context), 1, 0);

				Stack.Add(Row);
			}

			return Stack;
		}

		private Label CreateMarker(string Marker, bool Numbered, double Size)
		{
			Label Label = new()
			{
				Text = Marker,
				FontFamily = MarkdownFonts.Body,
				FontSize = Size,
				LineHeight = this.metrics.LineHeight,
				MinimumWidthRequest = Numbered ? Size * 1.1 : Size * 0.75,
				HorizontalTextAlignment = Numbered ? Microsoft.Maui.TextAlignment.End : Microsoft.Maui.TextAlignment.Center,
				VerticalOptions = LayoutOptions.Start
			};
			Label.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Muted);
			SemanticProperties.SetDescription(Label, Numbered ? Marker : string.Empty);

			if (!Numbered)
				AutomationProperties.SetIsInAccessibleTree(Label, false);

			return Label;
		}

		private static View CreateCheckBox(bool Checked)
		{
			Border Box = new()
			{
				WidthRequest = 20,
				HeightRequest = 20,
				Margin = new Thickness(0, 1, 0, 0),
				StrokeThickness = 2,
				StrokeShape = new RoundRectangle { CornerRadius = 5 },
				VerticalOptions = LayoutOptions.Start,
				Padding = 0
			};

			if (Checked)
			{
				Box.SetDynamicResource(Border.StrokeProperty, MarkdownColors.Accent);
				Box.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Accent);

				Label Check = new()
				{
					Text = "✓",
					FontFamily = MarkdownFonts.Bold,
					FontSize = 13,
					FontAutoScalingEnabled = false,
					HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
					VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center
				};
				Check.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Surface);
				AutomationProperties.SetIsInAccessibleTree(Check, false);

				Box.Content = Check;
			}
			else
			{
				Box.SetDynamicResource(Border.StrokeProperty, MarkdownColors.Muted);
				Box.BackgroundColor = Colors.Transparent;
			}

			SemanticProperties.SetDescription(Box, ServiceRef.Localizer[Checked ? nameof(AppResources.MarkdownTaskDone) : nameof(AppResources.MarkdownTaskNotDone)]);

			return Box;
		}

		#endregion

		#region Frames, code, tables, rules

		private View CreateFrame(MdFrame Frame, Context Context)
		{
			if (Frame.Kind == MdFrameKind.Indented)
			{
				return new ContentView
				{
					Padding = new Thickness(16, 0, 0, 0),
					Content = this.CreateBlocks(Frame.Blocks, this.metrics.ListSpacing, Context)
				};
			}

			Border Border = new()
			{
				Padding = this.metrics.CardPadding,
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				Content = this.CreateBlocks(Frame.Blocks, this.metrics.ListSpacing,
					Context with { Muted = Frame.Kind == MdFrameKind.Quote || Context.Muted })
			};

			Border.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Card);
			Border.SetDynamicResource(Border.StrokeProperty, Frame.Kind switch
			{
				MdFrameKind.Inserted => MarkdownColors.Inserted,
				MdFrameKind.Deleted => MarkdownColors.Deleted,
				_ => MarkdownColors.Line
			});

			return Border;
		}

		private BoxView CreateRule()
		{
			BoxView Rule = new()
			{
				HeightRequest = 1,
				Margin = new Thickness(0, 4)
			};
			Rule.SetDynamicResource(BoxView.ColorProperty, MarkdownColors.Line);

			return Rule;
		}

		/// <summary>
		/// A code block: a card with the language, a Copy button, and the code, which scrolls sideways in the card.
		/// </summary>
		private View CreateCode(MdCode Code)
		{
			Label Language = new()
			{
				Text = string.IsNullOrEmpty(Code.Language) ? ServiceRef.Localizer[nameof(AppResources.MarkdownCode)] : Code.Language,
				FontFamily = MarkdownFonts.Medium,
				FontSize = this.metrics.SmallSize - 1,
				VerticalOptions = LayoutOptions.Center,
				Margin = new Thickness(14, 0, 0, 0),
				LineBreakMode = LineBreakMode.TailTruncation
			};
			Language.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Muted);

			Label CodeLabel = new()
			{
				Text = Code.Text,
				FontFamily = MarkdownFonts.Monospace,
				FontSize = this.metrics.CodeSize,
				LineHeight = 1.2,
				LineBreakMode = LineBreakMode.NoWrap
			};
			CodeLabel.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Text);

			ScrollView Scroll = new()
			{
				Orientation = ScrollOrientation.Horizontal,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
				Padding = new Thickness(14, 0, 14, 12),
				Content = CodeLabel
			};

			Grid Header = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto)
				}
			};
			Header.Add(Language, 0, 0);
			Header.Add(this.CreateCopyButton(Code.Text), 1, 0);

			Grid Layout = new()
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};
			Layout.Add(Header, 0, 0);
			Layout.Add(Scroll, 0, 1);

			Border Card = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				Padding = 0,
				Content = Layout
			};
			Card.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Chip);

			return Card;
		}

		/// <summary>
		/// Copy: a small pill in a tap area of at least 48 by 48.
		/// </summary>
		private View CreateCopyButton(string Text)
		{
			string CopyText = ServiceRef.Localizer[nameof(AppResources.MarkdownCopy)];
			string CopiedText = ServiceRef.Localizer[nameof(AppResources.MarkdownCopied)];

			Microsoft.Maui.Controls.Shapes.Path Icon = new()
			{
				Data = Geometries.ClipboardIconPath,
				Aspect = Stretch.Uniform,
				WidthRequest = 14,
				HeightRequest = 14,
				VerticalOptions = LayoutOptions.Center
			};
			Icon.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, MarkdownColors.Text);

			Label Caption = new()
			{
				Text = CopyText,
				FontFamily = MarkdownFonts.Medium,
				FontSize = this.metrics.SmallSize,
				VerticalOptions = LayoutOptions.Center
			};
			Caption.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Text);

			Border Pill = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 15 },
				Padding = new Thickness(10, 5),
				VerticalOptions = LayoutOptions.Center,
				Content = new HorizontalStackLayout
				{
					Spacing = 6,
					Children = { Icon, Caption }
				}
			};
			Pill.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Card);

			ContentView Button = new()
			{
				MinimumHeightRequest = 48,
				MinimumWidthRequest = 48,
				Padding = new Thickness(8, 4),
				Content = Pill
			};
			SemanticProperties.SetDescription(Button, CopyText);

			bool Busy = false;
			TapGestureRecognizer Tap = new();
			Tap.Tapped += async (Sender, e) =>
			{
				if (Busy)
					return;

				Busy = true;

				try
				{
					await Clipboard.Default.SetTextAsync(Text);

					Caption.Text = CopiedText;
					ScreenReader.Announce(CopiedText);

					await Task.Delay(2000);
					Caption.Text = CopyText;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
				finally
				{
					Busy = false;
				}
			};
			Button.GestureRecognizers.Add(Tap);

			return Button;
		}

		/// <summary>
		/// A table: a card whose rows scroll sideways in the card when they are wider than it.
		/// </summary>
		private View CreateTable(MdTable Table, Context Context)
		{
			Grid Cells = new()
			{
				ColumnSpacing = 0,
				RowSpacing = 0
			};

			for (int i = 0; i < Table.Columns; i++)
				Cells.ColumnDefinitions.Add(new ColumnDefinition(i == Table.Columns - 1 ? GridLength.Star : GridLength.Auto));

			double Size = this.TextSize(Context) - 1;
			int RowIndex = 0;

			foreach (MdTableRow Row in Table.Rows)
			{
				Cells.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

				// Header rows are tinted, and other rows have a line above them.
				if (Row.IsHeader || RowIndex > 0)
				{
					BoxView Background = new()
					{
						VerticalOptions = Row.IsHeader ? LayoutOptions.Fill : LayoutOptions.Start,
						HeightRequest = Row.IsHeader ? -1 : 1
					};
					Background.SetDynamicResource(BoxView.ColorProperty, Row.IsHeader ? MarkdownColors.Chip : MarkdownColors.Line);
					Cells.Add(Background, 0, RowIndex);
					Microsoft.Maui.Controls.Grid.SetColumnSpan(Background, Math.Max(1, Table.Columns));
				}

				foreach (MdTableCell Cell in Row.Cells)
				{
					View Content;

					if (Cell.Runs is not null)
					{
						Label Label = new()
						{
							FontFamily = Row.IsHeader ? MarkdownFonts.Medium : MarkdownFonts.Body,
							FontSize = Size,
							LineHeight = this.metrics.LineHeight,
							LineBreakMode = LineBreakMode.WordWrap,
							MaximumWidthRequest = 280,
							HorizontalTextAlignment = ToTextAlignment(Cell.Alignment)
						};
						Label.SetDynamicResource(Label.TextColorProperty, Context.Muted ? MarkdownColors.Muted : MarkdownColors.Text);
						this.SetText(Label, Cell.Runs, Size, false);
						Content = Label;
					}
					else
					{
						Content = this.CreateBlocks(Cell.Blocks ?? [], this.metrics.ListSpacing, Context);
						Content.MaximumWidthRequest = 320;
					}

					ContentView Host = new()
					{
						Padding = new Thickness(12, 10),
						Content = Content,
						HorizontalOptions = Cell.Alignment switch
						{
							MdAlignment.Center => LayoutOptions.Center,
							MdAlignment.End => LayoutOptions.End,
							_ => LayoutOptions.Fill
						}
					};

					Cells.Add(Host, Cell.Column, RowIndex);

					if (Cell.ColumnSpan > 1)
						Microsoft.Maui.Controls.Grid.SetColumnSpan(Host, Cell.ColumnSpan);
				}

				RowIndex++;
			}

			ScrollView Scroll = new()
			{
				Orientation = ScrollOrientation.Horizontal,
				Content = Cells
			};

			// A narrow table fills the card; a wide one scrolls sideways in it.
			Scroll.SizeChanged += (Sender, e) =>
			{
				if (Scroll.Width > 0 && Math.Abs(Cells.MinimumWidthRequest - Scroll.Width) > 0.5)
					Cells.MinimumWidthRequest = Scroll.Width;
			};

			Border Card = new()
			{
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				Padding = 0,
				Content = Scroll
			};
			Card.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Card);
			Card.SetDynamicResource(Border.StrokeProperty, MarkdownColors.Line);

			if (Table.Caption is null)
				return Card;

			Label Caption = new()
			{
				Text = Table.Caption,
				FontFamily = MarkdownFonts.Body,
				FontSize = this.metrics.SmallSize,
				LineBreakMode = LineBreakMode.WordWrap
			};
			Caption.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Muted);

			return new VerticalStackLayout
			{
				Spacing = 6,
				Children = { Card, Caption }
			};
		}

		#endregion

		#region Cards and footnotes

		private static Geometry EmbedIcon(MdEmbedKind Kind)
		{
			return Kind switch
			{
				MdEmbedKind.Token => Geometries.TokenPath,
				MdEmbedKind.Identity => Geometries.PersonPath,
				MdEmbedKind.Contract => Geometries.ContractPath,
				MdEmbedKind.Payment => Geometries.EDalerIconPath,
				MdEmbedKind.Chat => Geometries.PersonPath,
				MdEmbedKind.App => Geometries.TapPath,
				_ => Geometries.AttachmentPath
			};
		}

		/// <summary>
		/// A card for something the app can open, or a link that is a paragraph of its own. It is at least 56 high.
		/// </summary>
		private View CreateEmbed(MdEmbed Embed)
		{
			bool IsWeb = Embed.Kind == MdEmbedKind.Web;

			Microsoft.Maui.Controls.Shapes.Path Icon = new()
			{
				Data = EmbedIcon(Embed.Kind),
				Aspect = Stretch.Uniform,
				WidthRequest = 18,
				HeightRequest = 18,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			Icon.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, IsWeb ? MarkdownColors.Text : MarkdownColors.Accent);

			Border Avatar = new()
			{
				WidthRequest = 36,
				HeightRequest = 36,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 18 },
				VerticalOptions = LayoutOptions.Center,
				Content = Icon
			};
			Avatar.SetDynamicResource(VisualElement.BackgroundColorProperty, IsWeb ? MarkdownColors.Chip : MarkdownColors.AccentBackground);

			Label Title = new()
			{
				Text = Embed.Title,
				FontFamily = MarkdownFonts.Medium,
				FontSize = this.metrics.BodySize - 1,
				LineBreakMode = LineBreakMode.WordWrap,
				MaxLines = 2
			};
			Title.SetDynamicResource(Label.TextColorProperty, IsWeb ? MarkdownColors.Accent : MarkdownColors.Text);

			VerticalStackLayout Texts = new()
			{
				Spacing = 2,
				VerticalOptions = LayoutOptions.Center,
				Children = { Title }
			};

			if (!string.IsNullOrEmpty(Embed.Subtitle))
			{
				Label Subtitle = new()
				{
					Text = Embed.Subtitle,
					FontFamily = MarkdownFonts.Body,
					FontSize = this.metrics.SmallSize,
					LineBreakMode = LineBreakMode.TailTruncation,
					MaxLines = 1
				};
				Subtitle.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Muted);
				Texts.Add(Subtitle);
			}

			Microsoft.Maui.Controls.Shapes.Path Chevron = new()
			{
				Data = Geometries.ArrowRightPath,
				Aspect = Stretch.Uniform,
				WidthRequest = 14,
				HeightRequest = 14,
				VerticalOptions = LayoutOptions.Center
			};
			Chevron.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, MarkdownColors.Muted);

			Grid Layout = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Auto),
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto)
				},
				ColumnSpacing = 12
			};
			Layout.Add(Avatar, 0, 0);
			Layout.Add(Texts, 1, 0);
			Layout.Add(Chevron, 2, 0);

			Border Card = new()
			{
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				Padding = new Thickness(12, 8),
				MinimumHeightRequest = 56,
				Content = Layout
			};
			Card.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Card);
			Card.SetDynamicResource(Border.StrokeProperty, MarkdownColors.Line);

			SemanticProperties.SetDescription(Card, string.IsNullOrEmpty(Embed.Subtitle) ? Embed.Title : Embed.Title + ", " + Embed.Subtitle);
			AutomationProperties.SetIsInAccessibleTree(Texts, false);
			AutomationProperties.SetIsInAccessibleTree(Avatar, false);
			AutomationProperties.SetIsInAccessibleTree(Chevron, false);

			Card.GestureRecognizers.Add(new TapGestureRecognizer
			{
				Command = this.linkCommand,
				CommandParameter = Embed.Uri
			});

			return Card;
		}

		private View CreateFootnotes(MdFootnotes Footnotes)
		{
			VerticalStackLayout Stack = new() { Spacing = this.metrics.ListSpacing };
			Stack.Add(this.CreateRule());

			Context Small = new(true, true);

			foreach (MdFootnote Footnote in Footnotes.Items)
			{
				Grid Row = new()
				{
					ColumnDefinitions =
					{
						new ColumnDefinition(GridLength.Auto),
						new ColumnDefinition(GridLength.Star)
					},
					ColumnSpacing = 8
				};

				Label Number = new()
				{
					Text = Footnote.Number.ToString(CultureInfo.CurrentCulture),
					FontFamily = MarkdownFonts.Bold,
					FontSize = this.metrics.SmallSize,
					MinimumWidthRequest = this.metrics.SmallSize,
					HorizontalTextAlignment = Microsoft.Maui.TextAlignment.End,
					VerticalOptions = LayoutOptions.Start
				};
				Number.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Accent);

				Row.Add(Number, 0, 0);
				Row.Add(this.CreateBlocks(Footnote.Blocks, this.metrics.ListSpacing, Small), 1, 0);
				Stack.Add(Row);
			}

			return Stack;
		}

		#endregion
	}
}
