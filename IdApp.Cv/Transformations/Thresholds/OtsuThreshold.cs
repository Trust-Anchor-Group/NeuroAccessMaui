using IdApp.Cv.Statistics;

namespace IdApp.Cv.Transformations.Thresholds
{
	/// <summary>
	/// Static class for Transformation Operations, implemented as extensions.
	/// </summary>
	public static partial class TransformationOperations
	{
		/// <summary>
		/// Calculates the threshold level using Otsu's method.
		/// 
		/// Reference:
		/// https://en.wikipedia.org/wiki/Otsu%27s_method
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Threshold level.</returns>
		public static float OtsuThreshold(this Matrix<float> M)
		{
			return M.OtsuThreshold(256);
		}

		/// <summary>
		/// Calculates the threshold level using Otsu's method.
		/// 
		/// Reference:
		/// https://en.wikipedia.org/wiki/Otsu%27s_method
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		/// <returns>Threshold level.</returns>
		public static float OtsuThreshold(this Matrix<float> M, int Buckets)
		{
			return M.OtsuThreshold(Buckets, 0, 1f);
		}

		/// <summary>
		/// Calculates the threshold level using Otsu's method.
		/// 
		/// Reference:
		/// https://en.wikipedia.org/wiki/Otsu%27s_method
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		/// <param name="Min">Smallest value to consider (default 0)</param>
		/// <param name="Max">Largest value to consider (default 1)</param>
		/// <returns>Threshold level.</returns>
		public static float OtsuThreshold(this Matrix<float> M, int Buckets, float Min, float Max)
		{
			int[] Histogram = M.Histogram(Buckets, Min, Max);
			int NrPixels = M.Width * M.Height;
			int i, j, c = Buckets;

			long SumB = 0;
			long WSumHistogram = 0;
			float t, mF, wF, wB = 0;
			float BestT = 0;
			float MaxT = 0;

			for (i = 1; i < c; i++)
				WSumHistogram += i * Histogram[i];

			for (i = 0; i < c; i++)
			{
				wF = NrPixels - wB;
				if (wB > 0 && wF > 0)
				{
					mF = (WSumHistogram - SumB) / wF;
					t = (SumB / wB) - mF;
					t = wB * wF * t * t;
					if (t >= MaxT)
					{
						BestT = i;
						MaxT = t;
					}
				}

				j = Histogram[i];
				wB += j;
				SumB += i * j;
			}

			return BestT * (Max - Min) / Buckets + Min;
		}

		/// <summary>
		/// Calculates the threshold level using Otsu's method.
		/// 
		/// Reference:
		/// https://en.wikipedia.org/wiki/Otsu%27s_method
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>Threshold level.</returns>
		public static int OtsuThreshold(this Matrix<int> M)
		{
			return M.OtsuThreshold(256);
		}

		/// <summary>
		/// Calculates the threshold level using Otsu's method.
		/// 
		/// Reference:
		/// https://en.wikipedia.org/wiki/Otsu%27s_method
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		/// <returns>Threshold level.</returns>
		public static int OtsuThreshold(this Matrix<int> M, int Buckets)
		{
			return M.OtsuThreshold(Buckets, 0, 0x01000000);
		}

		/// <summary>
		/// Calculates the threshold level using Otsu's method.
		/// 
		/// Reference:
		/// https://en.wikipedia.org/wiki/Otsu%27s_method
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Buckets">Number of buckets in histogram.</param>
		/// <param name="Min">Smallest value to consider (default 0)</param>
		/// <param name="Max">Largest value to consider (default 0x01000000)</param>
		/// <returns>Threshold level.</returns>
		public static int OtsuThreshold(this Matrix<int> M, int Buckets, int Min, int Max)
		{
			int[] Histogram = M.Histogram(Buckets, Min, Max);
			int NrPixels = M.Width * M.Height;
			int i, j, c = Buckets;

			long SumB = 0;
			long WSumHistogram = 0;
			float t, mF, wF, wB = 0;
			float BestT = 0;
			float MaxT = 0;

			for (i = 1; i < c; i++)
				WSumHistogram += i * Histogram[i];

			for (i = 0; i < c; i++)
			{
				wF = NrPixels - wB;
				if (wB > 0 && wF > 0)
				{
					mF = (WSumHistogram - SumB) / wF;
					t = (SumB / wB) - mF;
					t = wB * wF * t * t;
					if (t >= MaxT)
					{
						BestT = i;
						MaxT = t;
					}
				}

				j = Histogram[i];
				wB += j;
				SumB += i * j;
			}

			return (int)(BestT * (Max - Min) / Buckets + Min + 0.5);
		}
	}
}
