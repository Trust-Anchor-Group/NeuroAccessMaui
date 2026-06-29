using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Shares persisted OCR debug-artifact bundles.
	/// </summary>
	public interface IOcrArtifactExportService
	{
		/// <summary>
		/// Shares the supplied artifact bundle.
		/// </summary>
		/// <param name="Bundle">The artifact bundle to share.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ShareAsync(OcrArtifactBundle Bundle, CancellationToken CancellationToken);
	}
}
