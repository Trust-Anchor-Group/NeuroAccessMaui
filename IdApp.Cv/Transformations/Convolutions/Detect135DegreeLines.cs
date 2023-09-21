namespace IdApp.Cv.Transformations.Convolutions
{
	/// <summary>
	/// Static class for Convolution Operations, implemented as extensions.
	/// </summary>
	public static partial class ConvolutionOperations
	{
		/// <summary>
		/// Detects 135 degree lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where 135 degree lines are highlighted.</returns>
		public static Matrix<float> Detect135DegreeLines(this Matrix<float> M)
		{
			return M.Convolute(detect135DegreeLinesKernel);
		}

		/// <summary>
		/// Detects 135 degree lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where 135 degree lines are highlighted.</returns>
		public static Matrix<int> Detect135DegreeLines(this Matrix<int> M)
		{
			return M.Convolute(detect135DegreeLinesKernel);
		}

		/// <summary>
		/// Detects 135 degree lines in an image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Image where 135 degree lines are highlighted.</returns>
		public static IMatrix Detect135DegreeLines(this IMatrix M)
		{
			return M.Convolute(detect135DegreeLinesKernel);
		}

		private static readonly Matrix<int> detect135DegreeLinesKernel = new Matrix<int>(3, 3, new int[]
		{
			 2, -1, -1,
			-1,  2, -1,
			-1, -1,  2
		});
	}
}
