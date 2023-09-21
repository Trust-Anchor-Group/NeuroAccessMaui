using IdApp.Cv.Arithmetics;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> WhiteHat(this Matrix<float> M)
		{
			return M.WhiteHat(3, 3, 0f, 1f);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> WhiteHat(this Matrix<float> M, int NeighborhoodWidth)
		{
			return M.WhiteHat(NeighborhoodWidth, NeighborhoodWidth, 0f, 1f);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<float> WhiteHat(this Matrix<float> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			return M.WhiteHat(NeighborhoodWidth, NeighborhoodHeight, 0f, 1f);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> WhiteHat(this Matrix<float> M, int NeighborhoodWidth,
			int NeighborhoodHeight, float MinThreshold, float MaxThreshold)
		{
			Matrix<float> Result = M.Open(NeighborhoodWidth, NeighborhoodHeight, MinThreshold, MaxThreshold);
			Result.AbsoluteDifference(M);
			return Result;
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<int> WhiteHat(this Matrix<int> M)
		{
			return M.WhiteHat(3, 3, 0, 0x01000000);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<int> WhiteHat(this Matrix<int> M, int NeighborhoodWidth)
		{
			return M.WhiteHat(NeighborhoodWidth, NeighborhoodWidth, 0, 0x01000000);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<int> WhiteHat(this Matrix<int> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			return M.WhiteHat(NeighborhoodWidth, NeighborhoodHeight, 0, 0x01000000);
		}

		/// <summary>
		/// The absolute difference between an image, and its opening.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> WhiteHat(this Matrix<int> M, int NeighborhoodWidth,
			int NeighborhoodHeight, int MinThreshold, int MaxThreshold)
		{
			Matrix<int> Result = M.Open(NeighborhoodWidth, NeighborhoodHeight, MinThreshold, MaxThreshold);
			Result.AbsoluteDifference(M);
			return Result;
		}
	}
}
