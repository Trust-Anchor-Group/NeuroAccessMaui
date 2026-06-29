using IdApp.Cv;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a preview frame retained for stable commit capture.
	/// </summary>
	public sealed class MrzPreviewFrame
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzPreviewFrame"/> class.
		/// </summary>
		/// <param name="Image">The source image.</param>
		/// <param name="SourceRotationDegrees">Clockwise source-rotation degrees to normalize before scanning.</param>
		/// <param name="Timestamp">The frame timestamp.</param>
		public MrzPreviewFrame(IMatrix Image, int SourceRotationDegrees, DateTimeOffset Timestamp)
		{
			ArgumentNullException.ThrowIfNull(Image);

			this.Image = Image;
			this.SourceRotationDegrees = SourceRotationDegrees;
			this.Timestamp = Timestamp;
		}

		/// <summary>
		/// Gets the source image.
		/// </summary>
		public IMatrix Image { get; }

		/// <summary>
		/// Gets clockwise source-rotation degrees to normalize before scanning.
		/// </summary>
		public int SourceRotationDegrees { get; }

		/// <summary>
		/// Gets the frame timestamp.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
	}
}
