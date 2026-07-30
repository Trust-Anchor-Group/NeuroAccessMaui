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
	}
}
