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
		/// Gets or sets a value indicating whether a status message should be displayed.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
		[ObservableProperty]
		private bool hasStatusText;

		/// <summary>
		/// Gets or sets a value indicating whether an NFC readout is in progress.
		/// </summary>
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
		/// Gets a value indicating whether the user can rescan the document after a recoverable NFC error.
		/// </summary>
		public bool ShowRescanAction => this.HasMrz && this.HasErrorState && !this.IsNfcBusy && !this.HasReadout;

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
			? ServiceRef.Localizer["KycTravelDocumentNfcReadingTitle"]
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
			if (!this.nfcIsoDepSessionService.IsPlatformSupported)
			{
				await this.ReturnToApplicationAsync();
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

			if (!this.nfcIsoDepSessionService.IsPlatformSupported)
			{
				this.LogFlowEvent("StartNfcUnsupported");
				await this.ReturnToApplicationAsync();
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
			Evidence = await this.BindReservedPreviewIdentityAsync(Evidence, CancellationToken.None);
			if (Evidence is null || string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId))
			{
				this.LogFlowEvent("StartNfcReservationMissing");
				await this.SetStatusAsync("KycTravelDocumentNfcReservationFailed", false, true, false, KycTravelDocumentFlowState.Error);
				return;
			}

			Guid SessionId = Guid.NewGuid();
			this.activeSessionId = SessionId;
			this.activeSessionCancellationTokenSource = new CancellationTokenSource();
			this.LogFlowEvent(
				"StartNfcSessionStarting",
				new KeyValuePair<string, object?>("SessionId", SessionId),
				new KeyValuePair<string, object?>("EvidenceMrzLength", Evidence.MrzText.Length),
				new KeyValuePair<string, object?>("HasApplicationIdentityId", !string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId)));
			await this.SetStatusAsync("KycTravelDocumentNfcReady", true, false, false, KycTravelDocumentFlowState.NfcReady);

			try
			{
				await this.nfcIsoDepSessionService.StartSessionAsync(
					SessionId,
					(IsoDepInterface, CancellationToken) => this.ReadTravelDocumentAsync(SessionId, Evidence, IsoDepInterface, CancellationToken),
					(Failure, CancellationToken) => this.HandleNfcFailureAsync(SessionId, Failure, CancellationToken),
					NfcIsoDepPollingPreference.Auto,
					ServiceRef.Localizer["KycTravelDocumentNfcReady"],
					CancellationToken.None);
				this.LogFlowEvent(
					"StartNfcSessionStarted",
					new KeyValuePair<string, object?>("SessionId", SessionId));
			}
			catch (Exception Ex)
			{
				this.LogFlowEvent(
					"StartNfcSessionFailed",
					new KeyValuePair<string, object?>("SessionId", SessionId),
					new KeyValuePair<string, object?>("ExceptionType", Ex.GetType().Name));
				throw;
			}
		}

		[RelayCommand]
		private async Task ReturnToApplicationAsync()
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
				}, BackMethod.CurrentPage);
			}
			else
				await ServiceRef.NavigationService.GoBackAsync();
		}

		private async Task ReadTravelDocumentAsync(
			Guid SessionId,
			TravelDocumentMrzEvidence Evidence,
			IIsoDepInterface IsoDepInterface,
			CancellationToken CancellationToken)
		{
			if (!this.IsActiveSession(SessionId))
			{
				this.LogFlowEvent(
					"ReadNfcIgnoredInactiveSession",
					new KeyValuePair<string, object?>("SessionId", SessionId));
				return;
			}

			this.LogFlowEvent(
				"ReadNfcStarted",
				new KeyValuePair<string, object?>("SessionId", SessionId),
				new KeyValuePair<string, object?>("HasApplicationIdentityId", !string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId)));

			if (string.IsNullOrWhiteSpace(Evidence.ApplicationIdentityId))
			{
				this.LogFlowEvent(
					"ReadNfcRejectedMissingApplicationIdentityId",
					new KeyValuePair<string, object?>("SessionId", SessionId));
				await this.SetStatusAsync("KycTravelDocumentNfcReservationFailed", false, true, false, KycTravelDocumentFlowState.Error);
				await this.CompleteNfcSessionAsync(SessionId);
				return;
			}

			CancellationTokenSource? ActiveCancellationTokenSource = this.activeSessionCancellationTokenSource;
			using CancellationTokenSource LinkedCancellationTokenSource = ActiveCancellationTokenSource is null
				? CancellationTokenSource.CreateLinkedTokenSource(CancellationToken)
				: CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, ActiveCancellationTokenSource.Token);
			CancellationToken ReadCancellationToken = LinkedCancellationTokenSource.Token;

			await this.nfcIsoDepSessionService.UpdateSessionAlertAsync(
				SessionId,
				ServiceRef.Localizer["KycTravelDocumentNfcReading"],
				ReadCancellationToken);
			await this.SetStatusAsync("KycTravelDocumentNfcReading", true, false, false, KycTravelDocumentFlowState.NfcReading);

			try
			{
				TravelDocumentReadoutRequest Request = new TravelDocumentReadoutRequest(
					Evidence.DocumentInformation,
					Evidence.MrzText,
					Evidence.ApplicationIdentityId);
				TravelDocumentReadoutResult Result = await this.readoutService.ReadAsync(IsoDepInterface, Request, ReadCancellationToken);

				if (!this.IsActiveSession(SessionId))
				{
					this.LogFlowEvent(
						"ReadNfcResultIgnoredInactiveSession",
						new KeyValuePair<string, object?>("SessionId", SessionId),
						new KeyValuePair<string, object?>("Status", Result.Status.ToString()));
					return;
				}

				this.LogFlowEvent(
					"ReadNfcResultReceived",
					new KeyValuePair<string, object?>("SessionId", SessionId),
					new KeyValuePair<string, object?>("Status", Result.Status.ToString()),
					new KeyValuePair<string, object?>("IsSuccess", Result.IsSuccess),
					new KeyValuePair<string, object?>("XmlLength", Result.Xml?.Length ?? 0));

				if (Result.IsSuccess &&
					!string.IsNullOrWhiteSpace(Result.Xml) &&
					await this.SaveReadoutXmlAsync(Result.Xml, ReadCancellationToken))
				{
					await this.SetStatusAsync("KycTravelDocumentNfcSuccess", false, false, true, KycTravelDocumentFlowState.Success);
				}
				else
				{
					await this.UploadFailedReservedPreviewReadoutAsync(Result);
					await this.SetStatusAsync(this.ResolveReadoutFailureResourceKey(Result.Status), false, true, false, KycTravelDocumentFlowState.Error);
				}
			}
			catch (OperationCanceledException) when (ReadCancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				await this.CompleteNfcSessionAsync(SessionId);
			}
		}

		private async Task HandleNfcFailureAsync(
			Guid SessionId,
			NfcIsoDepSessionFailure Failure,
			CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (!this.IsActiveSession(SessionId))
			{
				this.LogFlowEvent(
					"NfcFailureIgnoredInactiveSession",
					new KeyValuePair<string, object?>("SessionId", SessionId),
					new KeyValuePair<string, object?>("FailureCode", Failure.FailureCode.ToString()));
				return;
			}

			this.LogFlowEvent(
				"NfcFailureReceived",
				new KeyValuePair<string, object?>("SessionId", SessionId),
				new KeyValuePair<string, object?>("FailureCode", Failure.FailureCode.ToString()));

			await this.SetStatusAsync(this.ResolveNfcFailureResourceKey(Failure), false, true, false, KycTravelDocumentFlowState.Error);
			await this.ForgetReservedPreviewIdentityAsync("NfcSessionFailureReservationForgotten");

			if (this.activeSessionId == SessionId)
				this.activeSessionId = null;

			this.CancelAndDisposeActiveSessionCancellation();
			await this.nfcIsoDepSessionService.StopSessionAsync(SessionId, CancellationToken.None);
		}

		private async Task CompleteNfcSessionAsync(Guid SessionId)
		{
			if (this.activeSessionId == SessionId)
				this.activeSessionId = null;

			this.CancelAndDisposeActiveSessionCancellation();
			await this.nfcIsoDepSessionService.StopSessionAsync(SessionId, CancellationToken.None);
			await MainThread.InvokeOnMainThreadAsync(() => this.IsNfcBusy = false);
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
				return new TravelDocumentMrzEvidence(
					Evidence.MrzText,
					Evidence.DocumentInformation,
					ExistingIdentity.Id);
			}

			IReadOnlyList<Property> ReservationProperties = await ServiceRef.KycService
				.PreparePreviewReservationPropertiesAsync(this.reference, CancellationToken)
				.ConfigureAwait(false);
			bool GenerateNewKeys = !await this.HasCurrentIdentityPrivateKeyAsync().ConfigureAwait(false);
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

		private async Task<bool> HasCurrentIdentityPrivateKeyAsync()
		{
			string IdentityId = ServiceRef.TagProfile.LegalIdentity?.Id?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(IdentityId))
				return false;

			return await this.HasPrivateKeyAsync(IdentityId).ConfigureAwait(false);
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

		private async Task<bool> SaveReadoutXmlAsync(string Xml, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrWhiteSpace(Xml))
				return false;

			if (this.reference is not null)
			{
				await this.SaveReferenceEvidenceAsync(null, Xml);
				return true;
			}

			return await this.evidenceService.SaveReadoutXmlAsync(Xml, CancellationToken);
		}

		private async Task UploadFailedReservedPreviewReadoutAsync(TravelDocumentReadoutResult Result)
		{
			if (this.reference is null ||
				string.IsNullOrWhiteSpace(Result.Xml))
			{
				return;
			}

			string ReservedPreviewIdentityId = this.reference.ReservedPreviewIdentityId?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(ReservedPreviewIdentityId))
				return;

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

			await this.ForgetReservedPreviewIdentityAsync("FailedPreviewReadoutReservationForgotten").ConfigureAwait(false);
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

			await ServiceRef.KycService.ForgetReservedPreviewIdentityAsync(this.reference, ReservedPreviewIdentityId).ConfigureAwait(false);
			this.LogFlowEvent(
				EventName,
				new KeyValuePair<string, object?>("HasReservedPreviewIdentityId", true));
		}

		private async Task SaveReferenceEvidenceAsync(string? MrzText, string? ReadoutXml, DocumentInformation? DocumentInformation = null)
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
			}

			if (DocumentInformation is not null)
				await this.SeedReferenceFieldsFromMrzAsync(DocumentInformation);

			await ServiceRef.KycService.SaveKycReferenceAsync(this.reference);
			this.LogFlowEvent(
				"ReferenceEvidenceSaved",
				new KeyValuePair<string, object?>("SavedMrz", !string.IsNullOrWhiteSpace(MrzText)),
				new KeyValuePair<string, object?>("SavedReadout", !string.IsNullOrWhiteSpace(ReadoutXml)),
				new KeyValuePair<string, object?>("ReferenceHasFullMrz", this.reference.HasFullTravelDocumentMrz),
				new KeyValuePair<string, object?>("ReferenceHasReadout", !string.IsNullOrWhiteSpace(this.reference.NfcReadoutXml)));
		}

		private async Task SeedReferenceFieldsFromMrzAsync(DocumentInformation DocumentInformation)
		{
			if (this.reference is null)
				return;

			string CountryCode = ResolveIssuingCountryCode(DocumentInformation);
			Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["firstNames"] = JoinNameParts(DocumentInformation.SecondaryIdentifier),
				["lastNames"] = JoinNameParts(DocumentInformation.PrimaryIdentifier)
			};

			if (!string.IsNullOrWhiteSpace(CountryCode))
				Values["country"] = CountryCode;

			if (TryResolveGenderCode(DocumentInformation.Gender, out string GenderCode))
				Values["gender"] = GenderCode;

			string PersonalNumber = await ResolvePersonalNumberAsync(DocumentInformation, CountryCode);
			if (!string.IsNullOrWhiteSpace(PersonalNumber))
				Values["personalNumber"] = PersonalNumber;

			if (TryParseMrzBirthDate(DocumentInformation.DateOfBirth, out string DateOfBirth))
				Values["dob"] = DateOfBirth;

			List<KycFieldValue> Fields = this.reference.Fields?.ToList() ?? new List<KycFieldValue>();
			foreach (KeyValuePair<string, string> Pair in Values)
			{
				if (string.IsNullOrWhiteSpace(Pair.Value))
					continue;

				KycFieldValue? Existing = Fields.FirstOrDefault(Field => string.Equals(Field.FieldId, Pair.Key, StringComparison.Ordinal));
				if (Existing is null)
					Fields.Add(new KycFieldValue(Pair.Key, Pair.Value));
				else
					Existing.Value = Pair.Value;
			}

			this.reference.Fields = Fields.ToArray();
			string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			await this.reference.ApplyFieldsToProcessAsync(Language);
		}

		private async Task StopActiveNfcSessionAsync()
		{
			Guid? SessionId = this.activeSessionId;
			if (!SessionId.HasValue)
				return;

			this.activeSessionId = null;
			this.CancelAndDisposeActiveSessionCancellation();
			await this.nfcIsoDepSessionService.StopSessionAsync(SessionId.Value, CancellationToken.None);
			await MainThread.InvokeOnMainThreadAsync(() => this.IsNfcBusy = false);
		}

		private Task SetStatusAsync(
			string ResourceKey,
			bool IsBusy,
			bool IsError = false,
			bool ReadoutAvailable = false,
			KycTravelDocumentFlowState? FlowState = null)
		{
			string Message = ServiceRef.Localizer[ResourceKey];
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.StatusText = Message;
				this.HasStatusText = !string.IsNullOrWhiteSpace(Message);
				this.IsNfcBusy = IsBusy;
				this.HasErrorState = IsError;
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

		private bool IsActiveSession(Guid SessionId)
		{
			return this.activeSessionId == SessionId && !this.HasReadout && this.FlowState != KycTravelDocumentFlowState.Success;
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

		private static string ResolveIssuingCountryCode(DocumentInformation DocumentInformation)
		{
			return ResolveAlpha2CountryCode(DocumentInformation.IssuingState);
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

		private static async Task<string> ResolvePersonalNumberAsync(DocumentInformation DocumentInformation, string CountryCode)
		{
			string Candidate = DocumentInformation.OptionalData?.Replace("<", string.Empty).Trim() ?? string.Empty;
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
			string Normalized = Value?.Trim() ?? string.Empty;
			if (Normalized.Length != 6)
				return false;

			if (!int.TryParse(Normalized[..2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int Year) ||
				!int.TryParse(Normalized.Substring(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int Month) ||
				!int.TryParse(Normalized.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int Day))
			{
				return false;
			}

			try
			{
				DateOnly Parsed = new DateOnly(2000 + Year, Month, Day);
				if (Parsed > DateOnly.FromDateTime(DateTime.UtcNow.Date))
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
