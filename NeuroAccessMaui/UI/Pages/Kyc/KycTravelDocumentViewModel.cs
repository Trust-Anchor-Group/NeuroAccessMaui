using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.TravelDocuments;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// View model for guided travel-document MRZ and NFC evidence capture.
	/// </summary>
	public partial class KycTravelDocumentViewModel : BaseViewModel
	{
		private readonly ITravelDocumentEvidenceService evidenceService = ServiceRef.Provider.GetRequiredService<ITravelDocumentEvidenceService>();
		private readonly INfcIsoDepSessionService nfcIsoDepSessionService = ServiceRef.Provider.GetRequiredService<INfcIsoDepSessionService>();
		private readonly ITravelDocumentReadoutService readoutService = ServiceRef.Provider.GetRequiredService<ITravelDocumentReadoutService>();
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycReference? reference;
		private Guid? activeSessionId;

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
		[ObservableProperty]
		private bool hasReadout;

		/// <summary>
		/// Gets or sets a value indicating whether the current status represents a recoverable error.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(ShowStatusPanel))]
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
		public bool ShowNfcPlacementPanel => this.HasMrz || this.IsNfcBusy || this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether the introductory chip-first guide should be shown.
		/// </summary>
		public bool ShowIntro => !this.HasMrz && !this.IsNfcBusy && !this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether the NFC placement step should be shown.
		/// </summary>
		public bool ShowNfcStep => this.HasMrz && !this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether the readout success state should be shown.
		/// </summary>
		public bool ShowSuccess => this.HasReadout;

		/// <summary>
		/// Gets a value indicating whether the status panel should be visible.
		/// </summary>
		public bool ShowStatusPanel => this.HasStatusText && (!this.ShowIntro || this.HasErrorState) && !this.ShowSuccess;

		/// <summary>
		/// Gets a value indicating whether a retry action for NFC should be shown.
		/// </summary>
		public bool ShowNfcRetryAction => this.CanStartNfc && !this.ShowIntro;

		/// <summary>
		/// Gets a value indicating whether the manual fallback action should be shown.
		/// </summary>
		public bool ShowManualFallback => !this.HasReadout && !this.IsNfcBusy;

		/// <summary>
		/// Gets the current MRZ step description.
		/// </summary>
		public string MrzStepDescription => this.HasMrz
			? ServiceRef.Localizer["KycTravelDocumentMrzStepReadyDescription"]
			: ServiceRef.Localizer["KycTravelDocumentMrzStepDescription"];

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
			this.MrzText = this.reference?.TravelDocumentMrz ?? string.Empty;
			this.HasReadout = !string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml);
			this.StatusText = this.ResolveInitialStatusText();
			this.HasStatusText = !string.IsNullOrWhiteSpace(this.StatusText);
			this.HasErrorState = false;
		}

		[RelayCommand]
		private async Task ScanMrzAsync()
		{
			if (this.IsNfcBusy)
				return;

			TaskCompletionSource<TravelDocumentMrzResult?> CompletionSource = new TaskCompletionSource<TravelDocumentMrzResult?>(
				TaskCreationOptions.RunContinuationsAsynchronously);

			await ServiceRef.NavigationService.GoToAsync(
				nameof(KycDocumentMrzScannerPage),
				new KycDocumentMrzScannerNavigationArgs
				{
					CompletionSource = CompletionSource,
					PreferredDocumentKind = OcrDocumentKindHint.Passport
				});

			TravelDocumentMrzResult? Result = await CompletionSource.Task;
			string? ScannedMrz = Result?.Document?.MRZ_Information;
			if (string.IsNullOrWhiteSpace(ScannedMrz))
			{
				await this.SetStatusAsync("KycTravelDocumentInvalidMrz", false);
				return;
			}

			this.MrzText = ScannedMrz.Trim();
			TravelDocumentMrzEvidence? Evidence = await this.TryCreateMrzEvidenceAsync(CancellationToken.None);
			if (Evidence is null)
			{
				await this.SetStatusAsync("KycTravelDocumentInvalidMrz", false);
				return;
			}

			await this.SetStatusAsync("KycTravelDocumentSaved", false);
			await this.StartNfcReadoutAsync();
		}

		[RelayCommand]
		private async Task StartNfcReadoutAsync()
		{
			if (this.IsNfcBusy)
				return;

			TravelDocumentMrzEvidence? Evidence = await this.TryCreateMrzEvidenceAsync(CancellationToken.None);
			if (Evidence is null)
			{
				await this.SetStatusAsync("KycTravelDocumentInvalidMrz", false);
				return;
			}

			Guid SessionId = Guid.NewGuid();
			this.activeSessionId = SessionId;
			await this.SetStatusAsync("KycTravelDocumentNfcReady", true);

			await this.nfcIsoDepSessionService.StartSessionAsync(
				SessionId,
				(IsoDepInterface, CancellationToken) => this.ReadTravelDocumentAsync(SessionId, Evidence, IsoDepInterface, CancellationToken),
				(Failure, CancellationToken) => this.HandleNfcFailureAsync(SessionId, Failure, CancellationToken),
				NfcIsoDepPollingPreference.Auto,
				ServiceRef.Localizer["KycTravelDocumentNfcReady"],
				CancellationToken.None);
		}

		[RelayCommand]
		private async Task ReturnToApplicationAsync()
		{
			await this.StopActiveNfcSessionAsync();

			KycReference? Reference = this.reference;
			if (Reference is not null)
				await ServiceRef.NavigationService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(Reference));
			else
				await ServiceRef.NavigationService.GoBackAsync();
		}

		private async Task ReadTravelDocumentAsync(
			Guid SessionId,
			TravelDocumentMrzEvidence Evidence,
			IIsoDepInterface IsoDepInterface,
			CancellationToken CancellationToken)
		{
			await this.nfcIsoDepSessionService.UpdateSessionAlertAsync(
				SessionId,
				ServiceRef.Localizer["KycTravelDocumentNfcReading"],
				CancellationToken);
			await this.SetStatusAsync("KycTravelDocumentNfcReading", true);

			try
			{
				TravelDocumentReadoutRequest Request = new TravelDocumentReadoutRequest(
					Evidence.DocumentInformation,
					Evidence.MrzText,
					Evidence.ApplicationIdentityId);
				TravelDocumentReadoutResult Result = await this.readoutService.ReadAsync(IsoDepInterface, Request, CancellationToken);

				if (Result.IsSuccess &&
					await this.SaveReadoutXmlAsync(Result.Xml, CancellationToken))
				{
					await this.SetStatusAsync("KycTravelDocumentNfcSuccess", false, false, true);
				}
				else
				{
					await this.SetStatusAsync(this.ResolveReadoutFailureResourceKey(Result.Status), false, true);
				}
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
			await this.SetStatusAsync(this.ResolveNfcFailureResourceKey(Failure), false, true);

			if (this.activeSessionId == SessionId)
				this.activeSessionId = null;

			await this.nfcIsoDepSessionService.StopSessionAsync(SessionId, CancellationToken.None);
		}

		private async Task CompleteNfcSessionAsync(Guid SessionId)
		{
			if (this.activeSessionId == SessionId)
				this.activeSessionId = null;

			await this.nfcIsoDepSessionService.StopSessionAsync(SessionId, CancellationToken.None);
			await MainThread.InvokeOnMainThreadAsync(() => this.IsNfcBusy = false);
		}

		private async Task<TravelDocumentMrzEvidence?> TryCreateMrzEvidenceAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			string NormalizedMrz = this.MrzText?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(NormalizedMrz) ||
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
		}

		private async Task SeedReferenceFieldsFromMrzAsync(DocumentInformation DocumentInformation)
		{
			if (this.reference is null)
				return;

			Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["firstNames"] = JoinNameParts(DocumentInformation.SecondaryIdentifier),
				["lastNames"] = JoinNameParts(DocumentInformation.PrimaryIdentifier),
				["country"] = ResolveCountryCode(DocumentInformation)
			};

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
			await this.nfcIsoDepSessionService.StopSessionAsync(SessionId.Value, CancellationToken.None);
			await MainThread.InvokeOnMainThreadAsync(() => this.IsNfcBusy = false);
		}

		private Task SetStatusAsync(string ResourceKey, bool IsBusy, bool IsError = false, bool ReadoutAvailable = false)
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
			});
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

		private string ResolveInitialStatusText()
		{
			if (!string.IsNullOrWhiteSpace(this.reference?.NfcReadoutXml))
				return ServiceRef.Localizer["KycTravelDocumentReadoutReady"];

			if (!string.IsNullOrWhiteSpace(this.reference?.TravelDocumentMrz))
				return ServiceRef.Localizer["KycTravelDocumentSummaryMrzReady"];

			return string.Empty;
		}

		private static string JoinNameParts(string[]? Parts)
		{
			return string.Join(" ", Parts ?? Array.Empty<string>()).Trim();
		}

		private static string ResolveCountryCode(DocumentInformation DocumentInformation)
		{
			string CountryCode = DocumentInformation.Nationality?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(CountryCode))
				CountryCode = DocumentInformation.IssuingState?.Trim() ?? string.Empty;

			return CountryCode.ToUpperInvariant();
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
