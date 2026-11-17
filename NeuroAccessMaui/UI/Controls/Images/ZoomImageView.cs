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
		/// The largest move between two pan reports that is taken as a real move. Android measures a pan from the first finger
		/// on the screen, so when that finger is lifted the total jumps to another finger.
		/// </summary>
		private const double maxPanStep = 80;

		/// <summary>
		/// The image.
		/// </summary>
		public static readonly BindableProperty SourceProperty =
			BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(ZoomImageView), null,
				propertyChanged: (Bindable, OldValue, NewValue) => ((ZoomImageView)Bindable).image.Source = NewValue as ImageSource);

		/// <summary>
		/// What the image shows, read by screen readers, such as a report's title.
		/// </summary>
		public static readonly BindableProperty ImageDescriptionProperty =
			BindableProperty.Create(nameof(ImageDescription), typeof(string), typeof(ZoomImageView), null,
				propertyChanged: (Bindable, OldValue, NewValue) =>
					SemanticProperties.SetDescription(((ZoomImageView)Bindable).image, NewValue as string));

		private readonly Grid layout;
		private readonly Grid stage;
		private readonly Microsoft.Maui.Controls.Image image;
		private readonly Label percentLabel;
		private readonly Label hint;
		private readonly Border zoomBar;
		private readonly Border paper;
		private bool isFullScreen;
		private Thickness overlayPadding;
		private double lastPanX;
		private double lastPanY;
		private bool isPinching;
		private bool rebasePan;
		private Point? lastPinchOrigin;

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
			this.paper = new Border
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				Content = this.image
			};
			this.paper.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.White, Color.FromRgb(0xE4, 0xE4, 0xE4));

			this.stage = new Grid
			{
				IsClippedToBounds = true,
				MinimumHeightRequest = 240,
				Children = { this.paper }
			};

			// When the area changes size, such as when the phone turns, the image is kept within it.
			this.stage.SizeChanged += (Sender, Args) =>
				this.SetZoom(this.image.Scale, this.image.TranslationX, this.image.TranslationY);

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

			this.hint = new Label
			{
				Text = ServiceRef.Localizer[nameof(AppResources.ZoomHint)],
				FontSize = 13,
				HorizontalTextAlignment = TextAlignment.Center
			};
			this.hint.SetDynamicResource(Label.TextColorProperty, "ContentSecondaryWL");

			this.zoomBar = this.CreateZoomBar();

			this.layout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto }
				},
				RowSpacing = 12
			};
			this.layout.Add(this.stage, 0, 0);
			this.layout.Add(this.hint, 0, 1);
			this.layout.Add(this.zoomBar, 0, 2);

			this.Content = this.layout;
		}

		/// <summary>
		/// If the image takes the whole view, edge to edge, with the zoom bar over its lower edge and no hint. Used in
		/// landscape, where there is little height to share.
		/// </summary>
		public bool IsFullScreen
		{
			get => this.isFullScreen;
			set
			{
				if (this.isFullScreen == value)
					return;

				this.isFullScreen = value;

				Grid.SetRowSpan(this.stage, value ? 3 : 1);
				this.hint.IsVisible = !value;
				this.layout.RowSpacing = value ? 0 : 12;
				this.stage.MinimumHeightRequest = value ? 0 : 240;
				this.paper.StrokeShape = new RoundRectangle { CornerRadius = value ? 0 : 12 };
				this.UpdateZoomBarMargin();
			}
		}

		/// <summary>
		/// Room to leave around the zoom bar when the image is shown full screen, such as for the system bars the image
		/// is drawn under.
		/// </summary>
		public Thickness OverlayPadding
		{
			get => this.overlayPadding;
			set
			{
				this.overlayPadding = value;
				this.UpdateZoomBarMargin();
			}
		}

		/// <summary>
		/// Lifts the zoom bar off the lower edge when it lies over the image.
		/// </summary>
		private void UpdateZoomBarMargin()
		{
			this.zoomBar.Margin = this.isFullScreen
				? new Thickness(this.overlayPadding.Left, 0, this.overlayPadding.Right, this.overlayPadding.Bottom + 12)
				: new Thickness(0);
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
		/// What the image shows, read by screen readers, such as a report's title.
		/// </summary>
		public string? ImageDescription
		{
			get => (string?)this.GetValue(ImageDescriptionProperty);
			set => this.SetValue(ImageDescriptionProperty, value);
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
			ButtonRole.SetIsButton(Button, true);
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
			ButtonRole.SetIsButton(FitButton, true);
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
			// While two fingers pinch, the pan gesture also reports moves; those are left out, so the image doesn't jump.
			switch (e.Status)
			{
				case GestureStatus.Started:
					this.isPinching = true;
					this.rebasePan = true;
					this.lastPinchOrigin = null;
					return;

				case GestureStatus.Completed:
				case GestureStatus.Canceled:
					this.isPinching = false;
					this.rebasePan = true;
					return;
			}

			// The point between the fingers, relative to the middle of the image's area.
			double OriginX = (e.ScaleOrigin.X - 0.5) * this.stage.Width;
			double OriginY = (e.ScaleOrigin.Y - 0.5) * this.stage.Height;
			(double MoveX, double MoveY) = GetPinchMove(this.lastPinchOrigin, OriginX, OriginY);
			this.lastPinchOrigin = new Point(OriginX, OriginY);

			// Android can report a very large or small step when a finger is lifted. Such a step is not a real pinch.
			if (e.Scale < 0.5 || e.Scale > 2)
				return;

			double Scale = Math.Clamp(this.image.Scale * e.Scale, minScale, maxScale);
			double Factor = Scale / this.image.Scale;

			// The image moves with the fingers, and zooms around the point between them, so it stays under them.
			this.SetZoom(Scale,
				OriginX - (OriginX - (this.image.TranslationX + MoveX)) * Factor,
				OriginY - (OriginY - (this.image.TranslationY + MoveY)) * Factor);
		}

		/// <summary>
		/// Gets how far the point between two pinching fingers moved since the last report, so the content can move with
		/// them. A jump, such as when a finger is lifted, doesn't count.
		/// </summary>
		/// <param name="Last">Where the point was at the last report, or null at the first.</param>
		/// <param name="X">Where the point is now, across.</param>
		/// <param name="Y">Where the point is now, down.</param>
		/// <returns>How far it moved.</returns>
		public static (double X, double Y) GetPinchMove(Point? Last, double X, double Y)
		{
			if (Last is not Point Previous)
				return (0, 0);

			double MoveX = X - Previous.X;
			double MoveY = Y - Previous.Y;

			return Math.Abs(MoveX) > maxPanStep || Math.Abs(MoveY) > maxPanStep ? (0, 0) : (MoveX, MoveY);
		}

		private void Pan_PanUpdated(object? Sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType)
			{
				case GestureStatus.Started:
					this.lastPanX = 0;
					this.lastPanY = 0;
					this.rebasePan = false;
					break;

				case GestureStatus.Running:
					// The image moves by how far the finger moved since the last report, so a pinch in between can't make
					// it jump.
					double Dx = e.TotalX - this.lastPanX;
					double Dy = e.TotalY - this.lastPanY;
					this.lastPanX = e.TotalX;
					this.lastPanY = e.TotalY;

					// The first report after a pinch, and a jump to another finger, only set where the next move starts from.
					if (this.rebasePan || Math.Abs(Dx) > maxPanStep || Math.Abs(Dy) > maxPanStep)
					{
						this.rebasePan = false;
						break;
					}

					if (!this.isPinching && this.image.Scale > minScale)
						this.SetZoom(this.image.Scale, this.image.TranslationX + Dx, this.image.TranslationY + Dy);
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
