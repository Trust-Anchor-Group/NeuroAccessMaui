using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Controls.Markdown;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// An action on a <see cref="ChatSheetPopup"/>.
	/// </summary>
	/// <param name="Text">What the action says.</param>
	/// <param name="Icon">Icon, if any.</param>
	/// <param name="Execute">Runs after the sheet has closed.</param>
	public sealed record ChatSheetAction(string Text, Geometry? Icon, Func<Task> Execute);

	/// <summary>
	/// A sheet that rises from the bottom with actions: as tiles, for what to share in a chat, or as rows, for what to do
	/// with a message. Every action can be tapped in at least 48 by 48.
	/// </summary>
	internal sealed class ChatSheetPopup : BasicPopup
	{
		private const int tileColumns = 3;

		private bool selecting;

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		/// <param name="Title">Title, if any.</param>
		/// <param name="Actions">Actions, in order.</param>
		/// <param name="AsTiles">If the actions show as a grid of tiles, rather than as rows.</param>
		public ChatSheetPopup(string? Title, IReadOnlyList<ChatSheetAction> Actions, bool AsTiles)
		{
			this.Placement = PopupPlacement.Bottom;
			this.PopupMargin = new Thickness(0);
			this.CardWidthFraction = 1;
			this.CardMaxHeightFraction = 0.85;
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

			VerticalStackLayout Content = new() { Spacing = AsTiles ? 16 : 2 };

			BoxView Handle = new()
			{
				WidthRequest = 40,
				HeightRequest = 4,
				CornerRadius = 2,
				HorizontalOptions = LayoutOptions.Center,
				Margin = new Thickness(0, 0, 0, 4)
			};
			Handle.SetDynamicResource(BoxView.ColorProperty, MarkdownColors.Line);
			AutomationProperties.SetIsInAccessibleTree(Handle, false);
			Content.Add(Handle);

			if (!string.IsNullOrEmpty(Title))
				Content.Add(CreateHeader(Title));

			if (AsTiles)
				Content.Add(this.CreateTiles(Actions));
			else
			{
				foreach (ChatSheetAction Action in Actions)
					Content.Add(this.CreateRow(Action));
			}

			Border Sheet = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
				Padding = new Thickness(16, 12, 16, 28),
				Content = new ScrollView
				{
					Content = Content
				}
			};
			Sheet.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Card);

			this.CardContent = Sheet;
		}

		/// <summary>
		/// Shows a sheet.
		/// </summary>
		/// <param name="Title">Title, if any.</param>
		/// <param name="Actions">Actions, in order.</param>
		/// <param name="AsTiles">If the actions show as a grid of tiles, rather than as rows.</param>
		public static async Task ShowAsync(string? Title, IReadOnlyList<ChatSheetAction> Actions, bool AsTiles)
		{
			PopupOptions Options = new()
			{
				Placement = PopupPlacement.Bottom,
				ShowTransition = PopupTransition.SlideUp
			};

			await ServiceRef.PopupService.PushAsync(new ChatSheetPopup(Title, Actions, AsTiles), Options);
		}

		private static View CreateHeader(string Title)
		{
			Label TitleLabel = new()
			{
				Text = Title,
				FontFamily = MarkdownFonts.Heading,
				FontSize = 22,
				VerticalOptions = LayoutOptions.Center,
				Margin = new Thickness(4, 0, 0, 0)
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

			return Header;
		}

		private Grid CreateTiles(IReadOnlyList<ChatSheetAction> Actions)
		{
			Grid Tiles = new()
			{
				ColumnSpacing = 10,
				RowSpacing = 10
			};

			for (int c = 0; c < tileColumns; c++)
				Tiles.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			int Rows = (Actions.Count + tileColumns - 1) / tileColumns;
			for (int r = 0; r < Rows; r++)
				Tiles.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

			for (int i = 0; i < Actions.Count; i++)
				Tiles.Add(this.CreateTile(Actions[i]), i % tileColumns, i / tileColumns);

			return Tiles;
		}

		private View CreateTile(ChatSheetAction Action)
		{
			VerticalStackLayout Content = new()
			{
				Spacing = 8,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			if (Action.Icon is not null)
			{
				Microsoft.Maui.Controls.Shapes.Path Icon = new()
				{
					Data = Action.Icon,
					Aspect = Stretch.Uniform,
					WidthRequest = 22,
					HeightRequest = 22,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				Icon.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, MarkdownColors.Accent);

				Border IconTile = new()
				{
					WidthRequest = 44,
					HeightRequest = 44,
					StrokeThickness = 0,
					StrokeShape = new RoundRectangle { CornerRadius = 14 },
					HorizontalOptions = LayoutOptions.Center,
					Content = Icon
				};
				IconTile.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.AccentBackground);

				Content.Add(IconTile);
			}

			Label Text = new()
			{
				Text = Action.Text,
				FontFamily = MarkdownFonts.Medium,
				FontSize = 14,
				HorizontalTextAlignment = TextAlignment.Center,
				LineBreakMode = LineBreakMode.WordWrap
			};
			Text.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Text);
			AutomationProperties.SetIsInAccessibleTree(Text, false);
			Content.Add(Text);

			Border Tile = new()
			{
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 18 },
				MinimumHeightRequest = 96,
				Padding = new Thickness(4, 10),
				Content = Content
			};
			Tile.SetDynamicResource(Border.StrokeProperty, MarkdownColors.Line);
			Tile.SetDynamicResource(VisualElement.BackgroundColorProperty, MarkdownColors.Surface);

			this.MakeTappable(Tile, Action);

			return Tile;
		}

		private View CreateRow(ChatSheetAction Action)
		{
			Grid Content = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(new GridLength(24)),
					new ColumnDefinition(GridLength.Star)
				},
				ColumnSpacing = 16,
				VerticalOptions = LayoutOptions.Center
			};

			if (Action.Icon is not null)
			{
				Microsoft.Maui.Controls.Shapes.Path Icon = new()
				{
					Data = Action.Icon,
					Aspect = Stretch.Uniform,
					WidthRequest = 20,
					HeightRequest = 20,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				Icon.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Path.FillProperty, MarkdownColors.Text);
				Content.Add(Icon, 0, 0);
			}

			Label Text = new()
			{
				Text = Action.Text,
				FontFamily = MarkdownFonts.Body,
				FontSize = 16,
				LineBreakMode = LineBreakMode.WordWrap,
				VerticalOptions = LayoutOptions.Center
			};
			Text.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Text);
			AutomationProperties.SetIsInAccessibleTree(Text, false);
			Content.Add(Text, 1, 0);

			Border Row = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				MinimumHeightRequest = 48,
				Padding = new Thickness(12, 10),
				BackgroundColor = Colors.Transparent,
				Content = Content
			};

			this.MakeTappable(Row, Action);

			return Row;
		}

		private void MakeTappable(View View, ChatSheetAction Action)
		{
			SemanticProperties.SetDescription(View, Action.Text);
			ButtonRole.SetIsButton(View, true);

			TapGestureRecognizer Tap = new();
			Tap.Tapped += async (Sender, e) => await this.SelectAsync(Action);
			View.GestureRecognizers.Add(Tap);
		}

		private async Task SelectAsync(ChatSheetAction Action)
		{
			if (this.selecting)
				return;

			this.selecting = true;

			try
			{
				await ServiceRef.PopupService.PopAsync();
				await Action.Execute();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}
	}
}
