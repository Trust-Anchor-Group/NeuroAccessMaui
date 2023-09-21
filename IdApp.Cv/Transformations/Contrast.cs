using IdApp.Cv.Arithmetics;
using IdApp.Cv.Statistics;

namespace IdApp.Cv.Transformations
{
	/// <summary>
	/// Static class for Transformation Operations, implemented as extensions.
	/// </summary>
	public static partial class TransformationOperations
	{
		/// <summary>
		/// Improves contrast by maximizing the range of visible values.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Contrast(this Matrix<float> M)
		{
			M.Range(out float Min, out float Max);
			M.Contrast(Min, Max);
		}

		/// <summary>
		/// Improves contrast by setting the range of visible values.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Smallest value after contrast transformation.</param>
		/// <param name="Max">Largest value after contrast transformation.</param>
		public static void Contrast(this Matrix<float> M, float Min, float Max)
		{
			M.ScalarLinearTransform(-Min, 1f / (Max - Min), 0);
		}

		/// <summary>
		/// Improves contrast by maximizing the range of visible values.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Contrast(this Matrix<int> M)
		{
			M.Range(out int Min, out int Max);
			M.Contrast(Min, Max);
		}

		/// <summary>
		/// Improves contrast by setting the range of visible values.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Smallest value after contrast transformation.</param>
		/// <param name="Max">Largest value after contrast transformation.</param>
		public static void Contrast(this Matrix<int> M, int Min, int Max)
		{
			M.ScalarLinearTransform(-Min, 0x01000000, Max - Min, 0);
		}
	}
}
