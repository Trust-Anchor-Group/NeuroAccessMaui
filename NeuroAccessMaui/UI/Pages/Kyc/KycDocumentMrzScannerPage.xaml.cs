using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Live document-line scanner page used by the KYC chip-scan path.
	/// </summary>
	public partial class KycDocumentMrzScannerPage
	{
		private readonly ScannerOverlayDrawable overlayDrawable;
		private readonly Microsoft.Maui.Dispatching.IDispatcherTimer outlineAnimationTimer;
		private bool cameraReleased;
		private bool animationTimerReleased;
		private bool viewModelSubscriptionReleased;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycDocumentMrzScannerPage"/> class.
		/// </summary>
		public KycDocumentMrzScannerPage()
		{
			this.InitializeComponent();

			KycDocumentMrzScannerNavigationArgs? NavigationArgs = ServiceRef.NavigationService.PopLatestArgs<KycDocumentMrzScannerNavigationArgs>();
			KycDocumentMrzScannerViewModel ViewModel = new KycDocumentMrzScannerViewModel(NavigationArgs);
			this.BindingContext = ViewModel;
			this.overlayDrawable = new ScannerOverlayDrawable(ViewModel);
			this.DocumentOutlineView.Drawable = this.overlayDrawable;
			ViewModel.PropertyChanged += this.ViewModel_PropertyChanged;
			ViewModel.CameraView = this.CameraView;
			this.CameraView.SizeChanged += this.CameraView_SizeChanged;
			this.outlineAnimationTimer = this.Dispatcher.CreateTimer();
			this.outlineAnimationTimer.Interval = TimeSpan.FromMilliseconds(33);
			this.outlineAnimationTimer.Tick += this.OutlineAnimationTimer_Tick;
			this.outlineAnimationTimer.Start();
			ViewModel.UpdateCameraViewportSize(this.CameraView.Width, this.CameraView.Height);
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			if (this.BindingContext is KycDocumentMrzScannerViewModel ViewModel)
			{
				ViewModel.CompletePendingSuccessfulResult();
			}

			await this.ReleaseCameraAsync();
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			await this.ReleaseCameraAsync();
			this.ReleaseOutlineAnimationTimer();
			this.ReleaseViewModelSubscription();
			if (this.BindingContext is KycDocumentMrzScannerViewModel ViewModel)
			{
				ViewModel.CameraView = null;
				ViewModel.CompletePendingSuccessfulResult();
			}

			await base.OnDisposeAsync();
		}

		private void OutlineAnimationTimer_Tick(object? Sender, EventArgs e)
		{
			_ = Sender;
			_ = e;
			this.DocumentOutlineView.Invalidate();
		}

		private void ViewModel_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			_ = Sender;
			if (e.PropertyName == nameof(KycDocumentMrzScannerViewModel.HasDocumentOutline)
				|| e.PropertyName == nameof(KycDocumentMrzScannerViewModel.DocumentOutlineTopLeft)
				|| e.PropertyName == nameof(KycDocumentMrzScannerViewModel.DocumentOutlineTopRight)
				|| e.PropertyName == nameof(KycDocumentMrzScannerViewModel.DocumentOutlineBottomRight)
				|| e.PropertyName == nameof(KycDocumentMrzScannerViewModel.DocumentOutlineBottomLeft)
				|| e.PropertyName == nameof(KycDocumentMrzScannerViewModel.CurrentGuidanceState)
				|| e.PropertyName == nameof(KycDocumentMrzScannerViewModel.IsCaptureSucceeded))
			{
				this.DocumentOutlineView.Invalidate();
			}
		}

		private void CameraView_SizeChanged(object? Sender, EventArgs e)
		{
			_ = Sender;
			_ = e;
			if (this.BindingContext is KycDocumentMrzScannerViewModel ViewModel)
			{
				ViewModel.UpdateCameraViewportSize(this.CameraView.Width, this.CameraView.Height);
			}
		}

		private async Task ReleaseCameraAsync()
		{
			if (this.cameraReleased)
			{
				return;
			}

			this.cameraReleased = true;
			this.CameraView.SizeChanged -= this.CameraView_SizeChanged;
			await this.CameraView.ReleaseAsync();
		}

		private void ReleaseViewModelSubscription()
		{
			if (this.viewModelSubscriptionReleased)
			{
				return;
			}

			this.viewModelSubscriptionReleased = true;
			if (this.BindingContext is KycDocumentMrzScannerViewModel ViewModel)
			{
				ViewModel.PropertyChanged -= this.ViewModel_PropertyChanged;
			}
		}

		private void ReleaseOutlineAnimationTimer()
		{
			if (this.animationTimerReleased)
			{
				return;
			}

			this.animationTimerReleased = true;
			this.outlineAnimationTimer.Stop();
			this.outlineAnimationTimer.Tick -= this.OutlineAnimationTimer_Tick;
		}

		/// <summary>
		/// Draws the scanner target: a document-shaped frame that snaps to the detected document, a dimmed
		/// surrounding, a highlighted band for the machine-readable lines, and the capture confirmation.
		/// </summary>
		private sealed class ScannerOverlayDrawable : IDrawable
		{
			private const double guideAspectRatio = 1.46d;
			private const double snapRatio = 0.3d;
			private const float bandTop = 0.72f;
			private const float bandBottom = 0.94f;
			private const double successDurationMs = 420d;

			private readonly KycDocumentMrzScannerViewModel viewModel;
			private readonly Stopwatch clock = Stopwatch.StartNew();
			private bool hasRenderedQuad;
			private Point renderedTopLeft;
			private Point renderedTopRight;
			private Point renderedBottomRight;
			private Point renderedBottomLeft;
			private double? successStartedAt;

			/// <summary>
			/// Initializes a new instance of the <see cref="ScannerOverlayDrawable"/> class.
			/// </summary>
			/// <param name="ViewModel">The scanner view model providing the detected outline and state.</param>
			public ScannerOverlayDrawable(KycDocumentMrzScannerViewModel ViewModel)
			{
				this.viewModel = ViewModel;
			}

			/// <inheritdoc/>
			public void Draw(ICanvas Canvas, RectF DirtyRect)
			{
				if (DirtyRect.Width <= 0 || DirtyRect.Height <= 0)
					return;

				bool Animate = !IsReducedMotion();
				double Now = this.clock.Elapsed.TotalMilliseconds;
				this.SnapTowardTarget(DirtyRect, Animate ? snapRatio : 1d);

				Point TopLeft = this.renderedTopLeft;
				Point TopRight = this.renderedTopRight;
				Point BottomRight = this.renderedBottomRight;
				Point BottomLeft = this.renderedBottomLeft;

				bool Succeeded = this.viewModel.IsCaptureSucceeded;
				bool Detected = this.viewModel.HasDocumentOutline;
				MrzCaptureGuidanceState GuidanceState = this.viewModel.CurrentGuidanceState;
				bool IsReading = Detected && IsStableGuidanceState(GuidanceState);
				Color FrameColor = Succeeded || IsReading
					? ScannerColors.Success
					: GuidanceState is MrzCaptureGuidanceState.ReduceGlare or MrzCaptureGuidanceState.MoveCloser
						? ScannerColors.Attention
						: ScannerColors.Neutral;

				Canvas.SaveState();
				DrawScrim(Canvas, DirtyRect, TopLeft, TopRight, BottomRight, BottomLeft);

				Canvas.StrokeLineCap = LineCap.Round;
				Canvas.StrokeLineJoin = LineJoin.Round;
				Canvas.StrokeColor = FrameColor.WithAlpha(0.45f);
				Canvas.StrokeSize = 1.5f;
				Canvas.DrawPath(CreateQuadPath(TopLeft, TopRight, BottomRight, BottomLeft));

				DrawTextBand(Canvas, TopLeft, TopRight, BottomRight, BottomLeft, FrameColor, !Detected && !Succeeded);
				if (IsReading && !Succeeded && Animate)
					DrawSweep(Canvas, TopLeft, TopRight, BottomRight, BottomLeft, Now);

				Canvas.StrokeColor = FrameColor;
				Canvas.StrokeSize = 5f;
				DrawCorner(Canvas, TopLeft, TopRight, BottomLeft);
				DrawCorner(Canvas, TopRight, TopLeft, BottomRight);
				DrawCorner(Canvas, BottomRight, BottomLeft, TopRight);
				DrawCorner(Canvas, BottomLeft, BottomRight, TopLeft);

				if (Succeeded)
				{
					this.successStartedAt ??= Now;
					double Progress = Animate ? Math.Clamp((Now - this.successStartedAt.Value) / successDurationMs, 0d, 1d) : 1d;
					DrawSuccess(Canvas, TopLeft, TopRight, BottomRight, BottomLeft, Progress);
				}
				else
					this.successStartedAt = null;

				Canvas.RestoreState();
			}

			private void SnapTowardTarget(RectF Bounds, double Ratio)
			{
				Point TargetTopLeft;
				Point TargetTopRight;
				Point TargetBottomRight;
				Point TargetBottomLeft;

				if (this.viewModel.HasDocumentOutline)
				{
					TargetTopLeft = this.viewModel.DocumentOutlineTopLeft;
					TargetTopRight = this.viewModel.DocumentOutlineTopRight;
					TargetBottomRight = this.viewModel.DocumentOutlineBottomRight;
					TargetBottomLeft = this.viewModel.DocumentOutlineBottomLeft;
				}
				else
				{
					RectF Guide = ResolveGuideRect(Bounds);
					TargetTopLeft = new Point(Guide.Left, Guide.Top);
					TargetTopRight = new Point(Guide.Right, Guide.Top);
					TargetBottomRight = new Point(Guide.Right, Guide.Bottom);
					TargetBottomLeft = new Point(Guide.Left, Guide.Bottom);
				}

				if (!this.hasRenderedQuad)
				{
					Ratio = 1d;
					this.hasRenderedQuad = true;
				}

				this.renderedTopLeft = Interpolate(this.renderedTopLeft, TargetTopLeft, Ratio);
				this.renderedTopRight = Interpolate(this.renderedTopRight, TargetTopRight, Ratio);
				this.renderedBottomRight = Interpolate(this.renderedBottomRight, TargetBottomRight, Ratio);
				this.renderedBottomLeft = Interpolate(this.renderedBottomLeft, TargetBottomLeft, Ratio);
			}

			private static RectF ResolveGuideRect(RectF Bounds)
			{
				float Width = Math.Min(Bounds.Width - 48f, 560f);
				float Height = (float)(Width / guideAspectRatio);
				float MaxHeight = Bounds.Height * 0.5f;
				if (Height > MaxHeight)
				{
					Height = MaxHeight;
					Width = (float)(Height * guideAspectRatio);
				}

				float CenterX = Bounds.Center.X;
				float CenterY = Bounds.Top + Bounds.Height * 0.44f;
				return new RectF(CenterX - Width / 2f, CenterY - Height / 2f, Width, Height);
			}

			private static void DrawScrim(ICanvas Canvas, RectF Bounds, Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft)
			{
				PathF Scrim = new PathF();
				Scrim.MoveTo(Bounds.Left, Bounds.Top);
				Scrim.LineTo(Bounds.Right, Bounds.Top);
				Scrim.LineTo(Bounds.Right, Bounds.Bottom);
				Scrim.LineTo(Bounds.Left, Bounds.Bottom);
				Scrim.Close();
				Scrim.MoveTo((float)TopLeft.X, (float)TopLeft.Y);
				Scrim.LineTo((float)BottomLeft.X, (float)BottomLeft.Y);
				Scrim.LineTo((float)BottomRight.X, (float)BottomRight.Y);
				Scrim.LineTo((float)TopRight.X, (float)TopRight.Y);
				Scrim.Close();

				Canvas.FillColor = ScannerColors.Scrim;
				Canvas.FillPath(Scrim, WindingMode.EvenOdd);
			}

			private static void DrawTextBand(
				ICanvas Canvas,
				Point TopLeft,
				Point TopRight,
				Point BottomRight,
				Point BottomLeft,
				Color FrameColor,
				bool ShowLineHints)
			{
				Canvas.FillColor = FrameColor.WithAlpha(0.12f);
				Canvas.FillPath(CreateQuadPath(
					Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, 0.04f, bandTop),
					Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, 0.96f, bandTop),
					Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, 0.96f, bandBottom),
					Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, 0.04f, bandBottom)));

				if (!ShowLineHints)
					return;

				// Dashed lines hint at the "<<<" text that must sit in this band.
				Canvas.SaveState();
				Canvas.StrokeColor = ScannerColors.Neutral.WithAlpha(0.5f);
				Canvas.StrokeSize = 3f;
				Canvas.StrokeDashPattern = new float[] { 1.8f, 1.2f };
				foreach (float Row in new float[] { 0.79f, 0.87f })
				{
					Point Start = Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, 0.09f, Row);
					Point End = Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, 0.91f, Row);
					Canvas.DrawLine((float)Start.X, (float)Start.Y, (float)End.X, (float)End.Y);
				}

				Canvas.RestoreState();
			}

			private static void DrawSweep(ICanvas Canvas, Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft, double Now)
			{
				float U = (float)(0.5d + 0.44d * Math.Sin(2d * Math.PI * Now / 1400d));
				Point Top = Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, U, bandTop);
				Point Bottom = Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, U, bandBottom);

				Canvas.StrokeColor = ScannerColors.Success.WithAlpha(0.35f);
				Canvas.StrokeSize = 14f;
				Canvas.DrawLine((float)Top.X, (float)Top.Y, (float)Bottom.X, (float)Bottom.Y);
				Canvas.StrokeColor = ScannerColors.Success;
				Canvas.StrokeSize = 3.5f;
				Canvas.DrawLine((float)Top.X, (float)Top.Y, (float)Bottom.X, (float)Bottom.Y);
			}

			private static void DrawSuccess(ICanvas Canvas, Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft, double Progress)
			{
				Canvas.FillColor = ScannerColors.Success.WithAlpha((float)(0.2d * Progress));
				Canvas.FillPath(CreateQuadPath(TopLeft, TopRight, BottomRight, BottomLeft));

				Point Center = Bilinear(TopLeft, TopRight, BottomRight, BottomLeft, 0.5f, 0.5f);
				double Size = Math.Min(Distance(TopLeft, TopRight), Distance(TopLeft, BottomLeft)) * 0.2d;
				double Pop = BackOut(Math.Clamp(Progress / 0.6d, 0d, 1d));
				Canvas.FillColor = ScannerColors.Success;
				Canvas.FillCircle((float)Center.X, (float)Center.Y, (float)(Size * Pop));

				double CheckProgress = Math.Clamp((Progress - 0.35d) / 0.65d, 0d, 1d);
				if (CheckProgress <= 0d)
					return;

				Point Start = new Point(Center.X - Size * 0.45d, Center.Y + Size * 0.02d);
				Point Corner = new Point(Center.X - Size * 0.12d, Center.Y + Size * 0.34d);
				Point End = new Point(Center.X + Size * 0.48d, Center.Y - Size * 0.32d);
				double FirstLength = Distance(Start, Corner);
				double SecondLength = Distance(Corner, End);
				double Drawn = CheckProgress * (FirstLength + SecondLength);

				Canvas.StrokeColor = Colors.White;
				Canvas.StrokeSize = (float)Math.Max(3d, Size * 0.16d);
				PathF Check = new PathF();
				Check.MoveTo((float)Start.X, (float)Start.Y);
				if (Drawn <= FirstLength)
				{
					Point Tip = Interpolate(Start, Corner, Drawn / FirstLength);
					Check.LineTo((float)Tip.X, (float)Tip.Y);
				}
				else
				{
					Point Tip = Interpolate(Corner, End, (Drawn - FirstLength) / SecondLength);
					Check.LineTo((float)Corner.X, (float)Corner.Y);
					Check.LineTo((float)Tip.X, (float)Tip.Y);
				}

				Canvas.DrawPath(Check);
			}

			private static void DrawCorner(ICanvas Canvas, Point Corner, Point AlongFirstEdge, Point AlongSecondEdge)
			{
				Point First = Interpolate(Corner, AlongFirstEdge, 0.18d);
				Point Second = Interpolate(Corner, AlongSecondEdge, 0.18d);
				PathF Path = new PathF();
				Path.MoveTo((float)First.X, (float)First.Y);
				Path.LineTo((float)Corner.X, (float)Corner.Y);
				Path.LineTo((float)Second.X, (float)Second.Y);
				Canvas.DrawPath(Path);
			}

			private static PathF CreateQuadPath(Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft)
			{
				PathF Path = new PathF();
				Path.MoveTo((float)TopLeft.X, (float)TopLeft.Y);
				Path.LineTo((float)TopRight.X, (float)TopRight.Y);
				Path.LineTo((float)BottomRight.X, (float)BottomRight.Y);
				Path.LineTo((float)BottomLeft.X, (float)BottomLeft.Y);
				Path.Close();
				return Path;
			}

			private static Point Bilinear(Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft, float U, float V)
			{
				Point Top = Interpolate(TopLeft, TopRight, U);
				Point Bottom = Interpolate(BottomLeft, BottomRight, U);
				return Interpolate(Top, Bottom, V);
			}

			private static Point Interpolate(Point Start, Point End, double Ratio)
			{
				return new Point(
					Start.X + ((End.X - Start.X) * Ratio),
					Start.Y + ((End.Y - Start.Y) * Ratio));
			}

			private static double Distance(Point From, Point To)
			{
				double Dx = To.X - From.X;
				double Dy = To.Y - From.Y;
				return Math.Sqrt((Dx * Dx) + (Dy * Dy));
			}

			private static double BackOut(double X)
			{
				const double Overshoot = 1.70158d;
				double T = X - 1d;
				return 1d + ((Overshoot + 1d) * T * T * T) + (Overshoot * T * T);
			}

			private static bool IsStableGuidanceState(MrzCaptureGuidanceState GuidanceState)
			{
				return GuidanceState is MrzCaptureGuidanceState.HoldSteady
					or MrzCaptureGuidanceState.Capturing
					or MrzCaptureGuidanceState.Review;
			}

			private static bool IsReducedMotion()
			{
				try
				{
					return ServiceRef.Provider.GetService<IMotionSettings>()?.ReduceMotion == true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}
	}
}
