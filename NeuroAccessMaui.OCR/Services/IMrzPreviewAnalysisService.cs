using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Provides lightweight preview analysis for MRZ capture guidance.
	/// </summary>
	public interface IMrzPreviewAnalysisService
	{
		/// <summary>
		/// Analyzes a preview frame without invoking OCR.
		/// </summary>
		/// <param name="Request">The preview analysis request.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The preview analysis result.</returns>
		MrzPreviewAnalysisResult Analyze(MrzPreviewAnalysisRequest Request, CancellationToken CancellationToken);
	}
}
