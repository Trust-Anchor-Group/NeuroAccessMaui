using System.Collections.Generic;
using CvRect = IdApp.Cv.Basic.Rect;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a complete MRZ region candidate in rectified document coordinates.
	/// </summary>
	public sealed class MrzRegionCandidate
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzRegionCandidate"/> class.
		/// </summary>
		/// <param name="Name">The stable candidate name.</param>
		/// <param name="Format">The MRZ document format hypothesis.</param>
		/// <param name="Bounds">The region bounds in rectified document coordinates.</param>
		/// <param name="Lines">The detected line candidates.</param>
		/// <param name="Score">The candidate score.</param>
		/// <param name="Metadata">Additional candidate metadata.</param>
		public MrzRegionCandidate(
			string Name,
			MrzDocumentFormat Format,
			CvRect Bounds,
			IReadOnlyList<MrzLineCandidate> Lines,
			MrzCandidateScore Score,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(Name);
			ArgumentNullException.ThrowIfNull(Lines);
			ArgumentNullException.ThrowIfNull(Score);

			this.Name = Name;
			this.Format = Format;
			this.Bounds = Bounds;
			this.Lines = Lines;
			this.Score = Score;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the stable candidate name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the MRZ document format hypothesis.
		/// </summary>
		public MrzDocumentFormat Format { get; }

		/// <summary>
		/// Gets the region bounds in rectified document coordinates.
		/// </summary>
		public CvRect Bounds { get; }

		/// <summary>
		/// Gets the detected line candidates.
		/// </summary>
		public IReadOnlyList<MrzLineCandidate> Lines { get; }

		/// <summary>
		/// Gets the candidate score.
		/// </summary>
		public MrzCandidateScore Score { get; }

		/// <summary>
		/// Gets additional candidate metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
