using System;

namespace IdApp.Cv.Basic
{
	/// <summary>
	/// Static class for Basic Operations, implemented as extensions.
	/// </summary>
	public static partial class BasicOperations
	{
		/// <summary>
		/// Fills the matrix with a value.
		/// </summary>
		/// <typeparam name="T">Pixel type.</typeparam>
		/// <param name="M">Matrix of pixels</param>
		/// <param name="Value">Value to set.</param>
		public static void Fill<T>(this Matrix<T> M, T Value) 
			where T : struct
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			T[] Data = M.Data;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)			// TODO: For wide rows, this can be optimized using Array.Copy().
					Data[Index++] = Value;		// TODO: .NET Standard 2.1 also contains Array.Fill.
			}
		}
	}
}
