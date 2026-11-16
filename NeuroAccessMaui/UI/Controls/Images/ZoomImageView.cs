using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// An image, such as a chart drawn by the server, that fills the space it gets. It starts fitted, and can be zoomed
	/// with a pinch or the zoom bar under it, moved by dragging, and fitted again with a double tap or Fit.
	/// </summary>
	public class ZoomImageView : ContentView
	{
		private const double minScale = 1;
		private const double maxScale = 8;
		private const double buttonStep = 1.5;
		private const double doubleTapScale = 2.5;

		/// <summary>
		/// The image.
		/// </summary>
		public static readonly BindableProperty SourceProperty =
			BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(ZoomImageView), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((ZoomImageView)Bindable).image.Source = NewValue as ImageSource);

		private readonly Grid stage;
		private readonly Microsoft.Maui.Controls.Image image;
		private readonly Label percentLabel;
		private double startTranslationX;
		private double startTranslationY;

		/// <summary>
		/// Creates the view.
		/// </summary>
		public ZoomImageView()
		{
			this.image = new Microsoft.Maui.Controls.Image
			{
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
				MinimumHeightRequest = 240,
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

			Label Hint = new()
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ZoomHint)],
				FontSize = 13,
				HorizontalTextAlignment = TextAlignment.Center
			};
			Hint.SetDynamicResource(Label.TextColorProperty, "ContentSecondaryWL");

			Grid Layout = new()
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				},
				RowSpacing = 12
			};
			Layout.Add(this.stage, 0, 0);
			Layout.Add(Hint, 0, 1);
			Layout.Add(this.CreateZoomBar(), 0, 2);

			this.Content = Layout;
		}

		/// <summary>
		/// The image.
		/// </summary>
		public ImageSource? Source
		{
			get => (ImageSource?)this.GetValue(SourceProperty);
			set => this.SetValue(SourceProperty, value);
		}

		/// <summary>
		/// Fits the whole image in view again.
		/// </summary>
		public void Fit()
		{
			this.SetZoom(minScale, 0, 0);
		}

		/// <summary>
		/// Creates a round 48 point button with an icon, or with a sign such as "+".
		/// </summary>
		/// <param name="Icon">The icon, used when there is no sign.</param>
		/// <param name="Description">What the button does, for screen readers.</param>
		/// <param name="OnTap">What the button does.</param>
		/// <param name="Sign">A sign to show instead of the icon, or null.</param>
		/// <returns>The button.</returns>
		public static Border CreateRoundButton(Geometry? Icon, string Description, Action OnTap, string? Sign = null)
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
				WidthRequest = 48,
				HeightRequest = 48,
				Padding = 0,
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 24 },
				VerticalOptions = LayoutOptions.Center,
				Content = Content
			};
			Button.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation2WL");
			SemanticProperties.SetDescription(Button, Description);
			AddTap(Button, OnTap);

			return Button;
		}

		/// <summary>
		/// Creates the zoom bar: zoom out, Fit, how far it is zoomed, and zoom in.
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

			Border FitButton = new()
			{
				HeightRequest = 48,
				MinimumWidthRequest = 48,
				Padding = new Thickness(16, 0),
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 24 },
				Content = FitLabel
			};
			FitButton.SetDynamicResource(VisualElement.BackgroundColorProperty, "ButtonAccessPrimarybgWL");
			AddTap(FitButton, this.Fit);

			Border Bar = new()
			{
				Padding = new Thickness(4),
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 28 },
				HorizontalOptions = LayoutOptions.Center,
				Content = new HorizontalStackLayout
				{
					Spacing = 4,
					Children =
					{
						CreateRoundButton(null, ServiceRef.Localizer[nameof(AppResources.ZoomOut)],
							() => this.ZoomAroundCenter(1 / buttonStep), "−"),
						FitButton,
						this.percentLabel,
						CreateRoundButton(null, ServiceRef.Localizer[nameof(AppResources.ZoomIn)],
							() => this.ZoomAroundCenter(buttonStep), "+")
					}
				}
			};
			Bar.SetDynamicResource(Border.StrokeProperty, "SurfaceDividerWL");
			Bar.SetDynamicResource(VisualElement.BackgroundColorProperty, "SurfaceElevation1WL");

			return Bar;
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

		private void Pinch_PinchUpdated(object? Sender, PinchGestureUpdatedEventArgs e)
		{
			if (e.Status != GestureStatus.Running)
				return;

			double Scale = Math.Clamp(this.image.Scale * e.Scale, minScale, maxScale);
			double Factor = Scale / this.image.Scale;

			// Zoom around the point between the fingers, so it stays under them.
			double OriginX = (e.ScaleOrigin.X - 0.5) * this.stage.Width;
			double OriginY = (e.ScaleOrigin.Y - 0.5) * this.stage.Height;

			this.SetZoom(Scale,
				OriginX - (OriginX - this.image.TranslationX) * Factor,
				OriginY - (OriginY - this.image.TranslationY) * Factor);
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
				this.Fit();
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
