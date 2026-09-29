using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.Camera;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Captures an unmodified KYC profile photo through the app camera.
	/// </summary>
	public partial class KycProfilePhotoCameraViewModel : BaseViewModel, IDisposable
	{
		private readonly KycProfilePhotoCameraNavigationArgs? navigationArgs;
		private CancellationTokenSource? previewCancellationTokenSource;
		private byte[]? capturedPhotoBytes;
		private bool isDisposed;
		private bool resultReturned;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProfilePhotoCameraViewModel"/> class.
		/// </summary>
		/// <param name="NavigationArgs">The navigation arguments.</param>
		public KycProfilePhotoCameraViewModel(KycProfilePhotoCameraNavigationArgs? NavigationArgs)
		{
			this.navigationArgs = NavigationArgs;
		}

		/// <summary>
		/// Gets or sets the camera preview used by the page.
		/// </summary>
		public CameraView? CameraView { get; set; }

		/// <summary>
		/// Gets the localized back action text, used as the back button description.
		/// </summary>
		public string CancelText => ServiceRef.Localizer[nameof(AppResources.Back)];

		/// <summary>
		/// Gets the localized capture action text, used as the shutter button description.
		/// </summary>
		public string CaptureText => ServiceRef.Localizer[nameof(AppResources.TakePhoto)];

		/// <summary>
		/// Gets the localized retake action text.
		/// </summary>
		public string RetakeText => ServiceRef.Localizer[nameof(AppResources.KycImageRetakeButton)];

		/// <summary>
		/// Gets the localized accept action text.
		/// </summary>
		public string UsePhotoText => ServiceRef.Localizer[nameof(AppResources.KycSelfieUsePhotoButton)];

		/// <summary>
		/// Gets the localized header title for the current step: taking the photo or checking it.
		/// </summary>
		public string TitleText => this.HasCapturedPhoto
			? ServiceRef.Localizer[nameof(AppResources.KycSelfieReviewTitle)]
			: ServiceRef.Localizer[nameof(AppResources.KycSelfieTitle)];

		/// <summary>
		/// Gets the localized camera guidance text.
		/// </summary>
		public string GuidanceText => ServiceRef.Localizer[nameof(AppResources.KycProfilePhotoCameraGuidance)];

		/// <summary>
		/// Gets a short localized tip for a good photo.
		/// </summary>
		public string TipText => ServiceRef.Localizer[nameof(AppResources.KycSelfieTip)];

		/// <summary>
		/// Gets the localized hint shown while the user checks the captured photo.
		/// </summary>
		public string ReviewHintText => ServiceRef.Localizer[nameof(AppResources.KycSelfieReviewHint)];

		/// <summary>
		/// Gets a value indicating whether a photo has been captured.
		/// </summary>
		public bool HasCapturedPhoto => this.capturedPhotoBytes is not null;

		/// <summary>
		/// Gets a value indicating whether the live camera preview should be shown.
		/// </summary>
		public bool ShowCameraPreview => !this.HasCapturedPhoto;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasCapturedPhoto))]
		[NotifyPropertyChangedFor(nameof(ShowCameraPreview))]
		[NotifyPropertyChangedFor(nameof(TitleText))]
		private ImageSource? capturedPhotoSource;

		[ObservableProperty]
		private bool isCapturing;

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			if (this.CameraView is null)
			{
				return;
			}

			bool CameraPermitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();
			if (!CameraPermitted)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer["KycDocumentScanCameraPermissionTitle"],
					ServiceRef.Localizer["KycDocumentScanCameraPermissionDescription"],
					ServiceRef.Localizer["Ok"]);
				this.ReturnResult(null);
				await ServiceRef.NavigationService.GoBackAsync();
				return;
			}

			this.DisposePreviewCancellation();
			this.previewCancellationTokenSource = new CancellationTokenSource();
			this.CameraView.Options = new CameraOptions
			{
				PreferRearCamera = false,
				FrameDeliveryInterval = TimeSpan.FromMilliseconds(250),
				PreviewScaling = CameraPreviewScaling.Fill,
				TargetFps = 15,
				TargetResolution = new Size(1280d, 720d),
				JpegQuality = 96
			};
			await this.CameraView.StartPreviewAsync(this.previewCancellationTokenSource.Token);
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			if (this.CameraView is not null)
			{
				await this.CameraView.StopPreviewAsync();
			}

			this.DisposePreviewCancellation();
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			this.Dispose();
			await base.OnDisposeAsync();
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases the resources used by the <see cref="KycProfilePhotoCameraViewModel"/>.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
			{
				return;
			}

			if (disposing)
			{
				this.CompleteWithoutResult();
				this.DisposePreviewCancellation();
			}

			this.isDisposed = true;
		}

		/// <summary>
		/// Completes the camera operation without a photo if no result has been returned.
		/// </summary>
		public void CompleteWithoutResult()
		{
			this.ReturnResult(null);
		}

		[RelayCommand]
		private async Task Cancel()
		{
			this.ReturnResult(null);
			await ServiceRef.NavigationService.GoBackAsync();
		}

		[RelayCommand]
		private async Task Capture()
		{
			if (this.CameraView is null || this.IsCapturing)
			{
				return;
			}

			try
			{
				this.IsCapturing = true;
				byte[]? PhotoBytes = await this.CameraView.CapturePhotoAsync(CancellationToken.None);
				if (PhotoBytes is null || PhotoBytes.Length == 0)
				{
					throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
				}

				this.capturedPhotoBytes = PhotoBytes;
				this.CapturedPhotoSource = ImageSource.FromStream(() => new MemoryStream(PhotoBytes));
				PerformHaptic();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.FailedToLoadPhoto)]);
			}
			finally
			{
				this.IsCapturing = false;
			}
		}

		[RelayCommand]
		private void Retake()
		{
			this.capturedPhotoBytes = null;
			this.CapturedPhotoSource = null;
		}

		[RelayCommand]
		private async Task UsePhoto()
		{
			this.ReturnResult(this.capturedPhotoBytes);
			await ServiceRef.NavigationService.GoBackAsync();
		}

		private void ReturnResult(byte[]? PhotoBytes)
		{
			if (this.resultReturned)
			{
				return;
			}

			this.resultReturned = true;
			this.navigationArgs?.CompletionSource?.TrySetResult(PhotoBytes);
		}

		private static void PerformHaptic()
		{
			try
			{
				HapticFeedback.Default.Perform(HapticFeedbackType.Click);
			}
			catch (Exception)
			{
				// Devices without haptics still show the captured photo.
			}
		}

		private void DisposePreviewCancellation()
		{
			this.previewCancellationTokenSource?.Cancel();
			this.previewCancellationTokenSource?.Dispose();
			this.previewCancellationTokenSource = null;
		}
	}
}
