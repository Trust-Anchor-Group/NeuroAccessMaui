using System;

namespace IdApp.Cv.Basic
{
	/// <summary>
	/// Static class for Basic Operations, implemented as extensions.
	/// </summary>
	public static partial class BasicOperations
	{
		/// <summary>
		/// Makes a true copy of the matrix.
		/// </summary>
		/// <typeparam name="T">Pixel type.</typeparam>
		/// <param name="M">Matrix of pixels</param>
		public static Matrix<T> Copy<T>(this Matrix<T> M)
			where T : struct
		{
			int y, h = M.Height;
			int w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcDy = M.RowSize;
			T[] Src = M.Data;
			T[] Dest = new T[w * h];

			for (y = 0; y < h; y++, SrcOffset += SrcDy, DestOffset += w)
				Array.Copy(Src, SrcOffset, Dest, DestOffset, w);

			return new Matrix<T>(w, h, Dest);
		}
	}
}
