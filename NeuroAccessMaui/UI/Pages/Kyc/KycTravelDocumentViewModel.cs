using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Data.PersonalNumbers;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.TravelDocuments;
using NeuroAccessMaui.Services.UI;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Identifies the current step in the guided travel-document evidence flow.
	/// </summary>
	public enum KycTravelDocumentFlowState
	{
		/// <summary>
		/// The user is viewing the document-chip introduction.
		/// </summary>
		Intro,

		/// <summary>
		/// The MRZ scanner is being opened or awaited.
		/// </summary>
		MrzCapture,

		/// <summary>
		/// MRZ evidence has been captured and saved.
		/// </summary>
		MrzCaptured,

		/// <summary>
		/// The NFC session is ready and waiting for chip placement.
		/// </summary>
		NfcReady,

		/// <summary>
		/// The document chip is being read.
		/// </summary>
		NfcReading,

		/// <summary>
		/// NFC readout has been saved and is ready for review.
		/// </summary>
		Success,

		/// <summary>
		/// The current flow step has a recoverable error.
		/// </summary>
		Error
	}

	/// <summary>
	/// View model for guided travel-document MRZ and NFC evidence capture.
	/// </summary>
	public partial class KycTravelDocumentViewModel : BaseViewModel, IDisposable
	{
		private readonly ITravelDocumentEvidenceService evidenceService = ServiceRef.Provider.GetRequiredService<ITravelDocumentEvidenceService>();
		private readonly INfcIsoDepSessionService nfcIsoDepSessionService = ServiceRef.Provider.GetRequiredService<INfcIsoDepSessionService>();
		private readonly ITravelDocumentReadoutService readoutService = ServiceRef.Provider.GetRequiredService<ITravelDocumentReadoutService>();
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycReference? reference;
		private Guid? activeSessionId;
		private Guid? finalizingSessionId;
		private Guid? reportedTelemetrySessionId;
		private DateTime? activeSessionStartedUtc;
		private CancellationTokenSource? activeSessionCancellationTokenSource;
		private bool disposedValue;

		/// <summary>
		/// Gets or sets the current guided travel-document flow state.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(CanStartNfc))]
		[NotifyPropertyChangedFor(nameof(ShowNfcPlacementPanel))]
		[NotifyPropertyChangedFor(nameof(ShowIntro))]
		[NotifyPropertyChangedFor(nameof(ShowNfcStep))]
		[NotifyPropertyChangedFor(nameof(ShowSuccess))]
		[NotifyPropertyChangedFor(nameof(ShowNfcRetryAction))]
		[NotifyPropertyChangedFor(nameof(ShowManualFallback))]
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
		[NotifyPropertyChangedFor(nameof(ShowNfcProgress))]
		[NotifyPropertyChangedFor(nameof(ShowNfcWaitingIndicator))]
		[NotifyPropertyChangedFor(nameof(ShowNfcReadingIndicator))]
		[NotifyPropertyChangedFor(nameof(ShowRescanAction))]
		[NotifyPropertyChangedFor(nameof(NfcStepTitle))]
		[NotifyPropertyChangedFor(nameof(NfcStepDescription))]
		[NotifyPropertyChangedFor(nameof(ShowCompactNfcIllustration))]
		[NotifyPropertyChangedFor(nameof(ShowFullNfcIllustration))]
		[NotifyPropertyChangedFor(nameof(NfcProgressDetectChipState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressSecureConnectionState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressReadDataState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressVerifyDocumentState))]
		[ObservableProperty]
		private KycTravelDocumentFlowState flowState = KycTravelDocumentFlowState.Intro;

		/// <summary>
		/// Gets or sets the MRZ text entered for the current KYC application.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(HasMrz))]
		[NotifyPropertyChangedFor(nameof(CanStartNfc))]
		[NotifyPropertyChangedFor(nameof(ShowNfcPlacementPanel))]
		[NotifyPropertyChangedFor(nameof(ShowIntro))]
		[NotifyPropertyChangedFor(nameof(ShowNfcStep))]
		[NotifyPropertyChangedFor(nameof(ShowNfcRetryAction))]
		[NotifyPropertyChangedFor(nameof(ShowManualFallback))]
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
		[NotifyPropertyChangedFor(nameof(ShowRescanAction))]
		[NotifyPropertyChangedFor(nameof(MrzStepDescription))]
		[ObservableProperty]
		private string mrzText = string.Empty;

		/// <summary>
		/// Gets or sets the current evidence or NFC status message.
		/// </summary>
		[ObservableProperty]
		private string statusText = string.Empty;

		/// <summary>
		/// Gets or sets the current in-page NFC progress stage, from waiting to reading.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(IsNfcChipDetected))]
		[NotifyPropertyChangedFor(nameof(IsNfcReadingStarted))]
		[ObservableProperty]
		private int nfcProgressStage;

		/// <summary>
		/// Gets whether the chip has been detected during the current attempt.
		/// </summary>
		public bool IsNfcChipDetected => this.NfcProgressStage >= 1;

		/// <summary>
		/// Gets whether reading has started during the current attempt.
		/// </summary>
		public bool IsNfcReadingStarted => this.NfcProgressStage >= 2;

		/// <summary>
		/// Gets whether Android should display the compact illustration while reading.
		/// </summary>
		public bool ShowCompactNfcIllustration => DeviceInfo.Platform == DevicePlatform.Android && this.ShowNfcReadingIndicator;

		/// <summary>
		/// Gets whether the full placement illustration and description should be displayed.
		/// </summary>
		public bool ShowFullNfcIllustration => !this.ShowCompactNfcIllustration;

		/// <summary>
		/// Gets or sets a value indicating whether a status message should be displayed.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
		[ObservableProperty]
		private bool hasStatusText;

		/// <summary>
		/// Gets or sets a value indicating whether an NFC readout is in progress.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(ShowCompactNfcIllustration))]
		[NotifyPropertyChangedFor(nameof(ShowFullNfcIllustration))]
		[NotifyPropertyChangedFor(nameof(CanStartNfc))]
		[NotifyPropertyChangedFor(nameof(ShowNfcPlacementPanel))]
		[NotifyPropertyChangedFor(nameof(ShowIntro))]
		[NotifyPropertyChangedFor(nameof(ShowNfcStep))]
		[NotifyPropertyChangedFor(nameof(ShowNfcRetryAction))]
		[NotifyPropertyChangedFor(nameof(ShowManualFallback))]
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
		[NotifyPropertyChangedFor(nameof(ShowNfcWaitingIndicator))]
		[NotifyPropertyChangedFor(nameof(ShowNfcReadingIndicator))]
		[NotifyPropertyChangedFor(nameof(ShowRescanAction))]
		[ObservableProperty]
		private bool isNfcBusy;

		/// <summary>
		/// Gets or sets a value indicating whether NFC readout XML is available.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(CanStartNfc))]
		[NotifyPropertyChangedFor(nameof(ShowNfcPlacementPanel))]
		[NotifyPropertyChangedFor(nameof(ShowIntro))]
		[NotifyPropertyChangedFor(nameof(ShowNfcStep))]
		[NotifyPropertyChangedFor(nameof(ShowSuccess))]
		[NotifyPropertyChangedFor(nameof(ShowNfcRetryAction))]
		[NotifyPropertyChangedFor(nameof(ShowManualFallback))]
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
		[NotifyPropertyChangedFor(nameof(ShowNfcProgress))]
		[NotifyPropertyChangedFor(nameof(ShowRescanAction))]
		[NotifyPropertyChangedFor(nameof(NfcProgressDetectChipState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressSecureConnectionState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressReadDataState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressVerifyDocumentState))]
		[ObservableProperty]
		private bool hasReadout;

		/// <summary>
		/// Gets or sets a value indicating whether the current status represents a recoverable error.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
		[NotifyPropertyChangedFor(nameof(ShowNfcProgress))]
		[NotifyPropertyChangedFor(nameof(ShowRescanAction))]
		[NotifyPropertyChangedFor(nameof(NfcProgressDetectChipState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressSecureConnectionState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressReadDataState))]
		[NotifyPropertyChangedFor(nameof(NfcProgressVerifyDocumentState))]
		[ObservableProperty]
		private bool hasErrorState;

		/// <summary>
		/// Gets a value indicating whether MRZ evidence has been captured.
		/// </summary>
		public bool HasMrz => !string.IsNullOrWhiteSpace(this.MrzText);

		/// <summary>
		/// Gets a value indicating whether the NFC readout can be started.
		/// </summary>
		public bool CanStartNfc => this.HasMrz && !this.IsNfcBusy && !this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether the NFC placement illustration should be shown.
		/// </summary>
		public bool ShowNfcPlacementPanel =>
			this.FlowState == KycTravelDocumentFlowState.MrzCaptured ||
			this.FlowState == KycTravelDocumentFlowState.NfcReady ||
			this.FlowState == KycTravelDocumentFlowState.NfcReading ||
			this.ShowSuccess;

		/// <summary>
		/// Gets a value indicating whether the introductory chip-first guide should be shown.
		/// </summary>
		public bool ShowIntro =>
			this.FlowState == KycTravelDocumentFlowState.Intro ||
			this.FlowState == KycTravelDocumentFlowState.MrzCapture ||
			(this.FlowState == KycTravelDocumentFlowState.Error && !this.HasMrz);

		/// <summary>
		/// Gets a value indicating whether the NFC placement step should be shown.
		/// </summary>
		public bool ShowNfcStep =>
			this.FlowState == KycTravelDocumentFlowState.MrzCaptured ||
			this.FlowState == KycTravelDocumentFlowState.NfcReady ||
			this.FlowState == KycTravelDocumentFlowState.NfcReading ||
			(this.FlowState == KycTravelDocumentFlowState.Error && this.HasMrz);

		/// <summary>
		/// Gets a value indicating whether the readout success state should be shown.
		/// </summary>
		public bool ShowSuccess =>
			this.FlowState == KycTravelDocumentFlowState.Success ||
			this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether NFC progress should be shown.
		/// </summary>
		public bool ShowNfcProgress => this.ShowNfcStep && !this.HasErrorState;

		/// <summary>
		/// Gets a value indicating whether the status panel should be visible.
		/// </summary>
		public bool ShowStatusPanel => this.HasStatusText && this.HasErrorState && !this.ShowSuccess;

		/// <summary>
		/// Gets a value indicating whether a retry action for NFC should be shown.
		/// </summary>
		public bool ShowNfcRetryAction => this.CanStartNfc && !this.ShowIntro;

		/// <summary>
		/// Gets a value indicating whether the manual fallback action should be shown.
		/// </summary>
		public bool ShowManualFallback => !this.ShowIntro && !this.HasReadout && !this.IsNfcBusy;

		/// <summary>
		/// Gets whether the document can be rescanned after an NFC error or to recover a legacy draft.
		/// </summary>
		public bool ShowRescanAction => !this.IsNfcBusy &&
			((this.HasMrz && this.HasErrorState && !this.HasReadout) || this.reference?.CanRescanLegacyNfcReadout == true);

		/// <summary>
		/// Gets a value indicating whether the NFC session is waiting for chip placement.
		/// </summary>
		public bool ShowNfcWaitingIndicator => this.FlowState == KycTravelDocumentFlowState.NfcReady && this.IsNfcBusy;

		/// <summary>
		/// Gets a value indicating whether the NFC chip is actively being read.
		/// </summary>
		public bool ShowNfcReadingIndicator => this.FlowState == KycTravelDocumentFlowState.NfcReading && this.IsNfcBusy;

		/// <summary>
		/// Gets the current MRZ step description.
		/// </summary>
		public string MrzStepDescription => this.HasMrz
			? ServiceRef.Localizer["KycTravelDocumentMrzStepReadyDescription"]
			: ServiceRef.Localizer["KycTravelDocumentMrzStepDescription"];

		/// <summary>
		/// Gets the current NFC step title.
		/// </summary>
		public string NfcStepTitle => this.FlowState == KycTravelDocumentFlowState.NfcReading
			? ServiceRef.Localizer[DeviceInfo.Platform == DevicePlatform.Android
				? "KycTravelDocumentProgressTitle" : "KycTravelDocumentNfcReadingTitle"]
			: ServiceRef.Localizer["KycTravelDocumentNfcStepTitle"];

		/// <summary>
		/// Gets the current NFC step description.
		/// </summary>
		public string NfcStepDescription => this.FlowState == KycTravelDocumentFlowState.NfcReading
			? ServiceRef.Localizer["KycTravelDocumentNfcReadingDescription"]
			: ServiceRef.Localizer["KycTravelDocumentNfcStepDescription"];

		/// <summary>
		/// Gets the progress state for detecting the chip.
		/// </summary>
		public string NfcProgressDetectChipState => this.ResolveProgressStateText(0);

		/// <summary>
		/// Gets the progress state for securing the chip connection.
		/// </summary>
		public string NfcProgressSecureConnectionState => this.ResolveProgressStateText(1);

		/// <summary>
		/// Gets the progress state for reading document data.
		/// </summary>
		public string NfcProgressReadDataState => this.ResolveProgressStateText(2);

		/// <summary>
		/// Gets the progress state for verifying the readout.
		/// </summary>
		public string NfcProgressVerifyDocumentState => this.ResolveProgressStateText(3);

		/// <summary>
		/// Initializes a new instance of the <see cref="KycTravelDocumentViewModel"/> class.
		/// </summary>
		public KycTravelDocumentViewModel()
		{
			this.navigationArguments = ServiceRef.NavigationService.PopLatestArgs<KycProcessNavigationArgs>();
			this.reference = this.navigationArguments?.Reference;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			this.reference = this.navigationArguments?.Reference ?? this.reference;
			this.LogFlowEvent(
				"Appearing",
				new KeyValuePair<string, object?>("NfcSupported", this.nfcIsoDepSessionService.IsPlatformSupported));
			if (!await this.IsNfcFlowAvailableAsync())
			{
				await this.ContinueToApplicationAsync(BackMethod.Pop2);
				return;
			}

			string SavedMrz = this.reference?.TravelDocumentMrz ?? string.Empty;
			bool HasInsufficientSavedMrz = !string.IsNullOrWhiteSpace(SavedMrz) &&
				!KycReference.IsFullTravelDocumentMrz(SavedMrz);
			this.MrzText = HasInsufficientSavedMrz ? string.Empty : SavedMrz;
			this.HasReadout = !string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml);
			this.FlowState = this.ResolveInitialFlowState(HasInsufficientSavedMrz);
			this.StatusText = this.ResolveInitialStatusText(HasInsufficientSavedMrz);
			this.HasStatusText = !string.IsNullOrWhiteSpace(this.StatusText);
			this.HasErrorState = HasInsufficientSavedMrz && !this.HasReadout;
			this.LogFlowEvent("AppearingStateResolved");
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.StopActiveNfcSessionAsync();
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			await this.StopActiveNfcSessionAsync();
			this.Dispose(true);
			await base.OnDisposeAsync();
		}

		[RelayCommand]
		private async Task ScanMrzAsync()
		{
			if (this.HasReadout && this.reference?.CanRescanLegacyNfcReadout != true)
				return;

			if (this.IsNfcBusy)
			{
				this.LogFlowEvent("ScanMrzIgnoredNfcBusy");
				return;
			}

			this.FlowState = KycTravelDocumentFlowState.MrzCapture;
			this.LogFlowEvent("ScanMrzStarting");
			TaskCompletionSource<TravelDocumentMrzResult?> CompletionSource = new TaskCompletionSource<TravelDocumentMrzResult?>(
				TaskCreationOptions.RunContinuationsAsynchronously);

			await ServiceRef.NavigationService.GoToAsync(
				nameof(KycDocumentMrzScannerPage),
				new KycDocumentMrzScannerNavigationArgs
				{
					CompletionSource = CompletionSource,
					PreferredDocumentKind = OcrDocumentKindHint.Passport
				});

			this.LogFlowEvent("ScanMrzAwaitingResult");
			TravelDocumentMrzResult? Result = await CompletionSource.Task;
			string ScannedMrz = Result?.NormalizedMrzText ?? string.Empty;
			this.LogFlowEvent(
				"ScanMrzResultReceived",
				new KeyValuePair<string, object?>("ResultIsNull", Result is null),
				new KeyValuePair<string, object?>("IsSuccessful", Result?.IsSuccessful),
				new KeyValuePair<string, object?>("HasMrzText", !string.IsNullOrWhiteSpace(ScannedMrz)),
				new KeyValuePair<string, object?>("NormalizedMrzLength", ScannedMrz.Length),
				new KeyValuePair<string, object?>("ChipAccessMrzLength", Result?.Document?.MRZ_Information?.Length ?? 0),
				new KeyValuePair<string, object?>("DocumentType", Result?.Document?.DocumentType ?? string.Empty));
			if (Result is null ||
				!Result.IsSuccessful ||
				string.IsNullOrWhiteSpace(ScannedMrz))
			{
				this.LogFlowEvent("ScanMrzRejectedEmptyMrz");
				await this.SetStatusAsync("KycTravelDocumentInvalidMrz", false, true, false, KycTravelDocumentFlowState.Error);
				return;
			}

			this.MrzText = ScannedMrz.Trim();
			TravelDocumentMrzEvidence? Evidence = await this.TryCreateMrzEvidenceAsync(Result, CancellationToken.None);
			if (Evidence is null)
			{
				this.LogFlowEvent("ScanMrzEvidenceCreationFailed");
				await this.SetStatusAsync("KycTravelDocumentInvalidMrz", false, true, false, KycTravelDocumentFlowState.Error);
				return;
			}

			if (this.reference?.CanRescanLegacyNfcReadout == true)
			{
				this.reference.NfcReadoutXml = null;
				this.reference.NfcReadoutUpdatedUtc = null;
				this.reference.Version++;
				this.reference.UpdatedUtc = DateTime.UtcNow;
				await ServiceRef.KycService.SaveKycReferenceAsync(this.reference);
				await MainThread.InvokeOnMainThreadAsync(() => this.HasReadout = false);
			}

			this.LogFlowEvent(
				"ScanMrzEvidenceReady",
				new KeyValuePair<string, object?>("EvidenceMrzLength", Evidence.MrzText.Length),
				new KeyValuePair<string, object?>("HasApplicationIdentityId", !string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId)));
			await this.SetStatusAsync("KycTravelDocumentSaved", false, false, false, KycTravelDocumentFlowState.MrzCaptured);
			await this.StartNfcReadoutFromEvidenceAsync(Evidence);
		}

		[RelayCommand]
		private async Task StartNfcReadoutAsync()
		{
			await this.StartNfcReadoutFromEvidenceAsync(null);
		}

		private async Task StartNfcReadoutFromEvidenceAsync(TravelDocumentMrzEvidence? AvailableEvidence)
		{
			if (this.IsNfcBusy || this.HasReadout)
			{
				this.LogFlowEvent(
					"StartNfcIgnored",
					new KeyValuePair<string, object?>("IsNfcBusy", this.IsNfcBusy),
					new KeyValuePair<string, object?>("HasReadout", this.HasReadout));
				return;
			}

			if (!await this.IsNfcFlowAvailableAsync())
			{
				this.LogFlowEvent("StartNfcUnsupported");
				await this.ContinueToApplicationAsync(BackMethod.Pop2);
				return;
			}

			await this.StopActiveNfcSessionAsync();

			TravelDocumentMrzEvidence? Evidence = AvailableEvidence ?? await this.TryCreateMrzEvidenceAsync(CancellationToken.None);
			if (Evidence is null)
			{
				this.LogFlowEvent("StartNfcEvidenceMissing");
				await this.SetStatusAsync("KycTravelDocumentInvalidMrz", false, true, false, KycTravelDocumentFlowState.Error);
				return;
			}

			await this.SetStatusAsync("KycTravelDocumentNfcPreparing", true, false, false, KycTravelDocumentFlowState.MrzCaptured);
			try
			{
				Evidence = await this.BindReservedPreviewIdentityAsync(Evidence, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex,
					new KeyValuePair<string, object?>("Operation", "KYC.PreviewReservation"));
				await this.SetStatusAsync("KycTravelDocumentNfcReservationFailed", false, true, false, KycTravelDocumentFlowState.Error);
				return;
			}
			if (Evidence is null || string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId))
			{
				this.LogFlowEvent("StartNfcReservationMissing");
				await this.ForgetReservedPreviewIdentityAsync("NfcReservationMissingForgotten");
				await this.SetStatusAsync("KycTravelDocumentNfcReservationFailed", false, true, false, KycTravelDocumentFlowState.Error);
				return;
			}

			Guid SessionId = Guid.NewGuid();
			this.activeSessionId = SessionId;
			this.activeSessionStartedUtc = DateTime.UtcNow;
			this.activeSessionCancellationTokenSource = new CancellationTokenSource();
			this.LogFlowEvent(
				"StartNfcSessionStarting",
				new KeyValuePair<string, object?>("SessionId", SessionId),
				new KeyValuePair<string, object?>("EvidenceMrzLength", Evidence.MrzText.Length),
				new KeyValuePair<string, object?>("HasApplicationIdentityId", !string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId)));
			await this.StartNfcSessionAsync(SessionId, Evidence, NfcIsoDepPollingPreference.Iso14443,
				this.activeSessionCancellationTokenSource.Token);
		}

		private async Task StartNfcSessionAsync(
			Guid SessionId,
			TravelDocumentMrzEvidence Evidence,
			NfcIsoDepPollingPreference PollingPreference,
			CancellationToken FlowCancellationToken,
			Guid? PreviousSessionId = null)
		{
			try
			{
				if (PreviousSessionId.HasValue)
					await this.StopNativeNfcSessionAsync(PreviousSessionId.Value, ServiceRef.Localizer["TravelDocumentScan_IosNfcRetrying"]);

				FlowCancellationToken.ThrowIfCancellationRequested();
				if (!this.IsActiveSession(SessionId, Evidence.ApplicationIdentityId))
					return;

				string PlacementResourceKey = Evidence.DocumentInformation.DocumentType?.StartsWith("P", StringComparison.OrdinalIgnoreCase) == true
					? "KycTravelDocumentNfcReadyPassport" : "KycTravelDocumentNfcReadyIdCard";
				await this.SetStatusAsync(PlacementResourceKey, true, false, false, KycTravelDocumentFlowState.NfcReady, SessionId);
				FlowCancellationToken.ThrowIfCancellationRequested();
				await this.nfcIsoDepSessionService.StartSessionAsync(
					SessionId,
					(IsoDepInterface, CancellationToken) => this.ReadTravelDocumentAsync(SessionId, Evidence, IsoDepInterface, PollingPreference, CancellationToken),
					(Failure, CancellationToken) => this.HandleNfcFailureAsync(
						SessionId,
						Evidence.ApplicationIdentityId,
						Failure,
						CancellationToken),
					PollingPreference,
					ServiceRef.Localizer[Evidence.DocumentInformation.DocumentType?.StartsWith("P", StringComparison.OrdinalIgnoreCase) == true
						? "TravelDocumentScan_IosNfcIntroPassport" : "TravelDocumentScan_IosNfcIntroIdCard"],
					FlowCancellationToken);
				this.LogFlowEvent(
					"StartNfcSessionStarted",
					new KeyValuePair<string, object?>("SessionId", SessionId),
					new KeyValuePair<string, object?>("PollingPreference", PollingPreference.ToString()));
			}
			catch (OperationCanceledException) when (FlowCancellationToken.IsCancellationRequested)
			{
				if (await this.TryBeginNfcFinalizationAsync(SessionId, Evidence.ApplicationIdentityId))
					await this.FinishFailedNfcSessionAsync(SessionId, "KycTravelDocumentNfcCancelled");
			}
			catch (Exception Ex)
			{
				if (!this.IsActiveSession(SessionId, Evidence.ApplicationIdentityId))
					return;

				this.TrackTerminalNfcSession(SessionId, "PreparationFailed", "SessionStartFailed");
				this.LogFlowEvent(
					"StartNfcSessionFailed",
					new KeyValuePair<string, object?>("SessionId", SessionId),
					new KeyValuePair<string, object?>("ExceptionType", Ex.GetType().Name));
				if (await this.TryBeginNfcFinalizationAsync(SessionId, Evidence.ApplicationIdentityId))
					await this.FinishFailedNfcSessionAsync(SessionId, "KycTravelDocumentNfcFailed");
			}
		}

		[RelayCommand]
		private async Task ReturnToApplicationAsync()
		{
			await this.ContinueToApplicationAsync(BackMethod.Pop);
		}

		private async Task ContinueToApplicationAsync(BackMethod BackMethod)
		{
			await this.StopActiveNfcSessionAsync();

			KycReference? Reference = this.reference;
			if (Reference is not null)
			{
				bool HasCompletedReadout = !string.IsNullOrWhiteSpace(Reference.NfcReadoutXml) || this.HasReadout;
				await ServiceRef.NavigationService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Reference)
				{
					ForceFormResume = !HasCompletedReadout,
					AbandonTravelDocumentAttempt = !HasCompletedReadout
				}, BackMethod);
			}
			else
				await ServiceRef.NavigationService.GoBackAsync();
		}

		/// <inheritdoc/>
		public override async Task GoBack()
		{
			await this.StopActiveNfcSessionAsync();
			await base.GoBack();
		}

		private async Task<bool> IsNfcFlowAvailableAsync()
		{
			if (!this.nfcIsoDepSessionService.IsPlatformSupported || this.reference is null)
				return false;

			KycProcess? Process = await this.reference.GetProcess(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
			return Process is not null &&
				Process.ApplicationPolicy.Mode == KycApplicationMode.Preview &&
				Process.EvidencePolicy.TravelDocument.Nfc.Enabled;
		}

		private async Task ReadTravelDocumentAsync(
			Guid SessionId,
			TravelDocumentMrzEvidence Evidence,
			IIsoDepInterface IsoDepInterface,
			NfcIsoDepPollingPreference PollingPreference,
			CancellationToken CancellationToken)
		{
			if (!this.IsActiveSession(SessionId, Evidence.ApplicationIdentityId))
				return;

			CancellationToken FlowCancellationToken = this.activeSessionCancellationTokenSource?.Token ?? CancellationToken;
			using CancellationTokenSource LinkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, FlowCancellationToken);
			bool IsFinalizing = false;
			try
			{
				await this.UpdateChipProgressAsync(SessionId, Evidence, TravelDocumentsState.Detected);
				TravelDocumentReadoutRequest Request = new TravelDocumentReadoutRequest(
					Evidence.DocumentInformation, Evidence.MrzText, Evidence.ApplicationIdentityId)
				{
					ProgressCallback = State => this.UpdateChipProgressAsync(SessionId, Evidence, State)
				};
				TravelDocumentReadoutResult Result = await this.readoutService.ReadAsync(IsoDepInterface, Request, LinkedCancellation.Token);
				IsFinalizing = await this.TryBeginNfcFinalizationAsync(SessionId, Evidence.ApplicationIdentityId);
				if (!IsFinalizing)
				{
					if (!Result.IsSuccess)
						_ = this.UploadFailedReservedPreviewReadoutAsync(Result, Evidence.ApplicationIdentityId);
					return;
				}

				this.LogFlowEvent("ReadNfcResultReceived",
					new KeyValuePair<string, object?>("SessionId", SessionId),
					new KeyValuePair<string, object?>("Status", Result.Status.ToString()),
					new KeyValuePair<string, object?>("AuthenticationResult", Result.AuthenticationResult?.ToString()),
					new KeyValuePair<string, object?>("IsSuccess", Result.IsSuccess),
					new KeyValuePair<string, object?>("XmlLength", Result.Xml?.Length ?? 0));

				if (Result.IsSuccess && await this.SaveReadoutAsync(Result, FlowCancellationToken))
				{
					this.TrackTerminalNfcSession(SessionId, "Succeeded", string.Empty);
					await this.UpdateNativeNfcAlertAsync(SessionId, "TravelDocumentScan_IosNfcSuccess");
					await this.SetStatusAsync("KycTravelDocumentNfcSuccess", true, false, true, KycTravelDocumentFlowState.Success, SessionId);
					await this.CompleteNfcSessionAsync(SessionId, null);
				}
				else if (!await this.TryRetryWithPacePollingAsync(SessionId, Evidence, PollingPreference, Result, FlowCancellationToken))
				{
					await this.FinishFailedNfcSessionAsync(SessionId, this.ResolveReadoutFailureResourceKey(Result.Status));
					// Upload the completed trace to its original preview without blocking the retry controls.
					_ = this.UploadFailedReservedPreviewReadoutAsync(Result, Evidence.ApplicationIdentityId);
				}
			}
			catch (OperationCanceledException) when (LinkedCancellation.IsCancellationRequested)
			{
				if (IsFinalizing || await this.TryBeginNfcFinalizationAsync(SessionId, Evidence.ApplicationIdentityId))
					await this.FinishFailedNfcSessionAsync(SessionId, "KycTravelDocumentNfcCancelled");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				if (IsFinalizing || await this.TryBeginNfcFinalizationAsync(SessionId, Evidence.ApplicationIdentityId))
					await this.FinishFailedNfcSessionAsync(SessionId, "KycTravelDocumentNfcFailed");
			}
		}

		private Task<bool> TryBeginNfcFinalizationAsync(Guid SessionId, string? PreviewIdentityId)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!this.IsActiveSession(SessionId, PreviewIdentityId) || this.finalizingSessionId == SessionId)
					return false;

				this.finalizingSessionId = SessionId;
				return true;
			});
		}

		private async Task UpdateChipProgressAsync(Guid SessionId, TravelDocumentMrzEvidence Evidence, TravelDocumentsState State)
		{
			string ResourceKey = State switch
			{
				TravelDocumentsState.Detected => "KycTravelDocumentNfcDetected",
				TravelDocumentsState.ValidatingCertificate => "KycTravelDocumentNfcCheckingSecurity",
				TravelDocumentsState.Idle => "KycTravelDocumentNfcCheckingDocument",
				_ => "KycTravelDocumentNfcReading"
			};
			string NativeResourceKey = State switch
			{
				TravelDocumentsState.Detected => "TravelDocumentScan_IosNfcDetected",
				TravelDocumentsState.ValidatingCertificate => "TravelDocumentScan_IosNfcCheckingSecurity",
				TravelDocumentsState.Idle => "TravelDocumentScan_IosNfcCheckingDocument",
				_ => "TravelDocumentScan_IosNfcReading"
			};
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (!this.IsActiveSession(SessionId, Evidence.ApplicationIdentityId) || this.finalizingSessionId == SessionId)
					return;

				this.StatusText = ServiceRef.Localizer[ResourceKey];
				this.HasStatusText = true;
				this.NfcProgressStage = Math.Max(this.NfcProgressStage, State == TravelDocumentsState.Detected ? 1 : 2);
				this.FlowState = KycTravelDocumentFlowState.NfcReading;
				await this.UpdateNativeNfcAlertAsync(SessionId, NativeResourceKey);
			});
		}

		private async Task UpdateNativeNfcAlertAsync(Guid SessionId, string ResourceKey)
		{
			try
			{
				await this.nfcIsoDepSessionService.UpdateSessionAlertAsync(SessionId, ServiceRef.Localizer[ResourceKey], CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task FinishFailedNfcSessionAsync(Guid SessionId, string ResourceKey)
		{
			if (this.activeSessionId != SessionId)
				return;

			this.TrackTerminalNfcSession(SessionId, ResourceKey == "KycTravelDocumentNfcCancelled" ? "Cancelled" : "Failed", ResourceKey);
			string NativeResourceKey = ResourceKey switch
			{
				"KycTravelDocumentNfcCancelled" => "TravelDocumentScan_IosNfcCancelled",
				"KycTravelDocumentNfcConnectionLost" => "TravelDocumentScan_IosNfcConnectionLost",
				"KycTravelDocumentNfcTimedOut" => "TravelDocumentScan_IosNfcTimedOut",
				_ => "TravelDocumentScan_IosNfcFailed"
			};
			string NativeMessage = ServiceRef.Localizer[NativeResourceKey];
			try
			{
				await this.StopNativeNfcSessionAsync(SessionId, NativeMessage);
				await this.SetStatusAsync(ResourceKey, true, true, false, KycTravelDocumentFlowState.Error, SessionId);
				if (this.activeSessionId == SessionId)
					await this.ForgetReservedPreviewIdentityAsync("FailedPreviewReadoutReservationForgotten");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				await this.CompleteNfcSessionAsync(SessionId, NativeMessage);
			}
		}

		private async Task StopNativeNfcSessionAsync(Guid SessionId, string? ErrorMessage)
		{
			try
			{
				await this.nfcIsoDepSessionService.StopSessionAsync(SessionId, ErrorMessage, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task<bool> TryRetryWithPacePollingAsync(
			Guid SessionId,
			TravelDocumentMrzEvidence Evidence,
			NfcIsoDepPollingPreference PollingPreference,
			TravelDocumentReadoutResult Result,
			CancellationToken FlowCancellationToken)
		{
			if (!OperatingSystem.IsIOSVersionAtLeast(16) ||
				PollingPreference != NfcIsoDepPollingPreference.Iso14443 ||
				Result.Status != TravelDocumentReadoutStatus.AuthenticationFailed ||
				Result.AuthenticationResult is not (AuthenticateResult.AlreadyEncrypted or
					AuthenticateResult.UnableToInitializePace or AuthenticateResult.UnableToAuthenticatePace or
					AuthenticateResult.UnableToGetBacChallenge or AuthenticateResult.UnableToAuthenticateBac))
			{
				return false;
			}

			FlowCancellationToken.ThrowIfCancellationRequested();
			if (!this.IsActiveSession(SessionId, Evidence.ApplicationIdentityId))
				return true;

			this.TrackTerminalNfcSession(SessionId, "Failed", Result.AuthenticationResult.Value.ToString());
			Guid ReplacementSessionId = Guid.NewGuid();
			this.activeSessionId = ReplacementSessionId;
			this.activeSessionStartedUtc = DateTime.UtcNow;

			// Keep the preview seed and flow token, but isolate callbacks from the old native session.
			await this.StartNfcSessionAsync(ReplacementSessionId, Evidence, NfcIsoDepPollingPreference.Pace,
				FlowCancellationToken, SessionId);
			return true;
		}

		private async Task HandleNfcFailureAsync(
			Guid SessionId,
			string? PreviewIdentityId,
			NfcIsoDepSessionFailure Failure,
			CancellationToken CancellationToken)
		{
			if (!await this.TryBeginNfcFinalizationAsync(SessionId, PreviewIdentityId))
				return;

			this.LogFlowEvent("NfcFailureReceived",
				new KeyValuePair<string, object?>("SessionId", SessionId),
				new KeyValuePair<string, object?>("FailureCode", Failure.FailureCode.ToString()));
			await this.FinishFailedNfcSessionAsync(SessionId, this.ResolveNfcFailureResourceKey(Failure));
		}

		private async Task CompleteNfcSessionAsync(Guid SessionId, string? ErrorMessage = null)
		{
			if (this.activeSessionId != SessionId)
			{
				await this.StopNativeNfcSessionAsync(SessionId, ErrorMessage);
				return;
			}

			this.activeSessionId = null;
			this.activeSessionStartedUtc = null;
			try
			{
				this.CancelAndDisposeActiveSessionCancellation();
				await this.StopNativeNfcSessionAsync(SessionId, ErrorMessage);
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (this.activeSessionId is null)
						this.IsNfcBusy = false;
				});
			}
		}

		private async Task<TravelDocumentMrzEvidence?> TryCreateMrzEvidenceAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			string NormalizedMrz = this.MrzText?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(NormalizedMrz) ||
				!KycReference.IsFullTravelDocumentMrz(NormalizedMrz) ||
				!MrzExtensions.ParseMrz(NormalizedMrz, out DocumentInformation? DocumentInformation))
			{
				return null;
			}

			if (this.reference is not null)
			{
				await this.SaveReferenceEvidenceAsync(NormalizedMrz, null, DocumentInformation);
				return new TravelDocumentMrzEvidence(
					NormalizedMrz,
					DocumentInformation,
					this.reference.ReservedPreviewIdentityId ?? this.reference.PreviewIdentityId ?? this.reference.GetActiveApplicationIdentityId());
			}

			if (!await this.evidenceService.SaveMrzAsync(NormalizedMrz, CancellationToken))
				return null;

			return await this.evidenceService.TryLoadMrzAsync(CancellationToken);
		}

		private async Task<TravelDocumentMrzEvidence?> TryCreateMrzEvidenceAsync(
			TravelDocumentMrzResult Result,
			CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			string NormalizedMrz = Result.NormalizedMrzText;
			DocumentInformation? DocumentInformation = Result.Document;
			if (string.IsNullOrWhiteSpace(NormalizedMrz) ||
				!KycReference.IsFullTravelDocumentMrz(NormalizedMrz) ||
				DocumentInformation is null)
			{
				return null;
			}

			this.MrzText = NormalizedMrz;
			if (this.reference is not null)
			{
				await this.SaveReferenceEvidenceAsync(NormalizedMrz, null, DocumentInformation);
				return new TravelDocumentMrzEvidence(
					NormalizedMrz,
					DocumentInformation,
					this.reference.ReservedPreviewIdentityId ?? this.reference.PreviewIdentityId ?? this.reference.GetActiveApplicationIdentityId());
			}

			if (!await this.evidenceService.SaveMrzAsync(NormalizedMrz, CancellationToken))
				return null;

			return new TravelDocumentMrzEvidence(NormalizedMrz, DocumentInformation, null);
		}

		private async Task<TravelDocumentMrzEvidence?> BindReservedPreviewIdentityAsync(
			TravelDocumentMrzEvidence Evidence,
			CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (this.reference is null)
				return string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId) ? null : Evidence;

			LegalIdentity? ExistingIdentity = await this.TryLoadUsableReservedPreviewIdentityAsync(this.reference, CancellationToken);
			if (ExistingIdentity is not null)
			{
				await this.ClearMatchingReservedProfileApplicationAsync(ExistingIdentity.Id).ConfigureAwait(false);
				return new TravelDocumentMrzEvidence(
					Evidence.MrzText,
					Evidence.DocumentInformation,
					ExistingIdentity.Id);
			}

			IReadOnlyList<Property> ReservationProperties = await ServiceRef.KycService
				.PreparePreviewReservationPropertiesAsync(this.reference, CancellationToken)
				.ConfigureAwait(false);
			bool GenerateNewKeys = !await this.HasExistingSigningKeyAsync().ConfigureAwait(false);
			(bool Succeeded, LegalIdentity? ReservedIdentity) = await ServiceRef.NetworkService.TryRequest(
				() => ServiceRef.XmppService.ApplyPreviewLegalIdentity(ReservationProperties.ToArray(), GenerateNewKeys));

			this.LogFlowEvent(
				"PreviewReservationCreated",
				new KeyValuePair<string, object?>("Succeeded", Succeeded),
				new KeyValuePair<string, object?>("HasReservedIdentity", ReservedIdentity is not null),
				new KeyValuePair<string, object?>("GeneratedNewKeys", GenerateNewKeys),
				new KeyValuePair<string, object?>("PropertyCount", ReservationProperties.Count));

			if (!Succeeded || ReservedIdentity is null || string.IsNullOrWhiteSpace(ReservedIdentity.Id))
				return null;

			await ServiceRef.KycService.SetReservedPreviewIdentityAsync(this.reference, ReservedIdentity).ConfigureAwait(false);
			await this.ClearMatchingReservedProfileApplicationAsync(ReservedIdentity.Id).ConfigureAwait(false);
			return new TravelDocumentMrzEvidence(
				Evidence.MrzText,
				Evidence.DocumentInformation,
				ReservedIdentity.Id);
		}

		private async Task<LegalIdentity?> TryLoadUsableReservedPreviewIdentityAsync(
			KycReference Reference,
			CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			string ReservedPreviewIdentityId = Reference.ReservedPreviewIdentityId?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(ReservedPreviewIdentityId))
				return null;

			(bool Succeeded, LegalIdentity? Identity) = await ServiceRef.NetworkService.TryRequest(
				() => ServiceRef.XmppService.GetLegalIdentity(ReservedPreviewIdentityId));
			bool HasPrivateKey = Identity is not null && await this.HasPrivateKeyAsync(Identity.Id).ConfigureAwait(false);
			this.LogFlowEvent(
				"PreviewReservationReuseChecked",
				new KeyValuePair<string, object?>("Succeeded", Succeeded),
				new KeyValuePair<string, object?>("HasIdentity", Identity is not null),
				new KeyValuePair<string, object?>("HasPrivateKey", HasPrivateKey));

			if (!Succeeded || Identity is null || !HasPrivateKey)
				return null;

			await ServiceRef.KycService.SetReservedPreviewIdentityAsync(Reference, Identity).ConfigureAwait(false);
			return Identity;
		}

		private async Task<bool> HasExistingSigningKeyAsync()
		{
			if (await ServiceRef.XmppService.HasSigningKeysAsync().ConfigureAwait(false))
				return true;

			string IdentityId = ServiceRef.TagProfile.LegalIdentity?.Id?.Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(IdentityId) &&
				await ServiceRef.XmppService.HasPrivateKey(IdentityId).ConfigureAwait(false))
			{
				return true;
			}

			string? ApplicationIdentityId = this.reference?.GetActiveApplicationIdentityId();
			return !string.IsNullOrWhiteSpace(ApplicationIdentityId) &&
				await ServiceRef.XmppService.HasPrivateKey(ApplicationIdentityId).ConfigureAwait(false);
		}

		private async Task<bool> HasPrivateKeyAsync(string? IdentityId)
		{
			if (string.IsNullOrWhiteSpace(IdentityId))
				return false;

			try
			{
				return await ServiceRef.XmppService.HasPrivateKey(IdentityId.Trim()).ConfigureAwait(false);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(
					Ex,
					new KeyValuePair<string, object?>("Operation", "KYC.PreviewReservationPrivateKeyCheck"));
				return false;
			}
		}

		private async Task<bool> SaveReadoutAsync(TravelDocumentReadoutResult Result, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrWhiteSpace(Result.Xml))
				return false;

			if (this.reference is not null)
			{
				await this.SaveReferenceEvidenceAsync(null, Result.Xml, null, Result.DocumentData);
				return true;
			}

			return await this.evidenceService.SaveReadoutXmlAsync(Result.Xml, CancellationToken);
		}

		private async Task UploadFailedReservedPreviewReadoutAsync(TravelDocumentReadoutResult Result, string? ReservedPreviewIdentityId)
		{
			if (this.reference is null ||
				string.IsNullOrWhiteSpace(Result.Xml))
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(ReservedPreviewIdentityId))
				return;

			try
			{
				KycNfcEvidencePolicy NfcPolicy = await this.ResolveNfcEvidencePolicyAsync().ConfigureAwait(false);
				string AttachmentName = string.IsNullOrWhiteSpace(NfcPolicy.AttachmentName)
					? KycNfcEvidencePolicy.DefaultAttachmentName
					: NfcPolicy.AttachmentName;
				string ContentType = string.IsNullOrWhiteSpace(NfcPolicy.ContentType)
					? KycNfcEvidencePolicy.DefaultContentType
					: NfcPolicy.ContentType;
				byte[] Data = Encoding.UTF8.GetBytes(Result.Xml);
				LegalIdentityAttachment Attachment = new LegalIdentityAttachment(AttachmentName, ContentType, Data);

				(bool Uploaded, LegalIdentity? Identity) = await ServiceRef.NetworkService.TryRequest(
					() => ServiceRef.XmppService.UploadLegalIdentityAttachments(ReservedPreviewIdentityId, Attachment));

				this.LogFlowEvent(
					"FailedPreviewReadoutUploaded",
					new KeyValuePair<string, object?>("Uploaded", Uploaded),
					new KeyValuePair<string, object?>("HasIdentity", Identity is not null),
					new KeyValuePair<string, object?>("Status", Result.Status.ToString()),
					new KeyValuePair<string, object?>("XmlLength", Result.Xml.Length),
					new KeyValuePair<string, object?>("AttachmentName", AttachmentName));
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task<KycNfcEvidencePolicy> ResolveNfcEvidencePolicyAsync()
		{
			if (this.reference is null)
				return new KycNfcEvidencePolicy();

			try
			{
				KycProcess? Process = await this.reference.ToProcess(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName).ConfigureAwait(false);
				return Process?.EvidencePolicy.TravelDocument.Nfc ?? new KycNfcEvidencePolicy();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return new KycNfcEvidencePolicy();
			}
		}

		private async Task ForgetReservedPreviewIdentityAsync(string EventName)
		{
			if (this.reference is null)
				return;

			string ReservedPreviewIdentityId = this.reference.ReservedPreviewIdentityId?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(ReservedPreviewIdentityId))
				return;

			await this.ClearMatchingReservedProfileApplicationAsync(ReservedPreviewIdentityId).ConfigureAwait(false);

			await ServiceRef.KycService.ForgetReservedPreviewIdentityAsync(this.reference, ReservedPreviewIdentityId).ConfigureAwait(false);
			this.LogFlowEvent(
				EventName,
				new KeyValuePair<string, object?>("HasReservedPreviewIdentityId", true));
		}

		private async Task ClearMatchingReservedProfileApplicationAsync(string PreviewIdentityId)
		{
			if (this.reference?.IsUnsubmittedReservedPreviewIdentity(PreviewIdentityId) == true &&
				string.Equals(ServiceRef.TagProfile.IdentityApplication?.Id, PreviewIdentityId, StringComparison.OrdinalIgnoreCase))
			{
				await ServiceRef.TagProfile.SetIdentityApplication(null, true).ConfigureAwait(false);
			}
		}

		private async Task SaveReferenceEvidenceAsync(
			string? MrzText,
			string? ReadoutXml,
			DocumentInformation? DocumentInformation = null,
			TravelDocumentData? DocumentData = null)
		{
			if (this.reference is null)
				return;

			if (!string.IsNullOrWhiteSpace(MrzText))
			{
				this.reference.TravelDocumentMrz = MrzText.Trim();
				this.reference.TravelDocumentMrzUpdatedUtc = DateTime.UtcNow;
			}

			if (!string.IsNullOrWhiteSpace(ReadoutXml))
			{
				this.reference.NfcReadoutXml = ReadoutXml;
				this.reference.NfcReadoutUpdatedUtc = DateTime.UtcNow;
				this.reference.NfcVerifiedFieldIds = null;
			}

			if (DocumentInformation is not null)
				await this.ApplyMrzFieldsAsync(DocumentInformation);

			if (DocumentData is not null && !string.IsNullOrWhiteSpace(ReadoutXml))
				this.reference.NfcVerifiedFieldIds = await this.ApplyNfcFieldsAsync(DocumentData);

			KycProcess? Process = await this.reference.GetProcess(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
			this.reference.ApplyEvidenceStateToProcess(Process);
			this.reference.Version++;
			this.reference.UpdatedUtc = DateTime.UtcNow;

			await ServiceRef.KycService.SaveKycReferenceAsync(this.reference);
			this.LogFlowEvent(
				"ReferenceEvidenceSaved",
				new KeyValuePair<string, object?>("SavedMrz", !string.IsNullOrWhiteSpace(MrzText)),
				new KeyValuePair<string, object?>("SavedReadout", !string.IsNullOrWhiteSpace(ReadoutXml)),
				new KeyValuePair<string, object?>("ReferenceHasFullMrz", this.reference.HasFullTravelDocumentMrz),
				new KeyValuePair<string, object?>("ReferenceHasReadout", !string.IsNullOrWhiteSpace(this.reference.NfcReadoutXml)));
		}

		private async Task ApplyMrzFieldsAsync(DocumentInformation DocumentInformation)
		{
			Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				[Constants.XmppProperties.FirstName] = JoinNameParts(DocumentInformation.SecondaryIdentifier),
				[Constants.XmppProperties.LastNames] = JoinNameParts(DocumentInformation.PrimaryIdentifier),
				[Constants.XmppProperties.Nationality] = ResolveAlpha2CountryCode(DocumentInformation.Nationality)
			};

			if (TryResolveGenderCode(DocumentInformation.Gender, out string GenderCode))
				Values[Constants.XmppProperties.Gender] = GenderCode;

			string CountryCode = ResolveAlpha2CountryCode(DocumentInformation.Nationality);
			if (string.IsNullOrWhiteSpace(CountryCode))
				CountryCode = ResolveAlpha2CountryCode(DocumentInformation.IssuingState);

			string PersonalNumber = await ResolvePersonalNumberAsync(DocumentInformation.OptionalData, CountryCode);
			if (!string.IsNullOrWhiteSpace(PersonalNumber))
				Values[Constants.XmppProperties.PersonalNumber] = PersonalNumber;

			if (TryParseMrzBirthDate(DocumentInformation.DateOfBirth, out string DateOfBirth))
				AddBirthDateMappings(Values, DateOfBirth);

			await this.ApplyMappedFieldsAsync(Values, false);
		}

		private async Task<string[]?> ApplyNfcFieldsAsync(TravelDocumentData DocumentData)
		{
			Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				[Constants.XmppProperties.FirstName] = DocumentData.SecondaryIdentifier?.Trim() ?? string.Empty,
				[Constants.XmppProperties.LastNames] = DocumentData.PrimaryIdentifier?.Trim() ?? string.Empty,
				[Constants.XmppProperties.Country] = ResolveAlpha2CountryCode(DocumentData.IssuingState),
				[Constants.XmppProperties.Nationality] = ResolveAlpha2CountryCode(DocumentData.Nationality)
			};

			if (TryResolveGenderCode(DocumentData.Gender, out string GenderCode))
				Values[Constants.XmppProperties.Gender] = GenderCode;

			string PersonalNumber = DocumentData.PersonalNumber?.Replace("<", string.Empty).Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(PersonalNumber))
			{
				string CountryCode = ResolveAlpha2CountryCode(DocumentData.Nationality);
				if (string.IsNullOrWhiteSpace(CountryCode))
					CountryCode = ResolveAlpha2CountryCode(DocumentData.IssuingState);

				PersonalNumber = await ResolvePersonalNumberAsync(DocumentData.OptionalData, CountryCode);
			}

			if (!string.IsNullOrWhiteSpace(PersonalNumber))
				Values[Constants.XmppProperties.PersonalNumber] = PersonalNumber;

			if ((TryParseMrzBirthDate(DocumentData.AdditionalDateOfBirth, out string DateOfBirth) ||
				TryParseMrzBirthDate(DocumentData.DateOfBirth, out DateOfBirth)))
			{
				AddBirthDateMappings(Values, DateOfBirth);
			}

			return await this.ApplyMappedFieldsAsync(Values, true);
		}

		private async Task<string[]?> ApplyMappedFieldsAsync(
			IReadOnlyDictionary<string, string> Values,
			bool TrackVerifiedFields)
		{
			if (this.reference is null)
				return null;

			KycProcess? Process = await this.reference.GetProcess(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
			if (Process is null)
				return null;

			List<string> AppliedFieldIds = new List<string>();
			IEnumerable<ObservableKycField> Fields = Process.Pages.SelectMany(Page =>
				Page.AllFields.Concat(Page.AllSections.SelectMany(Section => Section.AllFields)));
			foreach (ObservableKycField Field in Fields)
			{
				if (Field.FieldType == FieldType.Image || Field.FieldType == FieldType.File)
					continue;

				string Value = ResolveMappedFieldValue(Field, Values);
				if (string.IsNullOrWhiteSpace(Value))
					continue;

				string? PreviousValue = Field.StringValue;
				Field.StringValue = Value;
				string? AppliedValue = Field.StringValue;
				if (string.IsNullOrWhiteSpace(AppliedValue) ||
					!string.Equals(AppliedValue, Value, StringComparison.OrdinalIgnoreCase))
				{
					Field.StringValue = PreviousValue;
					continue;
				}

				Process.Values[Field.Id] = AppliedValue;
				if (TrackVerifiedFields)
					AppliedFieldIds.Add(Field.Id);
			}

			this.reference.Fields = KycReference.CreatePersistentFields(Process);
			return AppliedFieldIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		}

		private async Task StopActiveNfcSessionAsync()
		{
			Guid? SessionId = this.activeSessionId;
			if (!SessionId.HasValue)
				return;

			this.TrackTerminalNfcSession(SessionId.Value, "Cancelled", "Cancelled");
			this.activeSessionId = null;
			this.activeSessionStartedUtc = null;
			try
			{
				this.CancelAndDisposeActiveSessionCancellation();
				await this.StopNativeNfcSessionAsync(SessionId.Value, ServiceRef.Localizer["TravelDocumentScan_IosNfcCancelled"]);
				if (string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml))
					await this.ForgetReservedPreviewIdentityAsync("StoppedNfcSessionReservationForgotten");
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (this.activeSessionId is null)
						this.IsNfcBusy = false;
				});
			}
		}

		private Task SetStatusAsync(
			string ResourceKey,
			bool IsBusy,
			bool IsError = false,
			bool ReadoutAvailable = false,
			KycTravelDocumentFlowState? FlowState = null,
			Guid? SessionId = null)
		{
			string Message = ServiceRef.Localizer[ResourceKey];
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (SessionId.HasValue && this.activeSessionId != SessionId)
					return;

				this.StatusText = Message;
				this.HasStatusText = !string.IsNullOrWhiteSpace(Message);
				this.IsNfcBusy = IsBusy;
				this.HasErrorState = IsError;
				if (IsError || FlowState == KycTravelDocumentFlowState.MrzCaptured || FlowState == KycTravelDocumentFlowState.NfcReady)
					this.NfcProgressStage = 0;
				if (ReadoutAvailable)
					this.HasReadout = true;

				if (FlowState.HasValue)
					this.FlowState = FlowState.Value;

				this.LogFlowEvent(
					"StatusSet",
					new KeyValuePair<string, object?>("ResourceKey", ResourceKey),
					new KeyValuePair<string, object?>("IsBusy", IsBusy),
					new KeyValuePair<string, object?>("IsError", IsError),
					new KeyValuePair<string, object?>("ReadoutAvailable", ReadoutAvailable));
			});
		}

		private void LogFlowEvent(string EventName, params KeyValuePair<string, object?>[] Tags)
		{
			List<KeyValuePair<string, object?>> AllTags = new List<KeyValuePair<string, object?>>
			{
				new KeyValuePair<string, object?>("Event", EventName),
				new KeyValuePair<string, object?>("FlowState", this.FlowState.ToString()),
				new KeyValuePair<string, object?>("HasMrz", this.HasMrz),
				new KeyValuePair<string, object?>("HasReferenceFullMrz", this.reference?.HasFullTravelDocumentMrz == true),
				new KeyValuePair<string, object?>("HasReadout", this.HasReadout),
				new KeyValuePair<string, object?>("HasReferenceReadout", !string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml)),
				new KeyValuePair<string, object?>("IsNfcBusy", this.IsNfcBusy),
				new KeyValuePair<string, object?>("ActiveSessionId", this.activeSessionId)
			};
			AllTags.AddRange(Tags);
			ServiceRef.LogService.LogInformational("KYC travel document flow", AllTags.ToArray());
		}

		private KycTravelDocumentFlowState ResolveInitialFlowState(bool HasInsufficientSavedMrz)
		{
			if (!string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml))
				return KycTravelDocumentFlowState.Success;

			if (HasInsufficientSavedMrz)
				return KycTravelDocumentFlowState.Error;

			if (this.reference?.HasFullTravelDocumentMrz == true)
				return KycTravelDocumentFlowState.NfcReady;

			return KycTravelDocumentFlowState.Intro;
		}

		private bool IsActiveSession(Guid SessionId, string? PreviewIdentityId)
		{
			return this.activeSessionId == SessionId &&
				this.reference?.IsReservedPreviewIdentity(PreviewIdentityId) == true &&
				!this.HasReadout &&
				this.FlowState != KycTravelDocumentFlowState.Success;
		}

		private void TrackTerminalNfcSession(Guid SessionId, string Outcome, string FailureCode)
		{
			if (this.activeSessionId != SessionId || this.reportedTelemetrySessionId == SessionId)
				return;

			this.reportedTelemetrySessionId = SessionId;
			long DurationMs = Math.Max(0, (long)(DateTime.UtcNow - (this.activeSessionStartedUtc ?? DateTime.UtcNow)).TotalMilliseconds);
			try
			{
				ServiceRef.LogService.LogTelemetryEvent(
					Constants.LogEventIds.TravelDocumentNfcScanTelemetry,
					Constants.LogEventIds.TravelDocumentNfcScanTelemetry,
					new KeyValuePair<string, object?>("KycTemplateId", NormalizeTelemetryValue(this.reference?.KycTemplateId)),
					new KeyValuePair<string, object?>("AttemptId", SessionId.ToString("N")),
					new KeyValuePair<string, object?>("Outcome", Outcome),
					new KeyValuePair<string, object?>("FailureCode", FailureCode),
					new KeyValuePair<string, object?>("DurationMs", DurationMs));
			}
			catch
			{
			}
		}

		private static string NormalizeTelemetryValue(string? Value)
		{
			string Normalized = Value?.Trim() ?? string.Empty;
			return string.IsNullOrWhiteSpace(Normalized) ? "Unknown" : Normalized;
		}

		private void CancelAndDisposeActiveSessionCancellation()
		{
			CancellationTokenSource? CancellationTokenSource = this.activeSessionCancellationTokenSource;
			this.activeSessionCancellationTokenSource = null;
			if (CancellationTokenSource is null)
				return;

			try
			{
				CancellationTokenSource.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
			finally
			{
				CancellationTokenSource.Dispose();
			}
		}

		/// <summary>
		/// Releases resources used by the view model.
		/// </summary>
		/// <param name="Disposing">True when called from <see cref="Dispose()"/>.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.disposedValue)
				return;

			if (Disposing)
				this.CancelAndDisposeActiveSessionCancellation();

			this.disposedValue = true;
		}

		/// <summary>
		/// Releases resources used by the view model.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private string ResolveProgressStateText(int StepIndex)
		{
			if (this.FlowState == KycTravelDocumentFlowState.Success || this.HasReadout)
				return ServiceRef.Localizer["KycTravelDocumentProgressDone"];

			if (this.FlowState == KycTravelDocumentFlowState.Error || this.HasErrorState)
				return ServiceRef.Localizer["KycTravelDocumentProgressNeedsAttention"];

			if (this.FlowState == KycTravelDocumentFlowState.NfcReading)
				return StepIndex == 0
					? ServiceRef.Localizer["KycTravelDocumentProgressDone"]
					: ServiceRef.Localizer["KycTravelDocumentProgressInProgress"];

			return ServiceRef.Localizer["KycTravelDocumentProgressWaiting"];
		}

		private string ResolveReadoutFailureResourceKey(TravelDocumentReadoutStatus Status)
		{
			return Status switch
			{
				TravelDocumentReadoutStatus.AuthenticationFailed => "KycTravelDocumentNfcAuthenticationFailed",
				TravelDocumentReadoutStatus.ReadFailed => "KycTravelDocumentNfcReadFailed",
				TravelDocumentReadoutStatus.ConnectionLost => "KycTravelDocumentNfcConnectionLost",
				TravelDocumentReadoutStatus.TimedOut => "KycTravelDocumentNfcTimedOut",
				_ => "KycTravelDocumentNfcFailed"
			};
		}

		private string ResolveNfcFailureResourceKey(NfcIsoDepSessionFailure Failure)
		{
			return Failure.FailureCode switch
			{
				NfcIsoDepSessionFailureCode.NotSupported => "KycTravelDocumentNfcUnavailable",
				NfcIsoDepSessionFailureCode.Unavailable => "KycTravelDocumentNfcUnavailable",
				NfcIsoDepSessionFailureCode.Cancelled => "KycTravelDocumentNfcCancelled",
				NfcIsoDepSessionFailureCode.TimedOut => "KycTravelDocumentNfcTimedOut",
				NfcIsoDepSessionFailureCode.MultipleTagsDetected => "KycTravelDocumentNfcMultipleTags",
				NfcIsoDepSessionFailureCode.UnsupportedTag => "KycTravelDocumentNfcUnsupportedTag",
				NfcIsoDepSessionFailureCode.ConnectionLost => "KycTravelDocumentNfcConnectionLost",
				NfcIsoDepSessionFailureCode.EntitlementMissing => "KycTravelDocumentNfcUnavailable",
				_ => "KycTravelDocumentNfcFailed"
			};
		}

		private string ResolveInitialStatusText(bool HasInsufficientSavedMrz)
		{
			if (!string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml))
				return ServiceRef.Localizer["KycTravelDocumentReadoutReady"];

			if (HasInsufficientSavedMrz)
				return ServiceRef.Localizer["KycTravelDocumentInvalidMrz"];

			if (this.reference?.HasFullTravelDocumentMrz == true)
				return ServiceRef.Localizer["KycTravelDocumentSummaryMrzReady"];

			return string.Empty;
		}

		private static string JoinNameParts(string[]? Parts)
		{
			return string.Join(" ", Parts ?? Array.Empty<string>()).Trim();
		}

		private static void AddBirthDateMappings(IDictionary<string, string> Values, string DateOfBirth)
		{
			Values[Constants.XmppProperties.BirthDay] = DateOfBirth;
			Values[Constants.XmppProperties.BirthMonth] = DateOfBirth;
			Values[Constants.XmppProperties.BirthYear] = DateOfBirth;
		}

		private static string ResolveMappedFieldValue(
			ObservableKycField Field,
			IReadOnlyDictionary<string, string> Values)
		{
			List<KycMapping> SupportedMappings = Field.Mappings
				.Where(Mapping => Values.TryGetValue(Mapping.Key, out string? Value) && !string.IsNullOrWhiteSpace(Value))
				.ToList();
			if (SupportedMappings.Count == 0)
				return string.Empty;

			if (SupportedMappings.Any(Mapping => !IsBirthDateMapping(Mapping.Key)))
			{
				return SupportedMappings.Count == 1
					? Values[SupportedMappings[0].Key]
					: string.Empty;
			}

			KycMapping BirthDateMapping = SupportedMappings[0];
			string DateValue = Values[BirthDateMapping.Key];
			bool UsesDateTransform = BirthDateMapping.TransformNames.Any(Name =>
				string.Equals(Name, "day", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Name, "month", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Name, "year", StringComparison.OrdinalIgnoreCase));
			if (UsesDateTransform || SupportedMappings.Count > 1 || Field.FieldType == FieldType.Date)
				return DateValue;

			if (!DateOnly.TryParseExact(DateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly DateOfBirth))
				return string.Empty;

			if (string.Equals(BirthDateMapping.Key, Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase))
				return DateOfBirth.Day.ToString(CultureInfo.InvariantCulture);

			if (string.Equals(BirthDateMapping.Key, Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase))
				return DateOfBirth.Month.ToString(CultureInfo.InvariantCulture);

			return DateOfBirth.Year.ToString(CultureInfo.InvariantCulture);
		}

		private static bool IsBirthDateMapping(string Mapping)
		{
			return string.Equals(Mapping, Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Mapping, Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(Mapping, Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase);
		}

		private static string ResolveAlpha2CountryCode(string? CountryCode)
		{
			string Normalized = CountryCode?.Replace("<", string.Empty).Trim().ToUpperInvariant() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(Normalized))
				return string.Empty;

			if (ISO_3166_1.TryGetCountryByCode(Normalized, out ISO_3166_Country? Country))
				return Country.Alpha2;

			Country = ISO_3166_1.Countries.FirstOrDefault(Item =>
				string.Equals(Item.Alpha3, Normalized, StringComparison.OrdinalIgnoreCase));
			if (Country is not null)
				return Country.Alpha2;

			return string.Empty;
		}

		private static bool TryResolveGenderCode(string? Gender, out string GenderCode)
		{
			GenderCode = Gender?.Trim().ToUpperInvariant() ?? string.Empty;
			if ((string.Equals(GenderCode, "M", StringComparison.Ordinal) ||
				string.Equals(GenderCode, "F", StringComparison.Ordinal)) &&
				ISO_5218.LetterToGender(GenderCode, out _))
			{
				return true;
			}

			GenderCode = string.Empty;
			return false;
		}

		private static async Task<string> ResolvePersonalNumberAsync(string? OptionalData, string CountryCode)
		{
			string Candidate = OptionalData?.Replace("<", string.Empty).Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(Candidate) || string.IsNullOrWhiteSpace(CountryCode))
				return string.Empty;

			NumberInformation NumberInformation = await PersonalNumberSchemes.Validate(CountryCode, Candidate);
			if (NumberInformation.IsValid == true && !string.IsNullOrWhiteSpace(NumberInformation.PersonalNumber))
				return NumberInformation.PersonalNumber.Trim();

			return string.Empty;
		}

		private static bool TryParseMrzBirthDate(string? Value, out string DateOfBirth)
		{
			DateOfBirth = string.Empty;
			string Normalized = Value?.Replace("-", string.Empty).Trim() ?? string.Empty;
			if (Normalized.Length != 6 && Normalized.Length != 8)
				return false;

			int YearLength = Normalized.Length == 8 ? 4 : 2;
			if (!int.TryParse(Normalized[..YearLength], NumberStyles.Integer, CultureInfo.InvariantCulture, out int Year) ||
				!int.TryParse(Normalized.Substring(YearLength, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int Month) ||
				!int.TryParse(Normalized.Substring(YearLength + 2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int Day))
			{
				return false;
			}

			try
			{
				DateOnly Parsed = new DateOnly(YearLength == 2 ? 2000 + Year : Year, Month, Day);
				if (YearLength == 2 && Parsed > DateOnly.FromDateTime(DateTime.UtcNow.Date))
					Parsed = Parsed.AddYears(-100);

				DateOfBirth = Parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
