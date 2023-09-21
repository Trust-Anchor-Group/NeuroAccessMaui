using System;

namespace IdApp.Cv.ColorModels
{
	/// <summary>
	/// Static class for Channel Operations, implemented as extensions.
	/// </summary>
	public static partial class ColorModelOperations
	{
		/// <summary>
		/// Reduces the number of colors used in a matrix.
		/// </summary>
		/// <param name="M">Image matrix.</param>
		/// <param name="N">Number of values for each channel.</param>
		/// <returns>Matrix with reduced set of colors.</returns>
		public static Matrix<uint> ReduceColors(this Matrix<uint> M, uint N)
		{
			if (N < 2)
				throw new ArgumentOutOfRangeException(nameof(N));

			N--;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			uint[] Src = M.Data;
			uint[] Dest = new uint[w * h];
			uint ui, n, n2 = N >> 1;
			uint r, g, b;
			byte[] Lookup = new byte[256];

			for (ui = 0; ui < 256; ui++)
			{
				n = (ui * N + 128) / 255;
				Lookup[ui] = (byte)((n * 255 + n2) / N);
			}

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
				{
					ui = Src[SrcOffset++];

					r = Lookup[ui & 255];
					ui >>= 8;
					g = Lookup[ui & 255];
					ui >>= 8;
					b = Lookup[ui & 255];

					ui &= 0xff00;
					ui |= b;
					ui <<= 8;
					ui |= g;
					ui <<= 8;
					ui |= r;

					Dest[DestOffset++] = ui;
				}
			}

			return new Matrix<uint>(w, h, Dest);
		}

		/// <summary>
		/// Reduces the number of colors used in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixels.</param>
		/// <param name="N">Number of color values.</param>
		/// <returns>Matrix with reduced set of colors.</returns>
		public static Matrix<byte> ReduceColors(this Matrix<byte> M, uint N)
		{
			if (N < 2)
				throw new ArgumentOutOfRangeException(nameof(N));

			N--;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			byte[] Src = M.Data;
			byte[] Dest = new byte[w * h];
			uint ui, n, n2 = N >> 1;
			byte[] Lookup = new byte[256];

			for (ui = 0; ui < 256; ui++)
			{
				n = (ui * N + 128) / 255;
				Lookup[ui] = (byte)((n * 255 + n2) / N);
			}

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] = Lookup[Src[SrcOffset++]];
			}

			return new Matrix<byte>(w, h, Dest);
		}

		/// <summary>
		/// Reduces the number of colors used in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixels.</param>
		/// <param name="N">Number of color values.</param>
		/// <returns>Matrix with reduced set of colors.</returns>
		public static Matrix<float> ReduceColors(this Matrix<float> M, uint N)
		{
			if (N < 2)
				throw new ArgumentOutOfRangeException(nameof(N));

			N--;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			float[] Src = M.Data;
			float[] Dest = new float[w * h];
			float f, invN = 1.0f / N;

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
				{
					f = Src[SrcOffset++];
					Dest[DestOffset++] = (float)Math.Round(f * N) * invN;
				}
			}

			return new Matrix<float>(w, h, Dest);
		}

		/// <summary>
		/// Reduces the number of colors used in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixels.</param>
		/// <param name="N">Number of color values.</param>
		/// <returns>Matrix with reduced set of colors.</returns>
		public static Matrix<int> ReduceColors(this Matrix<int> M, uint N)
		{
			if (N < 2)
				throw new ArgumentOutOfRangeException(nameof(N));

			N--;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			int[] Src = M.Data;
			int[] Dest = new int[w * h];
			int f;

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
				{
					f = Src[SrcOffset++];
					Dest[DestOffset++] = (int)(((((long)f) * N + 8388608) & -0x1000000L) / N);
				}
			}

			return new Matrix<int>(w, h, Dest);
		}

		/// <summary>
		/// Reduces the number of colors used in a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixels.</param>
		/// <param name="N">Number of color values.</param>
		/// <returns>Matrix with reduced set of colors.</returns>
		public static IMatrix ReduceColors(this IMatrix M, uint N)
		{
			if (M is Matrix<uint> M2)
				return ReduceColors(M2, N);
			else if (M is Matrix<byte> M3)
				return ReduceColors(M3, N);
			else if (M is Matrix<float> M4)
				return ReduceColors(M4, N);
			else if (M is Matrix<int> M5)
				return ReduceColors(M5, N);
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}
	}
}
