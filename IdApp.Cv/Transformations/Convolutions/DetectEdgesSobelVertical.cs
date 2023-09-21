namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects vertical edges in an image using the Sobel Vertical Edge Operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where vertical edges are highlighted.</returns>
		public static Matrix<float> DetectEdgesSobelVertical(this Matrix<float> M)
		{
			return M.Convolute(detectVerticalEdgesSobelKernel);
		}

		/// <summary>
		/// Detects vertical edges in an image using the Sobel Vertical Edge Operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where vertical edges are highlighted.</returns>
		public static Matrix<int> DetectEdgesSobelVertical(this Matrix<int> M)
		{
			return M.Convolute(detectVerticalEdgesSobelKernel);
		}

		/// <summary>
		/// Detects vertical edges in an image using the Sobel Vertical Edge Operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where vertical edges are highlighted.</returns>
		public static IMatrix DetectEdgesSobelVertical(this IMatrix M)
		{
			return M.Convolute(detectVerticalEdgesSobelKernel);
		}

		private static readonly Matrix<int> detectVerticalEdgesSobelKernel = new Matrix<int>(3, 3, new int[]
		{
			-1,  0,  1,
			-2,  0,  2,
			-1,  0,  1
		});
	}
}
