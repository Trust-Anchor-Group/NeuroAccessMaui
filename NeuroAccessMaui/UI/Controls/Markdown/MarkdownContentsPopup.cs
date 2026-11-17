using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// A sheet that rises from the bottom with the headings of a text. Tapping one closes the sheet and scrolls to it. The
	/// section on screen is marked, and every row is at least 48 high.
	/// </summary>
	internal sealed class MarkdownContentsPopup : BasicPopup
	{
		private readonly Func<MdHeading, Task> onSelected;
		private bool selecting;

		/// <summary>
		/// Creates the Contents sheet.
		/// </summary>
		/// <param name="Headings">The text's top-level headings.</param>
		/// <param name="Current">The heading of the section on screen, if known.</param>
		/// <param name="OnSelected">Scrolls to a heading.</param>
		public MarkdownContentsPopup(IReadOnlyList<MdHeading> Headings, MdHeading? Current, Func<MdHeading, Task> OnSelected)
		{
			this.onSelected = OnSelected;

			this.Placement = PopupPlacement.Bottom;
			this.PopupMargin = new Thickness(0);
			this.CardWidthFraction = 1;
			this.CardMaxHeightFraction = 0.8;
			this.CardPadding = new Thickness(0);
			this.CardStyle = new Style(typeof(Border))
			{
				Setters =
				{
					new Setter { Property = Border.StrokeThicknessProperty, Value = 0.0 },
					new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.Transparent },
					new Setter { Property = Border.PaddingProperty, Value = new Thickness(0) }
				}
			};

			int MinLevel = int.MaxValue;
			foreach (MdHeading Heading in Headings)
				MinLevel = Math.Min(MinLevel, Heading.Level);

			string Title = ServiceRef.Localizer[nameof(AppResources.MarkdownContents)];

			Label TitleLabel = new()
			{
				Text = Title,
				FontFamily = MarkdownFonts.Heading,
				FontSize = 20,
				VerticalOptions = LayoutOptions.Center,
				Margin = new Thickness(12, 0, 0, 0)
			};
			TitleLabel.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Text);
			SemanticProperties.SetHeadingLevel(TitleLabel, SemanticHeadingLevel.Level1);

			Microsoft.Maui.Controls.Shapes.Path CloseIcon = new()
			{
				Data = Geometries.CancelPath,
				Aspect = Stretch.Uniform,
				WidthRequest = 16,
				HeightRequest = 16,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			CloseIcon.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, MarkdownColors.Text);

			ContentView CloseButton = new()
			{
				WidthRequest = 48,
				HeightRequest = 48,
				Content = CloseIcon
			};
			SemanticProperties.SetDescription(CloseButton, ServiceRef.Localizer[nameof(AppResources.Close)]);
			TapGestureRecognizer CloseTap = new();
			CloseTap.Tapped += async (Sender, e) => await ServiceRef.PopupService.PopAsync();
			CloseButton.GestureRecognizers.Add(CloseTap);

			Grid Header = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto)
				}
			};
			Header.Add(TitleLabel, 0, 0);
			Header.Add(CloseButton, 1, 0);

			VerticalStackLayout Rows = new() { Spacing = 2 };
			Rows.Add(Header);

			foreach (MdHeading Heading in Headings)
				Rows.Add(this.CreateRow(Heading, Heading.Level - MinLevel, ReferenceEquals(Heading, Current)));

			Border Sheet = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20, 20, 0, 0) },
				Padding = new Thickness(8, 8, 8, 28),
				Content = new ScrollView
				{
					VerticalOptions = LayoutOptions.Center,
					Content = Rows
				}
			};
			Sheet.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Card);

			this.CardContent = Sheet;
		}

		private View CreateRow(MdHeading Heading, int Depth, bool IsCurrent)
		{
			Label Text = new()
			{
				Text = Heading.Text,
				FontFamily = Depth == 0 || IsCurrent ? MarkdownFonts.Medium : MarkdownFonts.Body,
				FontSize = Depth >= 2 ? 15 : 16,
				LineBreakMode = LineBreakMode.WordWrap,
				VerticalOptions = LayoutOptions.Center
			};
			Text.SetDynamicResource(Label.TextColorProperty, IsCurrent ? MarkdownColors.Accent : Depth >= 2 ? MarkdownColors.Muted : MarkdownColors.Text);

			Border Row = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				MinimumHeightRequest = 48,
				Padding = new Thickness(12 + 20 * Math.Min(Depth, 2), 8, 12, 8),
				Content = Text
			};

			if (IsCurrent)
				Row.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.AccentBackground);
			else
				Row.BackgroundColor = Colors.Transparent;

			SemanticProperties.SetDescription(Row, IsCurrent ?
				Heading.Text + ", " + ServiceRef.Localizer[nameof(AppResources.MarkdownCurrentSection)] :
				Heading.Text);
			AutomationProperties.SetIsInAccessibleTree(Text, false);

			TapGestureRecognizer Tap = new();
			Tap.Tapped += async (Sender, e) => await this.SelectAsync(Heading);
			Row.GestureRecognizers.Add(Tap);

			return Row;
		}

		private async Task SelectAsync(MdHeading Heading)
		{
			if (this.selecting)
				return;

			this.selecting = true;

			try
			{
				await ServiceRef.PopupService.PopAsync();
				await this.onSelected(Heading);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}
	}
}
