using System.Collections.Generic;
using IdApp.Cv;
using CvRect = IdApp.Cv.Basic.Rect;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents one OCR-ready MRZ line crop.
	/// </summary>
	public sealed class MrzLineCrop
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzLineCrop"/> class.
		/// </summary>
		/// <param name="LineIndex">The zero-based line index.</param>
		/// <param name="Image">The OCR-ready line image.</param>
		/// <param name="Bounds">The crop bounds in rectified document coordinates.</param>
		/// <param name="Metadata">Additional crop metadata.</param>
		public MrzLineCrop(
			int LineIndex,
			Matrix<float> Image,
			CvRect Bounds,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Image);

			this.LineIndex = LineIndex;
			this.Image = Image;
			this.Bounds = Bounds;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the zero-based line index.
		/// </summary>
		public int LineIndex { get; }

		/// <summary>
		/// Gets the OCR-ready line image.
		/// </summary>
		public Matrix<float> Image { get; }

		/// <summary>
		/// Gets the crop bounds in rectified document coordinates.
		/// </summary>
		public CvRect Bounds { get; }

		/// <summary>
		/// Gets additional crop metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
