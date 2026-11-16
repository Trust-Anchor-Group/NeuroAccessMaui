using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Images
{
	/// <summary>
	/// Shows an image, such as a chart drawn by the server, on the whole screen. It opens fitted to the screen, and can be
	/// zoomed with a pinch or the zoom bar, moved by dragging, fitted again with a double tap or Fit, and shared.
	/// </summary>
	public class ImageViewerPopup : BasicPopup
	{
		private const double minScale = 1;
		private const double maxScale = 8;
		private const double buttonStep = 1.5;
		private const double doubleTapScale = 2.5;

		private readonly Grid stage;
		private readonly Microsoft.Maui.Controls.Image image;
		private readonly Label percentLabel;
		private readonly string? shareFile;
		private readonly string title;
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
			this.CardMaxHeightFraction = 1;
			this.PopupMargin = new Thickness(0);
			this.CardPadding = new Thickness(12, 12, 12, 20);

			this.title = string.IsNullOrEmpty(Title) ? ServiceRef.Localizer[nameof(AppResources.HistoryValueChart)] : Title;
			this.shareFile = Source is FileImageSource File && System.IO.File.Exists(File.File) ? File.File : null;

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
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
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

			this.percentLabel = new Label
			{
				FontFamily = "SpaceGroteskSemiBold",
				FontSize = 14,
				MinimumWidthRequest = 48,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalOptions = LayoutOptions.Center
			};
			this.percentLabel.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");
			this.UpdatePercent();

			Grid Layout = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				},
				RowSpacing = 12
			};

			// The viewer takes the whole screen, apart from a little room for the system bars.
			DisplayInfo Display = DeviceDisplay.Current.MainDisplayInfo;
			if (Display.Density > 0)
				Layout.HeightRequest = Display.Height / Display.Density * 0.9;

			Layout.Add(this.CreateHeader(), 0, 0);
			Layout.Add(this.stage, 0, 1);

			Label Hint = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ZoomHint)],
				FontSize = 13,
				HorizontalTextAlignment = TextAlignment.Center
			};
			Hint.SetDynamicResource(Label.TextColorProperty, "ContentSecondaryWL");
			Layout.Add(Hint, 0, 2);
			Layout.Add(this.CreateZoomBar(), 0, 3);

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

			Grid Header = new()
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Auto }
				},
				ColumnSpacing = 12
			};
			Header.Add(CreateIconButton(Geometries.CancelPath, ServiceRef.Localizer[nameof(AppResources.Close)],
				() => _ = ServiceRef.PopupService.PopAsync()), 0, 0);
			Header.Add(new VerticalStackLayout
			{
				VerticalOptions = LayoutOptions.Center,
				Children = { TitleLabel, SourceLabel }
			}, 1, 0);

			if (this.shareFile is not null)
			{
				Header.Add(CreateIconButton(Geometries.ShareIconPath, ServiceRef.Localizer[nameof(AppResources.Share)],
					() => _ = this.ShareAsync()), 2, 0);
			}

			return Header;
		}

		/// <summary>
		/// Creates the zoom bar at the bottom: zoom out, Fit, how far it is zoomed, and zoom in.
		/// </summary>
		/// <returns>The zoom bar.</returns>
		private Border CreateZoomBar()
		{
			Label FitLabel = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ZoomFit)],
				FontFamily = "HaasGroteskMedium",
				FontSize = 14,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.Center
			};
			FitLabel.SetDynamicResource(Label.TextColorProperty, "ButtonAccessPrimaryContentWL");

			Border Fit = new()
			{
				HeightRequest = 40,
				Padding = new Thickness(16, 0),
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 20 },
				Content = FitLabel
			};
			Fit.SetDynamicResource(VisualElement.BackgroundColorProperty, "ButtonAccessPrimarybgWL");
			AddTap(Fit, () => this.SetZoom(minScale, 0, 0));

			Border Bar = new()
			{
				Padding = new Thickness(4),
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 24 },
				HorizontalOptions = LayoutOptions.Center,
				Content = new HorizontalStackLayout
				{
					Spacing = 4,
					Children =
					{
						CreateIconButton(Geometries.ArrowDownPath, ServiceRef.Localizer[nameof(AppResources.ZoomOut)],
							() => this.ZoomAroundCenter(1 / buttonStep), "−"),
						Fit,
						this.percentLabel,
						CreateIconButton(Geometries.ArrowUpPath, ServiceRef.Localizer[nameof(AppResources.ZoomIn)],
							() => this.ZoomAroundCenter(buttonStep), "+")
					}
				}
			};
			Bar.SetDynamicResource(Border.StrokeProperty, "SurfaceDividerWL");
			Bar.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation1WL");

			return Bar;
		}

		/// <summary>
		/// Creates a round 40 point button with an icon, or with a sign such as "+".
		/// </summary>
		/// <param name="Icon">The icon, used when there is no sign.</param>
		/// <param name="Description">What the button does, for screen readers.</param>
		/// <param name="OnTap">What the button does.</param>
		/// <param name="Sign">A sign to show instead of the icon, or null.</param>
		/// <returns>The button.</returns>
		private static Border CreateIconButton(Geometry Icon, string Description, Action OnTap, string? Sign = null)
		{
			View Content;

			if (Sign is null)
			{
				Microsoft.Maui.Controls.Shapes.Path IconPath = new()
				{
					Data = Icon,
					Aspect = Stretch.Uniform,
					WidthRequest = 18,
					HeightRequest = 18,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				IconPath.SetDynamicResource(Shape.FillProperty, "ContentPrimaryWL");
				Content = IconPath;
			}
			else
			{
				Label SignLabel = new()
				{
					Text = Sign,
					FontFamily = "HaasGroteskMedium",
					FontSize = 20,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				SignLabel.SetDynamicResource(Label.TextColorProperty, "ContentPrimaryWL");
				Content = SignLabel;
			}

			Border Button = new()
			{
				WidthRequest = 40,
				HeightRequest = 40,
				Padding = 0,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 20 },
				VerticalOptions = LayoutOptions.Center,
				Content = Content
			};
			Button.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation2WL");
			SemanticProperties.SetDescription(Button, Description);
			AddTap(Button, OnTap);

			return Button;
		}

		/// <summary>
		/// Makes a view run an action when tapped.
		/// </summary>
		private static void AddTap(View View, Action OnTap)
		{
			TapGestureRecognizer Tap = new();
			Tap.Tapped += (Sender, Args) => OnTap();
			View.GestureRecognizers.Add(Tap);
		}

		/// <summary>
		/// Shares the image with another app.
		/// </summary>
		private async Task ShareAsync()
		{
			if (this.shareFile is null)
				return;

			try
			{
				byte[] Data = await File.ReadAllBytesAsync(this.shareFile);
				ServiceRef.PlatformSpecific.ShareImage(Data, this.title, this.title, System.IO.Path.GetFileName(this.shareFile));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
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
			this.UpdatePercent();
		}

		/// <summary>
		/// Shows how far the image is zoomed, such as "250%".
		/// </summary>
		private void UpdatePercent()
		{
			this.percentLabel.Text = (this.image.Scale * 100).ToString("0", CultureInfo.CurrentCulture) + "%";
		}
	}
}
