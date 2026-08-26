using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.Services.Kyc;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Represents MRZ evidence ready to be used for travel-document NFC readout.
	/// </summary>
	public sealed class TravelDocumentMrzEvidence
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TravelDocumentMrzEvidence"/> class.
		/// </summary>
		/// <param name="MrzText">The normalized MRZ text.</param>
		/// <param name="DocumentInformation">The document information parsed from the MRZ.</param>
		/// <param name="ApplicationIdentityId">The active application identity identifier, if available.</param>
		public TravelDocumentMrzEvidence(string MrzText, DocumentInformation DocumentInformation, string? ApplicationIdentityId)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(MrzText);
			ArgumentNullException.ThrowIfNull(DocumentInformation);

			this.MrzText = MrzText;
			this.DocumentInformation = DocumentInformation;
			this.ApplicationIdentityId = ApplicationIdentityId;
		}

		/// <summary>
		/// Gets the normalized MRZ text.
		/// </summary>
		public string MrzText { get; }

		/// <summary>
		/// Gets the document information parsed from the MRZ.
		/// </summary>
		public DocumentInformation DocumentInformation { get; }

		/// <summary>
		/// Gets the active application identity identifier, if available.
		/// </summary>
		public string? ApplicationIdentityId { get; }
	}

	/// <summary>
	/// Stores and retrieves travel-document evidence used by the KYC application flow.
	/// </summary>
	[DefaultImplementation(typeof(TravelDocumentEvidenceService))]
	public interface ITravelDocumentEvidenceService
	{
		/// <summary>
		/// Saves validated MRZ evidence for the active KYC application.
		/// </summary>
		/// <param name="MrzText">The MRZ text to validate and store.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>True if the MRZ was valid and stored; otherwise false.</returns>
		Task<bool> SaveMrzAsync(string MrzText, CancellationToken CancellationToken);

		/// <summary>
		/// Attempts to load validated MRZ evidence for NFC readout.
		/// </summary>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The MRZ evidence, or null if no valid evidence is available.</returns>
		Task<TravelDocumentMrzEvidence?> TryLoadMrzAsync(CancellationToken CancellationToken);

		/// <summary>
		/// Saves travel-document NFC readout XML for the active KYC application.
		/// </summary>
		/// <param name="Xml">The NFC readout XML.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>True if the readout was stored on an active KYC reference; otherwise false.</returns>
		Task<bool> SaveReadoutXmlAsync(string Xml, CancellationToken CancellationToken);
	}

	/// <summary>
	/// Default travel-document evidence service.
	/// </summary>
	public sealed class TravelDocumentEvidenceService : ITravelDocumentEvidenceService
	{
		private const string legacyMrzKey = "NFC.LastMrz";

		/// <summary>
		/// Initializes a new instance of the <see cref="TravelDocumentEvidenceService"/> class.
		/// </summary>
		public TravelDocumentEvidenceService()
		{
		}

		/// <inheritdoc/>
		public async Task<bool> SaveMrzAsync(string MrzText, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			string NormalizedMrz = MrzText?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(NormalizedMrz) ||
				!KycReference.IsFullTravelDocumentMrz(NormalizedMrz) ||
				!MrzExtensions.ParseMrz(NormalizedMrz, out _))
			{
				return false;
			}

			KycReference? Reference = await TravelDocumentEvidenceService.TryLoadKycReferenceAsync().ConfigureAwait(false);
			if (Reference is not null)
			{
				Reference.TravelDocumentMrz = NormalizedMrz;
				Reference.TravelDocumentMrzUpdatedUtc = DateTime.UtcNow;
				await ServiceRef.KycService.SaveKycReferenceAsync(Reference).ConfigureAwait(false);
			}

			await RuntimeSettings.SetAsync(legacyMrzKey, NormalizedMrz).ConfigureAwait(false);
			return true;
		}

		/// <inheritdoc/>
		public async Task<TravelDocumentMrzEvidence?> TryLoadMrzAsync(CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			KycReference? Reference = await TravelDocumentEvidenceService.TryLoadKycReferenceAsync().ConfigureAwait(false);
			string MrzText = Reference?.TravelDocumentMrz?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(MrzText))
				MrzText = await RuntimeSettings.GetAsync(legacyMrzKey, string.Empty).ConfigureAwait(false);

			CancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrWhiteSpace(MrzText) ||
				!KycReference.IsFullTravelDocumentMrz(MrzText) ||
				!MrzExtensions.ParseMrz(MrzText, out DocumentInformation? DocumentInformation))
			{
				return null;
			}

			string? ApplicationIdentityId = Reference?.ReservedPreviewIdentityId ??
				Reference?.PreviewIdentityId ??
				Reference?.GetActiveApplicationIdentityId();
			return new TravelDocumentMrzEvidence(MrzText, DocumentInformation, ApplicationIdentityId);
		}

		/// <inheritdoc/>
		public async Task<bool> SaveReadoutXmlAsync(string Xml, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrWhiteSpace(Xml))
				return false;

			KycReference? Reference = await TravelDocumentEvidenceService.TryLoadKycReferenceAsync().ConfigureAwait(false);
			if (Reference is null)
				return false;

			Reference.NfcReadoutXml = Xml;
			Reference.NfcReadoutUpdatedUtc = DateTime.UtcNow;
			Reference.NfcVerifiedFieldIds = null;
			await ServiceRef.KycService.SaveKycReferenceAsync(Reference).ConfigureAwait(false);
			return true;
		}

		private static async Task<KycReference?> TryLoadKycReferenceAsync()
		{
			try
			{
				IEnumerable<KycReference> References = await Database.Find<KycReference>().ConfigureAwait(false);
				LegalIdentity? IdentityApplication = ServiceRef.TagProfile.IdentityApplication;
				if (IdentityApplication is not null)
				{
					KycReference? Match = References
						.Where(Reference => Reference.MatchesIdentityId(IdentityApplication.Id))
						.OrderByDescending(Reference => Reference.UpdatedUtc)
						.FirstOrDefault();
					if (Match is not null)
						return Match;
				}

				return References
					.OrderByDescending(Reference => Reference.UpdatedUtc)
					.FirstOrDefault();
			}
			catch
			{
				return null;
			}
		}
	}
}
