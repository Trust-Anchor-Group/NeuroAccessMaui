namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Sharpens an image. (Subtracts the negative Laplacian).
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Sharpened image.</returns>
		public static Matrix<float> Sharpen(this Matrix<float> M)
		{
			return M.Convolute(sharpenKernel);
		}

		/// <summary>
		/// Sharpens an image. (Subtracts the negative Laplacian).
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Sharpened image.</returns>
		public static Matrix<int> Sharpen(this Matrix<int> M)
		{
			return M.Convolute(sharpenKernel);
		}

		/// <summary>
		/// Sharpens an image. (Subtracts the negative Laplacian).
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Sharpened image.</returns>
		public static IMatrix Sharpen(this IMatrix M)
		{
			return M.Convolute(sharpenKernel);
		}

		private static readonly Matrix<int> sharpenKernel = new Matrix<int>(3, 3, new int[]
		{
			 0, -1,  0,
			-1,  5, -1,
			 0, -1,  0
		});
	}
}
