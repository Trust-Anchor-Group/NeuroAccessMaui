using System;

namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Computes the absolute difference of two matrices.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Matrix">Matrix to subtract.</param>
		public static void AbsoluteDifference(this Matrix<float> M, Matrix<float> Matrix)
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
			float v;

			for (y = 0; y < h; y++, DestOffset += DestSkip, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
				{
					v = Dest[DestOffset] - Src[SrcOffset++];
					Dest[DestOffset++] = v < 0 ? -v : v;
				}
			}
		}

		/// <summary>
		/// Computes the absolute difference of two matrices.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Matrix">Matrix to subtract.</param>
		public static void AbsoluteDifference(this Matrix<int> M, Matrix<int> Matrix)
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
			int v;

			for (y = 0; y < h; y++, DestOffset += DestSkip, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
				{
					v = Dest[DestOffset] - Src[SrcOffset++];
					Dest[DestOffset++] = v < 0 ? -v : v;
				}
			}
		}
	}
}
