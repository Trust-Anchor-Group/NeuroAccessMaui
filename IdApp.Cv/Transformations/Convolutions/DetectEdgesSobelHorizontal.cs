namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects horizontal edges in an image using the Sobel Horizontal Edge Operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static Matrix<float> DetectEdgesSobelHorizontal(this Matrix<float> M)
		{
			return M.Convolute(detectHorizontalEdgesSobelKernel);
		}

		/// <summary>
		/// Detects horizontal edges in an image using the Sobel Horizontal Edge Operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static Matrix<int> DetectEdgesSobelHorizontal(this Matrix<int> M)
		{
			return M.Convolute(detectHorizontalEdgesSobelKernel);
		}

		/// <summary>
		/// Detects horizontal edges in an image using the Sobel Horizontal Edge Operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static IMatrix DetectEdgesSobelHorizontal(this IMatrix M)
		{
			return M.Convolute(detectHorizontalEdgesSobelKernel);
		}

		private static readonly Matrix<int> detectHorizontalEdgesSobelKernel = new Matrix<int>(3, 3, new int[]
		{
			-1, -2, -1,
			 0,  0,  0,
			 1,  2,  1
		});
	}
}
