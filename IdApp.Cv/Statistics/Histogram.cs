using System;

namespace IdApp.Cv.Statistics
{
	/// <summary>
	/// Static class for Statistical Operations, implemented as extensions.
	/// </summary>
	public static partial class StatisticsOperations
	{
		/// <summary>
		/// Computes a histogram from values in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		public static int[] Histogram(this Matrix<float> M, int Buckets)
		{
			return Histogram(M, Buckets, 0, 1);
		}

		/// <summary>
		/// Computes a histogram from values in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		/// <param name="Min">Smallest value to consider (default 0)</param>
		/// <param name="Max">Largest value to consider (default 1)</param>
		public static int[] Histogram(this Matrix<float> M, int Buckets, float Min, float Max)
		{
			if (Buckets <= 0)
				throw new ArgumentOutOfRangeException(nameof(Buckets));

			if (Max <= Min)
				throw new ArgumentOutOfRangeException(nameof(Max));

			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float v;
			float Scale = Buckets / (Max - Min);
			int i;
			int[] Result = new int[Buckets];

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index++];
					if (v >= Min && v <= Max)
					{
						i = (int)((v - Min) * Scale);
						if (i >= 0 && i < Buckets)
							Result[i]++;
						else if (i == Buckets)
							Result[Buckets - 1]++;
					}
				}
			}

			return Result;
		}

		/// <summary>
		/// Computes a histogram from values in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		public static int[] Histogram(this Matrix<int> M, int Buckets)
		{
			return Histogram(M, Buckets, 0, 0x01000000);
		}

		/// <summary>
		/// Computes a histogram from values in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		/// <param name="Min">Smallest value to consider (default 0)</param>
		/// <param name="Max">Largest value to consider (default 1)</param>
		public static int[] Histogram(this Matrix<int> M, int Buckets, int Min, int Max)
		{
			if (Buckets <= 0)
				throw new ArgumentOutOfRangeException(nameof(Buckets));

			if (Max <= Min)
				throw new ArgumentOutOfRangeException(nameof(Max));

			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;
			int v;
			int d = Max - Min;
			int i;
			int[] Result = new int[Buckets];

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					v = Data[Index++];
					if (v >= Min && v <= Max)
					{
						i = (int)((((long)(v - Min)) * Buckets) / d);
						if (i >= 0 && i < Buckets)
							Result[i]++;
						else if (i == Buckets)
							Result[Buckets - 1]++;
					}
				}
			}

			return Result;
		}
	}
}
