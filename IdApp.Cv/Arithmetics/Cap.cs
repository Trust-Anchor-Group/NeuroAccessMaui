using System;

namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Caps the smallest and largest values in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Smallest value allowed.</param>
		/// <param name="Max">Largest value allowed.</param>
		public static void Cap(this Matrix<float> M, float Min, float Max)
		{
			if (Max < Min)
				throw new ArgumentOutOfRangeException(nameof(Max));

			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index];
					if (v < Min)
						Data[Index] = Min;
					else if (v > Max)
						Data[Index] = Max;

					Index++;
				}
			}
		}

		/// <summary>
		/// Caps the smallest and largest values in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Smallest value allowed.</param>
		/// <param name="Max">Largest value allowed.</param>
		public static void Cap(this Matrix<int> M, int Min, int Max)
		{
			if (Max < Min)
				throw new ArgumentOutOfRangeException(nameof(Max));

			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;
			int v;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index];
					if (v < Min)
						Data[Index] = Min;
					else if (v > Max)
						Data[Index] = Max;

					Index++;
				}
			}
		}
	}
}
