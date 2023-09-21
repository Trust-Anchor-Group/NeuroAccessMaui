using IdApp.Cv.Basic;

namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Blurs an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Blurred image.</returns>
		public static Matrix<float> Blur(this Matrix<float> M)
		{
			return M.Convolute(blurKernel_3x3);
		}

		/// <summary>
		/// Blurs an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Blurred image.</returns>
		public static Matrix<int> Blur(this Matrix<int> M)
		{
			return M.Convolute(blurKernel_3x3);
		}

		/// <summary>
		/// Blurs an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Blurred image.</returns>
		public static IMatrix Blur(this IMatrix M)
		{
			return M.Convolute(blurKernel_3x3);
		}

		private static readonly Matrix<int> blurKernel_3x3 = new Matrix<int>(3, 3, new int[]
		{
			 1,  1,  1,
			 1,  1,  1,
			 1,  1,  1
		});

		/// <summary>
		/// Blurs an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="KernelWidth">Width of kernel.</param>
		/// <returns>Blurred image.</returns>
		public static IMatrix Blur(this IMatrix M, int KernelWidth)
		{
			Matrix<int> Kernel = new Matrix<int>(KernelWidth, KernelWidth);
			Kernel.Fill(1);
			return M.Convolute(Kernel);
		}
	}
}
