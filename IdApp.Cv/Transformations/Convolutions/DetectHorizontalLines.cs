namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects horizontal lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal lines are highlighted.</returns>
		public static Matrix<float> DetectHorizontalLines(this Matrix<float> M)
		{
			return M.Convolute(detectHorizontalLinesKernel);
		}

		/// <summary>
		/// Detects horizontal lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal lines are highlighted.</returns>
		public static Matrix<int> DetectHorizontalLines(this Matrix<int> M)
		{
			return M.Convolute(detectHorizontalLinesKernel);
		}

		/// <summary>
		/// Detects horizontal lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where horizontal lines are highlighted.</returns>
		public static IMatrix DetectHorizontalLines(this IMatrix M)
		{
			return M.Convolute(detectHorizontalLinesKernel);
		}

		private static readonly Matrix<int> detectHorizontalLinesKernel = new Matrix<int>(3, 3, new int[]
		{
			-1, -1, -1,
			 2,  2,  2,
			-1, -1, -1
		});
	}
}
