using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Popups.Images
{
	/// <summary>
	/// Shows an image, such as a chart drawn by the server, on the whole screen, where it can be zoomed, moved and shared.
	/// In portrait the top bar sits above the image; in landscape the image takes the whole screen and the buttons float
	/// over it.
	/// </summary>
	public class ImageViewerPopup : BasicPopup
	{
		private static readonly Thickness portraitPadding = new(12, 12, 12, 20);

		private readonly ImageSource source;
		private readonly string title;
		private readonly Grid layout;
		private readonly Grid header;
		private readonly Border titleBox;
		private readonly ZoomImageView zoomView;
		private bool? isLandscape;

		/// <summary>
		/// Creates a viewer for an image.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <param name="Title">A title shown above the image, if any.</param>
		public ImageViewerPopup(ImageSource Source, string? Title)
		{
			this.CardWidthFraction = 1;
			this.CardMaxHeightFraction = 1;
			this.PopupMargin = new Thickness(0);
			this.CardPadding = portraitPadding;

			this.source = Source;
			this.title = string.IsNullOrEmpty(Title) ? ServiceRef.Localizer[nameof(AppResources.HistoryValueChart)] : Title;

			this.titleBox = new Border
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 20 },
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Start
			};
			this.header = this.CreateHeader();
			this.zoomView = new ZoomImageView { Source = Source, ImageDescription = this.title };

			this.layout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star }
				},
				RowSpacing = 12
			};

			this.layout.Add(this.zoomView, 0, 1);
			this.layout.Add(this.header, 0, 0);

			this.CardContent = this.layout;
		}

		/// <inheritdoc/>
		protected override void OnSizeAllocated(double Width, double Height)
		{
			base.OnSizeAllocated(Width, Height);

			if (Width <= 0 || Height <= 0)
				return;

			this.Arrange(Width > Height);

			// The viewer fills the screen. The popup layer already leaves room for the system bars.
			Thickness Padding = this.CardPadding;
			this.layout.HeightRequest = Height - Padding.Top - Padding.Bottom;
		}

		/// <summary>
		/// Puts the top bar above the image in portrait, and over it in landscape, where the image takes the whole screen.
		/// </summary>
		/// <param name="Landscape">If the screen is wider than it is tall.</param>
		private void Arrange(bool Landscape)
		{
			if (this.isLandscape == Landscape)
				return;

			this.isLandscape = Landscape;
			this.CardPadding = Landscape ? new Thickness(0) : portraitPadding;

			Grid.SetRow(this.zoomView, Landscape ? 0 : 1);
			Grid.SetRowSpan(this.zoomView, Landscape ? 2 : 1);
			this.zoomView.IsFullScreen = Landscape;

			this.layout.RowSpacing = Landscape ? 0 : 12;
			this.header.Margin = Landscape ? new Thickness(12) : new Thickness(0);

			// Over the image, the title gets a background of its own so it can be read.
			this.titleBox.Padding = Landscape ? new Thickness(14, 6) : new Thickness(0);
			if (Landscape)
				this.titleBox.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation1WL");
			else
				this.titleBox.BackgroundColor = Colors.Transparent;
		}

		/// <inheritdoc/>
		public override Task OnAppearingAsync()
		{
			ServiceRef.PlatformSpecific.SetScreenRotation(true);
			return base.OnAppearingAsync();
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			ServiceRef.PlatformSpecific.SetScreenRotation(false);
			return base.OnDisappearingAsync();
		}

		/// <summary>
		/// Shows an image in the viewer.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <param name="Title">A title shown above the image, if any.</param>
		public static Task ShowAsync(ImageSource Source, string? Title)
		{
			return ServiceRef.PopupService.PushAsync(new ImageViewerPopup(Source, Title));
		}

		/// <summary>
		/// Creates the top bar: close, the title with where the image is from, and share.
		/// </summary>
		/// <returns>The top bar.</returns>
		private Grid CreateHeader()
		{
			Label TitleLabel = new()
			{
				Text = this.title,
				FontFamily = "SpaceGroteskSemiBold",
				FontSize = 17,
				LineBreakMode = LineBreakMode.TailTruncation,
				MaxLines = 1
			};
			TitleLabel.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");

			Label SourceLabel = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ZoomFromServer), DateTime.Now.ToString("t", CultureInfo.CurrentCulture)],
				FontSize = 12
			};
			SourceLabel.SetDynamicResource(Label.TextColorProperty, "ContentSecondaryWL");

			// Over the image, the bar's empty space lets touches through to the image; its buttons still take taps.
			Grid Header = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Auto }
				},
				ColumnSpacing = 12,
				VerticalOptions = LayoutOptions.Start,
				InputTransparent = true,
				CascadeInputTransparent = false
			};
			Header.Add(ZoomImageView.CreateRoundButton(Geometries.CancelPath, ServiceRef.Localizer[nameof(AppResources.Close)],
				() => _ = ServiceRef.PopupService.PopAsync()), 0, 0);

			this.titleBox.Content = new VerticalStackLayout
			{
				VerticalOptions = LayoutOptions.Center,
				Children = { TitleLabel, SourceLabel }
			};
			Header.Add(this.titleBox, 1, 0);

			if (ImageShare.GetFile(this.source) is not null)
			{
				Header.Add(ZoomImageView.CreateRoundButton(Geometries.ShareIconPath, ServiceRef.Localizer[nameof(AppResources.Share)],
					() => _ = ImageShare.ShareAsync(this.source, this.title)), 2, 0);
			}

			return Header;
		}
	}
}
