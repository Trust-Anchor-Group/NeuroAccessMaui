using System.Collections.Generic;
using IdApp.Cv;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a lightweight preview-analysis request for MRZ capture.
	/// </summary>
	public sealed class MrzPreviewAnalysisRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzPreviewAnalysisRequest"/> class.
		/// </summary>
		/// <param name="Image">The preview image.</param>
		/// <param name="SourceRotationDegrees">Clockwise source-rotation degrees to normalize before analysis.</param>
		/// <param name="ExpectedDocumentRegion">The optional whole-document guide region.</param>
		/// <param name="DocumentKindHint">The expected document kind, if known.</param>
		/// <param name="DocumentSideHint">The expected document side, if known.</param>
		/// <param name="Metadata">Additional caller metadata.</param>
		public MrzPreviewAnalysisRequest(
			IMatrix Image,
			int SourceRotationDegrees = 0,
			DocumentRegionHint? ExpectedDocumentRegion = null,
			OcrDocumentKindHint DocumentKindHint = OcrDocumentKindHint.Unknown,
			OcrDocumentSideHint DocumentSideHint = OcrDocumentSideHint.Unknown,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Image);

			this.Image = Image;
			this.SourceRotationDegrees = NormalizeSourceRotationDegrees(SourceRotationDegrees);
			this.ExpectedDocumentRegion = ExpectedDocumentRegion;
			this.DocumentKindHint = DocumentKindHint;
			this.DocumentSideHint = DocumentSideHint;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the preview image.
		/// </summary>
		public IMatrix Image { get; }

		/// <summary>
		/// Gets clockwise source-rotation degrees to normalize before analysis.
		/// </summary>
		public int SourceRotationDegrees { get; }

		/// <summary>
		/// Gets the optional whole-document guide region.
		/// </summary>
		public DocumentRegionHint? ExpectedDocumentRegion { get; }

		/// <summary>
		/// Gets the expected document kind, if known.
		/// </summary>
		public OcrDocumentKindHint DocumentKindHint { get; }

		/// <summary>
		/// Gets the expected document side, if known.
		/// </summary>
		public OcrDocumentSideHint DocumentSideHint { get; }

		/// <summary>
		/// Gets additional caller metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }

		private static int NormalizeSourceRotationDegrees(int SourceRotationDegrees)
		{
			int NormalizedRotationDegrees = SourceRotationDegrees % 360;
			if (NormalizedRotationDegrees < 0)
				NormalizedRotationDegrees += 360;

			return NormalizedRotationDegrees switch
			{
				90 => 90,
				180 => 180,
				270 => 270,
				_ => 0
			};
		}
	}
}
