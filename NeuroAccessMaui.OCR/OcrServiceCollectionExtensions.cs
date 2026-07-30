#if ANDROID || IOS || WINDOWS
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using NeuroAccessMaui.OCR.Pipeline;
using NeuroAccessMaui.OCR.Services;

namespace NeuroAccessMaui.OCR
{
	/// <summary>
	/// Provides dependency injection registration helpers for the OCR subsystem.
	/// </summary>
	public static class OcrServiceCollectionExtensions
	{
		/// <summary>
		/// Registers the OCR services and the active ONNX-backed MRZ scanner.
		/// </summary>
		/// <param name="Builder">The MAUI application builder.</param>
		/// <returns>The same builder instance for chaining.</returns>
		public static MauiAppBuilder RegisterOcrServices(this MauiAppBuilder Builder)
		{
			ArgumentNullException.ThrowIfNull(Builder);

			Builder.Services.AddSingleton<OnnxModelAssetLoader>();
			Builder.Services.AddSingleton<MrzScannerMrzDetector>();
			Builder.Services.AddSingleton<MrzScannerMrzRecognizer>();
			Builder.Services.AddSingleton<MrzRegionRectifier>();
			Builder.Services.AddSingleton<OnnxMrzScanEngine>();
			Builder.Services.AddSingleton<IMrzPreviewAnalysisService, MrzPreviewAnalysisService>();
			Builder.Services.AddSingleton<IOcrArtifactExportService, OcrArtifactExportService>();
			Builder.Services.AddSingleton<IOcrScanService, OcrScanService>();

			return Builder;
		}
	}
}
#endif
