using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Popups.Images
{
	/// <summary>
	/// Shows an image as large as the screen allows. It opens fitted to the screen, and can be zoomed with a pinch or the
	/// plus and minus buttons, moved by dragging, and fitted again with a double tap or the Fit button.
	/// </summary>
	public class ImageViewerPopup : BasicPopup
	{
		private const double minScale = 1;
		private const double maxScale = 8;
		private const double buttonStep = 1.5;
		private const double doubleTapScale = 2.5;

		private readonly Grid stage;
		private readonly Microsoft.Maui.Controls.Image image;
		private double startTranslationX;
		private double startTranslationY;

		/// <summary>
		/// Creates a viewer for an image.
		/// </summary>
		/// <param name="Source">The image.</param>
		/// <param name="Title">A title shown above the image, if any.</param>
		public ImageViewerPopup(ImageSource Source, string? Title)
		{
			this.CardWidthFraction = 1;
			this.CardMaxHeightFraction = 0.92;

			this.image = new Microsoft.Maui.Controls.Image
			{
				Source = Source,
				Aspect = Aspect.AspectFit,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};

			// Bitmaps keep their light background, also in dark mode, on a slightly dimmed paper tile.
			Border Paper = new()
			{
				StrokeThickness = 0,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
				Content = this.image
			};
			Paper.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.White, Color.FromRgb(0xE4, 0xE4, 0xE4));

			this.stage = new Grid
			{
				IsClippedToBounds = true,
				MinimumHeightRequest = 320,
				Children = { Paper }
			};

			PinchGestureRecognizer Pinch = new();
			Pinch.PinchUpdated += this.Pinch_PinchUpdated;
			this.stage.GestureRecognizers.Add(Pinch);

			PanGestureRecognizer Pan = new();
			Pan.PanUpdated += this.Pan_PanUpdated;
			this.stage.GestureRecognizers.Add(Pan);

			TapGestureRecognizer DoubleTap = new() { NumberOfTapsRequired = 2 };
			DoubleTap.Tapped += (Sender, Args) => this.ToggleZoom(Args.GetPosition(this.stage));
			this.stage.GestureRecognizers.Add(DoubleTap);

			Grid Header = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Auto }
				}
			};
			Header.Add(new Label
			{
				Text = Title ?? string.Empty,
				Style = AppStyles.SectionTitleLabel,
				VerticalOptions = LayoutOptions.Center,
				LineBreakMode = LineBreakMode.TailTruncation
			}, 0, 0);
			Header.Add(new TextButton
			{
				LabelData = ServiceRef.Localizer[nameof(AppResources.Close)],
				Style = AppStyles.SecondaryButton,
				Command = new Command(async () => await ServiceRef.PopupService.PopAsync())
			}, 1, 0);

			Grid Controls = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Auto }
				},
				ColumnSpacing = AppStyles.SmallSpacing
			};
			Controls.Add(new TextButton
			{
				LabelData = ServiceRef.Localizer[nameof(AppResources.ZoomFit)],
				Style = AppStyles.SecondaryButton,
				Command = new Command(() => this.SetZoom(minScale, 0, 0))
			}, 0, 0);
			Controls.Add(new Label
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ZoomHint)],
				Style = AppStyles.InfoLabel,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.Center
			}, 1, 0);
			Controls.Add(new TextButton
			{
				LabelData = "−",
				Style = AppStyles.SecondaryButton,
				Command = new Command(() => this.ZoomAroundCenter(1 / buttonStep))
			}, 2, 0);
			Controls.Add(new TextButton
			{
				LabelData = "+",
				Style = AppStyles.SecondaryButton,
				Command = new Command(() => this.ZoomAroundCenter(buttonStep))
			}, 3, 0);

			Grid Layout = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto }
				},
				RowSpacing = AppStyles.SmallSpacing
			};
			Layout.Add(Header, 0, 0);
			Layout.Add(this.stage, 0, 1);
			Layout.Add(Controls, 0, 2);

			this.CardContent = Layout;
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

		private void Pinch_PinchUpdated(object? Sender, PinchGestureUpdatedEventArgs e)
		{
			switch (e.Status)
			{
				case GestureStatus.Running:
					double Scale = Math.Clamp(this.image.Scale * e.Scale, minScale, maxScale);
					double Factor = Scale / this.image.Scale;

					// Zoom around the point between the fingers, so it stays under them.
					double OriginX = (e.ScaleOrigin.X - 0.5) * this.stage.Width;
					double OriginY = (e.ScaleOrigin.Y - 0.5) * this.stage.Height;

					this.SetZoom(Scale,
						OriginX - (OriginX - this.image.TranslationX) * Factor,
						OriginY - (OriginY - this.image.TranslationY) * Factor);
					break;
			}
		}

		private void Pan_PanUpdated(object? Sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType)
			{
				case GestureStatus.Started:
					this.startTranslationX = this.image.TranslationX;
					this.startTranslationY = this.image.TranslationY;
					break;

				case GestureStatus.Running:
					if (this.image.Scale > minScale)
						this.SetZoom(this.image.Scale, this.startTranslationX + e.TotalX, this.startTranslationY + e.TotalY);
					break;
			}
		}

		/// <summary>
		/// Zooms in on a point, or fits the image again when it is zoomed.
		/// </summary>
		/// <param name="Point">The point that was tapped, relative to the image's area.</param>
		private void ToggleZoom(Point? Point)
		{
			if (this.image.Scale > minScale)
			{
				this.SetZoom(minScale, 0, 0);
				return;
			}

			double OriginX = Point.HasValue ? Point.Value.X - this.stage.Width / 2 : 0;
			double OriginY = Point.HasValue ? Point.Value.Y - this.stage.Height / 2 : 0;

			this.SetZoom(doubleTapScale, -OriginX * (doubleTapScale - 1), -OriginY * (doubleTapScale - 1));
		}

		/// <summary>
		/// Zooms in or out around the middle of the image's area.
		/// </summary>
		/// <param name="Factor">How much to zoom, above 1 to zoom in.</param>
		private void ZoomAroundCenter(double Factor)
		{
			double Scale = Math.Clamp(this.image.Scale * Factor, minScale, maxScale);
			double Applied = Scale / this.image.Scale;

			this.SetZoom(Scale, this.image.TranslationX * Applied, this.image.TranslationY * Applied);
		}

		/// <summary>
		/// Sets the zoom and position, keeping the image within its area.
		/// </summary>
		private void SetZoom(double Scale, double TranslationX, double TranslationY)
		{
			// At a given zoom, the image can move half of its extra width and height in each direction.
			double MaxX = this.stage.Width * (Scale - 1) / 2;
			double MaxY = this.stage.Height * (Scale - 1) / 2;

			this.image.Scale = Scale;
			this.image.TranslationX = Math.Clamp(TranslationX, -MaxX, MaxX);
			this.image.TranslationY = Math.Clamp(TranslationY, -MaxY, MaxY);
		}
	}
}
