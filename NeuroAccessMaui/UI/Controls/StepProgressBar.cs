using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Draws a segmented progress bar where each segment represents one step of a flow.
	/// </summary>
	/// <remarks>
	/// <see cref="Value"/> is the number of filled segments and may be fractional. The fill eases toward a new value
	/// using the MAUI animation ticker and snaps when reduced motion is enabled. When many segments would become very
	/// narrow, the gaps shrink so the bar still reads as a single track.
	/// </remarks>
	public class StepProgressBar : GraphicsView, IDrawable
	{
		private const string fillAnimationName = "StepProgressBarFill";
		private const uint fillAnimationLength = 420;
		private const float maximumGap = 4f;
		private const float minimumSegmentWidth = 10f;

		private static readonly Color fallbackTrack = Color.FromArgb("#DFE1E3");
		private static readonly Color fallbackFill = Color.FromArgb("#085E50");

		private double displayedValue;

		/// <summary>
		/// Identifies the <see cref="SegmentCount"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SegmentCountProperty = BindableProperty.Create(
			nameof(SegmentCount),
			typeof(int),
			typeof(StepProgressBar),
			0,
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Identifies the <see cref="Value"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ValueProperty = BindableProperty.Create(
			nameof(Value),
			typeof(double),
			typeof(StepProgressBar),
			0.0,
			propertyChanged: (Bindable, OldValue, NewValue) => ((StepProgressBar)Bindable).OnValueChanged((double)NewValue));

		/// <summary>
		/// Identifies the <see cref="TrackColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
			nameof(TrackColor),
			typeof(Color),
			typeof(StepProgressBar),
			fallbackTrack,
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Identifies the <see cref="FillColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
			nameof(FillColor),
			typeof(Color),
			typeof(StepProgressBar),
			fallbackFill,
			propertyChanged: OnAppearanceChanged);

		/// <summary>
		/// Initializes a new instance of the <see cref="StepProgressBar"/> class.
		/// </summary>
		public StepProgressBar()
		{
			this.Drawable = this;
			this.InputTransparent = true;
			this.Unloaded += this.OnUnloaded;
		}

		/// <summary>
		/// Gets or sets the number of segments.
		/// </summary>
		public int SegmentCount
		{
			get => (int)this.GetValue(SegmentCountProperty);
			set => this.SetValue(SegmentCountProperty, value);
		}

		/// <summary>
		/// Gets or sets the number of filled segments. Fractional values fill part of a segment.
		/// </summary>
		public double Value
		{
			get => (double)this.GetValue(ValueProperty);
			set => this.SetValue(ValueProperty, value);
		}

		/// <summary>
		/// Gets or sets the color of unfilled segments.
		/// </summary>
		public Color TrackColor
		{
			get => (Color)this.GetValue(TrackColorProperty);
			set => this.SetValue(TrackColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the color of filled segments.
		/// </summary>
		public Color FillColor
		{
			get => (Color)this.GetValue(FillColorProperty);
			set => this.SetValue(FillColorProperty, value);
		}

		private static bool IsReducedMotion
		{
			get
			{
				try
				{
					return ServiceRef.Provider?.GetService<IMotionSettings>()?.ReduceMotion == true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		/// <inheritdoc/>
		public void Draw(ICanvas Canvas, RectF DirtyRect)
		{
			DrawSegments(Canvas, DirtyRect, this.SegmentCount, this.displayedValue, this.TrackColor ?? fallbackTrack, this.FillColor ?? fallbackFill);
		}

		/// <summary>
		/// Draws the segments into the given bounds.
		/// </summary>
		/// <param name="Canvas">The canvas to draw on.</param>
		/// <param name="Bounds">The drawing bounds.</param>
		/// <param name="Count">The number of segments.</param>
		/// <param name="FilledValue">The number of filled segments, possibly fractional.</param>
		/// <param name="Track">The unfilled segment color.</param>
		/// <param name="Fill">The filled segment color.</param>
		public static void DrawSegments(ICanvas Canvas, RectF Bounds, int Count, double FilledValue, Color Track, Color Fill)
		{
			if (Count <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
				return;

			float Gap = Count > 1
				? Math.Clamp((Bounds.Width - Count * minimumSegmentWidth) / (Count - 1), 0f, maximumGap)
				: 0f;
			float SegmentWidth = (Bounds.Width - Gap * (Count - 1)) / Count;
			float Radius = Bounds.Height / 2f;

			for (int i = 0; i < Count; i++)
			{
				float X = Bounds.Left + i * (SegmentWidth + Gap);
				Canvas.FillColor = Track;
				Canvas.FillRoundedRectangle(X, Bounds.Top, SegmentWidth, Bounds.Height, Radius);

				float Fraction = (float)Math.Clamp(FilledValue - i, 0d, 1d);
				if (Fraction <= 0f)
					continue;

				Canvas.SaveState();
				Canvas.ClipRectangle(X, Bounds.Top, SegmentWidth * Fraction, Bounds.Height);
				Canvas.FillColor = Fill;
				Canvas.FillRoundedRectangle(X, Bounds.Top, SegmentWidth, Bounds.Height, Radius);
				Canvas.RestoreState();
			}
		}

		private static void OnAppearanceChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((StepProgressBar)Bindable).Invalidate();
		}

		private void OnValueChanged(double NewValue)
		{
			this.AbortAnimation(fillAnimationName);

			if (this.Handler is null || this.Width <= 0 || IsReducedMotion)
			{
				this.displayedValue = NewValue;
				this.Invalidate();
				return;
			}

			try
			{
				this.Animate(
					fillAnimationName,
					Current =>
					{
						this.displayedValue = Current;
						this.Invalidate();
					},
					this.displayedValue,
					NewValue,
					16,
					fillAnimationLength,
					Easing.CubicOut);
			}
			catch (Exception)
			{
				this.displayedValue = NewValue;
				this.Invalidate();
			}
		}

		private void OnUnloaded(object? Sender, EventArgs E)
		{
			this.AbortAnimation(fillAnimationName);
			this.displayedValue = this.Value;
		}
	}
}
