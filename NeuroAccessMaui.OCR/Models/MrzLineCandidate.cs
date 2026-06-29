using System.Collections.Generic;
using CvRect = IdApp.Cv.Basic.Rect;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents one detected MRZ text line in rectified document coordinates.
	/// </summary>
	public sealed class MrzLineCandidate
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzLineCandidate"/> class.
		/// </summary>
		/// <param name="Bounds">The line bounds in rectified document coordinates.</param>
		/// <param name="Score">The line score.</param>
		/// <param name="BaselineY">The estimated baseline y-coordinate.</param>
		/// <param name="Metadata">Additional line metadata.</param>
		public MrzLineCandidate(
			CvRect Bounds,
			float Score,
			float BaselineY,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			this.Bounds = Bounds;
			this.Score = Score;
			this.BaselineY = BaselineY;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the line bounds in rectified document coordinates.
		/// </summary>
		public CvRect Bounds { get; }

		/// <summary>
		/// Gets the line score.
		/// </summary>
		public float Score { get; }

		/// <summary>
		/// Gets the estimated baseline y-coordinate.
		/// </summary>
		public float BaselineY { get; }

		/// <summary>
		/// Gets additional line metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
