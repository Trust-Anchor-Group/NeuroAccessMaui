using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using NeuroAccess.Nfc;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Nfc;
using NeuroAccessMaui.Services.TravelDocuments;
using NeuroAccessMaui.UI.Pages.Kyc;

namespace NeuroAccessMaui.UI.Pages.Main.NfcTester
{
	/// <summary>
	/// Identifies the current state of the standalone NFC tester.
	/// </summary>
	public enum NfcTesterFlowState
	{
		/// <summary>The tester is ready to start a document scan.</summary>
		Intro,

		/// <summary>The MRZ scanner is open or awaiting a result.</summary>
		MrzCapture,

		/// <summary>The NFC reader is waiting for the document chip.</summary>
		NfcReady,

		/// <summary>The document chip is being read.</summary>
		NfcReading,

		/// <summary>The readout result is available.</summary>
		Result,

		/// <summary>The current attempt ended with a recoverable error.</summary>
		Error
	}

	/// <summary>
	/// Provides a standalone, session-only travel-document NFC testing flow.
	/// </summary>
	public partial class NfcTesterViewModel : BaseViewModel, IDisposable
	{
		private readonly INfcIsoDepSessionService nfcIsoDepSessionService = ServiceRef.Provider.GetRequiredService<INfcIsoDepSessionService>();
		private readonly ITravelDocumentReadoutService readoutService = ServiceRef.Provider.GetRequiredService<ITravelDocumentReadoutService>();
		private DocumentInformation? documentInformation;
		private string mrzText = string.Empty;
		private Guid? activeSessionId;
		private CancellationTokenSource? activeSessionCancellationTokenSource;
		private string? temporaryShareFilePath;
		private bool disposedValue;

		/// <summary>
		/// Gets or sets the current tester flow state.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(ShowIntro))]
		[NotifyPropertyChangedFor(nameof(ShowNfcProgress))]
		[NotifyPropertyChangedFor(nameof(ShowResult))]
		[ObservableProperty]
		private NfcTesterFlowState flowState = NfcTesterFlowState.Intro;

		/// <summary>
		/// Gets or sets the current localized status message.
		/// </summary>
		[ObservableProperty]
		private string statusText = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether an NFC operation is active.
		/// </summary>
		[ObservableProperty]
		private bool isNfcBusy;

		/// <summary>
		/// Gets or sets the raw XML captured during the current readout.
		/// </summary>
		[NotifyPropertyChangedFor(nameof(HasRawXml))]
		[ObservableProperty]
		private string rawXml = string.Empty;

		/// <summary>
		/// Gets the human-readable fields for the current result.
		/// </summary>
		public ObservableCollection<NfcTesterResultItem> ResultItems { get; } = new ObservableCollection<NfcTesterResultItem>();

		/// <summary>
		/// Gets a value indicating whether the introductory state is visible.
		/// </summary>
		public bool ShowIntro => this.FlowState == NfcTesterFlowState.Intro;

		/// <summary>
		/// Gets a value indicating whether NFC progress is visible.
		/// </summary>
		public bool ShowNfcProgress => this.FlowState == NfcTesterFlowState.NfcReady || this.FlowState == NfcTesterFlowState.NfcReading;

		/// <summary>
		/// Gets a value indicating whether a result or error state is visible.
		/// </summary>
		public bool ShowResult => this.FlowState == NfcTesterFlowState.Result || this.FlowState == NfcTesterFlowState.Error;

		/// <summary>
		/// Gets a value indicating whether human-readable fields are available.
		/// </summary>
		public bool HasResultItems => this.ResultItems.Count > 0;

		/// <summary>
		/// Gets a value indicating whether raw NFC XML is available.
		/// </summary>
		public bool HasRawXml => !string.IsNullOrWhiteSpace(this.RawXml);

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			if (!this.nfcIsoDepSessionService.IsPlatformSupported)
			{
				this.StatusText = ServiceRef.Localizer["NfcTesterUnavailable"];
				this.FlowState = NfcTesterFlowState.Error;
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.ClearSessionAsync();
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			await this.ClearSessionAsync();
			this.Dispose(true);
			await base.OnDisposeAsync();
		}

		/// <inheritdoc/>
		public override async Task GoBack()
		{
			await this.ClearSessionAsync();
			await base.GoBack();
		}

