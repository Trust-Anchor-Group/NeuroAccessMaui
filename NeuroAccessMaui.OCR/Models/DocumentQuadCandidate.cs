using System.Collections.Generic;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a scored document quadrilateral candidate.
	/// </summary>
	public sealed class DocumentQuadCandidate
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentQuadCandidate"/> class.
		/// </summary>
		/// <param name="Quad">The document quadrilateral.</param>
		/// <param name="Score">The combined candidate score.</param>
		/// <param name="EdgeSupportScore">The edge-support score.</param>
		/// <param name="ContrastScore">The inside/outside contrast score.</param>
		/// <param name="AreaScore">The document area score.</param>
		/// <param name="FailureCategory">The failure category, if the candidate is unusable.</param>
		/// <param name="Metadata">Additional candidate metadata.</param>
		public DocumentQuadCandidate(
			DocumentQuad Quad,
			float Score,
			float EdgeSupportScore,
			float ContrastScore,
			float AreaScore,
			MrzScanFailureCategory FailureCategory = MrzScanFailureCategory.None,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Quad);

			this.Quad = Quad;
			this.Score = Score;
			this.EdgeSupportScore = EdgeSupportScore;
			this.ContrastScore = ContrastScore;
			this.AreaScore = AreaScore;
			this.FailureCategory = FailureCategory;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the document quadrilateral.
		/// </summary>
		public DocumentQuad Quad { get; }

		/// <summary>
		/// Gets the combined candidate score.
		/// </summary>
		public float Score { get; }

		/// <summary>
		/// Gets the edge-support score.
		/// </summary>
		public float EdgeSupportScore { get; }

		/// <summary>
		/// Gets the inside/outside contrast score.
		/// </summary>
		public float ContrastScore { get; }

		/// <summary>
		/// Gets the document area score.
		/// </summary>
		public float AreaScore { get; }

		/// <summary>
		/// Gets the failure category, if the candidate is unusable.
		/// </summary>
		public MrzScanFailureCategory FailureCategory { get; }

		/// <summary>
		/// Gets additional candidate metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
