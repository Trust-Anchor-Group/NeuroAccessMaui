namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects vertical lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where vertical lines are highlighted.</returns>
		public static Matrix<float> DetectVerticalLines(this Matrix<float> M)
		{
			return M.Convolute(detectVerticalLinesKernel);
		}

		/// <summary>
		/// Detects vertical lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where vertical lines are highlighted.</returns>
		public static Matrix<int> DetectVerticalLines(this Matrix<int> M)
		{
			return M.Convolute(detectVerticalLinesKernel);
		}

		/// <summary>
		/// Detects vertical lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where vertical lines are highlighted.</returns>
		public static IMatrix DetectVerticalLines(this IMatrix M)
		{
			return M.Convolute(detectVerticalLinesKernel);
		}

		private static readonly Matrix<int> detectVerticalLinesKernel = new Matrix<int>(3, 3, new int[]
		{
			-1,  2, -1,
			-1,  2, -1,
			-1,  2, -1
		});
	}
}
