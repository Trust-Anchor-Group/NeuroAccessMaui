using System.Collections.Generic;
using IdApp.Cv;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a source document image after perspective rectification.
	/// </summary>
	public sealed class RectifiedDocument
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RectifiedDocument"/> class.
		/// </summary>
		/// <param name="Image">The rectified grayscale document image.</param>
		/// <param name="SourceQuad">The source document quadrilateral.</param>
		/// <param name="QualityMetrics">The quality metrics for the rectified document.</param>
		/// <param name="Metadata">Additional rectification metadata.</param>
		public RectifiedDocument(
			Matrix<float> Image,
			DocumentQuad SourceQuad,
			DocumentQualityMetrics QualityMetrics,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Image);
			ArgumentNullException.ThrowIfNull(SourceQuad);
			ArgumentNullException.ThrowIfNull(QualityMetrics);

			this.Image = Image;
			this.SourceQuad = SourceQuad;
			this.QualityMetrics = QualityMetrics;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the rectified grayscale document image.
		/// </summary>
		public Matrix<float> Image { get; }

		/// <summary>
		/// Gets the source document quadrilateral.
		/// </summary>
		public DocumentQuad SourceQuad { get; }

		/// <summary>
		/// Gets the quality metrics for the rectified document.
		/// </summary>
		public DocumentQualityMetrics QualityMetrics { get; }

		/// <summary>
		/// Gets additional rectification metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
