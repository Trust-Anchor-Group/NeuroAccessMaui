using System.Diagnostics;
using NeuroAccessMaui.Animations;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// A thin line that runs along the top edge of a card while something loads. A short bar slides across it and
	/// repeats. The line is clipped to the card's rounded corners. When reduced motion is enabled, the bar stands still
	/// across the whole edge.
	/// </summary>
	public class IndeterminateProgressLine : SKCanvasView
	{
		private const double frameIntervalMs = 16;
		private const double cycleMs = 1300;
		private const float barFraction = 0.36f;
		private const float stillOpacity = 0.35f;

		private static readonly SKColor fallbackColor = new(0x08, 0x5E, 0x50);

		private readonly Stopwatch clock = Stopwatch.StartNew();
		private IDispatcherTimer? frameTimer;
		private IMotionSettings? motionSettings;
		private bool isLoaded;

		/// <summary>
		/// Identifies the <see cref="IsRunning"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
			nameof(IsRunning),
			typeof(bool),
			typeof(IndeterminateProgressLine),
			false,
			propertyChanged: (Bindable, OldValue, NewValue) => ((IndeterminateProgressLine)Bindable).OnIsRunningChanged());

		/// <summary>
		/// Identifies the <see cref="Color"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ColorProperty = BindableProperty.Create(
			nameof(Color),
			typeof(Color),
			typeof(IndeterminateProgressLine),
			Color.FromRgb(fallbackColor.Red, fallbackColor.Green, fallbackColor.Blue),
			propertyChanged: (Bindable, OldValue, NewValue) => ((IndeterminateProgressLine)Bindable).InvalidateSurface());

		/// <summary>
		/// Identifies the <see cref="CornerRadius"/> bindable property.
		/// </summary>
		public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
			nameof(CornerRadius),
			typeof(double),
			typeof(IndeterminateProgressLine),
			16.0,
			propertyChanged: (Bindable, OldValue, NewValue) => ((IndeterminateProgressLine)Bindable).InvalidateSurface());

		/// <summary>
		/// Initializes a new instance of the <see cref="IndeterminateProgressLine"/> class.
		/// </summary>
		public IndeterminateProgressLine()
		{
			this.IgnorePixelScaling = false;
			this.EnableTouchEvents = false;
			this.InputTransparent = true;
			this.HeightRequest = 3;
			this.VerticalOptions = LayoutOptions.Start;
			this.SetValue(AutomationProperties.IsInAccessibleTreeProperty, false);
			this.PaintSurface += this.OnPaintSurface;
			this.Loaded += this.OnLoaded;
			this.Unloaded += this.OnUnloaded;
		}

		/// <summary>
		/// If the line shows that something is loading. When false, nothing is drawn.
		/// </summary>
		public bool IsRunning
		{
			get => (bool)this.GetValue(IsRunningProperty);
			set => this.SetValue(IsRunningProperty, value);
		}

		/// <summary>
		/// The color of the sliding bar.
		/// </summary>
		public Color Color
		{
			get => (Color)this.GetValue(ColorProperty);
			set => this.SetValue(ColorProperty, value);
		}

		/// <summary>
		/// The corner radius of the card the line runs along. The bar is clipped to it at the ends.
		/// </summary>
		public double CornerRadius
		{
			get => (double)this.GetValue(CornerRadiusProperty);
			set => this.SetValue(CornerRadiusProperty, value);
		}

		private void OnIsRunningChanged()
		{
			this.InvalidateSurface();
			this.UpdateTimer();
		}

		private void OnLoaded(object? Sender, EventArgs E)
		{
			this.isLoaded = true;
			this.UnsubscribeFromMotionSettings();
			this.motionSettings = MotionPreferences.Settings;
			if (this.motionSettings is not null)
				this.motionSettings.MotionSettingsChanged += this.OnMotionSettingsChanged;

			this.UpdateTimer();
		}

		private void OnUnloaded(object? Sender, EventArgs E)
		{
			this.isLoaded = false;
			this.UnsubscribeFromMotionSettings();
			this.frameTimer?.Stop();
		}

		private void UnsubscribeFromMotionSettings()
		{
			if (this.motionSettings is not null)
				this.motionSettings.MotionSettingsChanged -= this.OnMotionSettingsChanged;

			this.motionSettings = null;
		}

		private void OnMotionSettingsChanged(object? Sender, EventArgs E)
		{
			this.Dispatcher?.Dispatch(() =>
			{
				this.InvalidateSurface();
				this.UpdateTimer();
			});
		}

		private void UpdateTimer()
		{
			if (!this.isLoaded || !this.IsRunning || MotionPreferences.ReduceMotion)
			{
				this.frameTimer?.Stop();
				return;
			}

			if (this.frameTimer is null)
			{
				IDispatcher? FrameDispatcher = this.Dispatcher;
				if (FrameDispatcher is null)
					return;

				this.frameTimer = FrameDispatcher.CreateTimer();
				this.frameTimer.Interval = TimeSpan.FromMilliseconds(frameIntervalMs);
				this.frameTimer.IsRepeating = true;
				this.frameTimer.Tick += this.OnFrameTimerTick;
			}

			if (!this.frameTimer.IsRunning)
				this.frameTimer.Start();
		}

		private void OnFrameTimerTick(object? Sender, EventArgs E)
		{
			this.InvalidateSurface();
		}

		private void OnPaintSurface(object? Sender, SKPaintSurfaceEventArgs E)
		{
			SKCanvas Canvas = E.Surface.Canvas;
			Canvas.Clear(SKColors.Transparent);

			double ViewWidth = this.Width;
			double ViewHeight = this.Height;
			if (!this.IsRunning || ViewWidth <= 0 || ViewHeight <= 0 || E.Info.Width <= 0 || E.Info.Height <= 0)
				return;

			float Width = (float)ViewWidth;
			float Height = (float)ViewHeight;
			float Radius = (float)Math.Clamp(this.CornerRadius, 0, ViewWidth / 2);
			SKColor BarColor = this.Color is null ? fallbackColor : this.Color.ToSKColor();

			int SaveCount = Canvas.Save();
			Canvas.Scale((float)(E.Info.Width / ViewWidth));

			// The line sits on the card's top edge, so it is clipped to the card's top corners.
			using (SKRoundRect Clip = new(new SKRect(0, 0, Width, Height + 2 * Radius), Radius, Radius))
				Canvas.ClipRoundRect(Clip, SKClipOperation.Intersect, true);

			using SKPaint Paint = new()
			{
				IsAntialias = true,
				Style = SKPaintStyle.Fill
			};

			if (MotionPreferences.ReduceMotion)
			{
				Paint.Color = BarColor.WithAlpha((byte)(BarColor.Alpha * stillOpacity));
				Canvas.DrawRect(new SKRect(0, 0, Width, Height), Paint);
			}
			else
			{
				double Phase = this.clock.Elapsed.TotalMilliseconds % cycleMs / cycleMs;
				float Eased = (float)EaseInOutCubic(Phase);
				float BarWidth = Width * barFraction;
				float Left = BarWidth * (-1.1f + 4.0f * Eased);

				Paint.Color = BarColor;
				Canvas.DrawRoundRect(new SKRect(Left, 0, Left + BarWidth, Height), Height / 2, Height / 2, Paint);
			}

			Canvas.RestoreToCount(SaveCount);
		}

		private static double EaseInOutCubic(double Value)
		{
			return Value < 0.5
				? 4 * Value * Value * Value
				: 1 - Math.Pow(-2 * Value + 2, 3) / 2;
		}
	}
}
