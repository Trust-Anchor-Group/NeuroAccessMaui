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
		UnableToParseEfDg
	}
}
