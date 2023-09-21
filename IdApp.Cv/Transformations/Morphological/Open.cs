using IdApp.Cv.Basic;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> Open(this Matrix<float> M)
		{
			return M.Open(3, 3, 0f, 1f);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> Open(this Matrix<float> M, int NeighborhoodWidth)
		{
			return M.Open(NeighborhoodWidth, NeighborhoodWidth, 0f, 1f);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<float> Open(this Matrix<float> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			return M.Open(NeighborhoodWidth, NeighborhoodHeight, 0f, 1f);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> Open(this Matrix<float> M, int NeighborhoodWidth,
			int NeighborhoodHeight, float MinThreshold, float MaxThreshold)
		{
			return M.
				Erode(NeighborhoodWidth, NeighborhoodHeight, MinThreshold).
				Dilate(NeighborhoodWidth, NeighborhoodHeight, MaxThreshold);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<float> Open(this Matrix<float> M, Shape Kernel)
		{
			return M.Open(Kernel, 0f, 1f);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> Open(this Matrix<float> M, Shape Kernel, float MinThreshold, 
			float MaxThreshold)
		{
			return M.
				Erode(Kernel, MinThreshold).
				Dilate(Kernel, MaxThreshold);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<int> Open(this Matrix<int> M)
		{
			return M.Open(3, 3, 0, 0x01000000);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<int> Open(this Matrix<int> M, int NeighborhoodWidth)
		{
			return M.Open(NeighborhoodWidth, NeighborhoodWidth, 0, 0x01000000);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<int> Open(this Matrix<int> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			return M.Open(NeighborhoodWidth, NeighborhoodHeight, 0, 0x01000000);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> Open(this Matrix<int> M, int NeighborhoodWidth,
			int NeighborhoodHeight, int MinThreshold, int MaxThreshold)
		{
			return M.
				Erode(NeighborhoodWidth, NeighborhoodHeight, MinThreshold).
				Dilate(NeighborhoodWidth, NeighborhoodHeight, MaxThreshold);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<int> Open(this Matrix<int> M, Shape Kernel)
		{
			return M.Open(Kernel, 0, 0x01000000);
		}

		/// <summary>
		/// Opens an image by first eroding it, and then dilating it.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> Open(this Matrix<int> M, Shape Kernel, int MinThreshold, 
			int MaxThreshold)
		{
			return M.
				Erode(Kernel, MinThreshold).
				Dilate(Kernel, MaxThreshold);
		}

	}
}
