using System;

namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Performs a weighted addition of a matrix to the current matrix, including a 
		/// scalar multiplication on each element in the matrix to be added.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Matrix">Matrix to add.</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void WeightedAddition(this Matrix<float> M, Matrix<float> Matrix, float Scalar)
		{
			int y, h = M.Height;
			if (h != Matrix.Height)
				throw new ArgumentOutOfRangeException(nameof(Matrix), "Heights to do not match.");

			int x, w = M.Width;
			if (w != Matrix.Width)
				throw new ArgumentOutOfRangeException(nameof(Matrix), "Widths to do not match.");

			int DestOffset = M.Start;
			int SrcOffset = Matrix.Start;
			int DestSkip = M.Skip;
			int SrcSkip = Matrix.Skip;
			float[] Dest = M.Data;
			float[] Src = Matrix.Data;

			for (y = 0; y < h; y++, DestOffset += DestSkip, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] += Src[SrcOffset++] * Scalar;
			}
		}

		/// <summary>
		/// Performs a weighted addition of a matrix to the current matrix, including a 
		/// scalar multiplication on each element in the matrix to be added.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Matrix">Matrix to add.</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void WeightedAddition(this Matrix<float> M, Matrix<int> Matrix, float Scalar)
		{
			int y, h = M.Height;
			if (h != Matrix.Height)
				throw new ArgumentOutOfRangeException(nameof(Matrix), "Heights to do not match.");

			int x, w = M.Width;
			if (w != Matrix.Width)
				throw new ArgumentOutOfRangeException(nameof(Matrix), "Widths to do not match.");

			int DestOffset = M.Start;
			int SrcOffset = Matrix.Start;
			int DestSkip = M.Skip;
			int SrcSkip = Matrix.Skip;
			float[] Dest = M.Data;
			int[] Src = Matrix.Data;

			for (y = 0; y < h; y++, DestOffset += DestSkip, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] += Src[SrcOffset++] * Scalar;
			}
		}

		/// <summary>
		/// Performs a weighted addition of a matrix to the current matrix, including a 
		/// scalar multiplication on each element in the matrix to be added.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Matrix">Matrix to add.</param>
		/// <param name="Scalar">Scalar Value to use in scalar multiplication.</param>
		public static void WeightedAddition(this Matrix<int> M, Matrix<int> Matrix, int Scalar)
		{
			int y, h = M.Height;
			if (h != Matrix.Height)
				throw new ArgumentOutOfRangeException(nameof(Matrix), "Heights to do not match.");

			int x, w = M.Width;
			if (w != Matrix.Width)
				throw new ArgumentOutOfRangeException(nameof(Matrix), "Widths to do not match.");

			int DestOffset = M.Start;
			int SrcOffset = Matrix.Start;
			int DestSkip = M.Skip;
			int SrcSkip = Matrix.Skip;
			int[] Dest = M.Data;
			int[] Src = Matrix.Data;

			for (y = 0; y < h; y++, DestOffset += DestSkip, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] += Src[SrcOffset++] * Scalar;
			}
		}
	}
}
