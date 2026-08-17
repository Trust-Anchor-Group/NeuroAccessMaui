namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Enumerations of possible results when reading a travel document.
	/// </summary>
	public enum ReadTravelDocumentResult
	{
		/// <summary>
		/// Readout successful.
		/// </summary>
		Success,

		/// <summary>
		/// LDS1 eMRTD application was not found on chip. (Not an electronic passport.)
		/// </summary>
		Lds1ApplicationNotFound,

		/// <summary>
		/// Unable to read EF.COM. (Try again.)
		/// </summary>
		UnableToReadEfCom,

		/// <summary>
		/// Unable to parse EF.COM. (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
		/// EF.COM used to identify services available on the chip.
		/// </summary>
		UnableToParseEfCom,

		/// <summary>
		/// Unable to read EF.SOD. (Try again.)
		/// </summary>
		UnableToReadEfSod,

		/// <summary>
		/// Unable to parse EF.SOD. (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
		/// EF.SOD used to identify issuers of documents.
		/// </summary>
		UnableToParseEfSod,

		/// <summary>
		/// Unable to read EF.DGx. (Try again.)
		/// </summary>
		UnableToReadEfDg,

		/// <summary>
		/// Unable to parse EF.DGx. (Incompatibility, missing support; suggest sending log to support for troubleshooting if problem persists.)
		/// </summary>
		UnableToParseEfDg,

		/// <summary>
		/// Hash Digest as reported by EF.SOD does not match the has digest of the data group read.
		/// (Data has been corrupted, either in transit or on the passport.)
		/// </summary>
		DgHashDigestInvalid,

		/// <summary>
		/// No certificates to validate available in EF.SOD.
		/// (Not a valid Travel Document)
		/// </summary>
		NoCertificates,

		/// <summary>
		/// Multiple certificates to validate available in EF.SOD were provided. Only one allowed.
		/// (Not a valid Travel Document)
		/// </summary>
		MultipleCertificates,

		/// <summary>
		/// Certificate provided in EF.SOD is not a valid certificate.
		/// </summary>
		InvalidCertificate,

		/// <summary>
		/// A certificate used in the travel document has been revoked.
		/// </summary>
		RevokedCertificate,

		/// <summary>
		/// Unable to validate the revocation status of a certificate used in the travel document.
		/// </summary>
		RevocationStatusUnknown
	}
}
