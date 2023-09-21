namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects edges in an image using the Laplacian operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where edges are highlighted.</returns>
		public static Matrix<float> DetectEdgesLaplacian(this Matrix<float> M)
		{
			return M.Convolute(detectEdgesKernelLaplacian);
		}

		/// <summary>
		/// Detects edges in an image using the Laplacian operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where edges are highlighted.</returns>
		public static Matrix<int> DetectEdgesLaplacian(this Matrix<int> M)
		{
			return M.Convolute(detectEdgesKernelLaplacian);
		}

		/// <summary>
		/// Detects edges in an image using the Laplacian operator.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where edges are highlighted.</returns>
		public static IMatrix DetectEdgesLaplacian(this IMatrix M)
		{
			return M.Convolute(detectEdgesKernelLaplacian);
		}

		private static readonly Matrix<int> detectEdgesKernelLaplacian = new Matrix<int>(3, 3, new int[]
		{
			 0,  1,  0,
			 1, -4,  1,
			 0,  1,  0
		});
	}
}
