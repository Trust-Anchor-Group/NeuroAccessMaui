using System.Globalization;
using System.Text.Json;
using System.Threading;
#if OCR_DEBUG_ARTIFACTS_TO_JID
using System.IO.Compression;
#endif
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IdApp.Cv;
using IdApp.Cv.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using NeuroAccess.Nfc.TravelDocuments;
#if OCR_DEBUG_ARTIFACTS_TO_JID || OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
using Microsoft.Maui.Storage;
#endif
using NeuroAccessMaui.Camera;
using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.OCR.Pipeline;
using NeuroAccessMaui.OCR.Services;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.TravelDocuments;
#if OCR_DEBUG_ARTIFACTS_TO_JID
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.HttpFileUpload;
#endif

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Runs live preview-based document-line recognition for the KYC chip-scan flow.
	/// </summary>
	public partial class KycDocumentMrzScannerViewModel : BaseViewModel, IDisposable
	{
		private enum ScannerHintKey
		{
			Initial,
			FindMrz,
			MoveCloser,
			ReduceGlare,
			HoldStill,
			Reading,
			TransientFailure,
			DocumentExpired,
			UnsupportedDocument
		}

		private enum ScannerHintPriority
		{
			Guidance = 0,
			ImportantGuidance = 1,
			Progress = 2,
			Error = 3
		}

		private enum ScannerHintTone
		{
			Neutral,
			Attention,
			Progress,
			Error
		}

		private enum ScannerMrzValidationOutcome
		{
			Accepted,
			Expired,
			UnsupportedDocumentType,
			BadRead
		}

		private readonly record struct OutlineSnapshot(
			Point TopLeft,
			Point TopRight,
			Point BottomRight,
			Point BottomLeft,
			string Source,
			double CenterX,
			double CenterY,
			double Width,
			double Height,
			double AspectRatio);

		private const double OutlineSmoothingRatio = 0.85d;
		private const float MinimumVisualDocumentQuadScore = 0.28f;
		private const double OutlineCompatibilityCenterRatio = 0.16d;
		private const double OutlineCompatibilitySizeRatio = 0.36d;
		private const double OutlineCompatibilityAspectRatio = 0.32d;
		private const double OutlineSourceSwitchCenterRatio = 0.10d;
		private const double OutlineSourceSwitchSizeRatio = 0.24d;
		private const double OutlineSourceSwitchAspectRatio = 0.22d;
		private const int UnsupportedDocumentEvidenceThreshold = 3;
		private static readonly TimeSpan OutlineGracePeriod = TimeSpan.FromMilliseconds(550);
		private static readonly TimeSpan MinimumGuidanceHintDuration = TimeSpan.FromMilliseconds(1800);
		private static readonly TimeSpan MinimumImportantGuidanceHintDuration = TimeSpan.FromMilliseconds(2600);
		private static readonly TimeSpan MinimumProgressHintDuration = TimeSpan.FromMilliseconds(900);
		private static readonly TimeSpan MinimumTransientFailureHintDuration = TimeSpan.FromMilliseconds(2600);

#if OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
		private static readonly JsonSerializerOptions PreviewDebugJsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			IncludeFields = true
		};
#endif

#if OCR_DEBUG_ARTIFACTS_TO_JID
		private const string OcrDebugArtifactRecipientJid = "maximiliam.berggren.lilsis@lab.tagroot.io";
		private const int OcrDebugArtifactMaxUploadsPerSession = 3;
		private const string OcrDebugArtifactContentType = "application/zip";
#endif
		private readonly KycDocumentMrzScannerNavigationArgs? navigationArgs;
		private readonly IOcrScanService ocrScanService;
		private readonly IMrzPreviewAnalysisService previewAnalysisService;
		private readonly MrzFrameStabilityTracker frameStabilityTracker = new MrzFrameStabilityTracker();
		private readonly object scannerHintSyncRoot = new object();
		private readonly OcrDocumentKindHint preferredDocumentKindHint;
#if OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
		private readonly IOcrArtifactExportService ocrArtifactExportService;
#endif
		private CancellationTokenSource? previewCancellationTokenSource;
		private MrzPreviewAnalysisResult? lastPreviewAnalysisResult;
		private double cameraViewportWidth;
		private double cameraViewportHeight;
		private double previewFrameAspectRatio;
		private int isProcessingFrame;
#if DEBUG && OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
		private int isSharingDebugArtifacts;
#endif
		private TravelDocumentMrzResult? pendingSuccessfulResult;
		private MrzPreviewFrame? latestPreviewFrame;
		private bool isDisposed;
		private bool resultReturned;
		private ScannerHintKey? currentHintKey;
		private ScannerHintPriority currentHintPriority;
		private DateTimeOffset currentHintLockedUntil = DateTimeOffset.MinValue;
		private OutlineSnapshot? displayedOutlineSnapshot;
		private DateTimeOffset lastGoodOutlineTimestamp = DateTimeOffset.MinValue;
		private string unsupportedDocumentEvidenceKey = string.Empty;
		private int unsupportedDocumentEvidenceCount;
#if OCR_DEBUG_ARTIFACTS_TO_JID
		private int ocrDebugArtifactUploadCount;
		private int ocrDebugArtifactUploadInProgress;
		private int ocrDebugArtifactConfigurationWarningLogged;
