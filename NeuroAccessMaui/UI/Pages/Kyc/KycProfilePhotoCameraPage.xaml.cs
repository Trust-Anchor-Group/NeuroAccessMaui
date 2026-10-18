using System.ComponentModel;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.Photography;
using NeuroAccessMaui.UI.Pages.Utility.Images;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// App-owned camera page for capturing KYC photos without cropping, with a framing guide and a review step.
	/// </summary>
	/// <remarks>
	/// Code-behind owns the camera lifetime and purely visual effects: the framing guide, the shutter flash, and the review
	/// sheet entrance. With reduced motion the flash is skipped and the review sheet appears at once; the haptic tap and the
	/// review screen still confirm the capture. Capture and result handling live in
	/// <see cref="KycProfilePhotoCameraViewModel"/>.
	/// </remarks>
	public partial class KycProfilePhotoCameraPage
	{
		private bool cameraReleased;
		private readonly IKycPhotoCamera camera;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProfilePhotoCameraPage"/> class.
		/// </summary>
		public KycProfilePhotoCameraPage()
		{
			this.InitializeComponent();
			KycProfilePhotoCameraNavigationArgs? NavigationArgs = ServiceRef.NavigationService.PopLatestArgs<KycProfilePhotoCameraNavigationArgs>();
#if UI_AUTOMATION
			// Automation builds capture a fixed photo instead of using the device camera.
			this.CameraView.IsVisible = false;
			this.camera = new UiAutomation.FixtureKycPhotoCamera();
#else
			this.camera = new NativeKycPhotoCamera(this.CameraView,
				(NavigationArgs?.CaptureProfile ?? KycCaptureProfile.Selfie) == KycCaptureProfile.Selfie);
#endif
			KycProfilePhotoCameraViewModel ViewModel = new KycProfilePhotoCameraViewModel(NavigationArgs, this.camera);
			this.BindingContext = ViewModel;
			ViewModel.PropertyChanged += this.OnViewModelPropertyChanged;
			this.CaptureGuide.Drawable = new CaptureGuideDrawable(ViewModel.CaptureProfile, ViewModel.GuideFrameAspectRatio);
		}

		/// <summary>
		/// Gets the view model created by the constructor.
		/// </summary>
		/// <remarks>
		/// Read from the binding context instead of a field: the view model disposes itself through the page lifecycle, so
		/// the page does not own it as a disposable field.
		/// </remarks>
		private KycProfilePhotoCameraViewModel ViewModel => (KycProfilePhotoCameraViewModel)this.BindingContext;

		/// <inheritdoc/>
		/// <remarks>
		/// The camera is released only when the page is disposed, not when it disappears: a released camera cannot start
		/// again, so the preview could not restart when the page reappears. The view model stops the preview and the torch
		/// when the page disappears.
		/// </remarks>
		public override async Task OnDisposeAsync()
		{
			this.ViewModel.PropertyChanged -= this.OnViewModelPropertyChanged;

			// The view model turns the torch off first, which needs the camera before it is released.
			await base.OnDisposeAsync();
			await this.ReleaseCameraAsync();
		}

		private async Task ReleaseCameraAsync()
		{
			if (this.cameraReleased)
			{
				return;
			}

			this.cameraReleased = true;
			await this.camera.ReleaseAsync();
		}

		private void OnViewModelPropertyChanged(object? Sender, PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(KycProfilePhotoCameraViewModel.IsCapturing) && this.ViewModel.IsCapturing)
				this.Dispatcher.Dispatch(() => _ = this.FlashAsync());
			else if (E.PropertyName == nameof(KycProfilePhotoCameraViewModel.HasCapturedPhoto) && this.ViewModel.HasCapturedPhoto)
				this.Dispatcher.Dispatch(() => _ = this.RevealReviewSheetAsync());
		}

		/// <summary>
		/// Flashes the screen white like a camera shutter.
		/// </summary>
		/// <returns>A task that completes when the flash has faded.</returns>
		private async Task FlashAsync()
		{
			if (MotionPreferences.ReduceMotion)
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

			if (MotionPreferences.ReduceMotion)
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
	}
}
