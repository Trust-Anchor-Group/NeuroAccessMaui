using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Validates configured KYC evidence requirements before identity application submission.
	/// </summary>
	public class KycEvidenceValidationService
	{
		/// <summary>
		/// Validates evidence required by the KYC process.
		/// </summary>
		/// <param name="Process">The KYC process containing evidence policy.</param>
		/// <param name="Reference">The persisted KYC reference containing captured evidence.</param>
		/// <returns>The evidence validation result.</returns>
		public KycEvidenceValidationResult Validate(KycProcess Process, KycReference Reference)
		{
			KycNfcEvidencePolicy NfcPolicy = Process.EvidencePolicy.TravelDocument.Nfc;

			if (NfcPolicy.Enabled &&
				NfcPolicy.Required &&
				string.IsNullOrWhiteSpace(Reference.NfcReadoutXml))
			{
				return KycEvidenceValidationResult.Blocked(
					nameof(AppResources.KycTravelDocumentSummaryTitle),
					nameof(AppResources.KycTravelDocumentSummaryMissing));
			}

			return KycEvidenceValidationResult.Allowed();
		}
	}

	/// <summary>
	/// Represents the result of validating KYC evidence requirements.
	/// </summary>
	public class KycEvidenceValidationResult
	{
		private KycEvidenceValidationResult(bool CanSubmit, string? TitleResourceKey, string? MessageResourceKey)
		{
			this.CanSubmit = CanSubmit;
			this.TitleResourceKey = TitleResourceKey;
			this.MessageResourceKey = MessageResourceKey;
		}

		/// <summary>
		/// Gets a value indicating whether submission may continue.
		/// </summary>
		public bool CanSubmit { get; }

		/// <summary>
		/// Gets the localized title resource key for a blocking validation result.
		/// </summary>
		public string? TitleResourceKey { get; }

		/// <summary>
		/// Gets the localized message resource key for a blocking validation result.
		/// </summary>
		public string? MessageResourceKey { get; }

		/// <summary>
		/// Creates a successful evidence validation result.
		/// </summary>
		/// <returns>A validation result that allows submission.</returns>
		public static KycEvidenceValidationResult Allowed()
		{
			return new KycEvidenceValidationResult(true, null, null);
		}

		/// <summary>
		/// Creates a blocking evidence validation result.
		/// </summary>
		/// <param name="TitleResourceKey">The localized title resource key.</param>
		/// <param name="MessageResourceKey">The localized message resource key.</param>
		/// <returns>A validation result that blocks submission.</returns>
		public static KycEvidenceValidationResult Blocked(string TitleResourceKey, string MessageResourceKey)
		{
			return new KycEvidenceValidationResult(false, TitleResourceKey, MessageResourceKey);
		}
	}
}
