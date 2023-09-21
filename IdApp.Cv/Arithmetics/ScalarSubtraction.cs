namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Performs a scalar subtraction on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scalar">Scalar Value to use in scalar subtraction.</param>
		public static void ScalarSubtraction(this Matrix<float> M, float Scalar)
		{
			M.ScalarAddition(-Scalar);
		}

		/// <summary>
		/// Performs a scalar subtraction on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scalar">Scalar Value to use in scalar subtraction.</param>
		public static void ScalarSubtraction(this Matrix<int> M, int Scalar)
		{
			M.ScalarAddition(-Scalar);
		}
	}
}
