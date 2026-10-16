using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Locates and reads the MRZ in live camera frames.
	/// </summary>
	public interface IMrzPreviewAnalysisService
	{
		/// <summary>
		/// Locates the MRZ in a preview frame, reads it once and validates the read against its check digits.
		/// </summary>
		/// <remarks>
		/// Each frame is analyzed on its own. Combine results across frames with
		/// <see cref="Pipeline.MrzReadConsensus"/> to decide when a read can be accepted.
		/// </remarks>
		/// <param name="Request">The preview analysis request.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The analysis result, including the MRZ outline, guidance and read.</returns>
		MrzPreviewAnalysisResult Analyze(MrzPreviewAnalysisRequest Request, CancellationToken CancellationToken);
	}
}
