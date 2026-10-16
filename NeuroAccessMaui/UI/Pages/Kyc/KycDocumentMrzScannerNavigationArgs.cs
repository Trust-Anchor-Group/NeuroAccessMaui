using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.Services.TravelDocuments;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Navigation arguments for the KYC live document-line scanner page.
	/// </summary>
	public sealed class KycDocumentMrzScannerNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Gets or sets the completion source that receives the validated document scan result.
		/// </summary>
		public TaskCompletionSource<TravelDocumentMrzResult?>? CompletionSource { get; init; }

		/// <summary>
		/// Gets or sets the preferred document type shown when the scanner opens.
		/// </summary>
		public OcrDocumentKindHint PreferredDocumentKind { get; init; } = OcrDocumentKindHint.Unknown;

		/// <summary>
		/// Gets or sets how the scanner session ended. The scanner sets this before it completes
		/// <see cref="CompletionSource"/>, so callers can tell a cancelled scan from a missing camera permission
		/// or a scanner failure when the result is <c>null</c>.
		/// </summary>
		public KycDocumentMrzScannerOutcome Outcome { get; set; } = KycDocumentMrzScannerOutcome.Pending;
	}
}
