namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Makes each element of the matrix the absolute value of itself.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Abs(this Matrix<float> M)
		{
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
					if (v < 0)
						Data[Index] = -v;
					
					Index++;
				}
			}
		}

		/// <summary>
		/// Makes each element of the matrix the absolute value of itself.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Abs(this Matrix<int> M)
		{
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
					if (v < 0)
						Data[Index] = -v;

					Index++;
				}
			}
		}
	}
}
