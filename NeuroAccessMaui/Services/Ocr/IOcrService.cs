using IdApp.Cv;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Ocr
{
	/// <summary>
	/// Interface for the Optical Character Recognition (OCR) Service.
	/// </summary>
	[DefaultImplementation(typeof(OcrService))]
	public interface IOcrService
	{
		/// <summary>
		/// Processes an image and tries to extract strings of characters from it.
		/// </summary>
		/// <param name="Image">Pre-processed image.</param>
		/// <param name="Language">Expected language on text in image.</param>
		/// <param name="PageSegmentationMode">Optional page segmentationmode.</param>
		/// <returns>Any lines of text found.</returns>
		Task<string[]> ProcessImage(IMatrix Image, string Language, PageSegmentationMode? PageSegmentationMode);

	}
}
