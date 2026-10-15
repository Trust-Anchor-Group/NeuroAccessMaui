using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.OCR.Pipeline;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Locates and reads the MRZ in live camera frames using the ONNX MRZ pipeline.
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
			ArgumentNullException.ThrowIfNull(Request);

			return this.scanEngine.ProcessFrame(
				Request.Image,
				Request.SourceRotationDegrees,
				Request.DocumentKindHint,
				Request.Metadata,
				CancellationToken).Analysis;
		}
	}
}
