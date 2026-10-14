namespace NeuroAccessMaui.Services.TravelDocuments
{
	/// <summary>
	/// Describes the preferred next action after a travel-document chip read failure.
	/// </summary>
	public enum TravelDocumentRecoveryAction
	{
		/// <summary>
		/// No explicit recovery action is available.
		/// </summary>
		None,

		/// <summary>
		/// Read the chip again with the same document details.
		/// </summary>
		RetryCurrentStep,

		/// <summary>
		/// Scan the document details again before reading the chip.
		/// </summary>
		RestartFlow,

		/// <summary>
		/// Leave the chip step and continue without the chip, because chip reading cannot work here.
		/// </summary>
		CloseFlow,

		/// <summary>
		/// Retrying is unlikely to help, so the user needs help from support or the identity provider.
		/// </summary>
		ContactSupport,

		/// <summary>
		/// Abandon the current document and scan a different document.
		/// </summary>
		UseAnotherDocument
	}
}