		[RelayCommand]
		private async Task ScanDocumentAsync()
		{
			if (!this.nfcIsoDepSessionService.IsPlatformSupported)
			{
				this.StatusText = ServiceRef.Localizer["NfcTesterUnavailable"];
				this.FlowState = NfcTesterFlowState.Error;
				return;
			}

			await this.ResetAttemptAsync();
			this.FlowState = NfcTesterFlowState.MrzCapture;
			TaskCompletionSource<TravelDocumentMrzResult?> CompletionSource = new TaskCompletionSource<TravelDocumentMrzResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
			await ServiceRef.NavigationService.GoToAsync(
				nameof(KycDocumentMrzScannerPage),
				new KycDocumentMrzScannerNavigationArgs
				{
					CompletionSource = CompletionSource,
					PreferredDocumentKind = OcrDocumentKindHint.Passport
				});

			TravelDocumentMrzResult? Result = await CompletionSource.Task;
			if (Result is null)
			{
				this.FlowState = NfcTesterFlowState.Intro;
				return;
			}

			string NormalizedMrz = Result.NormalizedMrzText?.Trim() ?? string.Empty;
			if (!Result.IsSuccessful || string.IsNullOrWhiteSpace(NormalizedMrz) || Result.Document is null)
			{
				this.StatusText = ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentInvalidMrz)];
				this.FlowState = NfcTesterFlowState.Error;
				return;
			}

