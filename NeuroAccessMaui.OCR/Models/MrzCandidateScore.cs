using System.Collections.Generic;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents visual and parser score components for an MRZ candidate.
	/// </summary>
	public sealed class MrzCandidateScore
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzCandidateScore"/> class.
		/// </summary>
		/// <param name="VisualScore">The visual score before OCR.</param>
		/// <param name="GeometryScore">The template geometry score.</param>
		/// <param name="WidthScore">The standards-derived width score.</param>
		/// <param name="AlignmentScore">The line alignment score.</param>
		/// <param name="InterlineScore">The interline spacing score.</param>
		/// <param name="BottomPlacementScore">The bottom-of-document placement score.</param>
		/// <param name="ParserScore">The parser/checksum score after OCR.</param>
		/// <param name="Metadata">Additional score metadata.</param>
		public MrzCandidateScore(
			float VisualScore,
			float GeometryScore,
			float WidthScore,
			float AlignmentScore,
			float InterlineScore,
			float BottomPlacementScore,
			float ParserScore = 0f,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			this.VisualScore = VisualScore;
			this.GeometryScore = GeometryScore;
			this.WidthScore = WidthScore;
			this.AlignmentScore = AlignmentScore;
			this.InterlineScore = InterlineScore;
			this.BottomPlacementScore = BottomPlacementScore;
			this.ParserScore = ParserScore;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the visual score before OCR.
		/// </summary>
		public float VisualScore { get; }

		/// <summary>
		/// Gets the template geometry score.
		/// </summary>
		public float GeometryScore { get; }

		/// <summary>
		/// Gets the standards-derived width score.
		/// </summary>
		public float WidthScore { get; }

		/// <summary>
		/// Gets the line alignment score.
		/// </summary>
		public float AlignmentScore { get; }

		/// <summary>
		/// Gets the interline spacing score.
		/// </summary>
		public float InterlineScore { get; }

		/// <summary>
		/// Gets the bottom-of-document placement score.
		/// </summary>
		public float BottomPlacementScore { get; }

		/// <summary>
		/// Gets the parser/checksum score after OCR.
		/// </summary>
		public float ParserScore { get; }

		/// <summary>
		/// Gets additional score metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
