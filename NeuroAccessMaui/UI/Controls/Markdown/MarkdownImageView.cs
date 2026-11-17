using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// An image or a chart in a text. Until it is shown, a placeholder of the image's size holds its place, so the text
	/// does not jump. The image is loaded, or the chart drawn, off the UI thread when the view is first shown.
	/// </summary>
	internal sealed class MarkdownImageView : ContentView
	{
		/// <summary>
		/// The tallest an image is shown in the text, as in <see cref="ZoomableImage"/>.
		/// </summary>
		private const double maxHeight = 480;

		/// <summary>
		/// Height of a chart's placeholder, before its size is known.
		/// </summary>
		private const double chartHeight = 196;

		private readonly MdImage image;
		private readonly MarkdownMetrics metrics;
		private readonly Border placeholder;
		private double aspectRatio;
		private bool loading;

		/// <summary>
		/// Creates the view of an image or a chart.
		/// </summary>
		/// <param name="Image">The image.</param>
		/// <param name="Metrics">Sizes and gaps.</param>
		public MarkdownImageView(MdImage Image, MarkdownMetrics Metrics)
		{
			this.image = Image;
			this.metrics = Metrics;

			int? Width = Image.Source.Width;
			int? Height = Image.Source.Height;
			this.aspectRatio = Width > 0 && Height > 0 ? (double)Height.Value / Width.Value : 0;

			this.placeholder = new Border
			{
				StrokeThickness = Image.IsChart ? 1 : 0,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				HeightRequest = Image.IsChart ? chartHeight : 160,
				Content = new ActivityIndicator
				{
					IsRunning = true,
					WidthRequest = 24,
					HeightRequest = 24,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};
			this.placeholder.SetDynamicResource(VisualElement.BackgroundColorProperty, Image.IsChart ? MarkdownColors.Card : MarkdownColors.Chip);
			this.placeholder.SetDynamicResource(Border.StrokeProperty, MarkdownColors.Line);
			((ActivityIndicator)this.placeholder.Content).SetDynamicResource(ActivityIndicator.ColorProperty, MarkdownColors.Muted);

			string? Description = Image.Description ?? Image.Caption;
			if (!string.IsNullOrEmpty(Description))
				SemanticProperties.SetDescription(this, Description);

			this.Content = this.placeholder;
			this.SizeChanged += this.OnSizeChanged;
			this.Loaded += this.OnLoaded;

			if (Image.Source.IsLoaded)
				this.OnLoaded(this, EventArgs.Empty);
		}

		private void OnSizeChanged(object? Sender, EventArgs e)
		{
			if (this.aspectRatio <= 0 || this.Width <= 0 || this.Content != this.placeholder)
				return;

			double Height = Math.Min(this.Width * this.aspectRatio, maxHeight);

			if (Math.Abs(this.placeholder.HeightRequest - Height) > 0.5)
				this.placeholder.HeightRequest = Height;
		}

		private async void OnLoaded(object? Sender, EventArgs e)
		{
			if (this.loading)
				return;

			this.loading = true;
			this.Loaded -= this.OnLoaded;

			try
			{
				MdImageData Data = await this.image.Source.LoadAsync();

				await MainThread.InvokeOnMainThreadAsync(() => this.ShowImage(Data));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await MainThread.InvokeOnMainThreadAsync(this.ShowError);
			}
		}

		private void ShowImage(MdImageData Data)
		{
			ZoomableImage Image = new(Data.ToImageSource(), Data.Width, Data.Height, this.image.Description ?? this.image.Caption);
			AutomationProperties.SetIsInAccessibleTree(Image, false);

			if (string.IsNullOrEmpty(this.image.Caption))
			{
				this.Content = Image;
				return;
			}

			Label Caption = new()
			{
				Text = this.image.Caption,
				FontFamily = MarkdownFonts.Body,
				FontSize = this.metrics.SmallSize,
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center
			};
			Caption.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Muted);

			this.Content = new VerticalStackLayout
			{
				Spacing = 6,
				Children = { Image, Caption }
			};
		}

		private void ShowError()
		{
			Label Message = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ReportImageNotShown)],
				FontFamily = MarkdownFonts.Body,
				FontSize = this.metrics.SmallSize + 1,
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
				VerticalTextAlignment = Microsoft.Maui.TextAlignment.Center,
				Margin = new Thickness(12)
			};
			Message.SetDynamicResource(Label.TextColorProperty, MarkdownColors.Muted);

			this.placeholder.HeightRequest = -1;
			this.placeholder.MinimumHeightRequest = 56;
			this.placeholder.Content = Message;
			this.Content = this.placeholder;
		}
	}
}
