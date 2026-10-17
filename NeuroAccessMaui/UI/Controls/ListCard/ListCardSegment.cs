using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Draws one row's part of a list card: the card's fill, its outline down the sides, the rounded top when the row is
	/// first, the rounded bottom when it is last, and an inset divider above every row but the first. It is placed behind
	/// the row's content, so the rows of a list read as one card.
	/// </summary>
	public class ListCardSegment : SKCanvasView
	{
		private static readonly SKColor fallbackFill = new(0xFC, 0xFC, 0xFC);
		private static readonly SKColor fallbackStroke = new(0xDF, 0xE1, 0xE3);

		/// <summary>
		/// Identifies the <see cref="Position"/> bindable property.
		/// </summary>
		public static readonly BindableProperty PositionProperty = BindableProperty.Create(
			nameof(Position),
			typeof(ListCardPosition),
			typeof(ListCardSegment),
			ListCardPosition.Only,
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Identifies the <see cref="FillColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
			nameof(FillColor),
			typeof(Color),
			typeof(ListCardSegment),
			Color.FromRgb(fallbackFill.Red, fallbackFill.Green, fallbackFill.Blue),
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Identifies the <see cref="StrokeColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
			nameof(StrokeColor),
			typeof(Color),
			typeof(ListCardSegment),
			Color.FromRgb(fallbackStroke.Red, fallbackStroke.Green, fallbackStroke.Blue),
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Identifies the <see cref="CornerRadius"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
			nameof(CornerRadius),
			typeof(double),
			typeof(ListCardSegment),
			16.0,
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Identifies the <see cref="DividerInset"/> bindable property.
		/// </summary>
		public static readonly BindableProperty DividerInsetProperty = BindableProperty.Create(
			nameof(DividerInset),
			typeof(double),
			typeof(ListCardSegment),
			74.0,
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Initializes a new instance of the <see cref="ListCardSegment"/> class.
		/// </summary>
		public ListCardSegment()
		{
			this.IgnorePixelScaling = false;
			this.EnableTouchEvents = false;
			this.InputTransparent = true;
			this.SetValue(AutomationProperties.IsInAccessibleTreeProperty, false);
			this.PaintSurface += this.OnPaintSurface;
		}

		/// <summary>
		/// Where the row sits in the list.
		/// </summary>
		public ListCardPosition Position
		{
			get => (ListCardPosition)this.GetValue(PositionProperty);
			set => this.SetValue(PositionProperty, value);
		}

		/// <summary>
		/// The card's fill.
		/// </summary>
		public Color FillColor
		{
			get => (Color)this.GetValue(FillColorProperty);
			set => this.SetValue(FillColorProperty, value);
		}

		/// <summary>
		/// The color of the card's outline and of the dividers between rows.
		/// </summary>
		public Color StrokeColor
		{
			get => (Color)this.GetValue(StrokeColorProperty);
			set => this.SetValue(StrokeColorProperty, value);
		}

		/// <summary>
		/// The radius of the card's rounded corners.
		/// </summary>
		public double CornerRadius
		{
			get => (double)this.GetValue(CornerRadiusProperty);
			set => this.SetValue(CornerRadiusProperty, value);
		}

		/// <summary>
		/// How far from the card's left edge the divider above the row starts, so it lines up with the row's text.
		/// </summary>
		public double DividerInset
		{
			get => (double)this.GetValue(DividerInsetProperty);
			set => this.SetValue(DividerInsetProperty, value);
		}

		private static void OnAppearanceChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((ListCardSegment)Bindable).InvalidateSurface();
		}

		private void OnPaintSurface(object? Sender, SKPaintSurfaceEventArgs E)
		{
			SKCanvas Canvas = E.Surface.Canvas;
			Canvas.Clear(SKColors.Transparent);

			double ViewWidth = this.Width;
			double ViewHeight = this.Height;
			if (ViewWidth <= 0 || ViewHeight <= 0 || E.Info.Width <= 0 || E.Info.Height <= 0)
				return;

			float Width = (float)ViewWidth;
			float Height = (float)ViewHeight;
			float Radius = (float)Math.Clamp(this.CornerRadius, 0, Math.Min(ViewWidth, ViewHeight * 2) / 2);
			const float StrokeWidth = 1;
			const float HalfStroke = StrokeWidth / 2;

			ListCardPosition Position = this.Position;
			bool IsTop = Position == ListCardPosition.Only || Position == ListCardPosition.First;
			bool IsBottom = Position == ListCardPosition.Only || Position == ListCardPosition.Last;

			// Where the card continues above or below the row, its shape is extended past the row and clipped, so only the
			// sides of the outline are drawn there.
			float Top = IsTop ? HalfStroke : -Radius - StrokeWidth;
			float Bottom = IsBottom ? Height - HalfStroke : Height + Radius + StrokeWidth;
			SKPoint TopCorner = IsTop ? new SKPoint(Radius, Radius) : SKPoint.Empty;
			SKPoint BottomCorner = IsBottom ? new SKPoint(Radius, Radius) : SKPoint.Empty;

			using SKRoundRect Shape = new();
			Shape.SetRectRadii(new SKRect(HalfStroke, Top, Width - HalfStroke, Bottom), [TopCorner, TopCorner, BottomCorner, BottomCorner]);

			using SKPaint Fill = new()
			{
				IsAntialias = true,
				Style = SKPaintStyle.Fill,
				Color = ToSkColor(this.FillColor, fallbackFill)
			};

			using SKPaint Stroke = new()
			{
				IsAntialias = true,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = StrokeWidth,
				Color = ToSkColor(this.StrokeColor, fallbackStroke)
			};

			int SaveCount = Canvas.Save();
			Canvas.Scale((float)(E.Info.Width / ViewWidth));
			Canvas.ClipRect(new SKRect(0, 0, Width, Height));

			Canvas.DrawRoundRect(Shape, Fill);
			Canvas.DrawRoundRect(Shape, Stroke);

			if (!IsTop)
			{
				float Inset = (float)Math.Clamp(this.DividerInset, 0, ViewWidth);
				Canvas.DrawLine(Inset, HalfStroke, Width - HalfStroke, HalfStroke, Stroke);
			}

			Canvas.RestoreToCount(SaveCount);
		}

		private static SKColor ToSkColor(Color? Value, SKColor Fallback)
		{
			return Value is null ? Fallback : Value.ToSKColor();
		}
	}
}
