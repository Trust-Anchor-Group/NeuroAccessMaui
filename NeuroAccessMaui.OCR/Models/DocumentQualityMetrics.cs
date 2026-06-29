namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents image quality metrics used by the MRZ capture pipeline.
	/// </summary>
	public sealed class DocumentQualityMetrics
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentQualityMetrics"/> class.
		/// </summary>
		/// <param name="Mean">The average luminance value.</param>
		/// <param name="StandardDeviation">The luminance standard deviation.</param>
		/// <param name="DynamicRange">The luminance dynamic range.</param>
		/// <param name="Sharpness">The Laplacian edge-energy sharpness estimate.</param>
		/// <param name="HighlightRatio">The ratio of near-white pixels.</param>
		/// <param name="ShadowRatio">The ratio of near-black pixels.</param>
		/// <param name="GlareRatio">The ratio of connected near-white glare candidates.</param>
		/// <param name="QualityScore">The combined quality score.</param>
		public DocumentQualityMetrics(
			float Mean,
			float StandardDeviation,
			float DynamicRange,
			float Sharpness,
			float HighlightRatio,
			float ShadowRatio,
			float GlareRatio,
			float QualityScore)
		{
			this.Mean = Mean;
			this.StandardDeviation = StandardDeviation;
			this.DynamicRange = DynamicRange;
			this.Sharpness = Sharpness;
			this.HighlightRatio = HighlightRatio;
			this.ShadowRatio = ShadowRatio;
			this.GlareRatio = GlareRatio;
			this.QualityScore = QualityScore;
		}

		/// <summary>
		/// Gets the average luminance value.
		/// </summary>
		public float Mean { get; }

		/// <summary>
		/// Gets the luminance standard deviation.
		/// </summary>
		public float StandardDeviation { get; }

		/// <summary>
		/// Gets the luminance dynamic range.
		/// </summary>
		public float DynamicRange { get; }

		/// <summary>
		/// Gets the Laplacian edge-energy sharpness estimate.
		/// </summary>
		public float Sharpness { get; }

		/// <summary>
		/// Gets the ratio of near-white pixels.
		/// </summary>
		public float HighlightRatio { get; }

		/// <summary>
		/// Gets the ratio of near-black pixels.
		/// </summary>
		public float ShadowRatio { get; }

		/// <summary>
		/// Gets the ratio of connected near-white glare candidates.
		/// </summary>
		public float GlareRatio { get; }

		/// <summary>
		/// Gets the combined quality score.
		/// </summary>
		public float QualityScore { get; }
	}
}
