using CvPoint = IdApp.Cv.Basic.Point;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a document quadrilateral in source image coordinates.
	/// </summary>
	public sealed class DocumentQuad
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentQuad"/> class.
		/// </summary>
		/// <param name="TopLeft">The top-left corner.</param>
		/// <param name="TopRight">The top-right corner.</param>
		/// <param name="BottomRight">The bottom-right corner.</param>
		/// <param name="BottomLeft">The bottom-left corner.</param>
		public DocumentQuad(CvPoint TopLeft, CvPoint TopRight, CvPoint BottomRight, CvPoint BottomLeft)
		{
			this.TopLeft = TopLeft;
			this.TopRight = TopRight;
			this.BottomRight = BottomRight;
			this.BottomLeft = BottomLeft;
		}

		/// <summary>
		/// Gets the top-left corner.
		/// </summary>
		public CvPoint TopLeft { get; }

		/// <summary>
		/// Gets the top-right corner.
		/// </summary>
		public CvPoint TopRight { get; }

		/// <summary>
		/// Gets the bottom-right corner.
		/// </summary>
		public CvPoint BottomRight { get; }

		/// <summary>
		/// Gets the bottom-left corner.
		/// </summary>
		public CvPoint BottomLeft { get; }
	}
}
