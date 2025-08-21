using System;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A cross-platform, highly customizable progress bar rendered with SkiaSharp for pixel-perfect appearance.
	/// Supports solid, linear, and radial gradient brushes for both track (background) and bar (progress) visuals.
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
				propertyChanged: (Bindable, OldValue, NewValue) => ((ProgressBar)Bindable).InvalidateSurface());

		/// <summary>
		/// Gets or sets the progress, as a value between 0.0 and 1.0.
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
				propertyChanged: (Bindable, OldValue, NewValue) => ((ProgressBar)Bindable).InvalidateSurface());

		/// <summary>
		/// Gets or sets the brush used to paint the progress portion of the bar.
		/// Supports solid color, linear, and radial gradient brushes.
		/// </summary>
		[TypeConverter(typeof(BrushTypeConverter))]
		public Brush BarBrush
		{
			get => (Brush)this.GetValue(BarBrushProperty);
			set => this.SetValue(BarBrushProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="TrackBrush"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TrackBrushProperty =
			BindableProperty.Create(
				nameof(TrackBrush),
				typeof(Brush),
				typeof(ProgressBar),
				default(Brush),
				propertyChanged: (Bindable, OldValue, NewValue) => ((ProgressBar)Bindable).InvalidateSurface());

		/// <summary>
		/// Gets or sets the brush used to paint the background (track) of the bar.
		/// Supports solid color, linear, and radial gradient brushes.
		/// </summary>
		[TypeConverter(typeof(BrushTypeConverter))]
		public Brush TrackBrush
		{
			get => (Brush)this.GetValue(TrackBrushProperty);
			set => this.SetValue(TrackBrushProperty, value);
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
				propertyChanged: (Bindable, OldValue, NewValue) => ((ProgressBar)Bindable).InvalidateSurface());

		/// <summary>
		/// Gets or sets the corner radius for the progress bar in device-independent pixels.
		/// </summary>
		public float CornerRadius
		{
			get => (float)this.GetValue(CornerRadiusProperty);
			set => this.SetValue(CornerRadiusProperty, value);
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
			this.PaintSurface += this.OnPaintSurface;
		}

		/// <summary>
		/// Handles the SkiaSharp paint event, drawing the track (background) and the progress indicator.
		/// </summary>
		/// <param name="Sender">The canvas view.</param>
		/// <param name="E">Event arguments containing the surface to paint.</param>
		private void OnPaintSurface(object? Sender, SKPaintSurfaceEventArgs E)
		{
			SKCanvas Canvas = E.Surface.Canvas;
			Canvas.Clear();

			float Width = E.Info.Width;
			float Height = E.Info.Height;
			float Radius = this.CornerRadius;

			// Draw the background (track)
			using (SKPaint TrackPaint = new()
			{ IsAntialias = true })
			{
				TrackPaint.Shader = ProgressBar.ToShader(this.TrackBrush, Width, Height)
					?? SKShader.CreateColor(SKColors.LightGray);

				SKRoundRect TrackRect = new(new SKRect(0, 0, Width, Height), Radius, Radius);
				Canvas.DrawRoundRect(TrackRect, TrackPaint);
			}

			// Draw the progress bar (fill)
			float ProgressWidth = (float)Math.Max(0, Math.Min(1, this.Progress)) * Width;
			if (ProgressWidth > 0.1f)
			{
				using SKPaint BarPaint = new() { IsAntialias = true };
				BarPaint.Shader = ProgressBar.ToShader(this.BarBrush, ProgressWidth, Height)
					?? SKShader.CreateColor(SKColors.Blue);

				SKRoundRect BarRect = new(new SKRect(Height/3, Height/3, ProgressWidth - Height/3, 2*Height/3), Radius/2, Radius/2);
				Canvas.DrawRoundRect(BarRect, BarPaint);
			}
		}

		/// <summary>
		/// Converts a .NET MAUI <see cref="Brush"/> to a SkiaSharp <see cref="SKShader"/> for rendering.
		/// </summary>
		/// <param name="Brush">The brush instance (solid, linear, or radial gradient).</param>
		/// <param name="Width">The width of the area to fill, in pixels.</param>
		/// <param name="Height">The height of the area to fill, in pixels.</param>
		/// <returns>An <see cref="SKShader"/> representing the brush, or null if not supported.</returns>
		private static SKShader? ToShader(Brush Brush, float Width, float Height)
		{
			if (Brush is null)
				return null;

			if (Brush is SolidColorBrush SolidBrush)
				return SKShader.CreateColor(SolidBrush.Color.ToSKColor());

			if (Brush is LinearGradientBrush LinearBrush)
			{
				GradientStopCollection Stops = LinearBrush.GradientStops;
				SKColor[] Colors = new SKColor[Stops.Count];
				float[] Positions = new float[Stops.Count];

				for (int i = 0; i < Stops.Count; i++)
				{
					Colors[i] = Stops[i].Color.ToSKColor();
					Positions[i] = Stops[i].Offset;
				}

				SKPoint StartPoint = new(
					(float)(LinearBrush.StartPoint.X * Width),
					(float)(LinearBrush.StartPoint.Y * Height));

				SKPoint EndPoint = new(
					(float)(LinearBrush.EndPoint.X * Width),
					(float)(LinearBrush.EndPoint.Y * Height));

				return SKShader.CreateLinearGradient(StartPoint, EndPoint, Colors, Positions, SKShaderTileMode.Clamp);
			}

			if (Brush is RadialGradientBrush RadialBrush)
			{
				GradientStopCollection Stops = RadialBrush.GradientStops;
				SKColor[] Colors = new SKColor[Stops.Count];
				float[] Positions = new float[Stops.Count];

				for (int i = 0; i < Stops.Count; i++)
				{
					Colors[i] = Stops[i].Color.ToSKColor();
					Positions[i] = Stops[i].Offset;
				}

				SKPoint CenterPoint = new(
					(float)(RadialBrush.Center.X * Width),
					(float)(RadialBrush.Center.Y * Height));

				float Radius = Math.Max(Width, Height) * 0.5f;
				return SKShader.CreateRadialGradient(CenterPoint, Radius, Colors, Positions, SKShaderTileMode.Clamp);
			}

			// Fallback: solid black (should never hit in normal usage)
			return SKShader.CreateColor(SKColors.Black);
		}
	}
}
