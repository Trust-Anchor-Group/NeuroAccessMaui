using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Renders an animated, theme-aware hero illustration for the NFC document chip scanning flow.
	/// </summary>
	/// <remarks>
	/// The control is purely presentational. It crossfades between <see cref="NfcScanVisualState"/> scenes,
	/// eases toward the bound <see cref="Progress"/>, and runs its frame timer only while something moves.
	/// When reduced motion is enabled, it renders static frames without a timer.
	/// </remarks>
	public class NfcScanVisual : SKCanvasView
	{
		private const double frameIntervalMs = 16;
		private const double crossfadeInMs = 320;
		private const double crossfadeOutMs = 220;
		private const double progressSmoothingMs = 150;
		private const double successProgressSmoothingMs = 90;
		private const double pulseIntervalMs = 350;
		private const double settledSceneTimeMs = 1_000_000;
		private const int segmentCount = 4;

		private static readonly SKColor fallbackAccent = new SKColor(0x08, 0x5E, 0x50);
		private static readonly SKColor fallbackOnAccent = new SKColor(0xF5, 0xF6, 0xF7);
		private static readonly SKColor fallbackContent = new SKColor(0x18, 0x1F, 0x25);
		private static readonly SKColor fallbackTrack = new SKColor(0xDF, 0xE1, 0xE3);
		private static readonly SKColor fallbackSurface = new SKColor(0xFC, 0xFC, 0xFC);
		private static readonly SKColor fallbackWarning = new SKColor(0xA8, 0x47, 0x00);

		private readonly Stopwatch clock = Stopwatch.StartNew();
		private readonly double[] segmentFlashStartedAt = new double[segmentCount];
		private readonly double[] segmentFlashAges = new double[segmentCount];
		private IDispatcherTimer? frameTimer;
		private NfcScanVisualState? previousState;
		private double previousSceneTimeAtChange;
		private double stateStartedAt;
		private double sceneTimeOffset;
		private bool isCrossfading;
		private double displayedProgress;
		private double lastFrameAt;
		private double lastPulseAt = double.NegativeInfinity;
		private bool isLoaded;

		/// <summary>
		/// Identifies the <see cref="State"/> bindable property.
		/// </summary>
		public static readonly BindableProperty StateProperty = BindableProperty.Create(
			nameof(State),
			typeof(NfcScanVisualState),
			typeof(NfcScanVisual),
			NfcScanVisualState.Intro,
			propertyChanged: (Bindable, OldValue, NewValue) =>
				((NfcScanVisual)Bindable).OnStateChanged((NfcScanVisualState)OldValue, (NfcScanVisualState)NewValue));

		/// <summary>
		/// Identifies the <see cref="Progress"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
			nameof(Progress),
			typeof(double),
			typeof(NfcScanVisual),
			0.0,
			propertyChanged: (Bindable, OldValue, NewValue) =>
				((NfcScanVisual)Bindable).OnProgressChanged((double)OldValue, (double)NewValue),
			coerceValue: (Bindable, Value) => Math.Clamp((double)Value, 0, 1));

		/// <summary>
		/// Identifies the <see cref="IsPassport"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IsPassportProperty = BindableProperty.Create(
			nameof(IsPassport),
			typeof(bool),
			typeof(NfcScanVisual),
			true,
			propertyChanged: OnAppearancePropertyChanged);

		/// <summary>
		/// Identifies the <see cref="AntennaPlacement"/> bindable property.
		/// </summary>
		public static readonly BindableProperty AntennaPlacementProperty = BindableProperty.Create(
			nameof(AntennaPlacement),
			typeof(NfcAntennaPlacement),
			typeof(NfcScanVisual),
			NfcAntennaPlacement.BackCenter,
			propertyChanged: OnAppearancePropertyChanged);

		/// <summary>
		/// Identifies the <see cref="SuccessRevealDelay"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SuccessRevealDelayProperty = BindableProperty.Create(
			nameof(SuccessRevealDelay),
			typeof(double),
			typeof(NfcScanVisual),
			0.0);

		/// <summary>
		/// Identifies the <see cref="IsAnimationActive"/> bindable property.
		/// </summary>
		public static readonly BindableProperty IsAnimationActiveProperty = BindableProperty.Create(
			nameof(IsAnimationActive),
			typeof(bool),
			typeof(NfcScanVisual),
			true,
			propertyChanged: (Bindable, OldValue, NewValue) => ((NfcScanVisual)Bindable).OnIsAnimationActiveChanged((bool)NewValue));

		/// <summary>
		/// Identifies the <see cref="AccentColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty AccentColorProperty = CreateColorProperty(nameof(AccentColor), fallbackAccent);

		/// <summary>
		/// Identifies the <see cref="OnAccentColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty OnAccentColorProperty = CreateColorProperty(nameof(OnAccentColor), fallbackOnAccent);

		/// <summary>
		/// Identifies the <see cref="ContentColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty ContentColorProperty = CreateColorProperty(nameof(ContentColor), fallbackContent);

		/// <summary>
		/// Identifies the <see cref="TrackColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty TrackColorProperty = CreateColorProperty(nameof(TrackColor), fallbackTrack);

		/// <summary>
		/// Identifies the <see cref="SurfaceColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty SurfaceColorProperty = CreateColorProperty(nameof(SurfaceColor), fallbackSurface);

		/// <summary>
		/// Identifies the <see cref="WarningColor"/> bindable property.
		/// </summary>
		public static readonly BindableProperty WarningColorProperty = CreateColorProperty(nameof(WarningColor), fallbackWarning);

		/// <summary>
		/// Initializes a new instance of the <see cref="NfcScanVisual"/> class.
		/// </summary>
		public NfcScanVisual()
		{
			Array.Fill(this.segmentFlashStartedAt, double.NegativeInfinity);
			this.IgnorePixelScaling = false;
			this.EnableTouchEvents = false;
			this.SetValue(AutomationProperties.IsInAccessibleTreeProperty, false);
			this.PaintSurface += this.OnPaintSurface;
			this.Loaded += this.OnLoaded;
			this.Unloaded += this.OnUnloaded;
		}

		/// <summary>
		/// Gets or sets the scene to display.
		/// </summary>
		public NfcScanVisualState State
		{
			get => (NfcScanVisualState)this.GetValue(StateProperty);
			set => this.SetValue(StateProperty, value);
		}

		/// <summary>
		/// Gets or sets the chip read progress, from 0 to 1. The ring eases toward this value; decreases reset it immediately.
		/// </summary>
		public double Progress
		{
			get => (double)this.GetValue(ProgressProperty);
			set => this.SetValue(ProgressProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether placement guidance shows a passport rather than an ID card.
		/// </summary>
		public bool IsPassport
		{
			get => (bool)this.GetValue(IsPassportProperty);
			set => this.SetValue(IsPassportProperty, value);
		}

		/// <summary>
		/// Gets or sets where the phone's NFC antenna is located in placement guidance.
		/// </summary>
		public NfcAntennaPlacement AntennaPlacement
		{
			get => (NfcAntennaPlacement)this.GetValue(AntennaPlacementProperty);
			set => this.SetValue(AntennaPlacementProperty, value);
		}

		/// <summary>
		/// Gets or sets how long, in milliseconds, the full ring is held before success is revealed.
		/// </summary>
		/// <remarks>
		/// Useful when a system NFC sheet covers the page while it dismisses after a successful read.
		/// </remarks>
		public double SuccessRevealDelay
		{
			get => (double)this.GetValue(SuccessRevealDelayProperty);
			set => this.SetValue(SuccessRevealDelayProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether animation frames may run, for example while the hosting page is visible.
		/// </summary>
		public bool IsAnimationActive
		{
			get => (bool)this.GetValue(IsAnimationActiveProperty);
			set => this.SetValue(IsAnimationActiveProperty, value);
		}

		/// <summary>
		/// Gets or sets the accent color used for documents, rings, and the success disk.
		/// </summary>
		public Color AccentColor
		{
			get => (Color)this.GetValue(AccentColorProperty);
			set => this.SetValue(AccentColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the color drawn on top of the accent color, such as the check mark.
		/// </summary>
		public Color OnAccentColor
		{
			get => (Color)this.GetValue(OnAccentColorProperty);
			set => this.SetValue(OnAccentColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the primary content color used for the phone outline.
		/// </summary>
		public Color ContentColor
		{
			get => (Color)this.GetValue(ContentColorProperty);
			set => this.SetValue(ContentColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the neutral color used for empty ring segments.
		/// </summary>
		public Color TrackColor
		{
			get => (Color)this.GetValue(TrackColorProperty);
			set => this.SetValue(TrackColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the elevated surface color used to fill documents and the phone.
		/// </summary>
		public Color SurfaceColor
		{
			get => (Color)this.GetValue(SurfaceColorProperty);
			set => this.SetValue(SurfaceColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the warning color used for failed attempts.
		/// </summary>
		public Color WarningColor
		{
			get => (Color)this.GetValue(WarningColorProperty);
			set => this.SetValue(WarningColorProperty, value);
		}

		private double Now => this.clock.Elapsed.TotalMilliseconds;

		private static bool IsReducedMotion
		{
			get
			{
				try
				{
					IServiceProvider? Provider = ServiceRef.Provider;
					return Provider?.GetService<IMotionSettings>()?.ReduceMotion == true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		private static BindableProperty CreateColorProperty(string Name, SKColor Fallback)
		{
			return BindableProperty.Create(
				Name,
				typeof(Color),
				typeof(NfcScanVisual),
				Color.FromRgba(Fallback.Red, Fallback.Green, Fallback.Blue, Fallback.Alpha),
				propertyChanged: OnAppearancePropertyChanged);
		}

		private static void OnAppearancePropertyChanged(BindableObject Bindable, object OldValue, object NewValue)
		{
			((NfcScanVisual)Bindable).InvalidateSurface();
		}

		private void OnStateChanged(NfcScanVisualState OldState, NfcScanVisualState NewState)
		{
			double CurrentTime = this.Now;
			this.previousState = OldState;
			this.previousSceneTimeAtChange = this.GetSceneTime(CurrentTime);
			this.stateStartedAt = CurrentTime;
			this.sceneTimeOffset = ResolveSceneTimeOffset(OldState, NewState);

			// Reading flows straight into its result, so the ring stays in place instead of crossfading with itself.
			this.isCrossfading = OldState != NfcScanVisualState.Reading ||
				NewState is not (NfcScanVisualState.Success or NfcScanVisualState.Failure);

			this.RequestFrames();
		}

		private static double ResolveSceneTimeOffset(NfcScanVisualState OldState, NfcScanVisualState NewState)
		{
			return NewState switch
			{
				// Celebrate only a readout that completed while the user watched, not a previously saved one.
				NfcScanVisualState.Success when OldState != NfcScanVisualState.Reading => settledSceneTimeMs,
				NfcScanVisualState.Failure when OldState is not (NfcScanVisualState.Reading or
					NfcScanVisualState.Searching or NfcScanVisualState.Preparing) => settledSceneTimeMs,
				_ => 0
			};
		}

		private void OnProgressChanged(double OldValue, double NewValue)
		{
			if (NewValue < this.displayedProgress - 0.0001)
			{
				this.displayedProgress = NewValue;
				this.lastPulseAt = double.NegativeInfinity;
				Array.Fill(this.segmentFlashStartedAt, double.NegativeInfinity);
			}
			else if (NewValue > OldValue)
			{
				double CurrentTime = this.Now;
				if (CurrentTime - this.lastPulseAt >= pulseIntervalMs)
					this.lastPulseAt = CurrentTime;
			}

			this.RequestFrames();
		}

		private void OnIsAnimationActiveChanged(bool IsActive)
		{
			if (IsActive)
				this.RequestFrames();
			else
				this.frameTimer?.Stop();
		}

		private void OnLoaded(object? Sender, EventArgs E)
		{
			this.isLoaded = true;
			this.RequestFrames();
		}

		private void OnUnloaded(object? Sender, EventArgs E)
		{
			this.isLoaded = false;
			this.frameTimer?.Stop();
		}

		private void RequestFrames()
		{
			this.InvalidateSurface();
			if (!this.isLoaded || !this.IsAnimationActive || IsReducedMotion || !this.NeedsFrames(this.Now))
				return;

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
			{
				this.lastFrameAt = this.Now;
				this.frameTimer.Start();
			}
		}

		private void OnFrameTimerTick(object? Sender, EventArgs E)
		{
			this.InvalidateSurface();
			if (!this.isLoaded || !this.IsAnimationActive || !this.NeedsFrames(this.Now))
				this.frameTimer?.Stop();
		}

		private bool NeedsFrames(double CurrentTime)
		{
			if (this.isCrossfading && CurrentTime - this.stateStartedAt < crossfadeInMs)
				return true;

			if (Math.Abs(this.Progress - this.displayedProgress) > 0.0005)
				return true;

			double SceneTime = this.GetSceneTime(CurrentTime);
			return this.State switch
			{
				NfcScanVisualState.Intro or
				NfcScanVisualState.Preparing or
				NfcScanVisualState.Searching or
				NfcScanVisualState.Reading => true,
				NfcScanVisualState.Success => SceneTime < this.SuccessRevealDelay + NfcScanVisualPainter.SuccessDurationMs,
				NfcScanVisualState.Failure => SceneTime < NfcScanVisualPainter.FailureDurationMs,
				_ => false
			};
		}

		private double GetSceneTime(double CurrentTime) => CurrentTime - this.stateStartedAt + this.sceneTimeOffset;

		private void AdvanceProgress(double CurrentTime, bool Animate)
		{
			double Elapsed = Math.Clamp(CurrentTime - this.lastFrameAt, 0, 100);
			this.lastFrameAt = CurrentTime;

			double Target = this.Progress;
			double Previous = this.displayedProgress;
			if (!Animate)
				this.displayedProgress = Target;
			else
			{
				double Smoothing = this.State == NfcScanVisualState.Success ? successProgressSmoothingMs : progressSmoothingMs;
				this.displayedProgress += (Target - this.displayedProgress) * (1 - Math.Exp(-Elapsed / Smoothing));
				if (Math.Abs(Target - this.displayedProgress) < 0.0005)
					this.displayedProgress = Target;
			}

			int CompletedBefore = CountCompletedSegments(Previous);
			int CompletedAfter = CountCompletedSegments(this.displayedProgress);
			for (int i = CompletedBefore; i < CompletedAfter; i++)
				this.segmentFlashStartedAt[i] = CurrentTime;
		}

		private static int CountCompletedSegments(double Value)
		{
			return Math.Clamp((int)Math.Floor(Value * segmentCount + 0.0001), 0, segmentCount);
		}

		private void OnPaintSurface(object? Sender, SKPaintSurfaceEventArgs E)
		{
			SKCanvas Canvas = E.Surface.Canvas;
			Canvas.Clear(SKColors.Transparent);

			double ViewWidth = this.Width;
			double ViewHeight = this.Height;
			if (ViewWidth <= 0 || ViewHeight <= 0 || E.Info.Width <= 0 || E.Info.Height <= 0)
				return;

			double CurrentTime = this.Now;
			bool Animate = !IsReducedMotion;
			this.AdvanceProgress(CurrentTime, Animate);

			float PixelScale = (float)(E.Info.Width / ViewWidth);
			float DesignScale = (float)(Math.Min(ViewWidth, ViewHeight) / NfcScanVisualPainter.DesignSize);
			NfcScanVisualPainter.ScenePalette Palette = this.CreatePalette();

			int SaveCount = Canvas.Save();
			Canvas.Scale(PixelScale);
			Canvas.Translate((float)(ViewWidth / 2), (float)(ViewHeight / 2));
			Canvas.Scale(DesignScale);

			double Age = CurrentTime - this.stateStartedAt;
			bool Crossfade = this.isCrossfading && Animate;
			if (Crossfade && this.previousState.HasValue && Age < crossfadeOutMs)
			{
				float OutOpacity = 1 - (float)(Age / crossfadeOutMs);
				NfcScanVisualPainter.SceneFrame PreviousFrame = this.CreateFrame(this.previousSceneTimeAtChange + Age, Animate, CurrentTime);
				DrawScene(Canvas, this.previousState.Value, PreviousFrame, Palette, OutOpacity);
			}

			float InOpacity = 1;
			if (Crossfade)
			{
				float Remaining = 1 - (float)Math.Clamp(Age / crossfadeInMs, 0, 1);
				InOpacity = 1 - Remaining * Remaining;
			}

			NfcScanVisualPainter.SceneFrame Frame = this.CreateFrame(this.GetSceneTime(CurrentTime), Animate, CurrentTime);
			DrawScene(Canvas, this.State, Frame, Palette, InOpacity);

			Canvas.RestoreToCount(SaveCount);
		}

		private static void DrawScene(
			SKCanvas Canvas,
			NfcScanVisualState State,
			NfcScanVisualPainter.SceneFrame Frame,
			NfcScanVisualPainter.ScenePalette Palette,
			float Opacity)
		{
			if (Opacity <= 0.001f)
				return;

			if (Opacity >= 0.999f)
			{
				NfcScanVisualPainter.Draw(Canvas, State, Frame, Palette);
				return;
			}

			using SKPaint Layer = new SKPaint { Color = SKColors.Black.WithAlpha((byte)(Opacity * 255)) };
			Canvas.SaveLayer(Layer);
			NfcScanVisualPainter.Draw(Canvas, State, Frame, Palette);
			Canvas.Restore();
		}

		private NfcScanVisualPainter.SceneFrame CreateFrame(double SceneTime, bool Animate, double CurrentTime)
		{
			for (int i = 0; i < segmentCount; i++)
				this.segmentFlashAges[i] = CurrentTime - this.segmentFlashStartedAt[i];

			return new NfcScanVisualPainter.SceneFrame(
				SceneTime,
				(float)this.displayedProgress,
				this.IsPassport,
				this.AntennaPlacement,
				Animate,
				CurrentTime - this.lastPulseAt,
				this.SuccessRevealDelay,
				this.segmentFlashAges);
		}

		private NfcScanVisualPainter.ScenePalette CreatePalette()
		{
			return new NfcScanVisualPainter.ScenePalette(
				ToSkColor(this.AccentColor, fallbackAccent),
				ToSkColor(this.OnAccentColor, fallbackOnAccent),
				ToSkColor(this.ContentColor, fallbackContent),
				ToSkColor(this.TrackColor, fallbackTrack),
				ToSkColor(this.SurfaceColor, fallbackSurface),
				ToSkColor(this.WarningColor, fallbackWarning));
		}

		private static SKColor ToSkColor(Color? Value, SKColor Fallback)
		{
			return Value is null ? Fallback : Value.ToSKColor();
		}
	}
}
