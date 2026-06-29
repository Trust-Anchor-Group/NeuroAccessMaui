using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.OCR.Pipeline;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Provides preview analysis for MRZ capture guidance using the ONNX MRZ pipeline.
	/// </summary>
	public sealed class MrzPreviewAnalysisService : IMrzPreviewAnalysisService
	{
		private readonly OnnxMrzScanEngine scanEngine;

		/// <summary>
		/// Initializes a new instance of the <see cref="MrzPreviewAnalysisService"/> class.
		/// </summary>
		/// <param name="ScanEngine">The ONNX MRZ scan engine.</param>
		public MrzPreviewAnalysisService(OnnxMrzScanEngine ScanEngine)
		{
			ArgumentNullException.ThrowIfNull(ScanEngine);

			this.scanEngine = ScanEngine;
		}

		/// <inheritdoc/>
		public MrzPreviewAnalysisResult Analyze(MrzPreviewAnalysisRequest Request, CancellationToken CancellationToken)
		{
			return this.scanEngine.AnalyzePreview(Request, CancellationToken);
		}
	}
}