			this.mrzText = NormalizedMrz;
			this.documentInformation = Result.Document;
			this.ResultItems.Clear();
			this.AddDocumentResultItems(new TravelDocumentData(Result.Document, null));
			this.OnPropertyChanged(nameof(this.HasResultItems));
			await this.StartNfcReadoutAsync();
		}

		[RelayCommand]
		private async Task StartNfcReadoutAsync()
		{
			if (this.IsNfcBusy || this.documentInformation is null || string.IsNullOrWhiteSpace(this.mrzText))
				return;

			await this.StopActiveNfcSessionAsync();
			Guid SessionId = Guid.NewGuid();
			this.activeSessionId = SessionId;
			this.activeSessionCancellationTokenSource = new CancellationTokenSource();
			this.IsNfcBusy = true;
			this.StatusText = ServiceRef.Localizer["NfcTesterReady"];
			this.FlowState = NfcTesterFlowState.NfcReady;

			try
			{
				await this.nfcIsoDepSessionService.StartSessionAsync(
					SessionId,
					(IsoDepInterface, CancellationToken) => this.ReadDocumentAsync(SessionId, IsoDepInterface, CancellationToken),
					(Failure, CancellationToken) => this.HandleNfcFailureAsync(SessionId, Failure, CancellationToken),
					NfcIsoDepPollingPreference.Auto,
					ServiceRef.Localizer["NfcTesterReady"],
					CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await this.ApplyFailureAsync(SessionId, ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcFailed)]);
			}
		}

		[RelayCommand]
		private async Task NewTestAsync()
		{
			await this.ResetAttemptAsync();
			await this.ScanDocumentAsync();
		}

		[RelayCommand]
		private async Task ShareXmlAsync()
		{
			if (!this.HasRawXml)
				return;

			bool Confirmed = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer["NfcTesterShareConfirmTitle"],
				ServiceRef.Localizer["NfcTesterShareWarning"],
				ServiceRef.Localizer["NfcTesterShareConfirmButton"],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);
			if (!Confirmed)
				return;

			try
			{
				await this.DeleteTemporaryShareFileAsync();
				string DirectoryPath = Path.Combine(FileSystem.CacheDirectory, "NfcTester");
				Directory.CreateDirectory(DirectoryPath);
				string FilePath = Path.Combine(DirectoryPath, "NFC.xml");
				await File.WriteAllTextAsync(FilePath, this.RawXml, new UTF8Encoding(false));
				this.temporaryShareFilePath = FilePath;
				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = ServiceRef.Localizer["NfcTesterShareButton"],
					File = new ShareFile(FilePath, "application/xml")
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer["NfcTesterShareFailed"],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		private async Task ReadDocumentAsync(Guid SessionId, IIsoDepInterface IsoDepInterface, CancellationToken CancellationToken)
		{
			if (!this.IsActiveSession(SessionId) || this.documentInformation is null)
				return;

			CancellationTokenSource? ActiveCancellation = this.activeSessionCancellationTokenSource;
			using CancellationTokenSource LinkedCancellation = ActiveCancellation is null
				? CancellationTokenSource.CreateLinkedTokenSource(CancellationToken)
				: CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, ActiveCancellation.Token);
			CancellationToken ReadCancellationToken = LinkedCancellation.Token;

			await this.nfcIsoDepSessionService.UpdateSessionAlertAsync(SessionId, ServiceRef.Localizer["NfcTesterReading"], ReadCancellationToken);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.StatusText = ServiceRef.Localizer["NfcTesterReading"];
				this.FlowState = NfcTesterFlowState.NfcReading;
			});

			try
			{
				TravelDocumentReadoutRequest Request = new TravelDocumentReadoutRequest(this.documentInformation, this.mrzText, null);
				TravelDocumentReadoutResult Result = await this.readoutService.ReadAsync(IsoDepInterface, Request, ReadCancellationToken);
				if (!this.IsActiveSession(SessionId))
					return;

				await MainThread.InvokeOnMainThreadAsync(() => this.ApplyReadoutResult(Result));
			}
			catch (OperationCanceledException) when (ReadCancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				await this.CompleteNfcSessionAsync(SessionId);
			}
		}

		private async Task HandleNfcFailureAsync(Guid SessionId, NfcIsoDepSessionFailure Failure, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (!this.IsActiveSession(SessionId))
				return;

			await this.ApplyFailureAsync(SessionId, this.ResolveNfcFailureMessage(Failure));
		}

		private void ApplyReadoutResult(TravelDocumentReadoutResult Result)
		{
			this.RawXml = Result.Xml ?? string.Empty;
			this.PopulateResultItems(Result.CertificateData, Result.DocumentData);

			this.StatusText = Result.IsSuccess
				? ServiceRef.Localizer["NfcTesterSuccess"]
				: this.ResolveReadoutFailureMessage(Result.Status);
			this.FlowState = Result.IsSuccess ? NfcTesterFlowState.Result : NfcTesterFlowState.Error;
		}

		private async Task ApplyFailureAsync(Guid SessionId, string Message)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.StatusText = Message;
				this.FlowState = NfcTesterFlowState.Error;
			});
			await this.CompleteNfcSessionAsync(SessionId);
		}

		private void PopulateResultItems(TravelDocumentCertificateData CertificateData, TravelDocumentData? Data)
		{
			this.ResultItems.Clear();
			this.AddResult("NfcTesterCertificateStatus", this.GetCertificateStatusText(CertificateData.Status));
			this.AddResult(null, this.GetCertificateReasonText(CertificateData.Reason));
			this.AddResult("NfcTesterCertificateSubject", CertificateData.Subject);
			this.AddResult("NfcTesterCertificateIssuer", CertificateData.Issuer);
			this.AddResult("NfcTesterCertificateSerialNumber", CertificateData.SerialNumber);
			this.AddResult("NfcTesterCertificateValidFrom", this.FormatCertificateDate(CertificateData.NotBefore));
			this.AddResult("NfcTesterCertificateValidUntil", this.FormatCertificateDate(CertificateData.NotAfter));

			if (Data is not null)
				this.AddDocumentResultItems(Data);

			this.OnPropertyChanged(nameof(this.HasResultItems));
		}

		private void AddDocumentResultItems(TravelDocumentData Data)
		{
			this.AddResult("NfcTesterDocumentType", Data.DocumentType);
			this.AddResult("NfcTesterIssuingState", Data.IssuingState);
			this.AddResult("NfcTesterDocumentNumber", Data.DocumentNumber);
			this.AddResult("NfcTesterLastNames", Data.PrimaryIdentifier);
			this.AddResult("NfcTesterFirstNames", Data.SecondaryIdentifier);
			this.AddResult("NfcTesterNationality", Data.Nationality);
			this.AddResult("NfcTesterDateOfBirth", Data.AdditionalDateOfBirth ?? Data.DateOfBirth);
			this.AddResult("NfcTesterGender", Data.Gender);
			this.AddResult("NfcTesterExpiryDate", Data.ExpiryDate);
			this.AddResult("NfcTesterOptionalData", Data.OptionalData);
			this.AddResult("NfcTesterFullName", Data.FullName);
			this.AddResult("NfcTesterOtherNames", Data.OtherNames);
			this.AddResult("NfcTesterPersonalNumber", Data.PersonalNumber);
			this.AddResult("NfcTesterPlaceOfBirth", Data.PlaceOfBirth);
			this.AddResult("NfcTesterPermanentAddress", Data.PermanentAddress);
			this.AddResult("NfcTesterTelephone", Data.Telephone);
			this.AddResult("NfcTesterProfession", Data.Profession);
			this.AddResult("NfcTesterPersonalTitle", Data.Title);
			this.AddResult("NfcTesterPersonalSummary", Data.PersonalSummary);
			this.AddResult("NfcTesterOtherNumbers", Data.OtherNumbers);
			this.AddResult("NfcTesterCustodyInformation", Data.CustodyInformation);
		}

		private string GetCertificateStatusText(TravelDocumentCertificateValidationStatus Status)
		{
			string ResourceKey = Status switch
			{
				TravelDocumentCertificateValidationStatus.Valid => "NfcTesterCertificateStatusValid",
				TravelDocumentCertificateValidationStatus.Invalid => "NfcTesterCertificateStatusInvalid",
				TravelDocumentCertificateValidationStatus.Incomplete => "NfcTesterCertificateStatusIncomplete",
				_ => "NfcTesterCertificateStatusNotVerified"
			};

			return ServiceRef.Localizer[ResourceKey];
		}

		private string? GetCertificateReasonText(TravelDocumentCertificateValidationReason Reason)
		{
			if (Reason == TravelDocumentCertificateValidationReason.None)
				return null;

			return ServiceRef.Localizer["NfcTesterCertificateReason" + Reason];
		}

		private string? FormatCertificateDate(DateTimeOffset? Value)
		{
			return Value?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
		}

		private void AddResult(string? ResourceKey, string? Value)
		{
			string Normalized = Value?.Replace("<", " ").Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(Normalized))
			{
				string Label = string.IsNullOrWhiteSpace(ResourceKey) ? string.Empty : ServiceRef.Localizer[ResourceKey];
				this.ResultItems.Add(new NfcTesterResultItem(Label, Normalized));
			}
		}

		private bool IsActiveSession(Guid SessionId)
		{
			return this.activeSessionId == SessionId;
		}

		private async Task CompleteNfcSessionAsync(Guid SessionId)
		{
			if (this.activeSessionId == SessionId)
				this.activeSessionId = null;

			this.CancelAndDisposeActiveSessionCancellation();
			await this.nfcIsoDepSessionService.StopSessionAsync(SessionId, CancellationToken.None);
			await MainThread.InvokeOnMainThreadAsync(() => this.IsNfcBusy = false);
		}

		private async Task StopActiveNfcSessionAsync()
		{
			Guid? SessionId = this.activeSessionId;
			this.activeSessionId = null;
			this.CancelAndDisposeActiveSessionCancellation();
			if (SessionId.HasValue)
				await this.nfcIsoDepSessionService.StopSessionAsync(SessionId.Value, CancellationToken.None);

			this.IsNfcBusy = false;
		}

		private async Task ResetAttemptAsync()
		{
			await this.StopActiveNfcSessionAsync();
			await this.DeleteTemporaryShareFileAsync();
			this.documentInformation = null;
			this.mrzText = string.Empty;
			this.RawXml = string.Empty;
			this.ResultItems.Clear();
			this.OnPropertyChanged(nameof(this.HasResultItems));
			this.StatusText = string.Empty;
			this.FlowState = NfcTesterFlowState.Intro;
		}

		private async Task ClearSessionAsync()
		{
			await this.ResetAttemptAsync();
		}

		private async Task DeleteTemporaryShareFileAsync()
		{
			string? FilePath = this.temporaryShareFilePath;
			this.temporaryShareFilePath = null;
			if (string.IsNullOrWhiteSpace(FilePath))
				return;

			try
			{
				if (File.Exists(FilePath))
					File.Delete(FilePath);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			await Task.CompletedTask;
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
		/// Releases resources owned by the tester view model.
		/// </summary>
		/// <param name="Disposing">True when managed resources should be released.</param>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.disposedValue)
				return;

			if (Disposing)
				this.CancelAndDisposeActiveSessionCancellation();

			this.disposedValue = true;
		}

		/// <summary>
		/// Releases resources owned by the tester view model.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private string ResolveReadoutFailureMessage(TravelDocumentReadoutStatus Status)
		{
			return Status switch
			{
				TravelDocumentReadoutStatus.AuthenticationFailed => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcAuthenticationFailed)],
				TravelDocumentReadoutStatus.ReadFailed => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcReadFailed)],
				_ => ServiceRef.Localizer["NfcTesterPartialResult"]
			};
		}

		private string ResolveNfcFailureMessage(NfcIsoDepSessionFailure Failure)
		{
			return Failure.FailureCode switch
			{
				NfcIsoDepSessionFailureCode.NotSupported => ServiceRef.Localizer["NfcTesterUnavailable"],
				NfcIsoDepSessionFailureCode.Unavailable => ServiceRef.Localizer["NfcTesterUnavailable"],
				NfcIsoDepSessionFailureCode.Cancelled => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcCancelled)],
				NfcIsoDepSessionFailureCode.TimedOut => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcTimedOut)],
				NfcIsoDepSessionFailureCode.MultipleTagsDetected => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcMultipleTags)],
				NfcIsoDepSessionFailureCode.UnsupportedTag => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcUnsupportedTag)],
				NfcIsoDepSessionFailureCode.ConnectionLost => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcConnectionLost)],
				NfcIsoDepSessionFailureCode.EntitlementMissing => ServiceRef.Localizer["NfcTesterUnavailable"],
				_ => ServiceRef.Localizer[nameof(AppResources.KycTravelDocumentNfcFailed)]
			};
		}
	}
}
