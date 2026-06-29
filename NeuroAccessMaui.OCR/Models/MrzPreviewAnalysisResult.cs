using System.Collections.Generic;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents the result of lightweight preview analysis for MRZ capture.
	/// </summary>
	public sealed class MrzPreviewAnalysisResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzPreviewAnalysisResult"/> class.
		/// </summary>
		/// <param name="GuidanceState">The next guidance state.</param>
		/// <param name="QualityMetrics">The preview quality metrics.</param>
		/// <param name="DocumentQuadCandidate">The document quad candidate, if any.</param>
		/// <param name="MrzPlausibilityScore">The rough MRZ plausibility score.</param>
		/// <param name="IsCommitReady">If the frame can be considered for commit capture.</param>
		/// <param name="FailureCategory">The primary failure category, if any.</param>
		/// <param name="Metadata">Additional analysis metadata.</param>
		/// <param name="MrzCandidate">The preview MRZ candidate, if any.</param>
		public MrzPreviewAnalysisResult(
			MrzCaptureGuidanceState GuidanceState,
			DocumentQualityMetrics QualityMetrics,
			DocumentQuadCandidate? DocumentQuadCandidate,
			float MrzPlausibilityScore,
			bool IsCommitReady,
			MrzScanFailureCategory FailureCategory = MrzScanFailureCategory.None,
			IReadOnlyDictionary<string, string>? Metadata = null,
			MrzRegionCandidate? MrzCandidate = null)
		{
			ArgumentNullException.ThrowIfNull(QualityMetrics);

			this.GuidanceState = GuidanceState;
			this.QualityMetrics = QualityMetrics;
			this.DocumentQuadCandidate = DocumentQuadCandidate;
			this.MrzCandidate = MrzCandidate;
			this.MrzPlausibilityScore = MrzPlausibilityScore;
			this.IsCommitReady = IsCommitReady;
			this.FailureCategory = FailureCategory;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the next guidance state.
		/// </summary>
		public MrzCaptureGuidanceState GuidanceState { get; }

		/// <summary>
		/// Gets the preview quality metrics.
		/// </summary>
		public DocumentQualityMetrics QualityMetrics { get; }

		/// <summary>
		/// Gets the document quad candidate, if any.
		/// </summary>
		public DocumentQuadCandidate? DocumentQuadCandidate { get; }

		/// <summary>
		/// Gets the preview MRZ candidate, if any.
		/// </summary>
		public MrzRegionCandidate? MrzCandidate { get; }

		/// <summary>
		/// Gets the rough MRZ plausibility score.
		/// </summary>
		public float MrzPlausibilityScore { get; }

		/// <summary>
		/// Gets a value indicating whether the frame can be considered for commit capture.
		/// </summary>
		public bool IsCommitReady { get; }

		/// <summary>
		/// Gets the primary failure category, if any.
		/// </summary>
		public MrzScanFailureCategory FailureCategory { get; }

		/// <summary>
		/// Gets additional analysis metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
