using IdApp.Cv.Arithmetics;
using IdApp.Cv.Basic;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M)
		{
			return M.MorphologicalGradient(3, 3, 0f, 1f);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M, int NeighborhoodWidth)
		{
			return M.MorphologicalGradient(NeighborhoodWidth, NeighborhoodWidth, 0f, 1f);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M,
			int NeighborhoodWidth, int NeighborhoodHeight)
		{
			return M.MorphologicalGradient(NeighborhoodWidth, NeighborhoodHeight, 0f, 1f);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M, 
			int NeighborhoodWidth, int NeighborhoodHeight, float MinThreshold, float MaxThreshold)
		{
			Matrix<float> Result = M.Dilate(NeighborhoodWidth, NeighborhoodHeight, MaxThreshold);
			Result.AbsoluteDifference(M.Erode(NeighborhoodWidth, NeighborhoodHeight, MinThreshold));
			return Result;
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M, Shape Kernel)
		{
			return M.MorphologicalGradient(Kernel, 0f, 1f);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<float> MorphologicalGradient(this Matrix<float> M, Shape Kernel,
			float MinThreshold, float MaxThreshold)
		{
			Matrix<float> Result = M.Dilate(Kernel, MaxThreshold);
			Result.AbsoluteDifference(M.Erode(Kernel, MinThreshold));
			return Result;
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<int> MorphologicalGradient(this Matrix<int> M)
		{
			return M.MorphologicalGradient(3, 3, 0, 0x01000000);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<int> MorphologicalGradient(this Matrix<int> M, int NeighborhoodWidth)
		{
			return M.MorphologicalGradient(NeighborhoodWidth, NeighborhoodWidth, 0, 0x01000000);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<int> MorphologicalGradient(this Matrix<int> M,
			int NeighborhoodWidth, int NeighborhoodHeight)
		{
			return M.MorphologicalGradient(NeighborhoodWidth, NeighborhoodHeight, 0, 0x01000000);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> MorphologicalGradient(this Matrix<int> M,
			int NeighborhoodWidth, int NeighborhoodHeight, int MinThreshold, int MaxThreshold)
		{
			Matrix<int> Result = M.Dilate(NeighborhoodWidth, NeighborhoodHeight, MaxThreshold);
			Result.AbsoluteDifference(M.Erode(NeighborhoodWidth, NeighborhoodHeight, MinThreshold));
			return Result;
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<int> MorphologicalGradient(this Matrix<int> M, Shape Kernel)
		{
			return M.MorphologicalGradient(Kernel, 0, 0x01000000);
		}

		/// <summary>
		/// The absolute difference between the dilation and the erosion of an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		/// <param name="MinThreshold">Minimum threshold</param>
		/// <param name="MaxThreshold">Maximum threshold</param>
		public static Matrix<int> MorphologicalGradient(this Matrix<int> M, Shape Kernel, 
			int MinThreshold, int MaxThreshold)
		{
			Matrix<int> Result = M.Dilate(Kernel, MaxThreshold);
			Result.AbsoluteDifference(M.Erode(Kernel, MinThreshold));
			return Result;
		}

	}
}
