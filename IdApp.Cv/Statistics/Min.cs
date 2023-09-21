namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Statistical Operations, implemented as extensions.
	/// </summary>
	public static partial class StatisticsOperations
	{
		/// <summary>
		/// Computes the smallest value in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static float Min(this Matrix<float> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;
			float Result = Data[Index];

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index++];
					if (v < Result)
						Result = v;
				}
			}

			return Result;
		}

		/// <summary>
		/// Computes the smallest value in the matrix, from only the pixels where the 
		/// corresponding element in the <paramref name="Kernel"/> is true.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel matrix.</param>
		public static float Min(this Matrix<float> M, Matrix<bool> Kernel)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int KIndex = Kernel.Start;
			int Skip = M.Skip;
			int KSkip = Kernel.Skip;
			float[] Data = M.Data;
			bool[] KData = Kernel.Data;
			float v;
			float Result = float.MaxValue;

			for (y = 0; y < h; y++, Index += Skip, KIndex += KSkip)
			{
				for (x = 0; x < w; x++)
				{
					if (KData[KIndex++])
					{
						v = Data[Index++];
						if (v < Result)
							Result = v;
					}
					else
						Index++;
				}
			}

			return Result;
		}

		/// <summary>
		/// Computes the smallest value in the matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static int Min(this Matrix<int> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;
			int v;
			int Result = Data[Index];

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index++];
					if (v < Result)
						Result = v;
				}
			}

			return Result;
		}

		/// <summary>
		/// Computes the smallest value in the matrix, from only the pixels where the 
		/// corresponding element in the <paramref name="Kernel"/> is true.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel matrix.</param>
		public static int Min(this Matrix<int> M, Matrix<bool> Kernel)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int KIndex = Kernel.Start;
			int Skip = M.Skip;
			int KSkip = Kernel.Skip;
			int[] Data = M.Data;
			bool[] KData = Kernel.Data;
			int v;
			int Result = int.MaxValue;

			for (y = 0; y < h; y++, Index += Skip, KIndex += KSkip)
			{
				for (x = 0; x < w; x++)
				{
					if (KData[KIndex++])
					{
						v = Data[Index++];
						if (v < Result)
							Result = v;
					}
					else
						Index++;
				}
			}

			return Result;
		}
	}
}
