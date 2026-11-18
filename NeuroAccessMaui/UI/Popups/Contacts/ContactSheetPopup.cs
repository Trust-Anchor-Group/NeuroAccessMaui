using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Popups;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// A sheet that rises from the bottom, for contacts: adding someone, answering a request, blocking or reporting. It has a
	/// handle, an optional title with a close button, and content that scrolls. Every tappable row is at least 48 high.
	/// </summary>
	public abstract class ContactSheetPopup : BasicPopup
	{
		private readonly VerticalStackLayout content;
		private bool closing;

		/// <summary>
		/// Creates the sheet.
		/// </summary>
		protected ContactSheetPopup()
		{
			this.Placement = PopupPlacement.Bottom;
			this.PopupMargin = new Thickness(0);
			this.CardWidthFraction = 1;
			this.CardMaxHeightFraction = 0.9;
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

			this.content = new VerticalStackLayout { Spacing = 14 };

			BoxView Handle = new()
			{
				WidthRequest = 40,
				HeightRequest = 4,
				CornerRadius = 2,
				HorizontalOptions = LayoutOptions.Center
			};
			Handle.SetDynamicResource(BoxView.ColorProperty, "SurfaceDividerWL");
			AutomationProperties.SetIsInAccessibleTree(Handle, false);
			this.content.Add(Handle);

			Border Sheet = new()
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
				Padding = new Thickness(16, 12, 16, 28),
				Content = new ScrollView { Content = this.content }
			};
			Sheet.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation1WL");

			this.CardContent = Sheet;
		}

		/// <summary>
		/// Shows a sheet, sliding up from the bottom.
		/// </summary>
		/// <param name="Sheet">The sheet.</param>
		protected static Task ShowSheetAsync(ContactSheetPopup Sheet)
		{
			PopupOptions Options = new()
			{
				Placement = PopupPlacement.Bottom,
				ShowTransition = PopupTransition.SlideUp
			};

			return ServiceRef.PopupService.PushAsync(Sheet, Options);
		}

		/// <summary>
		/// Adds a view to the sheet.
		/// </summary>
		/// <param name="View">The view.</param>
		protected void Add(View View)
		{
			this.content.Add(View);
		}

		/// <summary>
		/// Closes the sheet once, then runs an action.
		/// </summary>
		/// <param name="Then">What to do after the sheet has closed, if anything.</param>
		protected async Task CloseAsync(Func<Task>? Then = null)
		{
			if (this.closing)
				return;

			this.closing = true;

			try
			{
				await ServiceRef.PopupService.PopAsync();

				if (Then is not null)
					await Then();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// A title, with an optional line below it, and a close button.
		/// </summary>
		/// <param name="Title">The title.</param>
		/// <param name="Subtitle">A line below it, if any.</param>
		/// <returns>The header.</returns>
		protected View CreateHeader(string Title, string? Subtitle)
		{
			Label TitleLabel = new()
			{
				Text = Title,
				FontFamily = "SpaceGroteskBold",
				FontSize = 24,
				LineBreakMode = LineBreakMode.WordWrap
			};
			TitleLabel.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");
			SemanticProperties.SetHeadingLevel(TitleLabel, SemanticHeadingLevel.Level1);

			VerticalStackLayout Texts = new()
			{
				Spacing = 4,
				VerticalOptions = LayoutOptions.Center,
				Children = { TitleLabel }
			};

			if (!string.IsNullOrEmpty(Subtitle))
				Texts.Add(CreateText(Subtitle, 15, true));

			Grid Header = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto)
				},
				ColumnSpacing = 12,
				Padding = new Thickness(4, 0, 0, 0)
			};
			Header.Add(Texts, 0, 0);
			Header.Add(this.CreateCloseButton(), 1, 0);

			return Header;
		}

		/// <summary>
		/// A close button, 48 by 48.
		/// </summary>
		/// <returns>The button.</returns>
		protected View CreateCloseButton()
		{
			Border Circle = new()
			{
				WidthRequest = 48,
				HeightRequest = 48,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 14 },
				InputTransparent = true,
				Content = CreateIcon("M18 6L6 18M6 6l12 12", "ContentPrimaryWL")
			};
			Circle.SetDynamicResource(VisualElement.BackgroundColorProperty, "MarkdownChipWL");

			TemplatedButton Button = new()
			{
				VerticalOptions = LayoutOptions.Start,
				Content = Circle
			};
			SemanticProperties.SetDescription(Button, ServiceRef.Localizer[nameof(AppResources.Close)]);
			Button.Clicked += async (Sender, e) => await this.CloseAsync();

			return Button;
		}

		/// <summary>
		/// A row with an icon on a tint, a title, a line below it, and an action.
		/// </summary>
		/// <param name="IconData">The icon, as stroke path data in a 24 by 24 box.</param>
		/// <param name="Title">The title.</param>
		/// <param name="Subtitle">A line below the title, if any.</param>
		/// <param name="Danger">If the row does something that can't easily be undone, shown in the danger colour.</param>
		/// <param name="Action">What a tap does, after the sheet has closed.</param>
		/// <param name="Selected">What a tap does at once, before the sheet closes, such as noting the user's choice.</param>
		/// <returns>The row.</returns>
		protected View CreateRow(string IconData, string Title, string? Subtitle, bool Danger, Func<Task>? Action, Action? Selected = null)
		{
			Border Tile = new()
			{
				WidthRequest = 48,
				HeightRequest = 48,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 14 },
				VerticalOptions = LayoutOptions.Center,
				Content = CreateIcon(IconData, Danger ? "TnPDangerContentWL" : "ContentAccessWL")
			};
			Tile.SetDynamicResource(VisualElement.BackgroundColorProperty, Danger ? "TnPDangerbgWL" : "TnPSuccessbgWL");

			Label TitleLabel = CreateText(Title, 16, false);
			TitleLabel.FontFamily = "HaasGroteskMedium";
			if (Danger)
				TitleLabel.SetDynamicResource(Label.TextColorProperty, "TnPDangerContentWL");

			VerticalStackLayout Texts = new()
			{
				Spacing = 2,
				VerticalOptions = LayoutOptions.Center,
				Children = { TitleLabel }
			};

			if (!string.IsNullOrEmpty(Subtitle))
				Texts.Add(CreateText(Subtitle, 14, true));

			Grid Row = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Auto),
					new ColumnDefinition(GridLength.Star)
				},
				ColumnSpacing = 14,
				Padding = new Thickness(16, 12),
				MinimumHeightRequest = 72,
				InputTransparent = true
			};
			Row.Add(Tile, 0, 0);
			Row.Add(Texts, 1, 0);

			// The tap goes through a command, which the button keeps. The button's Clicked event only keeps weak references to
			// its handlers, so a handler that captures values here can be collected before the tap, and the row does nothing.
			TemplatedButton Button = new()
			{
				Content = Row,
				Command = new AsyncRelayCommand(async () =>
				{
					Selected?.Invoke();
					await this.CloseAsync(Action);
				})
			};
			SemanticProperties.SetDescription(Button, string.IsNullOrEmpty(Subtitle) ? Title : Title + ", " + Subtitle);

			return Button;
		}

		/// <summary>
		/// A card that holds rows, with lines between them.
		/// </summary>
		/// <param name="Rows">The rows.</param>
		/// <returns>The card.</returns>
		protected static View CreateCard(params View[] Rows)
		{
			VerticalStackLayout Stack = new();

			for (int i = 0; i < Rows.Length; i++)
			{
				if (i > 0)
				{
					BoxView Line = new() { HeightRequest = 1, Margin = new Thickness(78, 0, 0, 0) };
					Line.SetDynamicResource(BoxView.ColorProperty, "SurfaceDividerWL");
					Stack.Add(Line);
				}

				Stack.Add(Rows[i]);
			}

			Border Card = new()
			{
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 20 },
				Padding = new Thickness(0),
				Content = Stack
			};
			Card.SetDynamicResource(Border.StrokeProperty, "SurfaceDividerWL");
			Card.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceBackgroundWL");

			return Card;
		}

		/// <summary>
		/// A label, in the primary or secondary text colour.
		/// </summary>
		/// <param name="Text">The text.</param>
		/// <param name="Size">The font size.</param>
		/// <param name="Secondary">If the text is in the secondary colour.</param>
		/// <returns>The label.</returns>
		protected static Label CreateText(string Text, double Size, bool Secondary)
		{
			Label Label = new()
			{
				Text = Text,
				FontFamily = "HaasGroteskRegular",
				FontSize = Size,
				LineBreakMode = LineBreakMode.WordWrap
			};
			Label.SetDynamicResource(Label.TextColorProperty, Secondary ? "ContentSecondaryWL" : "ContentPrimaryWL");

			return Label;
		}

		/// <summary>
		/// A 24 by 24 stroke icon.
		/// </summary>
		/// <param name="Data">The path data, in a 24 by 24 box.</param>
		/// <param name="ColorKey">The colour's resource key.</param>
		/// <returns>The icon.</returns>
		protected static Microsoft.Maui.Controls.Shapes.Path CreateIcon(string Data, string ColorKey)
		{
			Microsoft.Maui.Controls.Shapes.Path Icon = new()
			{
				Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(Data)!,
				Aspect = Stretch.None,
				WidthRequest = 24,
				HeightRequest = 24,
				StrokeThickness = 2,
				StrokeLineCap = PenLineCap.Round,
				StrokeLineJoin = PenLineJoin.Round,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};
			Icon.SetDynamicResource(Microsoft.Maui.Controls.Shapes.Shape.StrokeProperty, ColorKey);
			AutomationProperties.SetIsInAccessibleTree(Icon, false);

			return Icon;
		}
	}
}
