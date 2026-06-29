using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Provides the simplified OCR scan entrypoint used by the application.
	/// </summary>
	public interface IOcrScanService
	{
		/// <summary>
		/// Processes the supplied scan request.
		/// </summary>
		/// <param name="Request">The scan request.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The scan result.</returns>
		Task<OcrScanResult> ScanAsync(OcrScanRequest Request, CancellationToken CancellationToken);
	}
}
