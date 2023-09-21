using System;

namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Performs a scalar division on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scalar">Scalar Value to use in scalar division.</param>
		public static void ScalarDivision(this Matrix<float> M, float Scalar)
		{
			if (Scalar == 0)
				throw new ArgumentException("Division by zero.", nameof(Scalar));

			M.ScalarMultiplication(1.0f / Scalar);
		}

		/// <summary>
		/// Performs a scalar division on each element in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Scalar">Scalar Value to use in scalar division.</param>
		public static void ScalarDivision(this Matrix<int> M, int Scalar)
		{
			if (Scalar == 0)
				throw new ArgumentException("Division by zero.", nameof(Scalar));

			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
					Data[Index++] /= Scalar;
			}
		}
	}
}
