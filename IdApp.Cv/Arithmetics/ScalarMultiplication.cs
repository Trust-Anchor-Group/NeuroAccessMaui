namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Performs a scalar multiplication on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void ScalarMultiplication(this Matrix<float> M, float Scalar)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
					Data[Index++] *= Scalar;
			}
		}

		/// <summary>
		/// Performs a scalar multiplication on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void ScalarMultiplication(this Matrix<int> M, int Scalar)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
					Data[Index++] *= Scalar;
			}
		}
	}
}
