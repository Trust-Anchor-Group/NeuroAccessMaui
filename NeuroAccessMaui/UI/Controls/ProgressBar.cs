using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A cross-platform, customizable progress bar implemented using SkiaSharp for consistency across devices.
	/// Supports solid color, linear, and radial gradient fills via the <see cref="BarBrush"/> property.
	/// </summary>
	public class ProgressBar : SKCanvasView
	{
		/// <summary>
		/// Identifies the <see cref="Progress"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ProgressProperty =
			BindableProperty.Create(
				nameof(Progress),
				typeof(double),
				typeof(ProgressBar),
				0.0,
				propertyChanged: (b, o, n) => ((ProgressBar)b).InvalidateSurface());

		/// <summary>
		/// Gets or sets the current progress, as a value between 0.0 and 1.0.
		/// </summary>
		public double Progress
		{
			get => (double)this.GetValue(ProgressProperty);
			set => this.SetValue(ProgressProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="BarBrush"/> bindable property.
		/// </summary>
		public static readonly BindableProperty BarBrushProperty =
			BindableProperty.Create(
				nameof(BarBrush),
				typeof(Brush),
				typeof(ProgressBar),
				default(Brush),
				propertyChanged: (b, o, n) => ((ProgressBar)b).InvalidateSurface());

		/// <summary>
		/// Gets or sets the brush used to paint the progress indicator.
		/// Supports <see cref="SolidColorBrush"/>, <see cref="LinearGradientBrush"/>, and <see cref="RadialGradientBrush"/>.
		/// </summary>
		public Brush BarBrush
		{
			get => (Brush)this.GetValue(BarBrushProperty);
			set => this.SetValue(BarBrushProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="CornerRadius"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CornerRadiusProperty =
			BindableProperty.Create(
				nameof(CornerRadius),
				typeof(float),
				typeof(ProgressBar),
				4f,
				propertyChanged: (b, o, n) => ((ProgressBar)b).InvalidateSurface());

		/// <summary>
		/// Gets or sets the corner radius for the progress bar, in device-independent pixels.
		/// </summary>
		public float CornerRadius
		{
			get => (float)this.GetValue(CornerRadiusProperty);
			set => this.SetValue(CornerRadiusProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="BarBackgroundColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty BarBackgroundColorProperty =
			BindableProperty.Create(
				nameof(BarBackgroundColor),
				typeof(Color),
				typeof(ProgressBar),
				Colors.LightGray,
				propertyChanged: (b, o, n) => ((ProgressBar)b).InvalidateSurface());

		/// <summary>
		/// Gets or sets the color used for the background of the progress bar (the "track").
		/// </summary>
		public Color BarBackgroundColor
		{
			get => (Color)this.GetValue(BarBackgroundColorProperty);
			set => this.SetValue(BarBackgroundColorProperty, value);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressBar"/> class.
		/// </summary>
		public ProgressBar()
		{
			this.HeightRequest = 10;
			this.Progress = 0;
			this.CornerRadius = 4f;
			this.IgnorePixelScaling = false;
			this.EnableTouchEvents = false;
			PaintSurface += this.OnPaintSurface;
		}

		/// <summary>
		/// Handles the SkiaSharp paint event, drawing the background and the progress indicator.
		/// </summary>
		/// <param name="sender">The canvas view.</param>
		/// <param name="e">Event arguments containing the surface to paint.</param>
		private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			SKCanvas Canvas = e.Surface.Canvas;
			Canvas.Clear();

			float Width = e.Info.Width;
			float Height = e.Info.Height;
			float Radius = Math.Min(this.CornerRadius, Height / 2);

			// Draw background
			using (SKPaint BgPaint = new()
			{ Color = this.BarBackgroundColor.ToSKColor(), IsAntialias = true })
			{
				SKRoundRect BgRect = new(new SKRect(0, 0, Width, Height), Radius, Radius);
				Canvas.DrawRoundRect(BgRect, BgPaint);
			}

			// Draw progress indicator
			float ProgressWidth = (float)Math.Max(0, Math.Min(1, this.Progress)) * Width;
			if (ProgressWidth > 0.1f && this.BarBrush is Brush Brush)
				using (SKPaint FgPaint = new()
				{ IsAntialias = true })
				{
					FgPaint.Shader = ToShader(Brush, ProgressWidth, Height);
					SKRoundRect FgRect = new(new SKRect(0, 0, ProgressWidth, Height), Radius, Radius);
					Canvas.DrawRoundRect(FgRect, FgPaint);
				}
		}

		/// <summary>
		/// Converts a .NET MAUI <see cref="Brush"/> into a SkiaSharp <see cref="SKShader"/>, supporting solid, linear, and radial gradients.
		/// </summary>
		/// <param name="brush">The .NET MAUI brush instance.</param>
		/// <param name="width">The width of the area to fill, in pixels.</param>
		/// <param name="height">The height of the area to fill, in pixels.</param>
		/// <returns>An <see cref="SKShader"/> that represents the given brush, or a solid black shader if not supported.</returns>
		private static SKShader ToShader(Brush brush, float width, float height)
		{
			if (brush is SolidColorBrush Solid)
				return SKShader.CreateColor(Solid.Color.ToSKColor());
			else if (brush is LinearGradientBrush Linear)
			{
				GradientStopCollection Stops = Linear.GradientStops;
				SKColor[] Colors = new SKColor[Stops.Count];
				float[] Positions = new float[Stops.Count];

				for (int i = 0; i < Stops.Count; i++)
				{
					Colors[i] = Stops[i].Color.ToSKColor();
					Positions[i] = Stops[i].Offset;
				}

				// Calculate start/end points (relative to area)
				SKPoint Start = new((float)(Linear.StartPoint.X * width), (float)(Linear.StartPoint.Y * height));
				SKPoint End = new((float)(Linear.EndPoint.X * width), (float)(Linear.EndPoint.Y * height));
				return SKShader.CreateLinearGradient(Start, End, Colors, Positions, SKShaderTileMode.Clamp);
			}
			else if (brush is RadialGradientBrush Radial)
			{
				GradientStopCollection Stops = Radial.GradientStops;
				SKColor[] Colors = new SKColor[Stops.Count];
				float[] Positions = new float[Stops.Count];
				for (int i = 0; i < Stops.Count; i++)
				{
					Colors[i] = Stops[i].Color.ToSKColor();
					Positions[i] = Stops[i].Offset;
				}

				// MAUI's Center property is normalized (0..1) relative to area
				SKPoint Center = new((float)(Radial.Center.X * width), (float)(Radial.Center.Y * height));
				float Radius = Math.Max(width, height) * 0.5f; // Approximate radius
				return SKShader.CreateRadialGradient(Center, Radius, Colors, Positions, SKShaderTileMode.Clamp);
			}
			// Fallback: solid black
			return SKShader.CreateColor(SKColors.Black);
		}
	}
}
