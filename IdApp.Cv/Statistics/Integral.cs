namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Statistical Operations, implemented as extensions.
	/// </summary>
	public static partial class StatisticsOperations
	{
		/// <summary>
		/// Computes the integral image of a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> Integral(this Matrix<float> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;
			int DestIndex = 0;
			float[] SrcData = M.Data;
			float[] Dest = new float[w * h];
			float RowSum;

			Dest[DestIndex++] = SrcData[SrcIndex++];
			for (x = 1; x < w; x++, DestIndex++)
				Dest[DestIndex] = Dest[DestIndex - 1] + SrcData[SrcIndex++];

			SrcIndex += SrcSkip;

			for (y = 1; y < h; y++, SrcIndex += SrcSkip)
			{
				RowSum = SrcData[SrcIndex++];
				Dest[DestIndex] = Dest[DestIndex - w] + RowSum;
				DestIndex++;

				for (x = 1; x < w; x++, DestIndex++)
				{
					RowSum += SrcData[SrcIndex++];
					Dest[DestIndex] = Dest[DestIndex - w] + RowSum;
				}
			}

			return new Matrix<float>(w, h, Dest);
		}

		/// <summary>
		/// Computes the integral image of a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<long> Integral(this Matrix<int> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;
			int DestIndex = 0;
			int[] SrcData = M.Data;
			long[] Dest = new long[w * h];
			long RowSum;

			Dest[DestIndex++] = SrcData[SrcIndex++];
			for (x = 1; x < w; x++, DestIndex++)
				Dest[DestIndex] = Dest[DestIndex - 1] + SrcData[SrcIndex++];

			SrcIndex += SrcSkip;

			for (y = 1; y < h; y++, SrcIndex += SrcSkip)
			{
				RowSum = SrcData[SrcIndex++];
				Dest[DestIndex] = Dest[DestIndex - w] + RowSum;
				DestIndex++;

				for (x = 1; x < w; x++, DestIndex++)
				{
					RowSum += SrcData[SrcIndex++];
					Dest[DestIndex] = Dest[DestIndex - w] + RowSum;
				}
			}

			return new Matrix<long>(w, h, Dest);
		}
	}
}
