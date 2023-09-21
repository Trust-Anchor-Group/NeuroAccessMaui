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
		public static Matrix<float> GrayScale(this Matrix<uint> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			uint[] Src = M.Data;
			float[] Dest = new float[w * h];
			uint ui;
			float f;

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
				{
					ui = Src[SrcOffset++];

					f = 0.299f * (ui & 255);
					ui >>= 8;
					f += 0.587f * (ui & 255);
					ui >>= 8;
					f += 0.114f * (ui & 255);

					Dest[DestOffset++] = f / 255f;
				}
			}

			return new Matrix<float>(w, h, Dest);
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of byte-values gray scale pixels.</param>
		/// <returns>Matrix of floating point gray scale pixels.</returns>
		public static Matrix<float> GrayScale(this Matrix<byte> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			byte[] Src = M.Data;
			float[] Dest = new float[w * h];

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] = Src[SrcOffset++] / 255f;
			}

			return new Matrix<float>(w, h, Dest);
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of byte-values gray scale pixels.</param>
		/// <returns>Matrix of floating point gray scale pixels.</returns>
		public static Matrix<float> GrayScale(this Matrix<float> M)
		{
			return M;
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of byte-values gray scale pixels.</param>
		/// <returns>Matrix of floating point gray scale pixels.</returns>
		public static Matrix<float> GrayScale(this Matrix<int> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			int[] Src = M.Data;
			float[] Dest = new float[w * h];

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] = Src[SrcOffset++] / 16777216f;
			}

			return new Matrix<float>(w, h, Dest);
		}

		/// <summary>
		/// Creates a matrix of grayscale values. Floating point scales are used
		/// to avoid round-off errors and loss when transforming the image.
		/// </summary>
		/// <param name="M">Matrix of pixels.</param>
		/// <returns>Matrix of alpha-channel pixel component values.</returns>
		public static IMatrix GrayScale(this IMatrix M)
		{
			if (M is Matrix<uint> M2)
				return GrayScale(M2);
			else if (M is Matrix<byte> M3)
				return GrayScale(M3);
			else if (M is Matrix<float> M4)
				return GrayScale(M4);
			else if (M is Matrix<int> M5)
				return GrayScale(M5);
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}
	}
}
