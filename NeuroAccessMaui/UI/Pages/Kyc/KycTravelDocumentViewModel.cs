using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.TravelDocuments;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// View model for entering travel-document MRZ evidence used by the KYC NFC readout path.
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
		[ObservableProperty]
		private bool hasStatusText;

		/// <summary>
		/// Gets or sets a value indicating whether an NFC readout is in progress.
		/// </summary>
		[ObservableProperty]
		private bool isNfcBusy;

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
			this.StatusText = this.ResolveInitialStatusText();
			this.HasStatusText = !string.IsNullOrWhiteSpace(this.StatusText);
		}

		[RelayCommand]
		private async Task SaveMrzAsync()
		{
			TravelDocumentMrzEvidence? Evidence = await this.TryCreateMrzEvidenceAsync(CancellationToken.None);
			if (Evidence is not null)
			{
				this.StatusText = ServiceRef.Localizer["KycTravelDocumentSaved"];
				this.HasStatusText = true;
			}
			else
			{
				this.StatusText = ServiceRef.Localizer["KycTravelDocumentInvalidMrz"];
				this.HasStatusText = true;
			}
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
					await this.SetStatusAsync("KycTravelDocumentNfcSuccess", false);
				}
				else
				{
					await this.SetStatusAsync(this.ResolveReadoutFailureResourceKey(Result.Status), false);
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
			await this.SetStatusAsync(this.ResolveNfcFailureResourceKey(Failure), false);

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
				await this.SaveReferenceEvidenceAsync(NormalizedMrz, null);
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

		private async Task SaveReferenceEvidenceAsync(string? MrzText, string? ReadoutXml)
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

			await ServiceRef.KycService.SaveKycReferenceAsync(this.reference);
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

		private Task SetStatusAsync(string ResourceKey, bool IsBusy)
		{
			string Message = ServiceRef.Localizer[ResourceKey];
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.StatusText = Message;
				this.HasStatusText = !string.IsNullOrWhiteSpace(Message);
				this.IsNfcBusy = IsBusy;
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
	}
}
