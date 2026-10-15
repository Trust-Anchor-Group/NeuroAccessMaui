using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Animations;
using NeuroAccessMaui.Camera;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>Manages camera sessions, preparation of scanned values, and local scan feedback for the QR screen.</summary>
	public partial class ScanQrCodeViewModel
	{
		private static readonly TimeSpan processingIndicatorDelay = TimeSpan.FromMilliseconds(200);
		private static readonly TimeSpan readyFeedbackDuration = TimeSpan.FromMilliseconds(180);
		private static readonly TimeSpan rejectedFeedbackDuration = TimeSpan.FromMilliseconds(1200);
		private static readonly TimeSpan rejectedCandidateCooldown = TimeSpan.FromSeconds(3);
		private static readonly TimeSpan unavailableCandidateCooldown = TimeSpan.FromSeconds(5);

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
		private string? retryCandidate;
		private PreparationStatus lastPreparationStatus;

		/// <summary>Gets or sets the current camera, preparation, and feedback state.</summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(FeedbackColor), nameof(FeedbackDescription), nameof(ShowSuccess),
			nameof(IsStarting), nameof(ShowCameraRecovery), nameof(ShowFeedbackMessage), nameof(ShowPreparationRetry))]
		private QrScannerState state = QrScannerState.Starting;

		/// <summary>Gets or sets the camera selected for preview.</summary>
		[ObservableProperty]
		private CameraDescriptor? selectedCamera;

		/// <summary>Gets or sets whether preparation has taken long enough to show progress.</summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(ShowProcessingSpinner), nameof(ShowFeedbackMessage), nameof(FeedbackDescription))]
		private bool showProcessing;

		/// <summary>Gets whether a mode change can interrupt the current presentation.</summary>
		public bool CanSwitchMode => this.pageVisible && this.State is not (QrScannerState.Ready or QrScannerState.Closing);

		/// <summary>Gets whether the current camera supports torch use while scanning.</summary>
		public bool CanUseTorch => this.pageVisible && this.IsAutomaticScan && this.IsAwaitingCandidate &&
			this.SelectedCamera?.SupportsTorch == true;

		/// <summary>Gets whether another camera can be selected.</summary>
		public bool CanSwitchCamera => this.pageVisible && this.IsAutomaticScan && this.IsAwaitingCandidate && this.cameras.Count > 1;

		/// <summary>Gets whether camera startup is in progress.</summary>
		public bool IsStarting => this.IsAutomaticScan && this.State == QrScannerState.Starting;

		/// <summary>Gets whether the acceptance checkmark is visible.</summary>
		public bool ShowSuccess => this.State == QrScannerState.Ready;

		/// <summary>Gets whether camera recovery controls are visible.</summary>
		public bool ShowCameraRecovery => this.IsAutomaticScan && this.State == QrScannerState.CameraUnavailable;

		/// <summary>Gets whether the preparation progress spinner is visible. It is replaced by text alone when motion is reduced.</summary>
		public bool ShowProcessingSpinner => this.ShowProcessing && !MotionPreferences.ReduceMotion;

		/// <summary>Gets whether the user can retry preparing a value after a temporary failure.</summary>
		public bool ShowPreparationRetry => this.State == QrScannerState.Unavailable && this.retryCandidate is not null;

		/// <summary>Gets whether a visible explanation should accompany the frame color.</summary>
		public bool ShowFeedbackMessage => this.State is QrScannerState.Rejected or QrScannerState.Unavailable or QrScannerState.CameraUnavailable ||
			(this.State == QrScannerState.Validating && this.ShowProcessing);

		/// <summary>Gets the current scan-frame color.</summary>
		public Color FeedbackColor => this.State switch
		{
			QrScannerState.Ready => ScannerColors.Success,
			QrScannerState.Rejected or QrScannerState.Unavailable or QrScannerState.CameraUnavailable => ScannerColors.Attention,
			_ => ScannerColors.Neutral
		};

		/// <summary>Gets localized feedback for the user and assistive technology.</summary>
		public string FeedbackDescription => ServiceRef.Localizer[this.State switch
		{
			QrScannerState.Ready => nameof(AppResources.ScannedQrCode),
			QrScannerState.Validating => nameof(AppResources.QrScannerValidating),
			QrScannerState.Rejected => this.lastPreparationStatus == PreparationStatus.Unsupported
				? nameof(AppResources.QrScannerCodeNotSupported) : nameof(AppResources.QrScannerCodeInvalid),
			QrScannerState.Unavailable => nameof(AppResources.QrScannerUnavailable),
			QrScannerState.CameraUnavailable => nameof(AppResources.QrScannerCameraUnavailable),
			_ => nameof(AppResources.ScanQRCode)
		}];

		/// <summary>Gets the accessible description of the torch action.</summary>
		public string TorchDescription => ServiceRef.Localizer[this.IsTorchOn ? nameof(AppResources.QrScannerTorchOff) : nameof(AppResources.QrScannerTorchOn)];

		/// <summary>Gets whether the scanner is waiting for a new value, possibly after a temporary failure.</summary>
		private bool IsAwaitingCandidate => this.State is QrScannerState.Scanning or QrScannerState.Unavailable;

		partial void OnStateChanged(QrScannerState value)
		{
			this.RefreshScannerControls();
			this.OnBackgroundColorChanged();
			if (value is QrScannerState.Ready or QrScannerState.Rejected or QrScannerState.Unavailable or QrScannerState.CameraUnavailable)
				this.Announce();
		}

		partial void OnShowProcessingChanged(bool value)
		{
			if (value)
				this.Announce();
		}

		partial void OnSelectedCameraChanged(CameraDescriptor? value) => this.RefreshScannerControls();

		private void Announce()
		{
			if (!this.pageVisible)
				return;

			try { SemanticScreenReader.Default.Announce(this.FeedbackDescription); }
			catch (Exception) { /* Accessibility support must not interrupt scanning. */ }
		}

		private void RefreshScannerControls()
		{
			this.OnPropertyChanged(nameof(this.CanSwitchMode));
			this.OnPropertyChanged(nameof(this.CanUseTorch));
			this.OnPropertyChanged(nameof(this.CanSwitchCamera));
			this.OnPropertyChanged(nameof(this.CanOpenManual));
			this.OnPropertyChanged(nameof(this.IsStarting));
			this.OnPropertyChanged(nameof(this.ShowCameraRecovery));
			this.OnPropertyChanged(nameof(this.ShowPreparationRetry));
			this.SwitchModeCommand.NotifyCanExecuteChanged();
			this.SwitchTorchCommand.NotifyCanExecuteChanged();
			this.SwitchCameraCommand.NotifyCanExecuteChanged();
			this.OpenUrlCommand.NotifyCanExecuteChanged();
			this.RetryPreparationCommand.NotifyCanExecuteChanged();
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

		/// <summary>Cancels camera startup, decoding results, preparation, and pending feedback.</summary>
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
			this.ShowProcessing = false;
			this.processingCandidate = false;
		}

		private void ResetScannerSession()
		{
			this.CancelScannerSession();
			this.rejectedText = null;
			this.retryCandidate = null;
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

		[RelayCommand(CanExecute = nameof(ShowPreparationRetry))]
		private Task RetryPreparationAsync()
		{
			string? Candidate = this.retryCandidate;
			if (Candidate is null || this.State != QrScannerState.Unavailable)
				return Task.CompletedTask;

			return this.ProcessCandidateAsync(Candidate);
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
					if (this.firstFrame.TrySetResult(true))
					{
						if (this.State == QrScannerState.Starting)
							this.State = QrScannerState.Scanning;
						if (this.IsAwaitingCandidate)
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

		/// <summary>Applies the scanner's scheme rules, prepares an accepted value, and presents the outcome.</summary>
		/// <param name="Text">The scanned or entered value.</param>
		/// <returns>A task representing preparation, feedback, and result delivery.</returns>
		private async Task ProcessCandidateAsync(string? Text)
		{
			if (!this.pageVisible || this.processingCandidate || this.scannerCancellation is null || this.State == QrScannerState.Closing)
				return;
			CancellationToken Token = this.scannerCancellation.Token;
			this.processingCandidate = true;
			this.IsDetecting = false;
			this.ScannedText = Text ?? string.Empty;
			string Candidate = Text?.Trim() ?? string.Empty;
			try
			{
				PreparationStatus Status;
				if (!this.CanOpen(Candidate))
					Status = PreparationStatus.Unsupported;
				else if (this.navigationArgs?.PrepareAsync is null)
					Status = PreparationStatus.Ready;
				else
					Status = await this.PrepareCandidateAsync(this.navigationArgs.PrepareAsync, Candidate, Token);

				Token.ThrowIfCancellationRequested();
				this.lastPreparationStatus = Status;

				switch (Status)
				{
					case PreparationStatus.Ready:
						this.retryCandidate = null;
						this.State = QrScannerState.Ready;
						try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
						catch (Exception) { /* Devices without haptics still complete the scan. */ }
						await Task.Delay(readyFeedbackDuration, Token);
						Token.ThrowIfCancellationRequested();
						await this.TrySetResultAndClosePageAsync(Candidate);
						break;

					case PreparationStatus.Unavailable:
						this.ShowUnavailable(Text, Candidate);
						break;

					case PreparationStatus.Cancelled:
						break;

					default:
						this.retryCandidate = null;
						this.SuppressCandidate(Text, DateTime.UtcNow.Add(rejectedCandidateCooldown));
						this.State = QrScannerState.Rejected;
						await Task.Delay(rejectedFeedbackDuration, Token);
						Token.ThrowIfCancellationRequested();
						this.State = QrScannerState.Scanning;
						this.IsDetecting = this.IsAutomaticScan;
						break;
				}
			}
			catch (OperationCanceledException) when (Token.IsCancellationRequested) { }
			catch (Exception Ex)
			{
				// Preparation is supplied by the caller; an unexpected failure is treated as temporary. Only the type is logged.
				ServiceRef.LogService.LogWarning("QrScannerPreparationFailed",
					new KeyValuePair<string, object?>("ExceptionType", Ex.GetType().Name));
				if (!Token.IsCancellationRequested && this.State != QrScannerState.Closing)
				{
					this.lastPreparationStatus = PreparationStatus.Unavailable;
					this.ShowUnavailable(Text, Candidate);
				}
			}
			finally
			{
				if (!Token.IsCancellationRequested)
					this.processingCandidate = false;
			}
		}

		/// <summary>Runs the caller's preparation, showing progress only when it takes noticeable time.</summary>
		/// <param name="PrepareAsync">The caller's preparation.</param>
		/// <param name="Candidate">The trimmed value to prepare.</param>
		/// <param name="Token">Cancellation for the scanner session.</param>
		/// <returns>The preparation outcome.</returns>
		private async Task<PreparationStatus> PrepareCandidateAsync(Func<string, CancellationToken, Task<PreparationStatus>> PrepareAsync,
			string Candidate, CancellationToken Token)
		{
			this.State = QrScannerState.Validating;
			using CancellationTokenSource Progress = CancellationTokenSource.CreateLinkedTokenSource(Token);
			Task ProgressIndicator = this.ShowProcessingAfterDelayAsync(Progress.Token);
			try
			{
				return await PrepareAsync(Candidate, Token);
			}
			finally
			{
				Progress.Cancel();
				await ProgressIndicator;
				this.ShowProcessing = false;
			}
		}

		private async Task ShowProcessingAfterDelayAsync(CancellationToken Token)
		{
			try
			{
				await Task.Delay(processingIndicatorDelay, Token);
				if (!Token.IsCancellationRequested && this.State == QrScannerState.Validating)
					this.ShowProcessing = true;
			}
			catch (OperationCanceledException) { }
		}

		/// <summary>Shows a temporary preparation failure, keeping the value for an explicit retry while other codes can be scanned.</summary>
		/// <param name="Text">The value as scanned.</param>
		/// <param name="Candidate">The trimmed value to retry.</param>
		private void ShowUnavailable(string? Text, string Candidate)
		{
			this.retryCandidate = Candidate;
			this.SuppressCandidate(Text, DateTime.UtcNow.Add(unavailableCandidateCooldown));
			this.State = QrScannerState.Unavailable;
			this.IsDetecting = this.IsAutomaticScan;
		}

		/// <summary>Stops the camera from picking up the same value again until the given time.</summary>
		/// <param name="Text">The value as scanned.</param>
		/// <param name="Until">When the value may be picked up again.</param>
		private void SuppressCandidate(string? Text, DateTime Until)
		{
			this.rejectedText = Text;
			this.rejectedUntil = Until;
		}
	}
}
