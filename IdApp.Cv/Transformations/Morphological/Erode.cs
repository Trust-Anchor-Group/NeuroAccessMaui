using IdApp.Cv.Basic;
using IdApp.Cv.Statistics;
using System;

namespace IdApp.Cv.Transformations.Morphological
{
	/// <summary>
	/// Static class for Morphological Operations, implemented as extensions.
	/// </summary>
	public static partial class MorphologicalOperations
	{
		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<float> Erode(this Matrix<float> M)
		{
			return M.Erode(3, 3, 0f);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<float> Erode(this Matrix<float> M, int NeighborhoodWidth)
		{
			return M.Erode(NeighborhoodWidth, NeighborhoodWidth, 0f);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<float> Erode(this Matrix<float> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			return M.Erode(NeighborhoodWidth, NeighborhoodHeight, 0f);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold.</param>
		public static Matrix<float> Erode(this Matrix<float> M, int NeighborhoodWidth,
			int NeighborhoodHeight, float MinThreshold)
		{
			if (NeighborhoodWidth <= 0 || NeighborhoodWidth >= M.Width)
				throw new ArgumentOutOfRangeException(nameof(NeighborhoodWidth));

			if (NeighborhoodHeight <= 0 || NeighborhoodHeight >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(NeighborhoodHeight));

			int FromX = -NeighborhoodWidth / 2;
			int ToX = NeighborhoodWidth + FromX;
			int FromY = -NeighborhoodHeight / 2;
			int ToY = NeighborhoodHeight + FromY;
			int y, h = M.Height;
			int x, w = M.Width;
			int x1, y1, x2, y2;
			Matrix<float> Neighborhood = M.Region(0, 0, NeighborhoodWidth, NeighborhoodHeight);
			float[] Result = new float[w * h];
			float[] Src = M.Data;
			int Index = 0;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				y1 = y + FromY;
				if (y1 < 0)
					y1 = 0;

				y2 = y + ToY;
				if (y2 >= h)
					y2 = h - 1;

				Neighborhood.SetYSpan(y1 + M.Top, y2 - y1 + 1);

				for (x = 0; x < w; x++, SrcIndex++)
				{
					if (Src[SrcIndex] <= MinThreshold)
						Result[Index++] = MinThreshold;
					else
					{
						x1 = x + FromX;
						if (x1 < 0)
							x1 = 0;

						x2 = x + ToX;
						if (x2 >= w)
							x2 = w - 1;

						Neighborhood.SetXSpan(x1 + M.Left, x2 - x1 + 1);

						Result[Index++] = Neighborhood.Min();
					}
				}
			}

			return new Matrix<float>(w, h, Result);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of pixels in a
		/// neighborhood of the pixel determined by the <paramref name="Kernel"/>.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<float> Erode(this Matrix<float> M, Shape Kernel)
		{
			return M.Erode(Kernel, 0f);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of pixels in a
		/// neighborhood of the pixel determined by the <paramref name="Kernel"/>.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		/// <param name="MinThreshold">Minimum threshold.</param>
		public static Matrix<float> Erode(this Matrix<float> M, Shape Kernel, float MinThreshold)
		{
			int KernelWidth = Kernel.Width;
			int KernelHeight = Kernel.Height;

			if (KernelWidth >= M.Width)
				throw new ArgumentOutOfRangeException(nameof(Kernel), "Kernel too wide for iamge.");

			if (KernelHeight >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(Kernel), "Kernel too high for iamge.");

			int FromX = -Kernel.PixelX;
			int ToX = KernelWidth - Kernel.PixelX - 1;
			int FromY = -Kernel.PixelY;
			int ToY = KernelHeight - Kernel.PixelY - 1;
			int y, h = M.Height;
			int x, w = M.Width;
			int x1, y1, x2, y2, kx1, ky1, kx2, ky2;
			Matrix<float> Neighborhood = M.Region(0, 0, KernelWidth, KernelHeight);
			Matrix<bool> KernelNeighborhood = Kernel.ShallowCopy();
			float[] Result = new float[w * h];
			float[] Src = M.Data;
			int Index = 0;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				y1 = y + FromY;
				if (y1 < 0)
				{
					ky1 = -y1;
					y1 = 0;
				}
				else
					ky1 = 0;

				y2 = y + ToY;
				if (y2 >= h)
				{
					ky2 = KernelHeight - (y2 - h) - 1;
					y2 = h - 1;
				}
				else
					ky2 = KernelHeight;

				Neighborhood.SetYSpan(y1 + M.Top, y2 - y1 + 1);
				KernelNeighborhood.SetYSpan(ky1 + Kernel.Top, ky2 - ky1 + 1);

				for (x = 0; x < w; x++, SrcIndex++)
				{
					if (Src[SrcIndex] <= MinThreshold)
						Result[Index++] = MinThreshold;
					else
					{
						x1 = x + FromX;
						if (x1 < 0)
						{
							kx1 = -x1;
							x1 = 0;
						}
						else
							kx1 = 0;

						x2 = x + ToX;
						if (x2 >= w)
						{
							kx2 = KernelWidth - (x2 - w) - 1;
							x2 = w - 1;
						}
						else
							kx2 = KernelWidth;

						Neighborhood.SetXSpan(x1 + M.Left, x2 - x1 + 1);
						KernelNeighborhood.SetXSpan(kx1 + Kernel.Left, kx2 - kx1 + 1);

						Result[Index++] = Neighborhood.Min(KernelNeighborhood);
					}
				}
			}

			return new Matrix<float>(w, h, Result);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static Matrix<int> Erode(this Matrix<int> M)
		{
			return M.Erode(3, 3, 0);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		public static Matrix<int> Erode(this Matrix<int> M, int NeighborhoodWidth)
		{
			return M.Erode(NeighborhoodWidth, NeighborhoodWidth, 0);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		public static Matrix<int> Erode(this Matrix<int> M, int NeighborhoodWidth,
			int NeighborhoodHeight)
		{
			return M.Erode(NeighborhoodWidth, NeighborhoodHeight, 0);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of a square 
		/// neighborhood of the pixel.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="NeighborhoodWidth">Width of neighborhood (default=3).</param>
		/// <param name="NeighborhoodHeight">Height of neighborhood (default=3).</param>
		/// <param name="MinThreshold">Minimum threshold.</param>
		public static Matrix<int> Erode(this Matrix<int> M, int NeighborhoodWidth,
			int NeighborhoodHeight, int MinThreshold)
		{
			if (NeighborhoodWidth <= 0 || NeighborhoodWidth >= M.Width)
				throw new ArgumentOutOfRangeException(nameof(NeighborhoodWidth));

			if (NeighborhoodHeight <= 0 || NeighborhoodHeight >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(NeighborhoodHeight));

			int FromX = -NeighborhoodWidth / 2;
			int ToX = NeighborhoodWidth + FromX;
			int FromY = -NeighborhoodHeight / 2;
			int ToY = NeighborhoodHeight + FromY;
			int y, h = M.Height;
			int x, w = M.Width;
			int x1, y1, x2, y2;
			Matrix<int> Neighborhood = M.Region(0, 0, NeighborhoodWidth, NeighborhoodHeight);
			int[] Result = new int[w * h];
			int[] Src = M.Data;
			int Index = 0;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				y1 = y + FromY;
				if (y1 < 0)
					y1 = 0;

				y2 = y + ToY;
				if (y2 >= h)
					y2 = h - 1;

				Neighborhood.SetYSpan(y1 + M.Top, y2 - y1 + 1);

				for (x = 0; x < w; x++, SrcIndex++)
				{
					if (Src[SrcIndex] <= MinThreshold)
						Result[Index++] = MinThreshold;
					else
					{
						x1 = x + FromX;
						if (x1 < 0)
							x1 = 0;

						x2 = x + ToX;
						if (x2 >= w)
							x2 = w - 1;

						Neighborhood.SetXSpan(x1 + M.Left, x2 - x1 + 1);

						Result[Index++] = Neighborhood.Min();
					}
				}
			}

			return new Matrix<int>(w, h, Result);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of pixels in a
		/// neighborhood of the pixel determined by the <paramref name="Kernel"/>.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		public static Matrix<int> Erode(this Matrix<int> M, Shape Kernel)
		{
			return M.Erode(Kernel, 0);
		}

		/// <summary>
		/// Erodes an image by replacing a pixel with the minimum value of pixels in a
		/// neighborhood of the pixel determined by the <paramref name="Kernel"/>.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Kernel">Kernel of morphological operation.</param>
		/// <param name="MinThreshold">Minimum threshold.</param>
		public static Matrix<int> Erode(this Matrix<int> M, Shape Kernel, int MinThreshold)
		{
			int KernelWidth = Kernel.Width;
			int KernelHeight = Kernel.Height;

			if (KernelWidth >= M.Width)
				throw new ArgumentOutOfRangeException(nameof(Kernel), "Kernel too wide for iamge.");

			if (KernelHeight >= M.Height)
				throw new ArgumentOutOfRangeException(nameof(Kernel), "Kernel too high for iamge.");

			int FromX = -Kernel.PixelX;
			int ToX = KernelWidth - Kernel.PixelX - 1;
			int FromY = -Kernel.PixelY;
			int ToY = KernelHeight - Kernel.PixelY - 1;
			int y, h = M.Height;
			int x, w = M.Width;
			int x1, y1, x2, y2, kx1, ky1, kx2, ky2;
			Matrix<int> Neighborhood = M.Region(0, 0, KernelWidth, KernelHeight);
			Matrix<bool> KernelNeighborhood = Kernel.ShallowCopy();
			int[] Result = new int[w * h];
			int[] Src = M.Data;
			int Index = 0;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				y1 = y + FromY;
				if (y1 < 0)
				{
					ky1 = -y1;
					y1 = 0;
				}
				else
					ky1 = 0;

				y2 = y + ToY;
				if (y2 >= h)
				{
					ky2 = KernelHeight - (y2 - h) - 1;
					y2 = h - 1;
				}
				else
					ky2 = KernelHeight;

				Neighborhood.SetYSpan(y1 + M.Top, y2 - y1 + 1);
				KernelNeighborhood.SetYSpan(ky1 + Kernel.Top, ky2 - ky1 + 1);

				for (x = 0; x < w; x++, SrcIndex++)
				{
					if (Src[SrcIndex] <= MinThreshold)
						Result[Index++] = MinThreshold;
					else
					{
						x1 = x + FromX;
						if (x1 < 0)
						{
							kx1 = -x1;
							x1 = 0;
						}
						else
							kx1 = 0;

						x2 = x + ToX;
						if (x2 >= w)
						{
							kx2 = KernelWidth - (x2 - w) - 1;
							x2 = w - 1;
						}
						else
							kx2 = KernelWidth;

						Neighborhood.SetXSpan(x1 + M.Left, x2 - x1 + 1);
						KernelNeighborhood.SetXSpan(kx1 + Kernel.Left, kx2 - kx1 + 1);

						Result[Index++] = Neighborhood.Min(KernelNeighborhood);
					}
				}
			}

			return new Matrix<int>(w, h, Result);
		}

	}
}
