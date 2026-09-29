using System.ComponentModel;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// App-owned camera page for capturing KYC profile photos without cropping.
	/// </summary>
	/// <remarks>
	/// Code-behind owns the camera lifetime and purely visual effects: the face guide overlay, the capture flash, and
	/// the review sheet entrance. Capture and result handling live in <see cref="KycProfilePhotoCameraViewModel"/>.
	/// </remarks>
	public partial class KycProfilePhotoCameraPage
	{
		private bool cameraReleased;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProfilePhotoCameraPage"/> class.
		/// </summary>
		public KycProfilePhotoCameraPage()
		{
			this.InitializeComponent();
			KycProfilePhotoCameraNavigationArgs? NavigationArgs = ServiceRef.NavigationService.PopLatestArgs<KycProfilePhotoCameraNavigationArgs>();
			KycProfilePhotoCameraViewModel ViewModel = new KycProfilePhotoCameraViewModel(NavigationArgs);
			this.BindingContext = ViewModel;
			ViewModel.CameraView = this.CameraView;
			ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
			this.FaceGuide.Drawable = new FaceGuideDrawable();
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.ReleaseCameraAsync();
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			await this.ReleaseCameraAsync();
			if (this.BindingContext is KycProfilePhotoCameraViewModel ViewModel)
			{
				ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				ViewModel.CameraView = null;
			}

			await base.OnDisposeAsync();
		}

		private static bool IsReducedMotion
		{
			get
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

		private async Task ReleaseCameraAsync()
		{
			if (this.cameraReleased)
			{
				return;
			}

			this.cameraReleased = true;
			await this.CameraView.ReleaseAsync();
		}

		private void OnViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (Sender is not KycProfilePhotoCameraViewModel ViewModel)
				return;

			if (E.PropertyName == nameof(KycProfilePhotoCameraViewModel.IsCapturing) && ViewModel.IsCapturing)
				this.Dispatcher.Dispatch(() => _ = this.FlashAsync());
			else if (E.PropertyName == nameof(KycProfilePhotoCameraViewModel.HasCapturedPhoto) && ViewModel.HasCapturedPhoto)
				this.Dispatcher.Dispatch(() => _ = this.RevealReviewSheetAsync());
		}

		/// <summary>
		/// Flashes the screen white like a camera shutter.
		/// </summary>
		/// <returns>A task that completes when the flash has faded.</returns>
		private async Task FlashAsync()
		{
			if (IsReducedMotion)
				return;

			try
			{
				this.Flash.CancelAnimations();
				this.Flash.Opacity = 0.85;
				await this.Flash.FadeToAsync(0, 320, Easing.CubicOut);
			}
			catch (Exception)
			{
				this.Flash.Opacity = 0;
			}
		}

		/// <summary>
		/// Slides the review sheet up from the bottom edge.
		/// </summary>
		/// <returns>A task that completes when the sheet is in place.</returns>
		private async Task RevealReviewSheetAsync()
		{
			VisualElement Sheet = this.ReviewSheet;
			Sheet.CancelAnimations();

			if (IsReducedMotion)
			{
				Sheet.TranslationY = 0;
				return;
			}

			try
			{
				Sheet.TranslationY = 220;
				await Sheet.TranslateToAsync(0, 0, 300, Easing.CubicOut);
			}
			catch (Exception)
			{
				Sheet.TranslationY = 0;
			}
		}

		/// <summary>
		/// Draws a dimmed surround with an oval cut-out that shows where to place the face.
		/// </summary>
		private sealed class FaceGuideDrawable : IDrawable
		{
			private const float maximumOvalWidth = 300f;
			private const float ovalAspect = 1.32f;

			/// <inheritdoc/>
			public void Draw(ICanvas Canvas, RectF DirtyRect)
			{
				if (DirtyRect.Width <= 0 || DirtyRect.Height <= 0)
					return;

				RectF Oval = GetOval(DirtyRect);

				PathF Scrim = new PathF();
				Scrim.AppendRectangle(DirtyRect);
				Scrim.AppendEllipse(Oval);
				Canvas.FillColor = ScannerColors.Scrim;
				Canvas.FillPath(Scrim, WindingMode.EvenOdd);

				Canvas.StrokeColor = ScannerColors.Neutral.WithAlpha(0.9f);
				Canvas.StrokeSize = 3f;
				Canvas.DrawEllipse(Oval);
			}

			/// <summary>
			/// Places the oval in the upper part of the screen, above the guidance card and shutter button.
			/// </summary>
			/// <param name="Bounds">The overlay bounds.</param>
			/// <returns>The oval bounds.</returns>
			private static RectF GetOval(RectF Bounds)
			{
				float Width = Math.Min(Bounds.Width * 0.7f, maximumOvalWidth);
				float Height = Width * ovalAspect;
				float MaximumHeight = Bounds.Height * 0.48f;
				if (Height > MaximumHeight)
				{
					Height = MaximumHeight;
					Width = Height / ovalAspect;
				}

				float CenterX = Bounds.Center.X;
				float CenterY = Bounds.Top + Bounds.Height * 0.4f;
				return new RectF(CenterX - Width / 2f, CenterY - Height / 2f, Width, Height);
			}
		}
	}
}
