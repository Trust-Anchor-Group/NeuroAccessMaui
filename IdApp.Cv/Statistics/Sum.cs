namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Statistical Operations, implemented as extensions.
	/// </summary>
	public static partial class StatisticsOperations
	{
		/// <summary>
		/// Computes the sum of all values in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static float Sum(this Matrix<float> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float Sum = 0;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
					Sum += Data[Index++];
			}

			return Sum;
		}

		/// <summary>
		/// Computes the sum of all values in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static long Sum(this Matrix<int> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;
			long Sum = 0;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
					Sum += Data[Index++];
			}

			return Sum;
		}
	}
}
