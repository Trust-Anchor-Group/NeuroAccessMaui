using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Live document-line scanner page used by the KYC chip-scan path.
	/// </summary>
	public partial class KycDocumentMrzScannerPage
	{
		private readonly DocumentOutlineDrawable documentOutlineDrawable;
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
			this.documentOutlineDrawable = new DocumentOutlineDrawable(ViewModel);
			this.DocumentOutlineView.Drawable = this.documentOutlineDrawable;
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
			this.documentOutlineDrawable.AdvanceAnimation();
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
				|| e.PropertyName == nameof(KycDocumentMrzScannerViewModel.CurrentGuidanceState))
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

		private sealed class DocumentOutlineDrawable : IDrawable
		{
			private const double RenderedOutlineInterpolationRatio = 0.80d;

			private readonly KycDocumentMrzScannerViewModel viewModel;
			private double animationPhase;
			private bool hasRenderedOutline;
			private Point renderedTopLeft;
			private Point renderedTopRight;
			private Point renderedBottomRight;
			private Point renderedBottomLeft;

			public DocumentOutlineDrawable(KycDocumentMrzScannerViewModel ViewModel)
			{
				this.viewModel = ViewModel;
			}

			public void AdvanceAnimation()
			{
				this.animationPhase += 0.018d;
				if (this.animationPhase > 1d)
				{
					this.animationPhase -= 1d;
				}
			}

			public void Draw(ICanvas Canvas, RectF DirtyRect)
			{
				if (!this.viewModel.HasDocumentOutline)
				{
					this.hasRenderedOutline = false;
					this.DrawSearchingState(Canvas, DirtyRect);
					return;
				}

				Point TargetTopLeft = this.viewModel.DocumentOutlineTopLeft;
				Point TargetTopRight = this.viewModel.DocumentOutlineTopRight;
				Point TargetBottomRight = this.viewModel.DocumentOutlineBottomRight;
				Point TargetBottomLeft = this.viewModel.DocumentOutlineBottomLeft;
				if (!this.hasRenderedOutline)
				{
					this.renderedTopLeft = TargetTopLeft;
					this.renderedTopRight = TargetTopRight;
					this.renderedBottomRight = TargetBottomRight;
					this.renderedBottomLeft = TargetBottomLeft;
					this.hasRenderedOutline = true;
				}
				else
				{
					this.renderedTopLeft = Interpolate(this.renderedTopLeft, TargetTopLeft, RenderedOutlineInterpolationRatio);
					this.renderedTopRight = Interpolate(this.renderedTopRight, TargetTopRight, RenderedOutlineInterpolationRatio);
					this.renderedBottomRight = Interpolate(this.renderedBottomRight, TargetBottomRight, RenderedOutlineInterpolationRatio);
					this.renderedBottomLeft = Interpolate(this.renderedBottomLeft, TargetBottomLeft, RenderedOutlineInterpolationRatio);
				}

				Point TopLeft = this.renderedTopLeft;
				Point TopRight = this.renderedTopRight;
				Point BottomRight = this.renderedBottomRight;
				Point BottomLeft = this.renderedBottomLeft;
				Color AccentColor = ResolveAccentColor(this.viewModel.CurrentGuidanceState);
				bool IsStable = IsStableGuidanceState(this.viewModel.CurrentGuidanceState);
				float PulseOpacity = (float)(0.74d + (Math.Sin(this.animationPhase * Math.PI * 2d) * 0.16d));
				Color FillColor = AccentColor.WithAlpha(IsStable ? 0.22f : 0.15f);

				Canvas.SaveState();
				this.DrawOutsideDim(Canvas, DirtyRect, TopLeft, TopRight, BottomRight, BottomLeft);
				PathF DocumentPath = CreatePath(TopLeft, TopRight, BottomRight, BottomLeft);
				Canvas.FillColor = FillColor;
				Canvas.FillPath(DocumentPath);
				Canvas.StrokeColor = AccentColor.WithAlpha(0.42f * PulseOpacity);
				Canvas.StrokeSize = 1.25f;
				Canvas.DrawPath(DocumentPath);

				Canvas.StrokeColor = AccentColor.WithAlpha(PulseOpacity);
				Canvas.StrokeSize = IsStable ? 2.25f : 1.75f;
				DrawCorner(Canvas, TopLeft, TopRight, BottomLeft, 0.18d);
				DrawCorner(Canvas, TopRight, TopLeft, BottomRight, 0.18d);
				DrawCorner(Canvas, BottomRight, BottomLeft, TopRight, 0.18d);
				DrawCorner(Canvas, BottomLeft, BottomRight, TopLeft, 0.18d);
				Canvas.RestoreState();
			}

			private void DrawSearchingState(ICanvas Canvas, RectF DirtyRect)
			{
				float SweepY = DirtyRect.Top + (DirtyRect.Height * (float)(0.24d + (0.52d * this.animationPhase)));
				float Left = DirtyRect.Left + (DirtyRect.Width * 0.16f);
				float Right = DirtyRect.Right - (DirtyRect.Width * 0.16f);
				float Center = DirtyRect.Left + (DirtyRect.Width * 0.5f);
				float HalfWidth = (Right - Left) * 0.32f;

				Canvas.SaveState();
				Canvas.StrokeColor = Color.FromArgb("#FFF7ED").WithAlpha(0.12f);
				Canvas.StrokeSize = 1f;
				Canvas.DrawLine(Left, SweepY, Right, SweepY);
				Canvas.StrokeColor = Color.FromArgb("#FFF7ED").WithAlpha(0.42f);
				Canvas.StrokeSize = 2f;
				Canvas.DrawLine(Center - HalfWidth, SweepY, Center + HalfWidth, SweepY);
				Canvas.RestoreState();
			}

			private void DrawOutsideDim(ICanvas Canvas, RectF DirtyRect, Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft)
			{
				RectF Bounds = ResolveBounds(TopLeft, TopRight, BottomRight, BottomLeft);
				Bounds = Bounds.Inflate(12f, 12f);

				Canvas.FillColor = Color.FromArgb("#000000").WithAlpha(0.12f);
				if (Bounds.Top > DirtyRect.Top)
				{
					Canvas.FillRectangle(DirtyRect.Left, DirtyRect.Top, DirtyRect.Width, Bounds.Top - DirtyRect.Top);
				}

				if (Bounds.Bottom < DirtyRect.Bottom)
				{
					Canvas.FillRectangle(DirtyRect.Left, Bounds.Bottom, DirtyRect.Width, DirtyRect.Bottom - Bounds.Bottom);
				}

				if (Bounds.Left > DirtyRect.Left)
				{
					Canvas.FillRectangle(DirtyRect.Left, Bounds.Top, Bounds.Left - DirtyRect.Left, Bounds.Height);
				}

				if (Bounds.Right < DirtyRect.Right)
				{
					Canvas.FillRectangle(Bounds.Right, Bounds.Top, DirtyRect.Right - Bounds.Right, Bounds.Height);
				}
			}

			private static RectF ResolveBounds(Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft)
			{
				float Left = (float)Math.Min(Math.Min(TopLeft.X, TopRight.X), Math.Min(BottomRight.X, BottomLeft.X));
				float Top = (float)Math.Min(Math.Min(TopLeft.Y, TopRight.Y), Math.Min(BottomRight.Y, BottomLeft.Y));
				float Right = (float)Math.Max(Math.Max(TopLeft.X, TopRight.X), Math.Max(BottomRight.X, BottomLeft.X));
				float Bottom = (float)Math.Max(Math.Max(TopLeft.Y, TopRight.Y), Math.Max(BottomRight.Y, BottomLeft.Y));
				return new RectF(Left, Top, Math.Max(1f, Right - Left), Math.Max(1f, Bottom - Top));
			}

			private static PathF CreatePath(Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft)
			{
				PathF Path = new PathF();
				Path.MoveTo((float)TopLeft.X, (float)TopLeft.Y);
				Path.LineTo((float)TopRight.X, (float)TopRight.Y);
				Path.LineTo((float)BottomRight.X, (float)BottomRight.Y);
				Path.LineTo((float)BottomLeft.X, (float)BottomLeft.Y);
				Path.Close();
				return Path;
			}

			private static void DrawCorner(ICanvas Canvas, Point Corner, Point AlongFirstEdge, Point AlongSecondEdge, double Ratio)
			{
				Point First = Interpolate(Corner, AlongFirstEdge, Ratio);
				Point Second = Interpolate(Corner, AlongSecondEdge, Ratio);
				Canvas.DrawLine((float)Corner.X, (float)Corner.Y, (float)First.X, (float)First.Y);
				Canvas.DrawLine((float)Corner.X, (float)Corner.Y, (float)Second.X, (float)Second.Y);
			}

			private static Point Interpolate(Point Start, Point End, double Ratio)
			{
				return new Point(
					Start.X + ((End.X - Start.X) * Ratio),
					Start.Y + ((End.Y - Start.Y) * Ratio));
			}

			private static Color ResolveAccentColor(MrzCaptureGuidanceState GuidanceState)
			{
				return GuidanceState switch
				{
					MrzCaptureGuidanceState.HoldSteady => Color.FromArgb("#7CFFD1"),
					MrzCaptureGuidanceState.Capturing => Color.FromArgb("#7CFFD1"),
					MrzCaptureGuidanceState.Review => Color.FromArgb("#7CFFD1"),
					MrzCaptureGuidanceState.ReduceGlare => Color.FromArgb("#FFD36E"),
					_ => Color.FromArgb("#FFF7ED")
				};
			}

			private static bool IsStableGuidanceState(MrzCaptureGuidanceState GuidanceState)
			{
				return GuidanceState is MrzCaptureGuidanceState.HoldSteady
					or MrzCaptureGuidanceState.Capturing
					or MrzCaptureGuidanceState.Review;
			}
		}
	}
}
