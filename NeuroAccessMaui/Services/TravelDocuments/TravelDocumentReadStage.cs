namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Identifies the user-facing stage of a travel-document chip readout.
	/// </summary>
	public enum TravelDocumentReadStage
	{
		/// <summary>
		/// Waiting for the document chip to be detected.
		/// </summary>
		Detect = 0,

		/// <summary>
		/// Establishing an authenticated, encrypted connection with the chip.
		/// </summary>
		Secure = 1,

		/// <summary>
		/// Reading and validating document data from the chip.
		/// </summary>
		Read = 2,

		/// <summary>
		/// Finishing verification and saving the readout.
		/// </summary>
		Verify = 3
	}
}
