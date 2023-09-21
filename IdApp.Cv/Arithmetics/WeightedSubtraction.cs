namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Performs a weighted subtraction of a matrix to the current matrix, including a 
		/// scalar multiplication on each element in the matrix to be subtracted.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Matrix">Matrix to subtract.</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void WeightedSubtraction(this Matrix<float> M, Matrix<float> Matrix, float Scalar)
		{
			 M.WeightedAddition(Matrix, -Scalar);
		}

		/// <summary>
		/// Performs a weighted subtraction of a matrix to the current matrix, including a 
		/// scalar multiplication on each element in the matrix to be subtracted.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Matrix">Matrix to subtract.</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void WeightedSubtraction(this Matrix<int> M, Matrix<int> Matrix, int Scalar)
		{
			M.WeightedAddition(Matrix, -Scalar);
		}
	}
}
