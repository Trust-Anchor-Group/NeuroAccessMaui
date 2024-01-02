namespace IdApp.Nfc.Records
{
	/// <summary>
	/// Type of NDEF Record
	/// </summary>
	public enum NDefRecordType
	{
		/// <summary>
		/// Absolute URI
		/// </summary>
		Uri,

		/// <summary>
		/// Well-known Type
		/// </summary>
		WellKnownType,

		/// <summary>
		/// External Type
		/// </summary>
		ExternalType,

		/// <summary>
		/// MIME-Type
		/// </summary>
		MimeType
	};
}
