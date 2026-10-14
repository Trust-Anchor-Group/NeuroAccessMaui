using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using NeuroAccessMaui.Camera;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Captures an unmodified KYC photo through the app camera, with a framing guide, torch and camera switching, and a
	/// review step before the photo is returned.
	/// </summary>
	/// <remarks>
	/// The caller receives the photo only when the user chooses to use it, and <c>null</c> when the user cancels.
	/// </remarks>
	public partial class KycProfilePhotoCameraViewModel : BaseViewModel, IDisposable
	{
		private const int previewStartMaxAttempts = 12;
		private static readonly TimeSpan previewStartRetryDelay = TimeSpan.FromMilliseconds(100);

		private readonly KycProfilePhotoCameraNavigationArgs? navigationArgs;
		private IReadOnlyList<CameraDescriptor> availableCameras = Array.Empty<CameraDescriptor>();
		private CancellationTokenSource? previewCancellationTokenSource;
		private byte[]? capturedPhotoBytes;
		private bool isDisposed;
		private bool isClosing;
		private bool resultReturned;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProfilePhotoCameraViewModel"/> class.
		/// </summary>
		/// <param name="NavigationArgs">The navigation arguments.</param>
		public KycProfilePhotoCameraViewModel(KycProfilePhotoCameraNavigationArgs? NavigationArgs)
		{
			this.navigationArgs = NavigationArgs;
			this.CaptureProfile = NavigationArgs?.CaptureProfile ?? KycCaptureProfile.Selfie;
		}

		/// <summary>
		/// Gets or sets the camera preview used by the page.
		/// </summary>
		public CameraView? CameraView { get; set; }

		/// <summary>
		/// Gets what the photo captures.
		/// </summary>
		public KycCaptureProfile CaptureProfile { get; }

		/// <summary>
		/// Gets a value indicating whether the photo is of the user's face.
		/// </summary>
		public bool IsSelfie => this.CaptureProfile == KycCaptureProfile.Selfie;

		/// <summary>
		/// Gets the document frame aspect ratio of the framing guide, as width divided by height.
		/// </summary>
		public double GuideFrameAspectRatio => this.CaptureProfile switch
		{
			KycCaptureProfile.Passport => 1.42,
			KycCaptureProfile.IdCardFront or KycCaptureProfile.IdCardBack => 1.586,
			_ => 1.41
		};

		/// <summary>
		/// Gets the page title: the capture title while the camera is active, and a review prompt once a photo is taken.
		/// </summary>
		public string TitleText
		{
			get
			{
				if (this.HasCapturedPhoto)
					return ServiceRef.Localizer[nameof(AppResources.KycCameraReviewTitle)];

				if (!string.IsNullOrWhiteSpace(this.navigationArgs?.Title))
					return this.navigationArgs.Title;

				return this.IsSelfie
					? ServiceRef.Localizer[nameof(AppResources.TakePhotoOfYourself)]
					: ServiceRef.Localizer[nameof(AppResources.TakePhoto)];
			}
		}

		/// <summary>
		/// Gets the short framing instruction shown in the guidance card.
		/// </summary>
		public string GuidanceText => this.CaptureProfile switch
		{
			KycCaptureProfile.Selfie => ServiceRef.Localizer[nameof(AppResources.KycProfilePhotoCameraGuidance)],
			KycCaptureProfile.Passport => ServiceRef.Localizer[nameof(AppResources.KycCameraGuidePassport)],
			KycCaptureProfile.IdCardFront => ServiceRef.Localizer[nameof(AppResources.KycCameraGuideIdFront)],
			KycCaptureProfile.IdCardBack => ServiceRef.Localizer[nameof(AppResources.KycCameraGuideIdBack)],
			_ => ServiceRef.Localizer[nameof(AppResources.KycCameraGuideDocument)]
		};

		/// <summary>
		/// Gets the field hint shown under the framing instruction, if any.
		/// </summary>
		public string GuideDetail => this.navigationArgs?.GuideHint?.Trim() ?? string.Empty;

		/// <summary>
		/// Gets a value indicating whether a field hint is shown in the guidance card.
		/// </summary>
		public bool HasGuideDetail => !string.IsNullOrWhiteSpace(this.GuideDetail);

		/// <summary>
		/// Gets the document illustration shown in the guidance card, or <c>null</c> for selfies and generic photos.
		/// </summary>
		public string? GuideIllustration => this.CaptureProfile switch
		{
			KycCaptureProfile.Passport => "kyc_doc_passport.svg",
			KycCaptureProfile.IdCardFront => "kyc_doc_id_front.svg",
			KycCaptureProfile.IdCardBack => "kyc_doc_id_back.svg",
			_ => null
		};

		/// <summary>
		/// Gets a value indicating whether a document illustration is shown in the guidance card.
		/// </summary>
		public bool HasGuideIllustration => this.GuideIllustration is not null;

		/// <summary>
		/// Gets the hint shown while the user checks the captured photo.
		/// </summary>
		public string ReviewHintText => this.IsSelfie
			? ServiceRef.Localizer[nameof(AppResources.KycCameraReviewSelfieHint)]
			: ServiceRef.Localizer[nameof(AppResources.KycCameraReviewDocumentHint)];

		/// <summary>
		/// Gets how the captured photo is fitted for review. Documents are shown whole so every edge can be checked.
		/// </summary>
		public Aspect ReviewAspect => this.IsSelfie ? Aspect.AspectFill : Aspect.AspectFit;

		/// <summary>
		/// Gets the accessible description of the cancel action.
		/// </summary>
		public string CancelText => ServiceRef.Localizer[nameof(AppResources.Cancel)];

		/// <summary>
		/// Gets the accessible description of the shutter button.
		/// </summary>
		public string CaptureText => ServiceRef.Localizer[nameof(AppResources.TakePhoto)];

		/// <summary>
		/// Gets the localized retake action text.
		/// </summary>
		public string RetakeText => ServiceRef.Localizer[nameof(AppResources.KycImageRetakeButton)];

		/// <summary>
		/// Gets the localized accept action text.
		/// </summary>
		public string UsePhotoText => ServiceRef.Localizer[nameof(AppResources.KycCameraUsePhotoButton)];

		/// <summary>
		/// Gets the accessible description of the torch button.
		/// </summary>
		public string TorchDescription => ServiceRef.Localizer[this.IsTorchOn ? "QrScannerTorchOff" : "QrScannerTorchOn"];

		/// <summary>
		/// Gets the accessible description of the camera switch button.
		/// </summary>
		public string SwitchCameraText => ServiceRef.Localizer["QrScannerSwitchCamera"];

		/// <summary>
		/// Gets a value indicating whether a photo has been captured and is waiting for review.
		/// </summary>
		public bool HasCapturedPhoto => this.CapturedPhotoSource is not null;

		/// <summary>
		/// Gets a value indicating whether the live camera controls and the framing guide should be shown.
		/// </summary>
		public bool ShowCameraPreview => !this.HasCapturedPhoto;

		/// <summary>
		/// Gets a value indicating whether the shutter and camera controls can be used.
		/// </summary>
		public bool CanCapture => !this.IsCapturing && !this.HasCapturedPhoto;

		/// <summary>
		/// Gets or sets the captured photo shown for review.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasCapturedPhoto))]
		[NotifyPropertyChangedFor(nameof(ShowCameraPreview))]
		[NotifyPropertyChangedFor(nameof(TitleText))]
		[NotifyPropertyChangedFor(nameof(CanCapture))]
		private ImageSource? capturedPhotoSource;

		/// <summary>
		/// Gets or sets a value indicating whether a photo is being taken.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCapture))]
		private bool isCapturing;

		/// <summary>
		/// Gets or sets a value indicating whether the torch is on.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(TorchDescription))]
		private bool isTorchOn;

		/// <summary>
		/// Gets or sets a value indicating whether the selected camera has a torch that can be used.
		/// </summary>
		[ObservableProperty]
		private bool canUseTorch;

		/// <summary>
		/// Gets or sets a value indicating whether there is both a front and a rear camera to switch between.
		/// </summary>
		[ObservableProperty]
		private bool canSwitchCamera;

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
			CancellationToken CancellationToken = this.previewCancellationTokenSource.Token;

			this.CameraView.Options = new CameraOptions
			{
				PreferRearCamera = !this.IsSelfie,
				FrameDeliveryInterval = TimeSpan.FromMilliseconds(250),
				PreviewScaling = CameraPreviewScaling.Fill,
				TargetFps = 15,
				TargetResolution = new Size(1280d, 720d),
				JpegQuality = 96
			};

			bool PreviewStarted;
			try
			{
				await this.SelectInitialCameraAsync(CancellationToken);
				PreviewStarted = await this.StartPreviewWithRetryAsync(CancellationToken);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (!PreviewStarted)
				await this.HandlePreviewStartupFailureAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			this.DisposePreviewCancellation();

			if (this.CameraView is not null)
			{
				await this.SetTorchAsync(false);
				await this.CameraView.StopPreviewAsync();
			}

			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			await this.SetTorchAsync(false);
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
			if (this.IsCapturing || this.isClosing)
				return;

			await this.CloseAsync(null);
		}

		[RelayCommand]
		private async Task Capture()
		{
			if (this.CameraView is null || !this.CanCapture)
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
				Announce(this.TitleText);

				// The torch cannot be switched off while the photo is reviewed, so it is not left on behind the review.
				await this.SetTorchAsync(false);
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
			if (this.capturedPhotoBytes is null || this.isClosing)
				return;

			await this.CloseAsync(this.capturedPhotoBytes);
		}

		/// <summary>
		/// Returns the result and leaves the page, ignoring further close and use actions until the navigation is done,
		/// so a second tap never navigates back past the calling page.
		/// </summary>
		/// <param name="PhotoBytes">The photo to return, or <c>null</c> when the user cancels.</param>
		/// <returns>A task that completes when the page has been left.</returns>
		private async Task CloseAsync(byte[]? PhotoBytes)
		{
			this.isClosing = true;
			try
			{
				this.ReturnResult(PhotoBytes);
				await ServiceRef.NavigationService.GoBackAsync();
			}
			finally
			{
				this.isClosing = false;
			}
		}

		[RelayCommand]
		private async Task ToggleTorch()
		{
			if (!this.CanUseTorch || !this.CanCapture)
				return;

			await this.SetTorchAsync(!this.IsTorchOn);
		}

		[RelayCommand]
		private async Task SwitchCamera()
		{
			if (this.CameraView is null || !this.CanSwitchCamera || !this.CanCapture)
				return;

			CameraPosition CurrentPosition = this.CameraView.SelectedCamera?.Position ?? CameraPosition.Unknown;
			CameraPosition DesiredPosition = CurrentPosition == CameraPosition.Front ? CameraPosition.Rear : CameraPosition.Front;
			CameraDescriptor? NextCamera = this.availableCameras.FirstOrDefault(Descriptor => Descriptor.Position == DesiredPosition);
			if (NextCamera is null)
				return;

			await this.SetTorchAsync(false);
			this.CameraView.SelectedCamera = NextCamera;
			this.CanUseTorch = NextCamera.SupportsTorch;
		}

		private async Task SetTorchAsync(bool IsEnabled)
		{
			if (this.CameraView?.Controller is not ICameraController Controller || (!IsEnabled && !this.IsTorchOn))
				return;

			try
			{
				await Controller.SetTorchAsync(IsEnabled, CancellationToken.None);
				this.IsTorchOn = IsEnabled;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.IsTorchOn = false;
			}
		}

		/// <summary>
		/// Selects the front camera for selfies and the rear camera for documents, and resolves the torch and camera
		/// switch availability.
		/// </summary>
		/// <param name="CancellationToken">Cancels the camera lookup.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task SelectInitialCameraAsync(CancellationToken CancellationToken)
		{
			if (this.CameraView is null)
				return;

			try
			{
				this.availableCameras = await NeuroAccessMaui.Camera.CameraView.GetAvailableCamerasAsync(CancellationToken);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.availableCameras = Array.Empty<CameraDescriptor>();
			}

			this.CanSwitchCamera = this.availableCameras.Any(Descriptor => Descriptor.Position == CameraPosition.Front) &&
				this.availableCameras.Any(Descriptor => Descriptor.Position == CameraPosition.Rear);

			CameraPosition PreferredPosition = this.IsSelfie ? CameraPosition.Front : CameraPosition.Rear;
			CameraDescriptor? SelectedCamera = this.CameraView.SelectedCamera is CameraDescriptor Current &&
				this.availableCameras.Any(Descriptor => Descriptor.Id == Current.Id)
				? Current
				: this.availableCameras.FirstOrDefault(Descriptor => Descriptor.Position == PreferredPosition) ?? this.availableCameras.FirstOrDefault();

			if (SelectedCamera is null)
			{
				this.CanUseTorch = false;
				return;
			}

			if (!ReferenceEquals(this.CameraView.SelectedCamera, SelectedCamera))
				this.CameraView.SelectedCamera = SelectedCamera;

			this.CanUseTorch = SelectedCamera.SupportsTorch;
		}

		/// <summary>
		/// Starts the preview, waiting briefly for the platform camera controller when the page has just been created.
		/// </summary>
		/// <param name="CancellationToken">Cancels the start.</param>
		/// <returns><c>true</c> if the preview is running; otherwise <c>false</c>.</returns>
		private async Task<bool> StartPreviewWithRetryAsync(CancellationToken CancellationToken)
		{
			if (this.CameraView is null)
				return false;

			for (int Attempt = 1; Attempt <= previewStartMaxAttempts; Attempt++)
			{
				CancellationToken.ThrowIfCancellationRequested();

				if (this.CameraView.Controller is not null)
				{
					try
					{
						await this.CameraView.StartPreviewAsync(CancellationToken);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}

					if (this.CameraView.IsPreviewRunning)
						return true;
				}

				if (Attempt < previewStartMaxAttempts)
					await Task.Delay(previewStartRetryDelay, CancellationToken);
			}

			return false;
		}

		private async Task HandlePreviewStartupFailureAsync()
		{
			this.ReturnResult(null);

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.KycCameraStartFailedTitle)],
				ServiceRef.Localizer[nameof(AppResources.KycCameraStartFailedMessage)],
				ServiceRef.Localizer[nameof(AppResources.Ok)]);

			await ServiceRef.NavigationService.GoBackAsync();
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

		private static void Announce(string Text)
		{
			try
			{
				SemanticScreenReader.Default.Announce(Text);
			}
			catch (Exception)
			{
				// Announcements are best effort.
			}
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

		private void DisposePreviewCancellation()
		{
			this.previewCancellationTokenSource?.Cancel();
			this.previewCancellationTokenSource?.Dispose();
			this.previewCancellationTokenSource = null;
		}
	}
}