#endif

		/// <summary>
		/// Initializes a new instance of the <see cref="KycDocumentMrzScannerViewModel"/> class.
		/// </summary>
		/// <param name="NavigationArgs">The navigation arguments.</param>
		public KycDocumentMrzScannerViewModel(KycDocumentMrzScannerNavigationArgs? NavigationArgs)
		{
			this.navigationArgs = NavigationArgs;
			this.ocrScanService = ServiceRef.Provider.GetRequiredService<IOcrScanService>();
			this.previewAnalysisService = ServiceRef.Provider.GetRequiredService<IMrzPreviewAnalysisService>();
			this.preferredDocumentKindHint = NavigationArgs?.PreferredDocumentKind ?? OcrDocumentKindHint.Unknown;
#if OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
			this.ocrArtifactExportService = ServiceRef.Provider.GetRequiredService<IOcrArtifactExportService>();
#endif
			this.SetInitialScannerHint();
		}

		/// <summary>
		/// Gets or sets the camera view used for preview.
		/// </summary>
		public CameraView? CameraView { get; set; }

		/// <summary>
		/// Gets the back action text.
		/// </summary>
		public string BackActionText => ServiceRef.Localizer["KycDocumentMrzScannerBackAction"];

		/// <summary>
		/// Gets the scanner title text.
		/// </summary>
		public string ScannerTitleText => ServiceRef.Localizer["KycDocumentMrzScannerTitle"];

		/// <summary>
		/// Gets or sets the main scanner status text.
		/// </summary>
		[ObservableProperty]
		private string statusText = string.Empty;

		/// <summary>
		/// Gets or sets the supporting scanner detail text.
		/// </summary>
		[ObservableProperty]
		private string detailText = string.Empty;

		/// <summary>
		/// Gets or sets the scanner hint card background color.
		/// </summary>
		[ObservableProperty]
		private Color hintBackgroundColor = Color.FromArgb("#DD1E1712");

		/// <summary>
		/// Gets or sets the scanner hint card stroke color.
		/// </summary>
		[ObservableProperty]
		private Color hintStrokeColor = Color.FromArgb("#33FFFFFF");

		/// <summary>
		/// Gets or sets the scanner hint accent color.
		/// </summary>
		[ObservableProperty]
		private Color hintAccentColor = Color.FromArgb("#FFF2E6");

		/// <summary>
		/// Gets or sets the scanner hint detail text color.
		/// </summary>
		[ObservableProperty]
		private Color hintDetailTextColor = Color.FromArgb("#FFF2E6");

		/// <summary>
		/// Gets or sets a value indicating whether the scanner hint should show progress.
		/// </summary>
		[ObservableProperty]
		private bool isHintProgressVisible;

		/// <summary>
		/// Gets or sets a value indicating whether a blocking validation overlay is visible.
		/// </summary>
		[ObservableProperty]
		private bool isValidationOverlayVisible;

		/// <summary>
		/// Gets or sets the blocking validation overlay title.
		/// </summary>
		[ObservableProperty]
		private string validationOverlayTitle = string.Empty;

		/// <summary>
		/// Gets or sets the blocking validation overlay detail text.
		/// </summary>
		[ObservableProperty]
		private string validationOverlayDetail = string.Empty;

		/// <summary>
		/// Gets the blocking validation overlay primary action text.
		/// </summary>
		public string ValidationOverlayPrimaryActionText => ServiceRef.Localizer["KycDocumentScanRetryAction"];

		/// <summary>
		/// Gets the blocking validation overlay secondary action text.
		/// </summary>
		public string ValidationOverlaySecondaryActionText => this.BackActionText;

		/// <summary>
		/// Gets or sets a value indicating whether a detected document outline is available.
		/// </summary>
		[ObservableProperty]
		private bool hasDocumentOutline;

		/// <summary>
		/// Gets or sets the top-left document outline point in page coordinates.
		/// </summary>
		[ObservableProperty]
		private Point documentOutlineTopLeft;

		/// <summary>
		/// Gets or sets the top-right document outline point in page coordinates.
		/// </summary>
		[ObservableProperty]
		private Point documentOutlineTopRight;

		/// <summary>
		/// Gets or sets the bottom-right document outline point in page coordinates.
		/// </summary>
		[ObservableProperty]
		private Point documentOutlineBottomRight;

		/// <summary>
		/// Gets or sets the bottom-left document outline point in page coordinates.
		/// </summary>
		[ObservableProperty]
		private Point documentOutlineBottomLeft;

		/// <summary>
		/// Gets or sets the current capture guidance state.
		/// </summary>
		[ObservableProperty]
		private MrzCaptureGuidanceState currentGuidanceState = MrzCaptureGuidanceState.FindPage;

		/// <summary>
		/// Gets a value indicating whether the debug artifact share button should be visible.
		/// </summary>
		public bool IsDebugArtifactShareVisible
		{
			get
			{
#if DEBUG && OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
				return true;
#else
				return false;
#endif
			}
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			this.RequestDocumentOutlineRefresh();
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
				this.navigationArgs?.CompletionSource?.TrySetResult(null);
				this.resultReturned = true;
				await ServiceRef.NavigationService.GoBackAsync();
				return;
			}

			this.DisposePreviewCancellation();
			this.previewCancellationTokenSource = new CancellationTokenSource();
			this.CameraView.Options = new CameraOptions
			{
				PreferRearCamera = true,
				FrameDeliveryInterval = TimeSpan.FromMilliseconds(150),
				PreviewScaling = CameraPreviewScaling.Fit,
				TargetFps = 15,
				TargetResolution = new Size(1920d, 1080d)
			};
			this.CameraView.FrameReady += this.CameraView_FrameReady;
			await this.CameraView.StartPreviewAsync(this.previewCancellationTokenSource.Token);
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			if (this.CameraView is not null)
			{
				this.CameraView.FrameReady -= this.CameraView_FrameReady;
				await this.CameraView.StopPreviewAsync();
			}

			this.DisposePreviewCancellation();
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			if (!this.resultReturned)
			{
				this.navigationArgs?.CompletionSource?.TrySetResult(null);
			}

			this.Dispose();
			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Releases resources owned by the scanner view model.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		[RelayCommand]
		private async Task CancelAsync()
		{
			this.navigationArgs?.CompletionSource?.TrySetResult(null);
			this.resultReturned = true;
			await ServiceRef.NavigationService.GoBackAsync();
		}

		[RelayCommand]
		private void DismissValidationOverlay()
		{
			if (!this.IsValidationOverlayVisible)
			{
				return;
			}

			this.HideValidationOverlay();
			this.ResetUnsupportedDocumentEvidence();
			this.frameStabilityTracker.Reset();
			this.ResetScannerHintLock();
			this.SetInitialScannerHint();
		}

		[RelayCommand]
		private async Task ShareDebugArtifactsAsync()
		{
#if DEBUG && OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
			if (Interlocked.CompareExchange(ref this.isSharingDebugArtifacts, 1, 0) != 0)
			{
				return;
			}

			try
			{
				MrzPreviewFrame? Frame = this.latestPreviewFrame;
				if (Frame is null)
				{
					this.SetScannerHint(
						ScannerHintKey.FindMrz,
						ServiceRef.Localizer["KycDocumentMrzScannerFindMrzStatus"],
						ServiceRef.Localizer["KycDocumentMrzScannerFindMrzDetail"],
						ScannerHintPriority.Guidance,
						MinimumGuidanceHintDuration);
					return;
				}

				Dictionary<string, string> DebugMetadata = new Dictionary<string, string>
				{
					["Flow"] = "KycDocumentScan",
					["DocumentMode"] = this.ResolveDocumentModeMetadata(),
					["DebugArtifactRequest"] = "ManualNativeShare"
				};
				AddCaptureEnvironmentMetadata(DebugMetadata, Frame);
				OcrScanRequest Request = new OcrScanRequest(
					Frame.Image,
					OcrScanTargetKind.Mrz,
					SourceKind: OcrScanSourceKind.PreviewFrame,
					ExpectedDocumentRegion: null,
					SourceRotationDegrees: Frame.SourceRotationDegrees,
					DocumentKindHint: this.ResolveDocumentKindHint(),
					DocumentSideHint: this.ResolveDocumentSideHint(),
					ScanMode: OcrScanMode.StillImage,
					CaptureDebugArtifacts: true,
					Metadata: DebugMetadata);

				OcrScanResult Result = await this.ocrScanService.ScanAsync(Request, CancellationToken.None);
				if (Result.ArtifactBundle is not null)
				{
					await MainThread.InvokeOnMainThreadAsync(async () =>
						await this.ocrArtifactExportService.ShareAsync(Result.ArtifactBundle, CancellationToken.None));
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				Interlocked.Exchange(ref this.isSharingDebugArtifacts, 0);
			}
#else
			await Task.CompletedTask;
#endif
		}

		[RelayCommand]
		private async Task SharePreviewDiagnosticsAsync()
		{
#if DEBUG && OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
			if (Interlocked.CompareExchange(ref this.isSharingDebugArtifacts, 1, 0) != 0)
			{
				return;
			}

			try
			{
				MrzPreviewFrame? Frame = this.latestPreviewFrame;
				MrzPreviewAnalysisResult? AnalysisResult = this.lastPreviewAnalysisResult;
				if (Frame is null || AnalysisResult is null)
				{
					this.SetScannerHint(
						ScannerHintKey.FindMrz,
						ServiceRef.Localizer["KycDocumentMrzScannerFindMrzStatus"],
						ServiceRef.Localizer["KycDocumentMrzScannerFindMrzDetail"],
						ScannerHintPriority.Guidance,
						MinimumGuidanceHintDuration);
					return;
				}

				OcrArtifactBundle Bundle = this.CreatePreviewDiagnosticsBundle(
					Frame,
					AnalysisResult,
					this.frameStabilityTracker.CreateDiagnostics());
				await MainThread.InvokeOnMainThreadAsync(async () =>
					await this.ocrArtifactExportService.ShareAsync(Bundle, CancellationToken.None));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				Interlocked.Exchange(ref this.isSharingDebugArtifacts, 0);
			}
#else
			await Task.CompletedTask;
#endif
		}

		internal void UpdateCameraViewportSize(double Width, double Height)
		{
			if (!double.IsFinite(Width) || !double.IsFinite(Height) || Width <= 0d || Height <= 0d)
			{
				return;
			}

			if (Math.Abs(this.cameraViewportWidth - Width) < 0.5d && Math.Abs(this.cameraViewportHeight - Height) < 0.5d)
			{
				return;
			}

			this.cameraViewportWidth = Width;
			this.cameraViewportHeight = Height;
			this.RequestDocumentOutlineRefresh();
		}

		internal void CompletePendingSuccessfulResult()
		{
			if (this.pendingSuccessfulResult is null)
			{
				this.LogScannerEvent(
					"CompletePendingSuccessfulResultSkipped",
					new KeyValuePair<string, object?>("HasCompletionSource", this.navigationArgs?.CompletionSource is not null));
				return;
			}

			bool Completed = this.navigationArgs?.CompletionSource?.TrySetResult(this.pendingSuccessfulResult) ?? false;
			this.LogScannerEvent(
				"CompletePendingSuccessfulResult",
				new KeyValuePair<string, object?>("Completed", Completed),
				new KeyValuePair<string, object?>("HasCompletionSource", this.navigationArgs?.CompletionSource is not null),
				new KeyValuePair<string, object?>("IsSuccessful", this.pendingSuccessfulResult.IsSuccessful),
				new KeyValuePair<string, object?>("NormalizedMrzLength", this.pendingSuccessfulResult.NormalizedMrzText.Length),
				new KeyValuePair<string, object?>("ChipAccessMrzLength", this.pendingSuccessfulResult.Document?.MRZ_Information?.Length ?? 0),
				new KeyValuePair<string, object?>("DocumentType", this.pendingSuccessfulResult.Document?.DocumentType ?? string.Empty));
			this.pendingSuccessfulResult = null;
		}

		private async void CameraView_FrameReady(object? Sender, CameraFrame Frame)
		{
			_ = Sender;
			if (this.resultReturned || this.IsValidationOverlayVisible || Interlocked.CompareExchange(ref this.isProcessingFrame, 1, 0) != 0)
			{
				return;
			}

			try
			{
				await this.ProcessFrameAsync(Frame, this.previewCancellationTokenSource?.Token ?? CancellationToken.None);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				Interlocked.Exchange(ref this.isProcessingFrame, 0);
			}
		}

		private async Task ProcessFrameAsync(CameraFrame Frame, CancellationToken CancellationToken)
		{
			if (this.IsValidationOverlayVisible)
			{
				return;
			}

			this.UpdatePreviewFrameAspectRatio(Frame);

			if (Frame.Format != CameraFrameFormat.Grayscale8)
			{
				this.ClearDocumentOutline();
				return;
			}

			Matrix<byte> Image = new Matrix<byte>(Frame.Width, Frame.Height, Frame.Buffer.ToArray());
			MrzPreviewFrame PreviewFrame = new MrzPreviewFrame(Image, Frame.RotationDegrees, Frame.Timestamp);
			this.latestPreviewFrame = PreviewFrame;
			OcrDocumentKindHint DocumentKindHint = this.ResolveDocumentKindHint();
			OcrDocumentSideHint DocumentSideHint = this.ResolveDocumentSideHint();
			Dictionary<string, string> PreviewMetadata = new Dictionary<string, string>
			{
				["Flow"] = "KycDocumentScan",
				["DocumentMode"] = this.ResolveDocumentModeMetadata()
			};
			AddCaptureEnvironmentMetadata(PreviewMetadata, Frame);
			MrzPreviewAnalysisRequest AnalysisRequest = new MrzPreviewAnalysisRequest(
				Image,
				Frame.RotationDegrees,
				null,
				DocumentKindHint,
				DocumentSideHint,
				PreviewMetadata);
			MrzPreviewAnalysisResult AnalysisResult = this.previewAnalysisService.Analyze(AnalysisRequest, CancellationToken);
			this.UpdateDocumentOutline(AnalysisResult);
			this.SetScannerTextForGuidance(AnalysisResult);
			this.frameStabilityTracker.Add(PreviewFrame, AnalysisResult);
			if (!this.frameStabilityTracker.TryGetCommitFrame(out MrzPreviewFrame? CommitFrame) || CommitFrame is null)
			{
				return;
			}

			if (this.IsValidationOverlayVisible)
			{
				return;
			}

			this.SetScannerHint(
				ScannerHintKey.Reading,
				ServiceRef.Localizer["KycDocumentMrzScannerConfirmingStatus"],
				ServiceRef.Localizer["KycDocumentMrzScannerKeepStillDetail"],
				ScannerHintPriority.Progress,
				MinimumProgressHintDuration);
			Dictionary<string, string> CommitMetadata = new Dictionary<string, string>
			{
				["Flow"] = "KycDocumentScan",
				["DocumentMode"] = this.ResolveDocumentModeMetadata(),
				["ScannerValidationOverlayVisible"] = this.IsValidationOverlayVisible.ToString(),
				["ScannerUnsupportedEvidenceCountBeforeCommit"] = this.unsupportedDocumentEvidenceCount.ToString(CultureInfo.InvariantCulture),
				["ScannerUnsupportedEvidenceKeyHashBeforeCommit"] = CreateEvidenceKeyDiagnosticHash(this.unsupportedDocumentEvidenceKey)
			};
			AddCaptureEnvironmentMetadata(CommitMetadata, CommitFrame);
			AddPreviewQuadMetadata(CommitMetadata, AnalysisResult);
			OcrScanRequest Request = new OcrScanRequest(
				CommitFrame.Image,
				OcrScanTargetKind.Mrz,
				SourceKind: OcrScanSourceKind.PreviewFrame,
				ExpectedDocumentRegion: null,
				SourceRotationDegrees: CommitFrame.SourceRotationDegrees,
				DocumentKindHint: DocumentKindHint,
				DocumentSideHint: DocumentSideHint,
				ScanMode: OcrScanMode.Commit,
#if OCR_DEBUG_ARTIFACTS_TO_JID
				CaptureDebugArtifacts: true,
#endif
				Metadata: CommitMetadata);

			OcrScanResult Result = await this.ocrScanService.ScanAsync(Request, CancellationToken);
			if (!Result.IsSuccessful || Result.Mrz?.Document is null)
			{
				this.ResetUnsupportedDocumentEvidence();
				this.AddScannerValidationMetadata(Result, ScannerMrzValidationOutcome.BadRead, false);
				this.WriteScannerValidationDiagnostics(Result, ScannerMrzValidationOutcome.BadRead, false);
				this.frameStabilityTracker.Reset();
				this.SetScannerTextForFailedCommit(Result);
				return;
			}

			ScannerMrzValidationOutcome ValidationOutcome = this.ClassifyScannerMrzValidation(Result, Result.Mrz.Document);
			if (ValidationOutcome == ScannerMrzValidationOutcome.Expired)
			{
				this.ResetUnsupportedDocumentEvidence();
				this.AddScannerValidationMetadata(Result, ValidationOutcome, true);
				this.WriteScannerValidationDiagnostics(Result, ValidationOutcome, true);
				this.frameStabilityTracker.Reset();
				this.ShowValidationOverlay(
					ServiceRef.Localizer["KycDocumentMrzScannerExpiredStatus"],
					ServiceRef.Localizer["KycDocumentMrzScannerExpiredDetail"]);
				return;
			}

			if (ValidationOutcome == ScannerMrzValidationOutcome.UnsupportedDocumentType)
			{
				bool ShouldShowUnsupportedOverlay = this.RegisterUnsupportedDocumentEvidence(Result.Mrz.Document);
				this.AddScannerValidationMetadata(Result, ValidationOutcome, ShouldShowUnsupportedOverlay);
				this.WriteScannerValidationDiagnostics(Result, ValidationOutcome, ShouldShowUnsupportedOverlay);
				this.frameStabilityTracker.Reset();
				if (ShouldShowUnsupportedOverlay)
				{
					this.ShowValidationOverlay(
						ServiceRef.Localizer["KycDocumentMrzScannerUnsupportedStatus"],
						ServiceRef.Localizer["KycDocumentMrzScannerUnsupportedDetail"]);
					return;
				}

				this.SetRecoverableBadReadHint();
				return;
			}

			if (ValidationOutcome == ScannerMrzValidationOutcome.BadRead)
			{
				this.ResetUnsupportedDocumentEvidence();
				this.AddScannerValidationMetadata(Result, ValidationOutcome, false);
				this.WriteScannerValidationDiagnostics(Result, ValidationOutcome, false);
				this.frameStabilityTracker.Reset();
				this.SetRecoverableBadReadHint();
				return;
			}

			this.ResetUnsupportedDocumentEvidence();
			this.AddScannerValidationMetadata(Result, ValidationOutcome, false);
			this.WriteScannerValidationDiagnostics(Result, ValidationOutcome, false);
			TravelDocumentMrzResult DocumentResult = new TravelDocumentMrzResult(Result, Result.Mrz.Document);
			if (!DocumentResult.IsSuccessful)
			{
				this.frameStabilityTracker.Reset();
				this.SetRecoverableBadReadHint();
				return;
			}

			this.resultReturned = true;
			this.pendingSuccessfulResult = DocumentResult;
			this.LogScannerEvent(
				"AcceptedMrzResult",
				new KeyValuePair<string, object?>("IsSuccessful", DocumentResult.IsSuccessful),
				new KeyValuePair<string, object?>("HasCompletionSource", this.navigationArgs?.CompletionSource is not null),
				new KeyValuePair<string, object?>("NormalizedMrzLength", DocumentResult.NormalizedMrzText.Length),
				new KeyValuePair<string, object?>("ChipAccessMrzLength", DocumentResult.Document?.MRZ_Information?.Length ?? 0),
				new KeyValuePair<string, object?>("DocumentType", DocumentResult.Document?.DocumentType ?? string.Empty));
			this.CompletePendingSuccessfulResult();
			this.LogScannerEvent("NavigateBackAfterAcceptedMrz");
			await MainThread.InvokeOnMainThreadAsync(async () => await ServiceRef.NavigationService.GoBackAsync());
		}

		private void LogScannerEvent(string EventName, params KeyValuePair<string, object?>[] Tags)
		{
			List<KeyValuePair<string, object?>> AllTags = new List<KeyValuePair<string, object?>>
			{
				new KeyValuePair<string, object?>("Event", EventName),
				new KeyValuePair<string, object?>("ResultReturned", this.resultReturned),
				new KeyValuePair<string, object?>("HasPendingSuccessfulResult", this.pendingSuccessfulResult is not null),
				new KeyValuePair<string, object?>("HasNavigationArgs", this.navigationArgs is not null)
			};
			AllTags.AddRange(Tags);
			ServiceRef.LogService.LogInformational("KYC MRZ scanner flow", AllTags.ToArray());
		}

#if DEBUG && OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
		private OcrArtifactBundle CreatePreviewDiagnosticsBundle(
			MrzPreviewFrame Frame,
			MrzPreviewAnalysisResult AnalysisResult,
			IReadOnlyDictionary<string, string> StabilityDiagnostics)
		{
			string SessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)
				+ "-preview-"
				+ Guid.NewGuid().ToString("N")[..8];
			string RootDirectory = Path.Combine(FileSystem.CacheDirectory, "ocr-preview-debug-sessions");
			string SessionDirectory = Path.Combine(RootDirectory, SessionId);
			Directory.CreateDirectory(SessionDirectory);

			string PreviewFramePath = Path.Combine(SessionDirectory, "preview-frame.png");
			File.WriteAllBytes(PreviewFramePath, Bitmaps.EncodeAsPng(Frame.Image));

			File.WriteAllText(
				Path.Combine(SessionDirectory, "preview-frame.json"),
				JsonSerializer.Serialize(
					new
					{
						Frame.Image.Width,
						Frame.Image.Height,
						Frame.SourceRotationDegrees,
						Frame.Timestamp,
						DocumentMode = this.ResolveDocumentModeMetadata(),
						this.StatusText,
						this.DetailText,
						this.cameraViewportWidth,
						this.cameraViewportHeight,
						this.previewFrameAspectRatio
					},
					PreviewDebugJsonOptions));

			File.WriteAllText(
				Path.Combine(SessionDirectory, "preview-analysis.json"),
				JsonSerializer.Serialize(
					new
					{
						AnalysisResult.GuidanceState,
						AnalysisResult.IsCommitReady,
						AnalysisResult.FailureCategory,
						AnalysisResult.MrzPlausibilityScore,
						AnalysisResult.QualityMetrics,
						DocumentQuadCandidate = CreateDocumentQuadDiagnostic(AnalysisResult.DocumentQuadCandidate),
						MrzCandidate = CreateMrzCandidateDiagnostic(AnalysisResult.MrzCandidate),
						AnalysisResult.Metadata
					},
					PreviewDebugJsonOptions));

			File.WriteAllText(
				Path.Combine(SessionDirectory, "stability.json"),
				JsonSerializer.Serialize(StabilityDiagnostics, PreviewDebugJsonOptions));

			string ManifestPath = Path.Combine(SessionDirectory, "manifest.json");
			File.WriteAllText(
				ManifestPath,
				JsonSerializer.Serialize(
					new
					{
						SessionId,
						CreatedUtc = DateTimeOffset.UtcNow,
						Type = "PreviewDiagnostics",
						Files = new[]
						{
							"preview-frame.png",
							"preview-frame.json",
							"preview-analysis.json",
							"stability.json"
						}
					},
					PreviewDebugJsonOptions));

			return new OcrArtifactBundle(SessionId, SessionDirectory, ManifestPath);
		}

		private static object? CreateDocumentQuadDiagnostic(DocumentQuadCandidate? Candidate)
		{
			if (Candidate is null)
			{
				return null;
			}

			return new
			{
				Quad = new
				{
					Candidate.Quad.TopLeft,
					Candidate.Quad.TopRight,
					Candidate.Quad.BottomRight,
					Candidate.Quad.BottomLeft
				},
				Candidate.Score,
				Candidate.EdgeSupportScore,
				Candidate.ContrastScore,
				Candidate.AreaScore,
				Candidate.FailureCategory,
				Candidate.Metadata
			};
		}

		private static object? CreateMrzCandidateDiagnostic(MrzRegionCandidate? Candidate)
		{
			if (Candidate is null)
			{
				return null;
			}

			return new
			{
				Candidate.Name,
				Candidate.Format,
				Candidate.Bounds,
				Candidate.Score,
				Candidate.Metadata,
				Lines = Candidate.Lines.Select(static Line => new
				{
					Line.Bounds,
					Line.BaselineY,
					Line.Score,
					Line.Metadata
				}).ToList()
			};
		}

