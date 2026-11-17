using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Images;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// An image in a text, such as a diagram in a report. It fits the width it gets and keeps its shape, and a tap opens it
	/// in a viewer where it can be zoomed.
	/// </summary>
	public class ZoomableImage : ContentView
	{
		/// <summary>
		/// The tallest the image is shown in the text. Taller images are scaled down, and can be zoomed in the viewer.
		/// </summary>
		private const double maxInlineHeight = 480;

		private readonly Image image;
		private readonly double aspectRatio;

		/// <summary>
		/// The image.
		/// </summary>
		public ImageSource Source { get; }

		/// <summary>
		/// The title for the viewer, if any.
		/// </summary>
		public string? Title { get; }

		/// <summary>
		/// The line under the title in the viewer. Null shows where the image is from, and an empty text shows no line.
		/// </summary>
		public string? Subtitle { get; }

		/// <summary>
		/// Creates an image that fits its width and opens in a viewer when tapped.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <param name="PixelWidth">The image's width in pixels, if known.</param>
		/// <param name="PixelHeight">The image's height in pixels, if known.</param>
		/// <param name="Title">A title for the viewer, if any.</param>
		/// <param name="Subtitle">The line under the title in the viewer. Null shows where the image is from, and an empty
		/// text shows no line.</param>
		public ZoomableImage(ImageSource Source, int? PixelWidth, int? PixelHeight, string? Title, string? Subtitle = null)
		{
			this.Source = Source;
			this.Title = Title;
			this.Subtitle = Subtitle;
			this.aspectRatio = PixelWidth > 0 && PixelHeight > 0 ? (double)PixelHeight.Value / PixelWidth.Value : 0;

			this.image = new Image
			{
				Source = Source,
				Aspect = Aspect.AspectFit,
				HorizontalOptions = LayoutOptions.Fill
			};

			// Bitmaps keep their light background, also in dark mode, on a slightly dimmed paper tile.
			Border Paper = new()
			{
				StrokeThickness = 0,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
				Padding = new Thickness(4),
				Content = this.image
			};
			Paper.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.White, Color.FromRgb(0xE4, 0xE4, 0xE4));

			TapGestureRecognizer Tap = new();
			Tap.Tapped += async (Sender, Args) =>
			{
				try
				{
					await ImageViewerPopup.ShowAsync(Source, Title, Subtitle);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			};
			Paper.GestureRecognizers.Add(Tap);

			this.Content = Paper;
			this.SizeChanged += this.ZoomableImage_SizeChanged;
		}

		/// <summary>
		/// Gives the image the height that keeps its shape at the width it got.
		/// </summary>
		private void ZoomableImage_SizeChanged(object? Sender, EventArgs e)
		{
			if (this.aspectRatio <= 0 || this.Width <= 0)
				return;

			double Height = Math.Min(this.Width * this.aspectRatio, maxInlineHeight);

			if (Math.Abs(this.image.HeightRequest - Height) > 0.5)
				this.image.HeightRequest = Height;
		}
	}
}
