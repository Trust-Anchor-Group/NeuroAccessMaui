using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Camera;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>Manages camera sessions and local scan feedback for the QR screen.</summary>
	public partial class ScanQrCodeViewModel
	{
		private readonly ScanQrFrameDecoder frameDecoder = new ScanQrFrameDecoder();
		private readonly List<CameraDescriptor> cameras = new List<CameraDescriptor>();
		private CameraView? cameraView;
		private CancellationTokenSource? scannerCancellation;
		private TaskCompletionSource<bool>? firstFrame;
		private bool pageVisible;
		private bool processingCandidate;
		private int processingFrame;
		private int scannerGeneration;
		private string? rejectedText;
		private DateTime rejectedUntil;

		/// <summary>Gets or sets the current camera and feedback state.</summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(FeedbackColor), nameof(FeedbackDescription), nameof(ShowSuccess),
			nameof(IsStarting), nameof(ShowCameraRecovery), nameof(ShowFeedbackMessage))]
		private QrScannerState state = QrScannerState.Starting;

		/// <summary>Gets or sets the camera selected for preview.</summary>
		[ObservableProperty]
		private CameraDescriptor? selectedCamera;

		/// <summary>Gets whether a mode change can interrupt the current presentation.</summary>
		public bool CanSwitchMode => this.pageVisible && this.State is not (QrScannerState.Accepted or QrScannerState.Closing);

		/// <summary>Gets whether the current camera supports torch use while scanning.</summary>
		public bool CanUseTorch => this.pageVisible && this.IsAutomaticScan && this.State == QrScannerState.Scanning &&
			this.SelectedCamera?.SupportsTorch == true;

		/// <summary>Gets whether another camera can be selected.</summary>
		public bool CanSwitchCamera => this.pageVisible && this.IsAutomaticScan && this.State == QrScannerState.Scanning && this.cameras.Count > 1;

		/// <summary>Gets whether camera startup is in progress.</summary>
		public bool IsStarting => this.IsAutomaticScan && this.State == QrScannerState.Starting;

		/// <summary>Gets whether the acceptance checkmark is visible.</summary>
		public bool ShowSuccess => this.State == QrScannerState.Accepted;

		/// <summary>Gets whether camera recovery controls are visible.</summary>
		public bool ShowCameraRecovery => this.IsAutomaticScan && this.State == QrScannerState.CameraUnavailable;

		/// <summary>Gets whether a visible explanation should accompany the frame color.</summary>
		public bool ShowFeedbackMessage => this.State is QrScannerState.Rejected or QrScannerState.CameraUnavailable;

		/// <summary>Gets the current scan-frame color.</summary>
		public Color FeedbackColor => this.State switch
		{
			QrScannerState.Accepted => ScannerColors.Success,
			QrScannerState.Rejected or QrScannerState.CameraUnavailable => ScannerColors.Attention,
			_ => ScannerColors.Neutral
		};

		/// <summary>Gets localized feedback for the user and assistive technology.</summary>
		public string FeedbackDescription => ServiceRef.Localizer[this.State switch
		{
			QrScannerState.Accepted => nameof(AppResources.ScannedQrCode),
			QrScannerState.Rejected => nameof(AppResources.CodeNotRecognized),
			QrScannerState.CameraUnavailable => "QrScannerCameraUnavailable",
			_ => nameof(AppResources.ScanQRCode)
		}];

		/// <summary>Gets the accessible description of the torch action.</summary>
		public string TorchDescription => ServiceRef.Localizer[this.IsTorchOn ? "QrScannerTorchOff" : "QrScannerTorchOn"];

		partial void OnStateChanged(QrScannerState value)
		{
			this.RefreshScannerControls();
			this.OnBackgroundColorChanged();
			if (this.pageVisible && value is QrScannerState.Accepted or QrScannerState.Rejected or QrScannerState.CameraUnavailable)
			{
				try { SemanticScreenReader.Default.Announce(this.FeedbackDescription); }
				catch (Exception) { /* Accessibility support must not interrupt scanning. */ }
			}
		}

		partial void OnSelectedCameraChanged(CameraDescriptor? value) => this.RefreshScannerControls();

		private void RefreshScannerControls()
		{
			this.OnPropertyChanged(nameof(this.CanSwitchMode));
			this.OnPropertyChanged(nameof(this.CanUseTorch));
			this.OnPropertyChanged(nameof(this.CanSwitchCamera));
			this.OnPropertyChanged(nameof(this.CanOpenManual));
			this.OnPropertyChanged(nameof(this.IsStarting));
			this.OnPropertyChanged(nameof(this.ShowCameraRecovery));
			this.SwitchModeCommand.NotifyCanExecuteChanged();
			this.SwitchTorchCommand.NotifyCanExecuteChanged();
			this.SwitchCameraCommand.NotifyCanExecuteChanged();
			this.OpenUrlCommand.NotifyCanExecuteChanged();
		}

		/// <summary>Connects frame delivery to the scanner without transferring camera ownership.</summary>
		/// <param name="CameraView">The camera view, or null during disposal.</param>
		internal void AttachCamera(CameraView? CameraView)
		{
			if (this.disposed && CameraView is not null)
				return;
			if (this.cameraView is not null)
				this.cameraView.FrameReady -= this.CameraFrameReady;
			this.cameraView = CameraView;
			if (CameraView is not null)
				CameraView.FrameReady += this.CameraFrameReady;
		}

		/// <summary>Allows input for a newly visible scanner page.</summary>
		internal void BeginScannerSession()
		{
			if (this.disposed)
				return;
			this.pageVisible = true;
			this.ResetScannerSession();
		}

		/// <summary>Cancels camera startup, decoding results, and pending feedback.</summary>
		internal void EndScannerSession()
		{
			this.pageVisible = false;
			this.CancelScannerSession();
			this.RefreshScannerControls();
		}

		private void CancelScannerSession()
		{
			Interlocked.Increment(ref this.scannerGeneration);
			this.scannerCancellation?.Cancel();
			this.scannerCancellation?.Dispose();
			this.scannerCancellation = null;
			this.firstFrame = null;
			this.IsDetecting = false;
			this.IsTorchOn = false;
			this.processingCandidate = false;
		}

		private void ResetScannerSession()
		{
			this.CancelScannerSession();
			this.rejectedText = null;
			if (this.pageVisible)
			{
				this.scannerCancellation = new CancellationTokenSource();
				this.State = this.IsAutomaticScan ? QrScannerState.Starting : QrScannerState.Scanning;
			}
			this.RefreshScannerControls();
		}

		/// <summary>Starts the selected camera and waits for its first usable frame.</summary>
		/// <returns>A task representing camera startup.</returns>
		internal async Task StartCameraAsync()
		{
			CameraView? Camera = this.cameraView;
			if (!this.pageVisible || !this.IsAutomaticScan || this.scannerCancellation is null || Camera?.Controller is null ||
				this.State is QrScannerState.CameraUnavailable or QrScannerState.Closing)
				return;

			CancellationToken Token = this.scannerCancellation.Token;
			try
			{
				if (Camera.IsPreviewRunning && Camera.SelectedCamera?.Id == this.SelectedCamera?.Id && this.State != QrScannerState.Starting)
					return;

				await Camera.StopPreviewAsync();
				Token.ThrowIfCancellationRequested();
				bool Permitted = await ServiceRef.PermissionService.CheckCameraPermissionAsync();
				Token.ThrowIfCancellationRequested();
				if (!Permitted)
				{
					this.HandleCameraFailure();
					return;
				}

				IReadOnlyList<CameraDescriptor> AvailableCameras = await CameraView.GetAvailableCamerasAsync(Token);
				Token.ThrowIfCancellationRequested();
				this.cameras.Clear();
				this.cameras.AddRange(AvailableCameras);
				this.SelectedCamera = this.cameras.FirstOrDefault(Item => Item.Id == this.SelectedCamera?.Id)
					?? this.cameras.FirstOrDefault(Item => Item.Position == CameraPosition.Rear)
					?? this.cameras.FirstOrDefault();
				if (this.SelectedCamera is null)
				{
					this.HandleCameraFailure();
					return;
				}

				Camera.SelectedCamera = this.SelectedCamera;
				TaskCompletionSource<bool> FirstFrame = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				this.firstFrame = FirstFrame;
				await Camera.StartPreviewAsync(Token);
				await FirstFrame.Task.WaitAsync(TimeSpan.FromSeconds(8), Token);
			}
			catch (OperationCanceledException) when (Token.IsCancellationRequested) { }
			catch (Exception)
			{
				if (!Token.IsCancellationRequested)
					this.HandleCameraFailure();
			}
		}

		/// <summary>Shows recoverable camera failure without affecting QR acceptance rules.</summary>
		internal void HandleCameraFailure()
		{
			if (!this.pageVisible || !this.IsAutomaticScan || this.State == QrScannerState.Closing)
				return;
			this.CancelScannerSession();
			this.State = QrScannerState.CameraUnavailable;
		}

		[RelayCommand]
		private void OpenSettings() => AppInfo.Current.ShowSettingsUI();

		[RelayCommand]
		private void RetryCamera()
		{
			if (this.ShowCameraRecovery)
				this.ResetScannerSession();
		}

		private async void CameraFrameReady(object? Sender, CameraFrame Frame)
		{
			int Generation = Volatile.Read(ref this.scannerGeneration);
			if (Interlocked.CompareExchange(ref this.processingFrame, 1, 0) != 0)
				return;
			try
			{
				CancellationToken Token = new CancellationToken(true);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Generation != this.scannerGeneration || this.firstFrame is null ||
						!this.pageVisible || !this.IsAutomaticScan || this.scannerCancellation is null ||
						!ReferenceEquals(Sender, this.cameraView) || Frame.Format != CameraFrameFormat.Grayscale8 ||
						Frame.Width <= 0 || Frame.Height <= 0 || (long)Frame.Width * Frame.Height != Frame.Buffer.Length)
						return;
					this.firstFrame?.TrySetResult(true);
					if (this.State == QrScannerState.Starting)
					{
						this.State = QrScannerState.Scanning;
						this.IsDetecting = true;
					}
					if (this.IsDetecting)
						Token = this.scannerCancellation.Token;
				});
				if (Token.IsCancellationRequested)
					return;
				string? Text = await this.frameDecoder.DecodeAsync(Frame, Token);
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					if (!Token.IsCancellationRequested && this.IsDetecting && !string.IsNullOrWhiteSpace(Text) &&
						(Text != this.rejectedText || DateTime.UtcNow >= this.rejectedUntil))
						await this.SetScannedText(Text);
				});
			}
			catch (OperationCanceledException) { }
			catch (Exception)
			{
				// A malformed frame must not terminate scanning or expose its payload in logs.
			}
			finally
			{
				Interlocked.Exchange(ref this.processingFrame, 0);
			}
		}

		private async Task ProcessCandidateAsync(string? Text)
		{
			if (!this.pageVisible || this.processingCandidate || this.scannerCancellation is null || this.State == QrScannerState.Closing)
				return;
			CancellationToken Token = this.scannerCancellation.Token;
			this.processingCandidate = true;
			this.IsDetecting = false;
			this.ScannedText = Text ?? string.Empty;
			try
			{
				if (this.CanOpen(Text))
				{
					this.State = QrScannerState.Accepted;
					try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
					catch (Exception) { /* Devices without haptics still complete the scan. */ }
					await Task.Delay(180, Token);
					Token.ThrowIfCancellationRequested();
					await this.TrySetResultAndClosePageAsync(Text!.Trim());
				}
				else
				{
					this.rejectedText = Text;
					this.rejectedUntil = DateTime.UtcNow.AddSeconds(3);
					this.State = QrScannerState.Rejected;
					await Task.Delay(650, Token);
					Token.ThrowIfCancellationRequested();
					this.State = QrScannerState.Scanning;
					this.IsDetecting = this.IsAutomaticScan;
				}
			}
			catch (OperationCanceledException) when (Token.IsCancellationRequested) { }
			finally
			{
				if (!Token.IsCancellationRequested)
					this.processingCandidate = false;
			}
		}
	}
}