#endif

#if OCR_DEBUG_ARTIFACTS_TO_JID
		private void QueueDebugArtifactSend(OcrScanResult Result)
		{
			ArgumentNullException.ThrowIfNull(Result);

			OcrArtifactBundle? Bundle = Result.ArtifactBundle;
			if (Bundle is null)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(OcrDebugArtifactRecipientJid))
			{
				if (Interlocked.CompareExchange(ref this.ocrDebugArtifactConfigurationWarningLogged, 1, 0) == 0)
				{
					ServiceRef.LogService.LogWarning("OCR debug artifact sending is enabled, but no recipient JID is configured.");
				}

				return;
			}

			if (this.ocrDebugArtifactUploadCount >= OcrDebugArtifactMaxUploadsPerSession ||
				Interlocked.CompareExchange(ref this.ocrDebugArtifactUploadInProgress, 1, 0) != 0)
			{
				return;
			}

			this.ocrDebugArtifactUploadCount++;
			_ = Task.Run(async () => await this.SendDebugArtifactAsync(Bundle, CancellationToken.None));
		}

		private async Task SendDebugArtifactAsync(OcrArtifactBundle Bundle, CancellationToken CancellationToken)
		{
			try
			{
				if (!ServiceRef.XmppService.IsOnline &&
					!await ServiceRef.XmppService.WaitForConnectedState(TimeSpan.FromSeconds(10)))
				{
					ServiceRef.LogService.LogWarning("Unable to send OCR debug artifacts because XMPP is not connected.");
					return;
				}

				if (!ServiceRef.XmppService.FileUploadIsSupported)
				{
					ServiceRef.LogService.LogWarning("Unable to send OCR debug artifacts because XMPP file upload is not supported.");
					return;
				}

				string ArchivePath = CreateDebugArtifactArchive(Bundle);
				byte[] ArchiveData = await File.ReadAllBytesAsync(ArchivePath, CancellationToken);
				string FileName = Path.GetFileName(ArchivePath);
				HttpFileUploadEventArgs Slot = await ServiceRef.XmppService.RequestUploadSlotAsync(
					FileName,
					OcrDebugArtifactContentType,
					ArchiveData.LongLength);

				if (!Slot.Ok)
				{
					ServiceRef.LogService.LogWarning(
						"OCR debug artifact upload slot request failed.",
						new KeyValuePair<string, object?>("Error", Slot.ErrorText));
					return;
				}

				await Slot.PUT(
					ArchiveData,
					OcrDebugArtifactContentType,
					(int)Constants.Timeouts.UploadFile.TotalMilliseconds);

				string Body = "OCR debug artifacts: " + Slot.GetUrl;
				ServiceRef.XmppService.SendMessage(
					QoSLevel.Unacknowledged,
					MessageType.Chat,
					Guid.NewGuid().ToString("N"),
					OcrDebugArtifactRecipientJid,
					string.Empty,
					Body,
					"OCR debug artifacts",
					string.Empty,
					string.Empty,
					string.Empty,
					null,
					null);

				ServiceRef.LogService.LogInformational(
					"OCR debug artifacts sent.",
					new KeyValuePair<string, object?>("RecipientJid", OcrDebugArtifactRecipientJid),
					new KeyValuePair<string, object?>("SessionId", Bundle.SessionId));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				Interlocked.Exchange(ref this.ocrDebugArtifactUploadInProgress, 0);
			}
		}

		private static string CreateDebugArtifactArchive(OcrArtifactBundle Bundle)
		{
			if (!Directory.Exists(Bundle.SessionDirectoryPath))
			{
				throw new DirectoryNotFoundException(Bundle.SessionDirectoryPath);
			}

			string ExportDirectory = Path.Combine(FileSystem.CacheDirectory, "ocr-debug-exports");
			Directory.CreateDirectory(ExportDirectory);
			string ArchivePath = Path.Combine(ExportDirectory, Bundle.SessionId + ".zip");
			if (File.Exists(ArchivePath))
			{
				File.Delete(ArchivePath);
			}

			ZipFile.CreateFromDirectory(Bundle.SessionDirectoryPath, ArchivePath, CompressionLevel.Fastest, false);
			return ArchivePath;
		}

