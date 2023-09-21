namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects horizontal edges in an image using the Sharr Horizontal Edge Operator.
		/// (Second order vertical derivative of image)
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static Matrix<float> DetectEdgesSharrHorizontal(this Matrix<float> M)
		{
			return M.Convolute(detectHorizontalEdgesSharrKernel);
		}

		/// <summary>
		/// Detects horizontal edges in an image using the Sharr Horizontal Edge Operator.
		/// (Second order vertical derivative of image)
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static Matrix<int> DetectEdgesSharrHorizontal(this Matrix<int> M)
		{
			return M.Convolute(detectHorizontalEdgesSharrKernel);
		}

		/// <summary>
		/// Detects horizontal edges in an image using the Sharr Horizontal Edge Operator.
		/// (Second order vertical derivative of image)
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal edges are highlighted.</returns>
		public static IMatrix DetectEdgesSharrHorizontal(this IMatrix M)
		{
			return M.Convolute(detectHorizontalEdgesSharrKernel);
		}

		private static readonly Matrix<int> detectHorizontalEdgesSharrKernel = new Matrix<int>(3, 3, new int[]
		{
			-3, -10, -3,
			 0,   0,  0,
			 3,  10,  3
		});
	}
}
