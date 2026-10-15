using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.Services.Nfc;

namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Creates structured travel-document failures from chip readout results and platform NFC session failures.
	/// </summary>
	internal static class TravelDocumentFailureFactory
	{
		/// <summary>
		/// Creates the failure for a chip readout that did not succeed, or that succeeded but could not be saved.
		/// </summary>
		/// <param name="Result">The readout result.</param>
		/// <returns>The structured failure.</returns>
		public static TravelDocumentFailure CreateReadoutFailure(TravelDocumentReadoutResult Result)
		{
			ArgumentNullException.ThrowIfNull(Result);

			return Result.Status switch
			{
				TravelDocumentReadoutStatus.AuthenticationFailed => CreateAuthenticationFailure(Result.AuthenticationResult),
				TravelDocumentReadoutStatus.ReadFailed => CreateReadFailure(Result.ReadResult, Result.CertificateData),
				TravelDocumentReadoutStatus.ConnectionLost => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcConnectionLost,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				TravelDocumentReadoutStatus.TimedOut => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcSessionTimedOut,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				TravelDocumentReadoutStatus.UnexpectedError => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipReadFailed,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				_ => CreateUnexpectedFailure()
			};
		}

		/// <summary>
		/// Creates the failure for a platform NFC session failure.
		/// </summary>
		/// <param name="Failure">The platform session failure.</param>
		/// <returns>The structured failure.</returns>
		public static TravelDocumentFailure CreatePlatformFailure(NfcIsoDepSessionFailure Failure)
		{
			ArgumentNullException.ThrowIfNull(Failure);

			return Failure.FailureCode switch
			{
				NfcIsoDepSessionFailureCode.NotSupported => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcNotSupported,
					TravelDocumentRecoveryAction.CloseFlow),
				NfcIsoDepSessionFailureCode.Unavailable => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcUnavailable,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				NfcIsoDepSessionFailureCode.Cancelled => CreateCancellation(),
				NfcIsoDepSessionFailureCode.TimedOut => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcSessionTimedOut,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				NfcIsoDepSessionFailureCode.MultipleTagsDetected => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcMultipleTagsDetected,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				NfcIsoDepSessionFailureCode.UnsupportedTag => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcUnsupportedTag,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				NfcIsoDepSessionFailureCode.ConnectionLost => new TravelDocumentFailure(
					TravelDocumentFailureCode.NfcConnectionLost,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				NfcIsoDepSessionFailureCode.EntitlementMissing => new TravelDocumentFailure(
					TravelDocumentFailureCode.PlatformEntitlementMissing,
					TravelDocumentRecoveryAction.CloseFlow),
				_ => CreateSessionFailure()
			};
		}

		/// <summary>
		/// Creates the failure for a chip read that the user cancelled.
		/// </summary>
		/// <returns>The structured failure.</returns>
		public static TravelDocumentFailure CreateCancellation()
		{
			return new TravelDocumentFailure(
				TravelDocumentFailureCode.NfcSessionCancelled,
				TravelDocumentRecoveryAction.RetryCurrentStep);
		}

		/// <summary>
		/// Creates the failure for an NFC session that could not be started or ended unexpectedly.
		/// </summary>
		/// <returns>The structured failure.</returns>
		public static TravelDocumentFailure CreateSessionFailure()
		{
			return new TravelDocumentFailure(
				TravelDocumentFailureCode.NfcSessionFailed,
				TravelDocumentRecoveryAction.RetryCurrentStep);
		}

		/// <summary>
		/// Creates the failure for an unexpected error outside the chip protocol, such as a readout that could not be saved.
		/// </summary>
		/// <returns>The structured failure.</returns>
		public static TravelDocumentFailure CreateUnexpectedFailure()
		{
			return new TravelDocumentFailure(
				TravelDocumentFailureCode.Unknown,
				TravelDocumentRecoveryAction.RetryCurrentStep);
		}

		/// <summary>
		/// Maps an authentication failure, separating chips that could not be opened from unsupported security methods.
		/// </summary>
		/// <param name="Result">The typed authentication outcome, if authentication was attempted.</param>
		/// <returns>The structured failure.</returns>
		public static TravelDocumentFailure CreateAuthenticationFailure(AuthenticateResult? Result)
		{
			return Result switch
			{
				// The chip refuses these before the access key is checked, so scanning the details again cannot help.
				AuthenticateResult.UnableToInitializePace or
				AuthenticateResult.BacNotImplemented => new TravelDocumentFailure(
					TravelDocumentFailureCode.UnsupportedSecurityMethod,
					TravelDocumentRecoveryAction.ContactSupport),

				// A rejected PACE or BAC exchange usually means a wrong access key, from misread or mistyped document details.
				_ => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipAuthenticationFailed,
					TravelDocumentRecoveryAction.RestartFlow)
			};
		}

		/// <summary>
		/// Maps a chip read failure after authentication, using the certificate validation reason when the read result is missing.
		/// </summary>
		/// <param name="Result">The typed read outcome, if the read was attempted.</param>
		/// <param name="CertificateData">The certificate validation outcome captured during the readout.</param>
		/// <returns>The structured failure.</returns>
		public static TravelDocumentFailure CreateReadFailure(ReadTravelDocumentResult? Result, TravelDocumentCertificateData? CertificateData)
		{
			if (!Result.HasValue)
				return CreateCertificateFailure(CertificateData?.Reason ?? TravelDocumentCertificateValidationReason.None);

			return Result.Value switch
			{
				ReadTravelDocumentResult.Lds1ApplicationNotFound => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipNotElectronicTravelDocument,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				ReadTravelDocumentResult.UnableToParseEfCom or
				ReadTravelDocumentResult.UnableToParseEfSod or
				ReadTravelDocumentResult.UnableToParseEfDg => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipReadFailed,
					TravelDocumentRecoveryAction.ContactSupport),
				ReadTravelDocumentResult.DgHashDigestInvalid => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipDataIntegrityInvalid,
					TravelDocumentRecoveryAction.ContactSupport),
				ReadTravelDocumentResult.NoCertificates or
				ReadTravelDocumentResult.MultipleCertificates or
				ReadTravelDocumentResult.InvalidCertificate => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipCertificateInvalid,
					TravelDocumentRecoveryAction.ContactSupport),
				ReadTravelDocumentResult.RevokedCertificate => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipCertificateRevoked,
					TravelDocumentRecoveryAction.UseAnotherDocument),
				ReadTravelDocumentResult.RevocationStatusUnknown => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipCertificateUnavailable,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				_ => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipReadFailed,
					TravelDocumentRecoveryAction.RetryCurrentStep)
			};
		}

		private static TravelDocumentFailure CreateCertificateFailure(TravelDocumentCertificateValidationReason Reason)
		{
			return Reason switch
			{
				TravelDocumentCertificateValidationReason.RevokedCertificate => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipCertificateRevoked,
					TravelDocumentRecoveryAction.UseAnotherDocument),
				TravelDocumentCertificateValidationReason.DataGroupHashMismatch => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipDataIntegrityInvalid,
					TravelDocumentRecoveryAction.ContactSupport),
				TravelDocumentCertificateValidationReason.NoCertificate or
				TravelDocumentCertificateValidationReason.MultipleCertificates or
				TravelDocumentCertificateValidationReason.InvalidCertificate => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipCertificateInvalid,
					TravelDocumentRecoveryAction.ContactSupport),
				TravelDocumentCertificateValidationReason.RevocationStatusUnknown => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipCertificateUnavailable,
					TravelDocumentRecoveryAction.RetryCurrentStep),
				_ => new TravelDocumentFailure(
					TravelDocumentFailureCode.ChipReadFailed,
					TravelDocumentRecoveryAction.RetryCurrentStep)
			};
		}
	}
}
