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
using NeuroAccessMaui.UI.Controls;
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
		/// The user is viewing where the machine-readable lines are, before the scanner opens.
		/// </summary>
		MrzGuide,

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
		private const string mrzGuideTitleResourceKey = "KycTravelDocumentMrzGuideTitle";
		private static readonly TimeSpan placementHelpDelay = TimeSpan.FromSeconds(12);
		private static readonly TimeSpan failureHapticInterval = TimeSpan.FromMilliseconds(140);
		private static readonly string[] presentationPropertyNames =
		[
			nameof(HasMrz),
			nameof(CanStartNfc),
			nameof(ShowIntro),
			nameof(ShowGuideStepper),
			nameof(ShowMrzGuide),
			nameof(ShowMrzGuideNotice),
			nameof(ShowMrzGuideError),
			nameof(ShowMrzGuideAction),
			nameof(MrzGuideTitleText),
			nameof(ShowSuccess),
			nameof(ShowNfcMessage),
			nameof(ShowStartNfcAction),
			nameof(PrimaryRecoveryAction),
			nameof(SecondaryRecoveryAction),
			nameof(ShowPrimaryRecoveryAction),
			nameof(ShowSecondaryRecoveryAction),
			nameof(PrimaryRecoveryText),
			nameof(SecondaryRecoveryText),
			nameof(ShowRescanAction),
			nameof(ShowManualFallback),
			nameof(HasFooterActions),
			nameof(ScanVisualState),
			nameof(IsPassportDocument),
			nameof(CurrentGuideStep),
			nameof(NfcHeadlineText),
			nameof(ShowPlacementTip),
			nameof(PlacementTipText)
		];

		private readonly ITravelDocumentEvidenceService evidenceService = ServiceRef.Provider.GetRequiredService<ITravelDocumentEvidenceService>();
		private readonly INfcIsoDepSessionService nfcIsoDepSessionService = ServiceRef.Provider.GetRequiredService<INfcIsoDepSessionService>();
		private readonly ITravelDocumentReadoutService readoutService = ServiceRef.Provider.GetRequiredService<ITravelDocumentReadoutService>();
		private readonly TravelDocumentReadProgress readProgress = new TravelDocumentReadProgress();
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycReference? reference;
		private Guid? activeSessionId;
		private Guid? finalizingSessionId;
		private Guid? reportedTelemetrySessionId;
		private DateTime? activeSessionStartedUtc;
		private CancellationTokenSource? activeSessionCancellationTokenSource;
		private string? lastNativeAlertResourceKey;
		private string? mrzGuideTitleKey;
		private bool hasAppeared;
		private bool wasNfcSessionInterrupted;
		private bool isMrzScanPending;
		private bool hasReachedChipStep;
		private bool disposedValue;

		/// <summary>
		/// Gets or sets the current guided travel-document flow state.
		/// </summary>
		[ObservableProperty]
		private KycTravelDocumentFlowState flowState = KycTravelDocumentFlowState.Intro;

		/// <summary>
		/// Gets or sets the MRZ text entered for the current KYC application.
		/// </summary>
		[ObservableProperty]
		private string mrzText = string.Empty;

		/// <summary>
		/// Gets or sets the current evidence or NFC status message.
		/// </summary>
		[ObservableProperty]
		private string statusText = string.Empty;

		/// <summary>
		/// Gets or sets the chip read progress for the current attempt, from 0 (waiting) to 1 (saved).
		/// </summary>
		[ObservableProperty]
		private double chipReadProgress;

		/// <summary>
		/// Gets or sets a value indicating whether a status message should be displayed.
		/// </summary>
		[ObservableProperty]
		private bool hasStatusText;

		/// <summary>
		/// Gets or sets a value indicating whether an NFC readout is in progress.
		/// </summary>
		[ObservableProperty]
		private bool isNfcBusy;

		/// <summary>
		/// Gets or sets a value indicating whether NFC readout XML is available.
		/// </summary>
		[ObservableProperty]
		private bool hasReadout;

		/// <summary>
		/// Gets or sets a value indicating whether the current status represents a recoverable error.
		/// </summary>
		[ObservableProperty]
		private bool hasErrorState;

		/// <summary>
		/// Gets or sets a value indicating whether the last attempt ended because the scan was cancelled.
		/// </summary>
		[ObservableProperty]
		private bool isScanCancelled;

		/// <summary>
		/// Gets or sets a value indicating whether extended placement help is shown after searching for a while.
		/// </summary>
		[ObservableProperty]
		private bool isPlacementHelpExpanded;

		/// <summary>
		/// Gets or sets the chip read failure currently shown, or <c>null</c> when no chip read has failed.
		/// </summary>
		[ObservableProperty]
		private TravelDocumentFailure? currentFailure;

		/// <summary>
		/// Gets or sets a value indicating whether the initial flow state has been resolved, so the page can show it
		/// without first showing the default state.
		/// </summary>
		[ObservableProperty]
		private bool isStateResolved;

		/// <summary>
		/// Gets a value indicating whether MRZ evidence has been captured.
		/// </summary>
		public bool HasMrz => !string.IsNullOrWhiteSpace(this.MrzText);

		/// <summary>
		/// Gets a value indicating whether the NFC readout can be started.
		/// </summary>
		public bool CanStartNfc => this.HasMrz && !this.IsNfcBusy && !this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether the introductory chip-symbol check should be shown.
		/// </summary>
		public bool ShowIntro => !this.ShowSuccess && this.FlowState == KycTravelDocumentFlowState.Intro;

		/// <summary>
		/// Gets a value indicating whether the step guide should be shown. It appears once the user has chosen the chip
		/// path, since the steps mean nothing while the user is still checking whether the document has a chip.
		/// </summary>
		public bool ShowGuideStepper => this.IsStateResolved && !this.ShowIntro;

		/// <summary>
		/// Gets a value indicating whether the guide to the machine-readable lines should be shown. It stays behind the
		/// scanner while it is open, and it is where scanning problems are explained.
		/// </summary>
		public bool ShowMrzGuide =>
			!this.ShowSuccess &&
			(this.FlowState == KycTravelDocumentFlowState.MrzGuide ||
			this.FlowState == KycTravelDocumentFlowState.MrzCapture ||
			(this.FlowState == KycTravelDocumentFlowState.Error && !this.HasMrz));

		/// <summary>
		/// Gets the headline of the guide, which names a scanning problem when there is one.
		/// </summary>
		public string MrzGuideTitleText => ServiceRef.Localizer[this.mrzGuideTitleKey ?? mrzGuideTitleResourceKey];

		/// <summary>
		/// Gets a value indicating whether a neutral notice, such as a cancelled scan, is shown in the guide.
		/// </summary>
		public bool ShowMrzGuideNotice => this.ShowMrzGuide && this.HasStatusText && !this.HasErrorState;

		/// <summary>
		/// Gets a value indicating whether a scanning problem, such as missing camera access, is shown in the guide.
		/// </summary>
		public bool ShowMrzGuideError => this.ShowMrzGuide && this.HasStatusText && this.HasErrorState;

		/// <summary>
		/// Gets a value indicating whether the action that opens the scanner from the guide should be shown.
		/// </summary>
		public bool ShowMrzGuideAction => this.ShowMrzGuide && !this.IsNfcBusy;

		/// <summary>
		/// Gets a value indicating whether the readout success state should be shown.
		/// </summary>
		public bool ShowSuccess =>
			this.FlowState == KycTravelDocumentFlowState.Success ||
			this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether the chip-reading headline and status should be shown.
		/// </summary>
		public bool ShowNfcMessage => !this.ShowIntro && !this.ShowMrzGuide;

		/// <summary>
		/// Gets a value indicating whether the action that starts a first chip read should be shown.
		/// </summary>
		public bool ShowStartNfcAction => this.CanStartNfc && this.ShowNfcMessage && !this.HasErrorState;

		/// <summary>
		/// Gets the main action offered after a failed or cancelled chip read.
		/// </summary>
		public TravelDocumentRecoveryAction PrimaryRecoveryAction => this.IsScanCancelled
			? TravelDocumentRecoveryAction.RetryCurrentStep
			: TravelDocumentPresentationMapper.GetPrimaryAction(this.CurrentFailure?.RecoveryAction ?? TravelDocumentRecoveryAction.RetryCurrentStep);

		/// <summary>
		/// Gets the alternative action offered after a failed chip read, or <see cref="TravelDocumentRecoveryAction.None"/>.
		/// </summary>
		public TravelDocumentRecoveryAction SecondaryRecoveryAction => this.IsScanCancelled
			? TravelDocumentRecoveryAction.None
			: TravelDocumentPresentationMapper.GetSecondaryAction(this.CurrentFailure?.RecoveryAction ?? TravelDocumentRecoveryAction.RetryCurrentStep);

		/// <summary>
		/// Gets a value indicating whether the main recovery action should be shown.
		/// </summary>
		public bool ShowPrimaryRecoveryAction => this.ShowRecoveryActions && this.PrimaryRecoveryAction != TravelDocumentRecoveryAction.None;

		/// <summary>
		/// Gets a value indicating whether the alternative recovery action should be shown.
		/// </summary>
		public bool ShowSecondaryRecoveryAction => this.ShowRecoveryActions && this.SecondaryRecoveryAction != TravelDocumentRecoveryAction.None;

		/// <summary>
		/// Gets the label of the main recovery action.
		/// </summary>
		public string PrimaryRecoveryText => this.GetRecoveryActionText(this.PrimaryRecoveryAction);

		/// <summary>
		/// Gets the label of the alternative recovery action.
		/// </summary>
		public string SecondaryRecoveryText => this.GetRecoveryActionText(this.SecondaryRecoveryAction);

		/// <summary>
		/// Gets a value indicating whether the manual fallback action should be shown.
		/// </summary>
		public bool ShowManualFallback => !this.ShowIntro && !this.HasReadout && !this.IsNfcBusy &&
			!(this.ShowPrimaryRecoveryAction && this.PrimaryRecoveryAction == TravelDocumentRecoveryAction.CloseFlow);

		/// <summary>
		/// Gets whether the document can be rescanned to recover a legacy draft whose chip readout must be redone.
		/// </summary>
		public bool ShowRescanAction => !this.IsNfcBusy &&
			!this.ShowMrzGuide &&
			!this.ShowRecoveryActions &&
			this.reference?.CanRescanLegacyNfcReadout == true;

		/// <summary>
		/// Gets a value indicating whether the footer has an action to show, so it stays hidden while the chip is read.
		/// </summary>
		public bool HasFooterActions => this.IsStateResolved &&
			(this.ShowIntro ||
			this.ShowMrzGuideAction ||
			this.ShowSuccess ||
			this.ShowStartNfcAction ||
			this.ShowPrimaryRecoveryAction ||
			this.ShowSecondaryRecoveryAction ||
			this.ShowRescanAction ||
			this.ShowManualFallback);

		/// <summary>
		/// Gets the scene shown by the NFC hero visual.
		/// </summary>
		public NfcScanVisualState ScanVisualState
		{
			get
			{
				if (this.ShowSuccess)
					return NfcScanVisualState.Success;

				if (this.ShowIntro)
					return NfcScanVisualState.Intro;

				if (this.ShowMrzGuide)
					return NfcScanVisualState.Details;

				if (this.HasErrorState)
					return this.IsScanCancelled ? NfcScanVisualState.Paused : NfcScanVisualState.Failure;

				if (this.FlowState == KycTravelDocumentFlowState.NfcReading)
					return NfcScanVisualState.Reading;

				if (this.IsNfcBusy)
				{
					return this.FlowState == KycTravelDocumentFlowState.NfcReady
						? NfcScanVisualState.Searching
						: NfcScanVisualState.Preparing;
				}

				return NfcScanVisualState.Paused;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the document is a passport, based on the MRZ document code.
		/// </summary>
		/// <remarks>
		/// Passports (TD3) have MRZ document codes starting with "P". Without an MRZ, a passport is assumed.
		/// </remarks>
		public bool IsPassportDocument
		{
			get
			{
				string Mrz = this.MrzText?.TrimStart() ?? string.Empty;
				return Mrz.Length == 0 || char.ToUpperInvariant(Mrz[0]) == 'P';
			}
		}

		/// <summary>
		/// Gets the zero-based active step of the guide: scan details, read chip, and done.
		/// </summary>
		/// <remarks>
		/// A value of 3 marks every step as complete.
		/// </remarks>
		public int CurrentGuideStep
		{
			get
			{
				if (this.ShowSuccess)
					return 3;

				return this.ShowIntro || this.ShowMrzGuide ? 0 : 1;
			}
		}

		/// <summary>
		/// Gets the headline for the current chip-reading state. A failed read is named by its failure code.
		/// </summary>
		public string NfcHeadlineText => ServiceRef.Localizer[this.ScanVisualState switch
		{
			NfcScanVisualState.Preparing => "KycTravelDocumentNfcPreparingTitle",
			NfcScanVisualState.Reading => "KycTravelDocumentNfcReadingTitle",
			NfcScanVisualState.Success => "KycTravelDocumentSuccessTitle",
			NfcScanVisualState.Failure => TravelDocumentPresentationMapper.GetHeadlineResourceKey(
				this.CurrentFailure?.FailureCode ?? TravelDocumentFailureCode.Unknown),
			NfcScanVisualState.Paused when this.IsScanCancelled => "KycTravelDocumentNfcCancelledTitle",
			_ => "KycTravelDocumentNfcStepTitle"
		}];

		/// <summary>
		/// Gets a value indicating whether a placement tip should be shown while searching for the chip.
		/// </summary>
		public bool ShowPlacementTip => this.ScanVisualState == NfcScanVisualState.Searching;

		/// <summary>
		/// Gets the placement tip, which expands with document-specific help after searching for a while.
		/// </summary>
		public string PlacementTipText => ServiceRef.Localizer[!this.IsPlacementHelpExpanded
			? "KycTravelDocumentNfcStepDescription"
			: this.IsPassportDocument ? "KycTravelDocumentNfcHelpPassport" : "KycTravelDocumentNfcHelpIdCard"];

		partial void OnFlowStateChanged(KycTravelDocumentFlowState value) => this.NotifyPresentationChanged();

		partial void OnMrzTextChanged(string value) => this.NotifyPresentationChanged();

		partial void OnHasStatusTextChanged(bool value) => this.NotifyPresentationChanged();

		partial void OnIsNfcBusyChanged(bool value) => this.NotifyPresentationChanged();

		partial void OnHasReadoutChanged(bool value) => this.NotifyPresentationChanged();

		partial void OnHasErrorStateChanged(bool value) => this.NotifyPresentationChanged();

		partial void OnIsScanCancelledChanged(bool value) => this.NotifyPresentationChanged();

		partial void OnIsPlacementHelpExpandedChanged(bool value) => this.NotifyPresentationChanged();

		partial void OnCurrentFailureChanged(TravelDocumentFailure? value) => this.NotifyPresentationChanged();

		partial void OnIsStateResolvedChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.HasFooterActions));
			this.OnPropertyChanged(nameof(this.ShowGuideStepper));
		}

		/// <summary>
		/// Gets a value indicating whether actions that recover from a failed or cancelled chip read should be shown.
		/// </summary>
		private bool ShowRecoveryActions => this.CanStartNfc && this.ShowNfcMessage && this.HasErrorState;

		/// <summary>
		/// Raises change notifications for every property derived from the flow state.
		/// </summary>
		private void NotifyPresentationChanged()
		{
			foreach (string PropertyName in presentationPropertyNames)
				this.OnPropertyChanged(PropertyName);
		}

		private string GetRecoveryActionText(TravelDocumentRecoveryAction Action)
		{
			string ResourceKey = TravelDocumentPresentationMapper.GetActionResourceKey(Action);
			return string.IsNullOrEmpty(ResourceKey) ? string.Empty : ServiceRef.Localizer[ResourceKey];
		}

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

			// The page reappears when the scanner closes, while the scan result is still being handled. The state is
			// only resolved again when leaving the page interrupted a chip read.
			bool IsFirstAppearance = !this.hasAppeared;
			this.hasAppeared = true;
			if (!IsFirstAppearance && !this.wasNfcSessionInterrupted)
			{
				this.LogFlowEvent("AppearingStateKept");
				return;
			}

			this.wasNfcSessionInterrupted = false;
			this.reference = this.navigationArguments?.Reference ?? this.reference;
			this.LogFlowEvent(
				"Appearing",
				new KeyValuePair<string, object?>("NfcSupported", this.nfcIsoDepSessionService.IsPlatformSupported));

			// The gate is released on every path, so a failed check or a failed navigation never leaves the page blank.
			try
			{
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
				if (this.HasReadout)
				{
					this.readProgress.Complete();
					this.ChipReadProgress = this.readProgress.Progress;
				}

				this.mrzGuideTitleKey = null;
				this.CurrentFailure = null;
				this.IsScanCancelled = false;
				this.FlowState = this.ResolveInitialFlowState(HasInsufficientSavedMrz);
				this.StatusText = this.ResolveInitialStatusText(HasInsufficientSavedMrz);
				this.HasStatusText = !string.IsNullOrWhiteSpace(this.StatusText);
				this.HasErrorState = HasInsufficientSavedMrz && !this.HasReadout;
				this.LogFlowEvent("AppearingStateResolved");
			}
			finally
			{
				this.IsStateResolved = true;
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			this.wasNfcSessionInterrupted |= this.activeSessionId.HasValue;
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
		private void OpenMrzGuide()
		{
			if (this.IsNfcBusy || this.isMrzScanPending)
				return;

			this.mrzGuideTitleKey = null;
			this.CurrentFailure = null;
			this.StatusText = string.Empty;
			this.HasStatusText = false;
			this.IsScanCancelled = false;
			this.HasErrorState = false;
			this.FlowState = KycTravelDocumentFlowState.MrzGuide;
			this.NotifyPresentationChanged();
			this.LogFlowEvent("MrzGuideOpened");
		}

		[RelayCommand]
		private async Task ScanMrzAsync()
		{
			if (this.HasReadout && this.reference?.CanRescanLegacyNfcReadout != true)
				return;

			if (this.IsNfcBusy || this.isMrzScanPending)
			{
				this.LogFlowEvent("ScanMrzIgnoredBusy");
				return;
			}

			// A rescan from the chip step leaves that step in place, so cancelling the scanner returns to it unchanged.
			bool WasOnChipStep = this.ShowNfcMessage;
			this.isMrzScanPending = true;
			try
			{
				if (!WasOnChipStep)
					this.FlowState = KycTravelDocumentFlowState.MrzCapture;

				this.LogFlowEvent("ScanMrzStarting");
				TaskCompletionSource<TravelDocumentMrzResult?> CompletionSource = new TaskCompletionSource<TravelDocumentMrzResult?>(
					TaskCreationOptions.RunContinuationsAsynchronously);
				KycDocumentMrzScannerNavigationArgs ScannerArguments = new KycDocumentMrzScannerNavigationArgs
				{
					CompletionSource = CompletionSource,
					PreferredDocumentKind = OcrDocumentKindHint.Passport
				};

				await ServiceRef.NavigationService.GoToAsync(nameof(KycDocumentMrzScannerPage), ScannerArguments);

				this.LogFlowEvent("ScanMrzAwaitingResult");
				TravelDocumentMrzResult? Result = await CompletionSource.Task;
				string ScannedMrz = Result?.NormalizedMrzText ?? string.Empty;
				this.LogFlowEvent(
					"ScanMrzResultReceived",
					new KeyValuePair<string, object?>("ResultIsNull", Result is null),
					new KeyValuePair<string, object?>("ScannerOutcome", ScannerArguments.Outcome.ToString()),
					new KeyValuePair<string, object?>("IsSuccessful", Result?.IsSuccessful),
					new KeyValuePair<string, object?>("HasMrzText", !string.IsNullOrWhiteSpace(ScannedMrz)),
					new KeyValuePair<string, object?>("NormalizedMrzLength", ScannedMrz.Length),
					new KeyValuePair<string, object?>("ChipAccessMrzLength", Result?.Document?.MRZ_Information?.Length ?? 0),
					new KeyValuePair<string, object?>("DocumentType", Result?.Document?.DocumentType ?? string.Empty));
				if (Result is null ||
					!Result.IsSuccessful ||
					string.IsNullOrWhiteSpace(ScannedMrz))
				{
					await this.ShowMrzScanOutcomeAsync(ResolveUnacceptedScanOutcome(Result, ScannerArguments.Outcome), WasOnChipStep);
					return;
				}

				await this.AcceptScannedMrzAsync(Result, ScannedMrz);
			}
			finally
			{
				this.isMrzScanPending = false;
			}
		}

		/// <summary>
		/// Classifies a scanner session that ended without usable document details.
		/// </summary>
		/// <param name="Result">The scanner result, if one was delivered.</param>
		/// <param name="Outcome">How the scanner reported that the session ended.</param>
		/// <returns>The outcome to present.</returns>
		private static KycDocumentMrzScannerOutcome ResolveUnacceptedScanOutcome(
			TravelDocumentMrzResult? Result,
			KycDocumentMrzScannerOutcome Outcome)
		{
			// A delivered read that cannot be used is a failed scan, whatever the scanner reported.
			if (Result is not null)
				return KycDocumentMrzScannerOutcome.Failed;

			return Outcome switch
			{
				KycDocumentMrzScannerOutcome.CameraPermissionDenied or
				KycDocumentMrzScannerOutcome.CameraUnavailable or
				KycDocumentMrzScannerOutcome.Failed => Outcome,
				_ => KycDocumentMrzScannerOutcome.Cancelled
			};
		}

		/// <summary>
		/// Explains why the scanner closed without document details, on the step the user can continue from.
		/// </summary>
		/// <param name="Outcome">How the scanner session ended.</param>
		/// <param name="WasOnChipStep">Whether the scanner was opened from the chip step to rescan the document.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task ShowMrzScanOutcomeAsync(KycDocumentMrzScannerOutcome Outcome, bool WasOnChipStep)
		{
			this.LogFlowEvent(
				"ScanMrzNotAccepted",
				new KeyValuePair<string, object?>("Outcome", Outcome.ToString()),
				new KeyValuePair<string, object?>("WasOnChipStep", WasOnChipStep));

			switch (Outcome)
			{
				case KycDocumentMrzScannerOutcome.Cancelled:
					if (!WasOnChipStep)
						await this.ShowMrzGuideNoticeAsync("KycTravelDocumentNfcCancelledTitle", "KycTravelDocumentNfcCancelledDescription", false);
					break;

				case KycDocumentMrzScannerOutcome.CameraPermissionDenied:
					// The scanner already explained the permission in an alert, so the guide only says how to fix it.
					await this.ShowMrzGuideNoticeAsync("KycDocumentScanCameraPermissionTitle", "KycTravelDocumentCameraPermissionDescription", true);
					break;

				default:
					await this.ShowMrzGuideNoticeAsync("KycTravelDocumentMrzScanFailedTitle", "KycTravelDocumentMrzScanFailedDescription", true);
					break;
			}
		}

		/// <summary>
		/// Shows the guide to the machine-readable lines with a notice about the last scan.
		/// </summary>
		/// <param name="TitleKey">The resource key of the guide headline, or <c>null</c> for the default headline.</param>
		/// <param name="MessageKey">The resource key of the notice.</param>
		/// <param name="IsError">Whether the notice describes a problem rather than a neutral outcome.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task ShowMrzGuideNoticeAsync(string? TitleKey, string MessageKey, bool IsError)
		{
			string Message = ServiceRef.Localizer[MessageKey];
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.mrzGuideTitleKey = TitleKey;
				this.CurrentFailure = null;
				this.StatusText = Message;
				this.HasStatusText = true;
				this.IsPlacementHelpExpanded = false;
				this.IsNfcBusy = false;
				this.IsScanCancelled = false;
				this.HasErrorState = IsError;
				this.FlowState = KycTravelDocumentFlowState.MrzGuide;
				this.NotifyPresentationChanged();
				this.LogFlowEvent(
					"MrzGuideNoticeSet",
					new KeyValuePair<string, object?>("ResourceKey", MessageKey),
					new KeyValuePair<string, object?>("IsError", IsError));

				if (IsError)
					Announce(this.MrzGuideTitleText + ". " + this.StatusText);
			});
		}

		/// <summary>
		/// Saves the scanned document details and starts reading the chip.
		/// </summary>
		/// <param name="Result">The accepted scanner result.</param>
		/// <param name="ScannedMrz">The normalized machine-readable lines.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task AcceptScannedMrzAsync(TravelDocumentMrzResult Result, string ScannedMrz)
		{
			this.MrzText = ScannedMrz.Trim();
			TravelDocumentMrzEvidence? Evidence = await this.TryCreateMrzEvidenceAsync(Result, CancellationToken.None);
			if (Evidence is null)
			{
				this.LogFlowEvent("ScanMrzEvidenceCreationFailed");
				await this.ShowMrzGuideNoticeAsync(null, "KycTravelDocumentInvalidMrz", true);
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
			this.hasReachedChipStep = false;

			TravelDocumentMrzEvidence? Evidence = AvailableEvidence ?? await this.TryCreateMrzEvidenceAsync(CancellationToken.None);
			if (Evidence is null)
			{
				this.LogFlowEvent("StartNfcEvidenceMissing");
				await this.ShowMrzGuideNoticeAsync(null, "KycTravelDocumentInvalidMrz", true);
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
				_ = this.ExpandPlacementHelpAfterDelayAsync(SessionId, FlowCancellationToken);
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
					await this.FinishFailedNfcSessionAsync(SessionId, TravelDocumentFailureFactory.CreateCancellation());
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
					await this.FinishFailedNfcSessionAsync(SessionId, TravelDocumentFailureFactory.CreateSessionFailure());
			}
		}

		[RelayCommand]
		private async Task ReturnToApplicationAsync()
		{
			await this.ContinueToApplicationAsync(BackMethod.Pop);
		}

		[RelayCommand]
		private Task RunPrimaryRecoveryAsync()
		{
			return this.RunRecoveryActionAsync(this.PrimaryRecoveryAction);
		}

		[RelayCommand]
		private Task RunSecondaryRecoveryAsync()
		{
			return this.RunRecoveryActionAsync(this.SecondaryRecoveryAction);
		}

		/// <summary>
		/// Runs the action the user chose to recover from a failed or cancelled chip read.
		/// </summary>
		/// <param name="Action">The recovery action.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task RunRecoveryActionAsync(TravelDocumentRecoveryAction Action)
		{
			this.LogFlowEvent(
				"RecoveryActionChosen",
				new KeyValuePair<string, object?>("Action", Action.ToString()),
				new KeyValuePair<string, object?>("FailureCode", this.CurrentFailure?.FailureCode.ToString()));

			switch (Action)
			{
				case TravelDocumentRecoveryAction.RetryCurrentStep:
					await this.StartNfcReadoutFromEvidenceAsync(null);
					break;

				case TravelDocumentRecoveryAction.RestartFlow:
				case TravelDocumentRecoveryAction.UseAnotherDocument:
					await this.ScanMrzAsync();
					break;

				case TravelDocumentRecoveryAction.CloseFlow:
					await this.ContinueToApplicationAsync(BackMethod.Pop);
					break;

				case TravelDocumentRecoveryAction.ContactSupport:
					// No support channel is configured for document checks, so the page closes and keeps the saved
					// document details for a later attempt.
					await this.GoBack();
					break;
			}
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
					await this.ProvideOutcomeFeedbackAsync(true);
					await this.CompleteNfcSessionAsync(SessionId, null);
				}
				else if (!await this.TryRetryWithPacePollingAsync(SessionId, Evidence, PollingPreference, Result, FlowCancellationToken))
				{
					await this.FinishFailedNfcSessionAsync(SessionId, TravelDocumentFailureFactory.CreateReadoutFailure(Result));
					// Upload the completed trace to its original preview without blocking the retry controls.
					_ = this.UploadFailedReservedPreviewReadoutAsync(Result, Evidence.ApplicationIdentityId);
				}
			}
			catch (OperationCanceledException) when (LinkedCancellation.IsCancellationRequested)
			{
				if (IsFinalizing || await this.TryBeginNfcFinalizationAsync(SessionId, Evidence.ApplicationIdentityId))
					await this.FinishFailedNfcSessionAsync(SessionId, TravelDocumentFailureFactory.CreateCancellation());
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				if (IsFinalizing || await this.TryBeginNfcFinalizationAsync(SessionId, Evidence.ApplicationIdentityId))
					await this.FinishFailedNfcSessionAsync(SessionId, TravelDocumentFailureFactory.CreateUnexpectedFailure());
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

				bool IsFirstDetection = this.readProgress.Apply(State);
				this.StatusText = ServiceRef.Localizer[ResolveChipProgressResourceKey(State, this.readProgress.Stage)];
				this.HasStatusText = true;
				this.ChipReadProgress = this.readProgress.Progress;
				this.FlowState = KycTravelDocumentFlowState.NfcReading;
				if (IsFirstDetection)
				{
					PerformHaptic(HapticFeedbackType.Click);
					Announce(ServiceRef.Localizer["KycTravelDocumentNfcDetected"]);
				}

				// Chip events arrive for every data chunk; only push the native sheet when its message changes.
				if (!string.Equals(this.lastNativeAlertResourceKey, NativeResourceKey, StringComparison.Ordinal))
				{
					this.lastNativeAlertResourceKey = NativeResourceKey;
					await this.UpdateNativeNfcAlertAsync(SessionId, NativeResourceKey);
				}
			});
		}

		private static string ResolveChipProgressResourceKey(TravelDocumentsState State, TravelDocumentReadStage Stage)
		{
			if (State == TravelDocumentsState.ValidatingCertificate)
				return "KycTravelDocumentNfcCheckingSecurity";

			return Stage switch
			{
				TravelDocumentReadStage.Detect or TravelDocumentReadStage.Secure => "KycTravelDocumentNfcSecuring",
				TravelDocumentReadStage.Verify => "KycTravelDocumentNfcCheckingDocument",
				_ => "KycTravelDocumentNfcReading"
			};
		}

		private async Task ExpandPlacementHelpAfterDelayAsync(Guid SessionId, CancellationToken CancellationToken)
		{
			try
			{
				await Task.Delay(placementHelpDelay, CancellationToken);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (this.activeSessionId == SessionId &&
						this.FlowState == KycTravelDocumentFlowState.NfcReady &&
						this.IsNfcBusy)
					{
						this.IsPlacementHelpExpanded = true;
					}
				});
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private Task ProvideOutcomeFeedbackAsync(bool Succeeded)
		{
			return MainThread.InvokeOnMainThreadAsync(async () =>
			{
				if (Succeeded ? !this.HasReadout : !this.HasErrorState)
					return;

				Announce(Succeeded ? this.NfcHeadlineText : this.NfcHeadlineText + ". " + this.StatusText);
				if (Succeeded)
					PerformHaptic(HapticFeedbackType.LongPress);
				else if (this.hasReachedChipStep)
				{
					// Two short taps set failure apart from the single long success pulse.
					PerformHaptic(HapticFeedbackType.Click);
					await Task.Delay(failureHapticInterval);
					PerformHaptic(HapticFeedbackType.Click);
				}
			});
		}

		/// <summary>
		/// Gives tactile feedback on Android, where the page is the only NFC feedback while the phone is held against the document.
		/// </summary>
		/// <remarks>
		/// iOS already gives feedback through its native NFC sheet, so haptics are not repeated there.
		/// </remarks>
		/// <param name="Type">The kind of haptic feedback.</param>
		private static void PerformHaptic(HapticFeedbackType Type)
		{
			if (DeviceInfo.Platform != DevicePlatform.Android)
				return;

			try
			{
				HapticFeedback.Default.Perform(Type);
			}
			catch (Exception)
			{
				// Devices without haptics still complete the flow.
			}
		}

		private static void Announce(string Text)
		{
			if (string.IsNullOrWhiteSpace(Text))
				return;

			try
			{
				SemanticScreenReader.Default.Announce(Text);
			}
			catch (Exception)
			{
				// Announcements are best effort; visual feedback remains.
			}
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

		private async Task FinishFailedNfcSessionAsync(Guid SessionId, TravelDocumentFailure Failure)
		{
			if (this.activeSessionId != SessionId)
				return;

			this.TrackTerminalNfcSession(SessionId, Failure.IsCancellation ? "Cancelled" : "Failed", Failure.FailureCode.ToString());
			string NativeMessage = ServiceRef.Localizer[TravelDocumentPresentationMapper.GetNativeAlertResourceKey(Failure.FailureCode)];
			try
			{
				await this.StopNativeNfcSessionAsync(SessionId, NativeMessage);
				await this.SetStatusAsync(
					TravelDocumentPresentationMapper.GetDetailResourceKey(Failure.FailureCode),
					true,
					true,
					false,
					KycTravelDocumentFlowState.Error,
					SessionId,
					Failure);
				if (!Failure.IsCancellation)
					await this.ProvideOutcomeFeedbackAsync(false);

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
			// Some PACE documents polled as ISO 14443 reject authentication, or drop the connection during the PACE key
			// exchange, but can be read with PACE polling.
			bool MayNeedPacePolling = Result.IsPaceExchangeInterrupted ||
				(Result.Status == TravelDocumentReadoutStatus.AuthenticationFailed &&
				Result.AuthenticationResult is AuthenticateResult.AlreadyEncrypted or
					AuthenticateResult.UnableToInitializePace or AuthenticateResult.UnableToAuthenticatePace or
					AuthenticateResult.UnableToGetBacChallenge or AuthenticateResult.UnableToAuthenticateBac);
			if (!OperatingSystem.IsIOSVersionAtLeast(16) ||
				PollingPreference != NfcIsoDepPollingPreference.Iso14443 ||
				!MayNeedPacePolling)
			{
				return false;
			}

			FlowCancellationToken.ThrowIfCancellationRequested();
			if (!this.IsActiveSession(SessionId, Evidence.ApplicationIdentityId))
				return true;

			this.TrackTerminalNfcSession(SessionId, "Failed", Result.AuthenticationResult?.ToString() ?? Result.Status.ToString());
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
			await this.FinishFailedNfcSessionAsync(SessionId, TravelDocumentFailureFactory.CreatePlatformFailure(Failure));
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
				await this.ClearMatchingReservedProfileApplicationAsync(ExistingIdentity.Id);
				return new TravelDocumentMrzEvidence(
					Evidence.MrzText,
					Evidence.DocumentInformation,
					ExistingIdentity.Id);
			}

			IReadOnlyList<Property> ReservationProperties = await ServiceRef.KycService
				.PreparePreviewReservationPropertiesAsync(this.reference, CancellationToken);
			bool GenerateNewKeys = !await this.HasExistingSigningKeyAsync();
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

			await ServiceRef.KycService.SetReservedPreviewIdentityAsync(this.reference, ReservedIdentity);
			await this.ClearMatchingReservedProfileApplicationAsync(ReservedIdentity.Id);
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
			bool CanSignWithCurrentKeys = Identity is not null && await this.CanSignForIdentityAsync(Identity);
			this.LogFlowEvent(
				"PreviewReservationReuseChecked",
				new KeyValuePair<string, object?>("Succeeded", Succeeded),
				new KeyValuePair<string, object?>("HasIdentity", Identity is not null),
				new KeyValuePair<string, object?>("CanSignWithCurrentKeys", CanSignWithCurrentKeys));

			if (!Succeeded || Identity is null || !CanSignWithCurrentKeys)
				return null;

			await ServiceRef.KycService.SetReservedPreviewIdentityAsync(Reference, Identity);
			return Identity;
		}

		private async Task<bool> HasExistingSigningKeyAsync()
		{
			if (await ServiceRef.XmppService.HasSigningKeysAsync())
				return true;

			string IdentityId = ServiceRef.TagProfile.LegalIdentity?.Id?.Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(IdentityId) &&
				await ServiceRef.XmppService.HasPrivateKey(IdentityId))
			{
				return true;
			}

			string? ApplicationIdentityId = this.reference?.GetActiveApplicationIdentityId();
			return !string.IsNullOrWhiteSpace(ApplicationIdentityId) &&
				await ServiceRef.XmppService.HasPrivateKey(ApplicationIdentityId);
		}

		private async Task<bool> CanSignForIdentityAsync(LegalIdentity Identity)
		{
			try
			{
				return await ServiceRef.XmppService.CanSignWithCurrentKeysAsync(Identity);
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
				KycNfcEvidencePolicy NfcPolicy = await this.ResolveNfcEvidencePolicyAsync();
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
				KycProcess? Process = await this.reference.ToProcess(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
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

			await this.ClearMatchingReservedProfileApplicationAsync(ReservedPreviewIdentityId);

			await ServiceRef.KycService.ForgetReservedPreviewIdentityAsync(this.reference, ReservedPreviewIdentityId);
			this.LogFlowEvent(
				EventName,
				new KeyValuePair<string, object?>("HasReservedPreviewIdentityId", true));
		}

		private async Task ClearMatchingReservedProfileApplicationAsync(string PreviewIdentityId)
		{
			if (this.reference?.IsUnsubmittedReservedPreviewIdentity(PreviewIdentityId) == true &&
				string.Equals(ServiceRef.TagProfile.IdentityApplication?.Id, PreviewIdentityId, StringComparison.OrdinalIgnoreCase))
			{
				await ServiceRef.TagProfile.SetIdentityApplication(null, true);
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
				[Constants.XmppProperties.LastNames] = JoinNameParts(DocumentInformation.PrimaryIdentifier),
				[Constants.XmppProperties.Country] = ResolveAlpha2CountryCode(DocumentInformation.IssuingState),
				[Constants.XmppProperties.Nationality] = ResolveAlpha2CountryCode(DocumentInformation.Nationality)
			};
			AddGivenNameMappings(Values, JoinNameParts(DocumentInformation.SecondaryIdentifier));

			if (TryResolveGenderCode(DocumentInformation.Gender, out string GenderCode))
				Values[Constants.XmppProperties.Gender] = GenderCode;

			string CountryCode = ResolvePersonalNumberCountryCode(DocumentInformation.IssuingState, DocumentInformation.Nationality);

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
				[Constants.XmppProperties.LastNames] = DocumentData.PrimaryIdentifier?.Trim() ?? string.Empty,
				[Constants.XmppProperties.Country] = ResolveAlpha2CountryCode(DocumentData.IssuingState),
				[Constants.XmppProperties.Nationality] = ResolveAlpha2CountryCode(DocumentData.Nationality)
			};
			AddGivenNameMappings(Values, DocumentData.SecondaryIdentifier);

			if (TryResolveGenderCode(DocumentData.Gender, out string GenderCode))
				Values[Constants.XmppProperties.Gender] = GenderCode;

			string PersonalNumber = DocumentData.PersonalNumber?.Replace("<", string.Empty).Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(PersonalNumber))
			{
				string CountryCode = ResolvePersonalNumberCountryCode(DocumentData.IssuingState, DocumentData.Nationality);
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
			Guid? SessionId = null,
			TravelDocumentFailure? Failure = null)
		{
			bool IsCancelled = IsError && Failure?.IsCancellation == true;
			string Message = ServiceRef.Localizer[ResourceKey];
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (SessionId.HasValue && this.activeSessionId != SessionId)
					return;

				// A new attempt starts from an empty ring; failures keep the progress reached so far.
				if (FlowState == KycTravelDocumentFlowState.MrzCaptured || FlowState == KycTravelDocumentFlowState.NfcReady)
				{
					this.readProgress.Reset();
					this.lastNativeAlertResourceKey = null;
					this.ChipReadProgress = this.readProgress.Progress;
				}

				if (FlowState == KycTravelDocumentFlowState.NfcReady)
					this.hasReachedChipStep = true;

				if (ReadoutAvailable)
				{
					this.readProgress.Complete();
					this.ChipReadProgress = this.readProgress.Progress;
				}

				this.mrzGuideTitleKey = null;
				this.StatusText = Message;
				this.HasStatusText = !string.IsNullOrWhiteSpace(Message);
				this.IsPlacementHelpExpanded = false;
				this.IsNfcBusy = IsBusy;

				// Set before the error flag so a cancelled scan never briefly presents as a failure, and the failure
				// headline is ready when the failure scene appears.
				this.CurrentFailure = IsError ? Failure : null;
				this.IsScanCancelled = IsCancelled;
				this.HasErrorState = IsError;
				if (ReadoutAvailable)
					this.HasReadout = true;

				if (FlowState.HasValue)
					this.FlowState = FlowState.Value;

				this.LogFlowEvent(
					"StatusSet",
					new KeyValuePair<string, object?>("ResourceKey", ResourceKey),
					new KeyValuePair<string, object?>("FailureCode", Failure?.FailureCode.ToString()),
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
				return KycTravelDocumentFlowState.MrzGuide;

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

		private string ResolveInitialStatusText(bool HasInsufficientSavedMrz)
		{
			if (!string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml))
				return ServiceRef.Localizer["KycTravelDocumentReadoutReady"];

			if (HasInsufficientSavedMrz)
				return ServiceRef.Localizer["KycTravelDocumentInvalidMrz"];

			if (this.reference?.HasFullTravelDocumentMrz == true)
				return ServiceRef.Localizer["KycTravelDocumentSaved"];

			return string.Empty;
		}

		private static string JoinNameParts(string[]? Parts)
		{
			return string.Join(" ", Parts ?? Array.Empty<string>()).Trim();
		}

		/// <summary>
		/// Adds given-name mappings, placing the first given name in the first name property and any remaining
		/// given names in the middle names property.
		/// </summary>
		/// <param name="Values">Mapped property values to update.</param>
		/// <param name="GivenNames">All given names read from the travel document.</param>
		private static void AddGivenNameMappings(IDictionary<string, string> Values, string? GivenNames)
		{
			string[] Names = (GivenNames ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			Values[Constants.XmppProperties.FirstName] = Names.Length > 0 ? Names[0] : string.Empty;
			Values[Constants.XmppProperties.MiddleNames] = string.Join(" ", Names.Skip(1));
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

		/// <summary>
		/// Resolves the country whose personal number scheme applies to a travel document.
		/// </summary>
		/// <remarks>
		/// The issuing state assigns the personal number printed on its documents, so it is preferred over the holder's nationality.
		/// </remarks>
		/// <param name="IssuingState">The document's issuing state code.</param>
		/// <param name="Nationality">The holder's nationality code.</param>
		/// <returns>An ISO 3166-1 alpha-2 country code, or an empty string if neither code can be resolved.</returns>
		private static string ResolvePersonalNumberCountryCode(string? IssuingState, string? Nationality)
		{
			string CountryCode = ResolveAlpha2CountryCode(IssuingState);
			if (string.IsNullOrWhiteSpace(CountryCode))
				CountryCode = ResolveAlpha2CountryCode(Nationality);

			return CountryCode;
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
