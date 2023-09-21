using IdApp.Cv.Statistics;
using System;

namespace IdApp.Cv.Transformations.Thresholds
{
	/// <summary>
	/// Static class for Transformation Operations, implemented as extensions.
	/// </summary>
	public static partial class TransformationOperations
	{
		/// <summary>
		/// Creates a black and white image based on the adaptive threshold levels provided.
		/// Each pixel is compared to the average pixel value in a square neighborhood of the
		/// pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">If the pixel is below thid threshold compared to the
		/// average pixel value in the square neighborhood of the pixel, it is colored black.
		/// Otherwise, the pixel is colored white.</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood.</param>
		public static void AdaptiveThreshold(this Matrix<float> M, float Threshold,
			int NeighborhoodWidth)
		{
			if (NeighborhoodWidth <= 0 || NeighborhoodWidth >= M.Width || NeighborhoodWidth >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(NeighborhoodWidth));

			Matrix<float> Integral = M.Integral();

			int From = -NeighborhoodWidth / 2;
			int To = NeighborhoodWidth + From;
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;
			float[] Sums = Integral.Data;
			float v, Avg;
			int x1, y1, x2, y2, dx, dy;

			for (y = 0; y < h; y++, Index += Skip)
			{
				y1 = y + From;
				if (y1 < 0)
					y1 = 0;

				y2 = y + To;
				if (y2 >= h)
					y2 = h - 1;

				dy = y2 - y1;

				for (x = 0; x < w; x++)
				{
					x1 = x + From;
					if (x1 < 0)
						x1 = 0;

					x2 = x + To;
					if (x2 >= w)
						x2 = w - 1;

					dx = x2 - x1;

					v = Data[Index];
					Avg = (Sums[x2 + y2 * w] - Sums[x1 + y2 * w] - Sums[x2 + y1 * w] + Sums[x1 + y1 * w]) / (dx * dy);

					Data[Index++] = v <= Avg - Threshold ? 0f : 1f;
				}
			}
		}

		/// <summary>
		/// Creates a black and white image based on the adaptive threshold levels provided.
		/// Each pixel is compared to the average pixel value in a square neighborhood of the
		/// pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Threshold">If the pixel is below thid threshold compared to the
		/// average pixel value in the square neighborhood of the pixel, it is colored black.
		/// Otherwise, the pixel is colored white.</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood.</param>
		public static void AdaptiveThreshold(this Matrix<int> M, int Threshold,
			int NeighborhoodWidth)
		{
			if (NeighborhoodWidth <= 0 || NeighborhoodWidth >= M.Width || NeighborhoodWidth >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(NeighborhoodWidth));

			Matrix<long> Integral = M.Integral();

			int From = -NeighborhoodWidth / 2;
			int To = NeighborhoodWidth + From;
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;
			long[] Sums = Integral.Data;
			int v, Avg;
			int x1, y1, x2, y2, dx, dy, dxdy;

			for (y = 0; y < h; y++, Index += Skip)
			{
				y1 = y + From;
				if (y1 < 0)
					y1 = 0;

				y2 = y + To;
				if (y2 >= h)
					y2 = h - 1;

				dy = y2 - y1;

				for (x = 0; x < w; x++)
				{
					x1 = x + From;
					if (x1 < 0)
						x1 = 0;

					x2 = x + To;
					if (x2 >= w)
						x2 = w - 1;

					dx = x2 - x1;
					dxdy = dx * dy;

					v = Data[Index];
					Avg = (int)((Sums[x2 + y2 * w] - Sums[x1 + y2 * w] - Sums[x2 + y1 * w] + Sums[x1 + y1 * w] + (dxdy >> 1)) / dxdy);

					Data[Index++] = v <= Avg - Threshold ? 0 : 0x01000000;
				}
			}
		}
	}
}
