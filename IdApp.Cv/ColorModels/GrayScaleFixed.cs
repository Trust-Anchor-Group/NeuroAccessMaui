using System;

namespace IdApp.Cv.ColorModels
{
	/// <summary>
	/// Static class for Channel Operations, implemented as extensions.
	/// </summary>
	public static partial class ColorModelOperations
	{
		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of colored pixels.</param>
		/// <returns>Matrix of gray scale pixels.</returns>
		public static Matrix<int> GrayScaleFixed(this Matrix<uint> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			uint[] Src = M.Data;
			int[] Dest = new int[w * h];
			uint ui;
			int f;

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
				{
					ui = Src[SrcOffset++];

					f = 19672 * (int)(ui & 255);
					ui >>= 8;
					f += 38620 * (int)(ui & 255);
					ui >>= 8;
					f += 7500 * (int)(ui & 255);

					Dest[DestOffset++] = f;
				}
			}

			return new Matrix<int>(w, h, Dest);
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of byte-values gray scale pixels.</param>
		/// <returns>Matrix of floating point gray scale pixels.</returns>
		public static Matrix<int> GrayScaleFixed(this Matrix<byte> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			byte[] Src = M.Data;
			int[] Dest = new int[w * h];

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] = 65793 * Src[SrcOffset++];
			}

			return new Matrix<int>(w, h, Dest);
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of byte-values gray scale pixels.</param>
		/// <returns>Matrix of floating point gray scale pixels.</returns>
		public static Matrix<int> GrayScaleFixed(this Matrix<float> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			float[] Src = M.Data;
			int[] Dest = new int[w * h];

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] = (int)(0x01000000 * Src[SrcOffset++] + 0.5);
			}

			return new Matrix<int>(w, h, Dest);
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of byte-values gray scale pixels.</param>
		/// <returns>Matrix of floating point gray scale pixels.</returns>
		public static Matrix<int> GrayScaleFixed(this Matrix<int> M)
		{
			return M;
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of pixels.</param>
		/// <returns>Matrix of alpha-channel pixel component values.</returns>
		public static IMatrix GrayScaleFixed(this IMatrix M)
		{
			if (M is Matrix<uint> M2)
				return GrayScaleFixed(M2);
			else if (M is Matrix<byte> M3)
				return GrayScaleFixed(M3);
			else if (M is Matrix<float> M4)
				return GrayScaleFixed(M4);
			else if (M is Matrix<int> M5)
				return GrayScaleFixed(M5);
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}
	}
}
