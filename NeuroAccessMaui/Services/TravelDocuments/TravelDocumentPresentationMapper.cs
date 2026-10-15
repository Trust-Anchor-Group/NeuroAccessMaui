namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Maps travel-document chip read failures to the resource keys and actions the travel-document page presents.
	/// </summary>
	/// <remarks>
	/// The mapper returns resource keys rather than localized text, so it stays free of UI dependencies. Each failure code
	/// has its own headline and detail, and each recovery action decides which actions are offered and in which order.
	/// </remarks>
	public static class TravelDocumentPresentationMapper
	{
		/// <summary>
		/// Gets the resource key of the headline shown for a failure.
		/// </summary>
		/// <param name="FailureCode">The failure code.</param>
		/// <returns>The headline resource key.</returns>
		public static string GetHeadlineResourceKey(TravelDocumentFailureCode FailureCode)
		{
			return FailureCode switch
			{
				TravelDocumentFailureCode.NfcNotSupported or
				TravelDocumentFailureCode.PlatformEntitlementMissing => "KycTravelDocumentFailureNfcNotSupportedTitle",
				TravelDocumentFailureCode.NfcUnavailable => "KycTravelDocumentFailureNfcUnavailableTitle",
				TravelDocumentFailureCode.NfcSessionCancelled => "KycTravelDocumentNfcCancelledTitle",
				TravelDocumentFailureCode.NfcSessionTimedOut => "KycTravelDocumentFailureTimedOutTitle",
				TravelDocumentFailureCode.NfcMultipleTagsDetected => "KycTravelDocumentFailureMultipleTagsTitle",
				TravelDocumentFailureCode.NfcUnsupportedTag => "KycTravelDocumentFailureUnsupportedTagTitle",
				TravelDocumentFailureCode.NfcConnectionLost => "KycTravelDocumentFailureConnectionLostTitle",
				TravelDocumentFailureCode.ChipAuthenticationFailed => "KycTravelDocumentFailureAuthenticationTitle",
				TravelDocumentFailureCode.UnsupportedSecurityMethod => "KycTravelDocumentFailureUnsupportedSecurityTitle",
				TravelDocumentFailureCode.ChipReadFailed => "KycTravelDocumentFailureReadTitle",
				TravelDocumentFailureCode.ChipNotElectronicTravelDocument => "KycTravelDocumentFailureNotTravelDocumentTitle",
				TravelDocumentFailureCode.ChipCertificateInvalid or
				TravelDocumentFailureCode.ChipDataIntegrityInvalid => "KycTravelDocumentFailureVerificationTitle",
				TravelDocumentFailureCode.ChipCertificateRevoked => "KycTravelDocumentFailureRevokedTitle",
				TravelDocumentFailureCode.ChipCertificateUnavailable => "KycTravelDocumentFailureCertificateCheckTitle",
				_ => "KycTravelDocumentNfcErrorTitle"
			};
		}

		/// <summary>
		/// Gets the resource key of the detail text shown for a failure.
		/// </summary>
		/// <param name="FailureCode">The failure code.</param>
		/// <returns>The detail resource key.</returns>
		public static string GetDetailResourceKey(TravelDocumentFailureCode FailureCode)
		{
			return FailureCode switch
			{
				TravelDocumentFailureCode.NfcNotSupported or
				TravelDocumentFailureCode.PlatformEntitlementMissing => "KycTravelDocumentFailureNfcNotSupportedDescription",
				TravelDocumentFailureCode.NfcUnavailable => "KycTravelDocumentFailureNfcUnavailableDescription",
				TravelDocumentFailureCode.NfcSessionCancelled => "KycTravelDocumentNfcCancelledDescription",
				TravelDocumentFailureCode.NfcSessionTimedOut => "KycTravelDocumentFailureTimedOutDescription",
				TravelDocumentFailureCode.NfcMultipleTagsDetected => "KycTravelDocumentFailureMultipleTagsDescription",
				TravelDocumentFailureCode.NfcUnsupportedTag => "KycTravelDocumentFailureUnsupportedTagDescription",
				TravelDocumentFailureCode.NfcConnectionLost => "KycTravelDocumentFailureConnectionLostDescription",
				TravelDocumentFailureCode.ChipAuthenticationFailed => "KycTravelDocumentFailureAuthenticationDescription",
				TravelDocumentFailureCode.UnsupportedSecurityMethod => "KycTravelDocumentFailureUnsupportedSecurityDescription",
				TravelDocumentFailureCode.ChipReadFailed => "KycTravelDocumentNfcReadFailed",
				TravelDocumentFailureCode.ChipNotElectronicTravelDocument => "KycTravelDocumentFailureNotTravelDocumentDescription",
				TravelDocumentFailureCode.ChipCertificateInvalid => "KycTravelDocumentFailureCertificateInvalidDescription",
				TravelDocumentFailureCode.ChipDataIntegrityInvalid => "KycTravelDocumentFailureDataIntegrityDescription",
				TravelDocumentFailureCode.ChipCertificateRevoked => "KycTravelDocumentFailureRevokedDescription",
				TravelDocumentFailureCode.ChipCertificateUnavailable => "KycTravelDocumentFailureCertificateCheckDescription",
				_ => "KycTravelDocumentNfcFailed"
			};
		}

		/// <summary>
		/// Gets the resource key of the message shown on the iOS NFC sheet when it closes after a failure.
		/// </summary>
		/// <param name="FailureCode">The failure code.</param>
		/// <returns>The native sheet message resource key.</returns>
		public static string GetNativeAlertResourceKey(TravelDocumentFailureCode FailureCode)
		{
			return FailureCode switch
			{
				TravelDocumentFailureCode.NfcSessionCancelled => "TravelDocumentScan_IosNfcCancelled",
				TravelDocumentFailureCode.NfcConnectionLost => "TravelDocumentScan_IosNfcConnectionLost",
				TravelDocumentFailureCode.NfcSessionTimedOut => "TravelDocumentScan_IosNfcTimedOut",
				_ => "TravelDocumentScan_IosNfcFailed"
			};
		}

		/// <summary>
		/// Gets the main action offered for a recovery action.
		/// </summary>
		/// <param name="RecoveryAction">The preferred recovery action of the failure.</param>
		/// <returns>The action for the primary button.</returns>
		public static TravelDocumentRecoveryAction GetPrimaryAction(TravelDocumentRecoveryAction RecoveryAction)
		{
			return RecoveryAction switch
			{
				TravelDocumentRecoveryAction.RestartFlow => TravelDocumentRecoveryAction.RestartFlow,
				TravelDocumentRecoveryAction.UseAnotherDocument => TravelDocumentRecoveryAction.UseAnotherDocument,
				TravelDocumentRecoveryAction.CloseFlow => TravelDocumentRecoveryAction.CloseFlow,

				// Support is offered next to a retry, because a failure that needs help can still be a one-off.
				_ => TravelDocumentRecoveryAction.RetryCurrentStep
			};
		}

		/// <summary>
		/// Gets the alternative action offered next to the main action for a recovery action.
		/// </summary>
		/// <param name="RecoveryAction">The preferred recovery action of the failure.</param>
		/// <returns>The action for the secondary button, or <see cref="TravelDocumentRecoveryAction.None"/> if there is none.</returns>
		public static TravelDocumentRecoveryAction GetSecondaryAction(TravelDocumentRecoveryAction RecoveryAction)
		{
			return RecoveryAction switch
			{
				TravelDocumentRecoveryAction.RestartFlow => TravelDocumentRecoveryAction.RetryCurrentStep,
				TravelDocumentRecoveryAction.ContactSupport => TravelDocumentRecoveryAction.ContactSupport,
				TravelDocumentRecoveryAction.UseAnotherDocument or
				TravelDocumentRecoveryAction.CloseFlow => TravelDocumentRecoveryAction.None,
				_ => TravelDocumentRecoveryAction.RestartFlow
			};
		}

		/// <summary>
		/// Gets the resource key of the button label for an action.
		/// </summary>
		/// <remarks>
		/// This app has no support channel for document checks, so the support action closes the page instead.
		/// </remarks>
		/// <param name="Action">The action.</param>
		/// <returns>The button label resource key, or an empty string for <see cref="TravelDocumentRecoveryAction.None"/>.</returns>
		public static string GetActionResourceKey(TravelDocumentRecoveryAction Action)
		{
			return Action switch
			{
				TravelDocumentRecoveryAction.RetryCurrentStep => "KycTravelDocumentRetryNfcButton",
				TravelDocumentRecoveryAction.RestartFlow => "KycTravelDocumentRescanButton",
				TravelDocumentRecoveryAction.UseAnotherDocument => "KycTravelDocumentUseAnotherDocumentButton",
				TravelDocumentRecoveryAction.CloseFlow => "KycTravelDocumentManualFallbackButton",
				TravelDocumentRecoveryAction.ContactSupport => "Close",
				_ => string.Empty
			};
		}
	}
}