#endif

		private void SetInitialScannerHint()
		{
			this.SetScannerHint(
				ScannerHintKey.Initial,
				ServiceRef.Localizer["KycDocumentMrzScannerInitialStatus"],
				ServiceRef.Localizer["KycDocumentMrzScannerInitialDetail"],
				ScannerHintPriority.Guidance,
				MinimumGuidanceHintDuration);
		}

		private void SetScannerHint(
			ScannerHintKey Key,
			string Status,
			string Detail,
			ScannerHintPriority Priority,
			TimeSpan MinimumVisibleDuration,
			bool IsSticky = false)
		{
			DateTimeOffset Now = DateTimeOffset.UtcNow;
			lock (this.scannerHintSyncRoot)
			{
				if (this.currentHintKey == Key)
				{
					DateTimeOffset RequestedLockUntil = IsSticky
						? DateTimeOffset.MaxValue
						: Now + MinimumVisibleDuration;
					if (RequestedLockUntil > this.currentHintLockedUntil)
					{
						this.currentHintLockedUntil = RequestedLockUntil;
					}

					return;
				}

				if (Now < this.currentHintLockedUntil && Priority <= this.currentHintPriority)
				{
					return;
				}

				this.currentHintKey = Key;
				this.currentHintPriority = Priority;
				this.currentHintLockedUntil = IsSticky
					? DateTimeOffset.MaxValue
					: Now + MinimumVisibleDuration;
			}

			this.SetScannerHintTone(ResolveScannerHintTone(Key, Priority));
			this.SetScannerText(Status, Detail);
		}

		private void ResetScannerHintLock()
		{
			lock (this.scannerHintSyncRoot)
			{
				this.currentHintKey = null;
				this.currentHintPriority = ScannerHintPriority.Guidance;
				this.currentHintLockedUntil = DateTimeOffset.MinValue;
			}
		}

		private static ScannerHintTone ResolveScannerHintTone(ScannerHintKey Key, ScannerHintPriority Priority)
		{
			if (Priority == ScannerHintPriority.Error)
			{
				return ScannerHintTone.Error;
			}

			if (Priority == ScannerHintPriority.Progress || Key == ScannerHintKey.HoldStill || Key == ScannerHintKey.Reading)
			{
				return ScannerHintTone.Progress;
			}

			return Priority == ScannerHintPriority.ImportantGuidance
				? ScannerHintTone.Attention
				: ScannerHintTone.Neutral;
		}

		private void SetScannerHintTone(ScannerHintTone Tone)
		{
			Action ApplyTone = () =>
			{
				switch (Tone)
				{
					case ScannerHintTone.Attention:
						this.HintBackgroundColor = Color.FromArgb("#E62A2016");
						this.HintStrokeColor = Color.FromArgb("#99F59E0B");
						this.HintAccentColor = Color.FromArgb("#FBBF24");
						this.HintDetailTextColor = Color.FromArgb("#FFF2E6");
						this.IsHintProgressVisible = false;
						break;

					case ScannerHintTone.Progress:
						this.HintBackgroundColor = Color.FromArgb("#E610202A");
						this.HintStrokeColor = Color.FromArgb("#887DD3FC");
						this.HintAccentColor = Color.FromArgb("#7DD3FC");
						this.HintDetailTextColor = Color.FromArgb("#E8F7FF");
						this.IsHintProgressVisible = true;
						break;

					case ScannerHintTone.Error:
						this.HintBackgroundColor = Color.FromArgb("#E6311111");
						this.HintStrokeColor = Color.FromArgb("#99F87171");
						this.HintAccentColor = Color.FromArgb("#F87171");
						this.HintDetailTextColor = Color.FromArgb("#FFE4E6");
						this.IsHintProgressVisible = false;
						break;

					default:
						this.HintBackgroundColor = Color.FromArgb("#DD1E1712");
						this.HintStrokeColor = Color.FromArgb("#33FFFFFF");
						this.HintAccentColor = Color.FromArgb("#FFF2E6");
						this.HintDetailTextColor = Color.FromArgb("#FFF2E6");
						this.IsHintProgressVisible = false;
						break;
				}
			};

			if (MainThread.IsMainThread)
			{
				ApplyTone();
				return;
			}

			MainThread.BeginInvokeOnMainThread(ApplyTone);
		}

		private void SetScannerText(string Status, string Detail)
		{
			if (MainThread.IsMainThread)
			{
				this.StatusText = Status;
				this.DetailText = Detail;
				return;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.StatusText = Status;
				this.DetailText = Detail;
			});
		}

		private void SetScannerTextForGuidance(MrzPreviewAnalysisResult Result)
		{
			ScannerHintKey HintKey;
			string Status;
			string Detail;
			ScannerHintPriority Priority = ScannerHintPriority.Guidance;
			TimeSpan MinimumVisibleDuration = MinimumGuidanceHintDuration;
			switch (Result.GuidanceState)
			{
				case MrzCaptureGuidanceState.ReduceGlare:
					HintKey = ScannerHintKey.ReduceGlare;
					Status = ServiceRef.Localizer["KycDocumentMrzScannerGlareStatus"];
					Detail = ServiceRef.Localizer["KycDocumentMrzScannerGlareDetail"];
					Priority = ScannerHintPriority.ImportantGuidance;
					MinimumVisibleDuration = MinimumImportantGuidanceHintDuration;
					break;

				case MrzCaptureGuidanceState.MoveCloser:
					HintKey = ScannerHintKey.MoveCloser;
					Status = ServiceRef.Localizer["KycDocumentMrzScannerMoveCloserStatus"];
					Detail = ServiceRef.Localizer["KycDocumentMrzScannerMoveCloserDetail"];
					Priority = ScannerHintPriority.ImportantGuidance;
					MinimumVisibleDuration = MinimumImportantGuidanceHintDuration;
					break;

				case MrzCaptureGuidanceState.FitAllCorners:
				case MrzCaptureGuidanceState.FindPage:
					HintKey = ScannerHintKey.FindMrz;
					Status = ServiceRef.Localizer["KycDocumentMrzScannerFindMrzStatus"];
					Detail = ServiceRef.Localizer["KycDocumentMrzScannerFindMrzDetail"];
					break;

				case MrzCaptureGuidanceState.HoldSteady:
					HintKey = ScannerHintKey.HoldStill;
					Status = ServiceRef.Localizer["KycDocumentMrzScannerConfirmingStatus"];
					Detail = ServiceRef.Localizer["KycDocumentMrzScannerOneMoreDetail"];
					Priority = ScannerHintPriority.Progress;
					MinimumVisibleDuration = MinimumProgressHintDuration;
					break;

				case MrzCaptureGuidanceState.Capturing:
				case MrzCaptureGuidanceState.Review:
					HintKey = ScannerHintKey.Reading;
					Status = ServiceRef.Localizer["KycDocumentMrzScannerConfirmingStatus"];
					Detail = ServiceRef.Localizer["KycDocumentMrzScannerKeepStillDetail"];
					Priority = ScannerHintPriority.Progress;
					MinimumVisibleDuration = MinimumProgressHintDuration;
					break;

				default:
					HintKey = ScannerHintKey.FindMrz;
					Status = ServiceRef.Localizer["KycDocumentMrzScannerFindMrzStatus"];
					Detail = ServiceRef.Localizer["KycDocumentMrzScannerFindMrzDetail"];
					break;
			}

			this.SetScannerHint(HintKey, Status, Detail, Priority, MinimumVisibleDuration);
		}

		private void SetRecoverableBadReadHint()
		{
			this.SetScannerHint(
				ScannerHintKey.TransientFailure,
				ServiceRef.Localizer["KycDocumentMrzScannerTryAgainStatus"],
				ServiceRef.Localizer["KycDocumentMrzScannerFindMrzDetail"],
				ScannerHintPriority.ImportantGuidance,
				MinimumTransientFailureHintDuration);
		}

		private void SetScannerTextForFailedCommit(OcrScanResult Result)
		{
			string FailureCategory = Result.Metadata.TryGetValue("MrzFailureCategory", out string? Value) ? Value : string.Empty;
			ScannerHintKey HintKey = FailureCategory switch
			{
				nameof(MrzScanFailureCategory.GlareInMrzArea) => ScannerHintKey.ReduceGlare,
				nameof(MrzScanFailureCategory.DocumentTooBlurred) => ScannerHintKey.MoveCloser,
				_ => ScannerHintKey.TransientFailure
			};
			string Status = FailureCategory switch
			{
				nameof(MrzScanFailureCategory.GlareInMrzArea) => ServiceRef.Localizer["KycDocumentMrzScannerGlareStatus"],
				nameof(MrzScanFailureCategory.DocumentTooBlurred) => ServiceRef.Localizer["KycDocumentMrzScannerMoveCloserStatus"],
				_ => ServiceRef.Localizer["KycDocumentMrzScannerTryAgainStatus"]
			};
			string Detail = FailureCategory switch
			{
				nameof(MrzScanFailureCategory.GlareInMrzArea) => ServiceRef.Localizer["KycDocumentMrzScannerGlareDetail"],
				nameof(MrzScanFailureCategory.DocumentTooBlurred) => ServiceRef.Localizer["KycDocumentMrzScannerMoveCloserDetail"],
				nameof(MrzScanFailureCategory.DocumentNotFound) => ServiceRef.Localizer["KycDocumentMrzScannerFindMrzDetail"],
				_ => ServiceRef.Localizer["KycDocumentMrzScannerFindMrzDetail"]
			};

			this.SetScannerHint(
				HintKey,
				Status,
				Detail,
				ScannerHintPriority.ImportantGuidance,
				MinimumTransientFailureHintDuration);
		}

		private ScannerMrzValidationOutcome ClassifyScannerMrzValidation(OcrScanResult Result, DocumentInformation Document)
		{
			if (Result.ValidationStatus != OcrScanValidationStatus.Succeeded)
			{
				return ScannerMrzValidationOutcome.BadRead;
			}

			if (!TryNormalizeDocumentType(Document.DocumentType, out string NormalizedDocumentType))
			{
				return ScannerMrzValidationOutcome.BadRead;
			}

			if (!IsSupportedScannerDocumentType(NormalizedDocumentType))
			{
				return HasUnsupportedDocumentEvidenceFields(Document)
					? ScannerMrzValidationOutcome.UnsupportedDocumentType
					: ScannerMrzValidationOutcome.BadRead;
			}

			if (!HasRequiredScannerDocumentFields(Document))
			{
				return ScannerMrzValidationOutcome.BadRead;
			}

			DateTime? BirthDate = ParseScannerMrzDate(Document.DateOfBirth, true);
			DateTime? ExpiryDate = ParseScannerMrzDate(Document.ExpiryDate, false);
			if (!BirthDate.HasValue || !ExpiryDate.HasValue)
			{
				return ScannerMrzValidationOutcome.BadRead;
			}

			return ExpiryDate.Value.Date < DateTime.UtcNow.Date
				? ScannerMrzValidationOutcome.Expired
				: ScannerMrzValidationOutcome.Accepted;
		}

		private bool RegisterUnsupportedDocumentEvidence(DocumentInformation Document)
		{
			string EvidenceKey = CreateUnsupportedDocumentEvidenceKey(Document);
			if (string.IsNullOrWhiteSpace(EvidenceKey))
			{
				this.ResetUnsupportedDocumentEvidence();
				return false;
			}

			if (string.Equals(this.unsupportedDocumentEvidenceKey, EvidenceKey, StringComparison.Ordinal))
			{
				this.unsupportedDocumentEvidenceCount++;
			}
			else
			{
				this.unsupportedDocumentEvidenceKey = EvidenceKey;
				this.unsupportedDocumentEvidenceCount = 1;
			}

			return this.unsupportedDocumentEvidenceCount >= UnsupportedDocumentEvidenceThreshold;
		}

		private void ResetUnsupportedDocumentEvidence()
		{
			this.unsupportedDocumentEvidenceKey = string.Empty;
			this.unsupportedDocumentEvidenceCount = 0;
		}

		private static string CreateUnsupportedDocumentEvidenceKey(DocumentInformation Document)
		{
			if (!TryNormalizeDocumentType(Document.DocumentType, out string NormalizedDocumentType))
			{
				return string.Empty;
			}

			string DocumentNumber = NormalizeEvidenceField(Document.DocumentNumber);
			string BirthDate = NormalizeEvidenceField(Document.DateOfBirth);
			string ExpiryDate = NormalizeEvidenceField(Document.ExpiryDate);
			string Nationality = NormalizeEvidenceField(Document.Nationality);
			if (string.IsNullOrWhiteSpace(DocumentNumber)
				|| string.IsNullOrWhiteSpace(BirthDate)
				|| string.IsNullOrWhiteSpace(ExpiryDate)
				|| string.IsNullOrWhiteSpace(Nationality))
			{
				return string.Empty;
			}

			return string.Join("|", NormalizedDocumentType, DocumentNumber, BirthDate, ExpiryDate, Nationality);
		}

		private static bool HasRequiredScannerDocumentFields(DocumentInformation Document)
		{
			return !string.IsNullOrWhiteSpace(Document.MRZ_Information) &&
				!string.IsNullOrWhiteSpace(Document.DocumentNumber) &&
				!string.IsNullOrWhiteSpace(Document.DateOfBirth) &&
				!string.IsNullOrWhiteSpace(Document.ExpiryDate) &&
				!string.IsNullOrWhiteSpace(Document.Nationality) &&
				((Document.PrimaryIdentifier?.Length ?? 0) > 0 || (Document.SecondaryIdentifier?.Length ?? 0) > 0);
		}

		private static bool HasUnsupportedDocumentEvidenceFields(DocumentInformation Document)
		{
			return !string.IsNullOrWhiteSpace(Document.DocumentNumber) &&
				!string.IsNullOrWhiteSpace(Document.DateOfBirth) &&
				!string.IsNullOrWhiteSpace(Document.ExpiryDate) &&
				!string.IsNullOrWhiteSpace(Document.Nationality);
		}

		private static bool IsSupportedScannerDocumentType(string NormalizedDocumentType)
		{
			return NormalizedDocumentType.StartsWith("P", StringComparison.Ordinal) ||
				NormalizedDocumentType.StartsWith("I", StringComparison.Ordinal);
		}

		private static bool TryNormalizeDocumentType(string? DocumentType, out string NormalizedDocumentType)
		{
			NormalizedDocumentType = NormalizeEvidenceField(DocumentType);
			return !string.IsNullOrWhiteSpace(NormalizedDocumentType);
		}

		private static string NormalizeEvidenceField(string? Value)
		{
			return string.IsNullOrWhiteSpace(Value)
				? string.Empty
				: Value.Trim().ToUpperInvariant();
		}

		private static DateTime? ParseScannerMrzDate(string? Value, bool AssumeBirthDate)
		{
			if (string.IsNullOrWhiteSpace(Value) || Value.Length != 6)
			{
				return null;
			}

			if (!int.TryParse(Value[..2], out int Year) ||
				!int.TryParse(Value.Substring(2, 2), out int Month) ||
				!int.TryParse(Value.Substring(4, 2), out int Day))
			{
				return null;
			}

			DateTime Candidate;
			try
			{
				Candidate = new DateTime(2000 + Year, Month, Day, 0, 0, 0, DateTimeKind.Utc);
			}
			catch
			{
				return null;
			}

			if (AssumeBirthDate && Candidate.Date > DateTime.UtcNow.Date)
			{
				Candidate = Candidate.AddYears(-100);
			}

			return Candidate;
		}

		private void ShowValidationOverlay(string Title, string Detail)
		{
			void ApplyOverlay()
			{
				this.ValidationOverlayTitle = Title;
				this.ValidationOverlayDetail = Detail;
				this.IsValidationOverlayVisible = true;
			}

			if (MainThread.IsMainThread)
			{
				ApplyOverlay();
				return;
			}

			MainThread.BeginInvokeOnMainThread(ApplyOverlay);
		}

		private void HideValidationOverlay()
		{
			if (MainThread.IsMainThread)
			{
				this.IsValidationOverlayVisible = false;
				return;
			}

			MainThread.BeginInvokeOnMainThread(() => this.IsValidationOverlayVisible = false);
		}

		private void AddScannerValidationMetadata(OcrScanResult Result, ScannerMrzValidationOutcome Outcome, bool OverlayShown)
		{
			if (Result.Metadata is not IDictionary<string, string> Metadata)
			{
				return;
			}

			Metadata["ScannerValidationUiOutcome"] = Outcome.ToString();
			Metadata["ScannerValidationOverlayShown"] = OverlayShown.ToString();
			Metadata["ScannerUnsupportedEvidenceCount"] = this.unsupportedDocumentEvidenceCount.ToString(CultureInfo.InvariantCulture);
			Metadata["ScannerUnsupportedEvidenceKeyHash"] = CreateEvidenceKeyDiagnosticHash(this.unsupportedDocumentEvidenceKey);
		}

		private void WriteScannerValidationDiagnostics(OcrScanResult Result, ScannerMrzValidationOutcome Outcome, bool OverlayShown)
		{
			if (Result.ArtifactBundle is null)
			{
				return;
			}

			try
			{
				string DiagnosticsPath = Path.Combine(Result.ArtifactBundle.SessionDirectoryPath, "scanner-validation-ui.json");
				File.WriteAllText(
					DiagnosticsPath,
					JsonSerializer.Serialize(new
					{
						Outcome,
						OverlayShown,
						this.unsupportedDocumentEvidenceCount,
						UnsupportedEvidenceKeyHash = CreateEvidenceKeyDiagnosticHash(this.unsupportedDocumentEvidenceKey),
						this.IsValidationOverlayVisible
					}));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

#if OCR_DEBUG_ARTIFACTS_TO_JID
			this.QueueDebugArtifactSend(Result);
#endif
		}

		private static string CreateEvidenceKeyDiagnosticHash(string EvidenceKey)
		{
			if (string.IsNullOrWhiteSpace(EvidenceKey))
			{
				return string.Empty;
			}

			byte[] Bytes = System.Text.Encoding.UTF8.GetBytes(EvidenceKey);
			byte[] Hash = System.Security.Cryptography.SHA256.HashData(Bytes);
			return Convert.ToHexString(Hash);
		}

		private string ResolveDocumentModeMetadata()
		{
			return this.preferredDocumentKindHint switch
			{
				OcrDocumentKindHint.Passport => "Passport",
				OcrDocumentKindHint.IdentityCard => "IdentityCard",
				_ => "Unknown"
			};
		}

		private OcrDocumentKindHint ResolveDocumentKindHint()
		{
			return this.preferredDocumentKindHint;
		}

		private OcrDocumentSideHint ResolveDocumentSideHint()
		{
			return this.preferredDocumentKindHint == OcrDocumentKindHint.Passport
				? OcrDocumentSideHint.BiodataPage
				: OcrDocumentSideHint.Unknown;
		}

		private void UpdatePreviewFrameAspectRatio(CameraFrame Frame)
		{
			double Width = Frame.RotationDegrees is 90 or 270 ? Frame.Height : Frame.Width;
			double Height = Frame.RotationDegrees is 90 or 270 ? Frame.Width : Frame.Height;
			if (Width <= 0d || Height <= 0d)
			{
				return;
			}

			double FrameAspectRatio = Width / Height;
			if (Math.Abs(this.previewFrameAspectRatio - FrameAspectRatio) < 0.01d)
			{
				return;
			}

			this.previewFrameAspectRatio = FrameAspectRatio;
			this.RequestDocumentOutlineRefresh();
		}

		private void RequestDocumentOutlineRefresh()
		{
			if (MainThread.IsMainThread)
			{
				if (this.lastPreviewAnalysisResult is not null)
				{
					this.SetDocumentOutlineCore(this.lastPreviewAnalysisResult);
				}

				return;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (this.lastPreviewAnalysisResult is not null)
				{
					this.SetDocumentOutlineCore(this.lastPreviewAnalysisResult);
				}
			});
		}

		private void UpdateDocumentOutline(MrzPreviewAnalysisResult Result)
		{
			this.lastPreviewAnalysisResult = Result;
			if (MainThread.IsMainThread)
			{
				this.SetDocumentOutlineCore(Result);
				return;
			}

			MainThread.BeginInvokeOnMainThread(() => this.SetDocumentOutlineCore(Result));
		}

		private void SetDocumentOutlineCore(MrzPreviewAnalysisResult Result)
		{
			this.CurrentGuidanceState = Result.GuidanceState;

			if (Result.DocumentQuadCandidate is null
				|| !TryGetMetadataInt(Result.Metadata, "MrzPreviewWidth", out int PreviewWidth)
				|| !TryGetMetadataInt(Result.Metadata, "MrzPreviewHeight", out int PreviewHeight)
				|| !this.TryResolvePreviewContentBounds(out Microsoft.Maui.Graphics.Rect PreviewBounds))
			{
				this.PreserveRecentOutlineOrHide();
				return;
			}

			if (Result.DocumentQuadCandidate.FailureCategory != MrzScanFailureCategory.None
				|| Result.DocumentQuadCandidate.Score < MinimumVisualDocumentQuadScore
				|| IsUnsafePreviewQuad(Result.Metadata))
			{
				this.PreserveRecentOutlineOrHide();
				return;
			}

			DocumentQuad Quad = Result.DocumentQuadCandidate.Quad;
			Point TargetTopLeft = MapPreviewPoint(Quad.TopLeft, PreviewWidth, PreviewHeight, PreviewBounds);
			Point TargetTopRight = MapPreviewPoint(Quad.TopRight, PreviewWidth, PreviewHeight, PreviewBounds);
			Point TargetBottomRight = MapPreviewPoint(Quad.BottomRight, PreviewWidth, PreviewHeight, PreviewBounds);
			Point TargetBottomLeft = MapPreviewPoint(Quad.BottomLeft, PreviewWidth, PreviewHeight, PreviewBounds);
			string Source = Result.DocumentQuadCandidate.Metadata.TryGetValue("DocumentQuadSource", out string? ParsedSource)
				? ParsedSource
				: string.Empty;
			OutlineSnapshot TargetOutline = CreateOutlineSnapshot(
				TargetTopLeft,
				TargetTopRight,
				TargetBottomRight,
				TargetBottomLeft,
				Source);

			if (this.HasDocumentOutline
				&& this.displayedOutlineSnapshot.HasValue
				&& !IsCompatibleOutline(this.displayedOutlineSnapshot.Value, TargetOutline))
			{
				this.PreserveRecentOutlineOrHide();
				return;
			}

			if (this.HasDocumentOutline)
			{
				double SmoothingRatio = ResolveOutlineSmoothingRatio(this.displayedOutlineSnapshot, TargetOutline);
				this.DocumentOutlineTopLeft = SmoothPoint(this.DocumentOutlineTopLeft, TargetTopLeft, SmoothingRatio);
				this.DocumentOutlineTopRight = SmoothPoint(this.DocumentOutlineTopRight, TargetTopRight, SmoothingRatio);
				this.DocumentOutlineBottomRight = SmoothPoint(this.DocumentOutlineBottomRight, TargetBottomRight, SmoothingRatio);
				this.DocumentOutlineBottomLeft = SmoothPoint(this.DocumentOutlineBottomLeft, TargetBottomLeft, SmoothingRatio);
			}
			else
			{
				this.DocumentOutlineTopLeft = TargetTopLeft;
				this.DocumentOutlineTopRight = TargetTopRight;
				this.DocumentOutlineBottomRight = TargetBottomRight;
				this.DocumentOutlineBottomLeft = TargetBottomLeft;
			}

			this.displayedOutlineSnapshot = CreateOutlineSnapshot(
				this.DocumentOutlineTopLeft,
				this.DocumentOutlineTopRight,
				this.DocumentOutlineBottomRight,
				this.DocumentOutlineBottomLeft,
				Source);
			this.lastGoodOutlineTimestamp = DateTimeOffset.UtcNow;
			this.HasDocumentOutline = true;
		}

		private void ClearDocumentOutline()
		{
			this.lastPreviewAnalysisResult = null;
			this.displayedOutlineSnapshot = null;
			this.lastGoodOutlineTimestamp = DateTimeOffset.MinValue;
			if (MainThread.IsMainThread)
			{
				this.HasDocumentOutline = false;
				this.CurrentGuidanceState = MrzCaptureGuidanceState.FindPage;
				return;
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.HasDocumentOutline = false;
				this.CurrentGuidanceState = MrzCaptureGuidanceState.FindPage;
			});
		}

		private bool TryResolvePreviewContentBounds(out Microsoft.Maui.Graphics.Rect PreviewBounds)
		{
			double ViewportWidth = this.cameraViewportWidth;
			double ViewportHeight = this.cameraViewportHeight;
			if (ViewportWidth <= 0d || ViewportHeight <= 0d)
			{
				PreviewBounds = Microsoft.Maui.Graphics.Rect.Zero;
				return false;
			}

			double PreviewAspectRatio = this.previewFrameAspectRatio > 0d ? this.previewFrameAspectRatio : ViewportWidth / ViewportHeight;
			double PreviewContentWidth = ViewportWidth;
			double PreviewContentHeight = PreviewContentWidth / PreviewAspectRatio;
			if (PreviewContentHeight > ViewportHeight)
			{
				PreviewContentHeight = ViewportHeight;
				PreviewContentWidth = PreviewContentHeight * PreviewAspectRatio;
			}

			double PreviewLeft = (ViewportWidth - PreviewContentWidth) * 0.5d;
			double PreviewTop = (ViewportHeight - PreviewContentHeight) * 0.5d;
			PreviewBounds = new Microsoft.Maui.Graphics.Rect(PreviewLeft, PreviewTop, PreviewContentWidth, PreviewContentHeight);
			return true;
		}

		private void PreserveRecentOutlineOrHide()
		{
			if (this.HasDocumentOutline && DateTimeOffset.UtcNow - this.lastGoodOutlineTimestamp <= OutlineGracePeriod)
			{
				return;
			}

			this.displayedOutlineSnapshot = null;
			this.lastGoodOutlineTimestamp = DateTimeOffset.MinValue;
			this.HasDocumentOutline = false;
		}

		private static bool IsUnsafePreviewQuad(IReadOnlyDictionary<string, string> Metadata)
		{
			return Metadata.TryGetValue("MrzPreviewQuadRectificationSafe", out string? Text)
				&& bool.TryParse(Text, out bool IsSafe)
				&& !IsSafe;
		}

		private static OutlineSnapshot CreateOutlineSnapshot(
			Point TopLeft,
			Point TopRight,
			Point BottomRight,
			Point BottomLeft,
			string Source)
		{
			double Width = (Distance(TopLeft, TopRight) + Distance(BottomLeft, BottomRight)) * 0.5d;
			double Height = (Distance(TopLeft, BottomLeft) + Distance(TopRight, BottomRight)) * 0.5d;
			double CenterX = (TopLeft.X + TopRight.X + BottomRight.X + BottomLeft.X) * 0.25d;
			double CenterY = (TopLeft.Y + TopRight.Y + BottomRight.Y + BottomLeft.Y) * 0.25d;
			return new OutlineSnapshot(
				TopLeft,
				TopRight,
				BottomRight,
				BottomLeft,
				Source,
				CenterX,
				CenterY,
				Width,
				Height,
				Width / Math.Max(1d, Height));
		}

		private static bool IsCompatibleOutline(OutlineSnapshot Current, OutlineSnapshot Target)
		{
			double Diagonal = Math.Sqrt((Current.Width * Current.Width) + (Current.Height * Current.Height));
			double CenterRatio = Distance(Current.CenterX, Current.CenterY, Target.CenterX, Target.CenterY) / Math.Max(1d, Diagonal);
			double WidthRatio = Math.Abs(Target.Width - Current.Width) / Math.Max(1d, Current.Width);
			double HeightRatio = Math.Abs(Target.Height - Current.Height) / Math.Max(1d, Current.Height);
			double AspectRatio = Math.Abs(Target.AspectRatio - Current.AspectRatio) / Math.Max(0.1d, Current.AspectRatio);
			bool IsSourceSwitch = !string.Equals(Current.Source, Target.Source, StringComparison.Ordinal);
			double AllowedCenterRatio = IsSourceSwitch ? OutlineSourceSwitchCenterRatio : OutlineCompatibilityCenterRatio;
			double AllowedSizeRatio = IsSourceSwitch ? OutlineSourceSwitchSizeRatio : OutlineCompatibilitySizeRatio;
			double AllowedAspectRatio = IsSourceSwitch ? OutlineSourceSwitchAspectRatio : OutlineCompatibilityAspectRatio;

			return CenterRatio <= AllowedCenterRatio
				&& WidthRatio <= AllowedSizeRatio
				&& HeightRatio <= AllowedSizeRatio
				&& AspectRatio <= AllowedAspectRatio;
		}

		private static Point MapPreviewPoint(IdApp.Cv.Basic.Point SourcePoint, int PreviewWidth, int PreviewHeight, Microsoft.Maui.Graphics.Rect PreviewBounds)
		{
			double NormalizedX = SourcePoint.X / (double)Math.Max(1, PreviewWidth);
			double NormalizedY = SourcePoint.Y / (double)Math.Max(1, PreviewHeight);
			return new Point(
				PreviewBounds.Left + (NormalizedX * PreviewBounds.Width),
				PreviewBounds.Top + (NormalizedY * PreviewBounds.Height));
		}

		private static Point SmoothPoint(Point Current, Point Target, double Ratio)
		{
			return new Point(
				Current.X + ((Target.X - Current.X) * Ratio),
				Current.Y + ((Target.Y - Current.Y) * Ratio));
		}

		private static double ResolveOutlineSmoothingRatio(OutlineSnapshot? Current, OutlineSnapshot Target)
		{
			if (!Current.HasValue)
				return 1d;

			OutlineSnapshot CurrentValue = Current.Value;
			double Diagonal = Math.Sqrt((CurrentValue.Width * CurrentValue.Width) + (CurrentValue.Height * CurrentValue.Height));
			double CenterRatio = Distance(CurrentValue.CenterX, CurrentValue.CenterY, Target.CenterX, Target.CenterY) / Math.Max(1d, Diagonal);
			if (CenterRatio < 0.018d)
				return 0.55d;
			if (CenterRatio < 0.060d)
				return 0.75d;

			return OutlineSmoothingRatio;
		}

		private static double Distance(Point Left, Point Right)
		{
			return Distance(Left.X, Left.Y, Right.X, Right.Y);
		}

		private static double Distance(double LeftX, double LeftY, double RightX, double RightY)
		{
			double dx = LeftX - RightX;
			double dy = LeftY - RightY;
			return Math.Sqrt((dx * dx) + (dy * dy));
		}

		private static void AddPreviewQuadMetadata(Dictionary<string, string> Metadata, MrzPreviewAnalysisResult AnalysisResult)
		{
			DocumentQuadCandidate? Candidate = AnalysisResult.DocumentQuadCandidate;
			if (Candidate is null
				|| !AnalysisResult.Metadata.TryGetValue("MrzPreviewWidth", out string? PreviewWidth)
				|| !AnalysisResult.Metadata.TryGetValue("MrzPreviewHeight", out string? PreviewHeight))
			{
				return;
			}

			Metadata["CommitPreviewQuadAvailable"] = true.ToString();
			Metadata["CommitPreviewWidth"] = PreviewWidth;
			Metadata["CommitPreviewHeight"] = PreviewHeight;
			Metadata["CommitPreviewQuadTopLeft"] = Candidate.Quad.TopLeft.X.ToString(CultureInfo.InvariantCulture) + "," + Candidate.Quad.TopLeft.Y.ToString(CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadTopRight"] = Candidate.Quad.TopRight.X.ToString(CultureInfo.InvariantCulture) + "," + Candidate.Quad.TopRight.Y.ToString(CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadBottomRight"] = Candidate.Quad.BottomRight.X.ToString(CultureInfo.InvariantCulture) + "," + Candidate.Quad.BottomRight.Y.ToString(CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadBottomLeft"] = Candidate.Quad.BottomLeft.X.ToString(CultureInfo.InvariantCulture) + "," + Candidate.Quad.BottomLeft.Y.ToString(CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadScore"] = Candidate.Score.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadEdgeSupportScore"] = Candidate.EdgeSupportScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadContrastScore"] = Candidate.ContrastScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadAreaScore"] = Candidate.AreaScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata["CommitPreviewQuadSource"] = Candidate.Metadata.TryGetValue("DocumentQuadSource", out string? Source) ? Source : string.Empty;
		}

		private static void AddCaptureEnvironmentMetadata(Dictionary<string, string> Metadata, CameraFrame Frame)
		{
			Metadata["CaptureDeviceManufacturer"] = DeviceInfo.Current.Manufacturer;
			Metadata["CaptureDeviceModel"] = DeviceInfo.Current.Model;
			Metadata["CaptureDevicePlatform"] = DeviceInfo.Current.Platform.ToString();
			Metadata["CaptureDeviceVersion"] = DeviceInfo.Current.VersionString;
			Metadata["CaptureFrameWidth"] = Frame.Width.ToString(CultureInfo.InvariantCulture);
			Metadata["CaptureFrameHeight"] = Frame.Height.ToString(CultureInfo.InvariantCulture);
			Metadata["CaptureFrameFormat"] = Frame.Format.ToString();
			Metadata["CaptureFrameRotationDegrees"] = Frame.RotationDegrees.ToString(CultureInfo.InvariantCulture);
			Metadata["CaptureFrameTimestampUtc"] = Frame.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
		}

		private static void AddCaptureEnvironmentMetadata(Dictionary<string, string> Metadata, MrzPreviewFrame Frame)
		{
			Metadata["CaptureDeviceManufacturer"] = DeviceInfo.Current.Manufacturer;
			Metadata["CaptureDeviceModel"] = DeviceInfo.Current.Model;
			Metadata["CaptureDevicePlatform"] = DeviceInfo.Current.Platform.ToString();
			Metadata["CaptureDeviceVersion"] = DeviceInfo.Current.VersionString;
			Metadata["CaptureFrameWidth"] = Frame.Image.Width.ToString(CultureInfo.InvariantCulture);
			Metadata["CaptureFrameHeight"] = Frame.Image.Height.ToString(CultureInfo.InvariantCulture);
			Metadata["CaptureFrameFormat"] = CameraFrameFormat.Grayscale8.ToString();
			Metadata["CaptureFrameRotationDegrees"] = Frame.SourceRotationDegrees.ToString(CultureInfo.InvariantCulture);
			Metadata["CaptureFrameTimestampUtc"] = Frame.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
		}

		private static bool TryGetMetadataInt(IReadOnlyDictionary<string, string> Metadata, string Key, out int Value)
		{
			if (Metadata.TryGetValue(Key, out string? Text) &&
				int.TryParse(Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out Value))
			{
				return true;
			}

			Value = 0;
			return false;
		}

		/// <summary>
		/// Releases resources owned by the scanner view model.
		/// </summary>
		/// <param name="Disposing">True when managed resources should be released.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
			{
				return;
			}

			if (Disposing)
			{
				this.DisposePreviewCancellation();
			}

			this.isDisposed = true;
		}

		private void DisposePreviewCancellation()
		{
			CancellationTokenSource? Cancellation = this.previewCancellationTokenSource;
			this.previewCancellationTokenSource = null;
			if (Cancellation is null)
			{
				return;
			}

			try
			{
				Cancellation.Cancel();
			}
			catch
			{
			}

			Cancellation.Dispose();
		}

	}
}
