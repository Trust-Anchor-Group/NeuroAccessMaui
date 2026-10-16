namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Identifies a specific travel-document chip read failure condition.
	/// </summary>
	public enum TravelDocumentFailureCode
	{
		/// <summary>
		/// An unspecified failure occurred.
		/// </summary>
		Unknown,

		/// <summary>
		/// NFC is not supported on the current device or platform.
		/// </summary>
		NfcNotSupported,

		/// <summary>
		/// NFC is unavailable due to runtime configuration or device state.
		/// </summary>
		NfcUnavailable,

		/// <summary>
		/// The platform NFC session was cancelled by the user.
		/// </summary>
		NfcSessionCancelled,

		/// <summary>
		/// The platform NFC session or a chip command timed out.
		/// </summary>
		NfcSessionTimedOut,

		/// <summary>
		/// The platform NFC session failed unexpectedly.
		/// </summary>
		NfcSessionFailed,

		/// <summary>
		/// The device detected multiple tags instead of a single document.
		/// </summary>
		NfcMultipleTagsDetected,

		/// <summary>
		/// The detected tag is not a supported ICAO travel document.
		/// </summary>
		NfcUnsupportedTag,

		/// <summary>
		/// The connection to the tag was lost during the read.
		/// </summary>
		NfcConnectionLost,

		/// <summary>
		/// The chip could not be authenticated.
		/// </summary>
		ChipAuthenticationFailed,

		/// <summary>
		/// The document uses a security method this app does not support yet.
		/// </summary>
		UnsupportedSecurityMethod,

		/// <summary>
		/// The chip read could not be completed.
		/// </summary>
		ChipReadFailed,

		/// <summary>
		/// The detected chip is not an ICAO LDS1 electronic travel document.
		/// </summary>
		ChipNotElectronicTravelDocument,

		/// <summary>
		/// The chip returned invalid or untrusted certificate information.
		/// </summary>
		ChipCertificateInvalid,

		/// <summary>
		/// The revocation status of the chip's certificates could not be checked.
		/// </summary>
		ChipCertificateUnavailable,

		/// <summary>
		/// The chip data integrity validation failed.
		/// </summary>
		ChipDataIntegrityInvalid,

		/// <summary>
		/// A required platform entitlement or permission is missing.
		/// </summary>
		PlatformEntitlementMissing,

		/// <summary>
		/// A certificate used by the travel document has been revoked.
		/// </summary>
		ChipCertificateRevoked
	}
}
