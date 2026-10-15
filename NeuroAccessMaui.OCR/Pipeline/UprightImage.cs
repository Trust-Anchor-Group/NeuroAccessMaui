using IdApp.Cv;
using NeuroAccessMaui.OCR.Models;
using CvPoint = IdApp.Cv.Basic.Point;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Presents a grayscale image in its upright orientation without rotating its pixels.
	/// </summary>
	/// <remarks>
	/// Geometry is expressed in upright coordinates and mapped to the stored image only where pixels are sampled, which
	/// avoids copying a full camera frame for every analyzed frame. Coordinates use the pixel-area convention: pixel
	/// <c>(x, y)</c> covers <c>[x, x + 1) × [y, y + 1)</c>.
	/// </remarks>
	internal sealed class UprightImage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UprightImage"/> class.
		/// </summary>
		/// <param name="Source">The image as stored, for example as delivered by the camera sensor.</param>
		/// <param name="RotationDegrees">The clockwise rotation, a multiple of 90 degrees, that makes the image upright.</param>
		public UprightImage(Matrix<byte> Source, int RotationDegrees)
		{
			ArgumentNullException.ThrowIfNull(Source);

			int NormalizedDegrees = ((RotationDegrees % 360) + 360) % 360;
			this.Source = Source;
			this.RotationDegrees = NormalizedDegrees switch
			{
				< 45 or >= 315 => 0,
				< 135 => 90,
				< 225 => 180,
				_ => 270
			};
		}

		/// <summary>
		/// Gets the image as stored.
		/// </summary>
		public Matrix<byte> Source { get; }

		/// <summary>
		/// Gets the clockwise rotation, 0, 90, 180 or 270 degrees, that makes the stored image upright.
		/// </summary>
		public int RotationDegrees { get; }

		/// <summary>
		/// Gets the upright width.
		/// </summary>
		public int Width => this.RotationDegrees is 90 or 270 ? this.Source.Height : this.Source.Width;

		/// <summary>
		/// Gets the upright height.
		/// </summary>
		public int Height => this.RotationDegrees is 90 or 270 ? this.Source.Width : this.Source.Height;

		/// <summary>
		/// Maps a quadrilateral from upright coordinates to coordinates in the stored image, keeping its corner roles.
		/// </summary>
		/// <param name="Quad">The quadrilateral in upright coordinates.</param>
		/// <returns>The quadrilateral in stored image coordinates.</returns>
		public DocumentQuad ToSource(DocumentQuad Quad)
		{
			ArgumentNullException.ThrowIfNull(Quad);

			return new DocumentQuad(
				this.ToSource(Quad.TopLeft),
				this.ToSource(Quad.TopRight),
				this.ToSource(Quad.BottomRight),
				this.ToSource(Quad.BottomLeft));
		}

		private CvPoint ToSource(CvPoint Point)
		{
			int SourceWidth = this.Source.Width;
			int SourceHeight = this.Source.Height;
			return this.RotationDegrees switch
			{
				90 => new CvPoint(Point.Y, SourceHeight - Point.X),
				180 => new CvPoint(SourceWidth - Point.X, SourceHeight - Point.Y),
				270 => new CvPoint(SourceWidth - Point.Y, Point.X),
				_ => Point
			};
		}
	}
}
