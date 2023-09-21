using System;

namespace IdApp.Cv.Arithmetics
{
	/// <summary>
	/// Static class for Arithmetics Operations, implemented as extensions.
	/// </summary>
	public static partial class ArithmeticsOperations
	{
		/// <summary>
		/// Negates the pixels of a matrix, creating the negative image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Negate(this Matrix<float> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			float[] Data = M.Data;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++, Index++)
					Data[Index] = 1.0f - Data[Index];
			}
		}

		/// <summary>
		/// Negates the pixels of a matrix, creating the negative image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Negate(this Matrix<int> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			int[] Data = M.Data;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++, Index++)
					Data[Index] = 0x01000000 - Data[Index];
			}
		}

		/// <summary>
		/// Negates the pixels of a matrix, creating the negative image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Negate(this Matrix<byte> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			byte[] Data = M.Data;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++, Index++)
					Data[Index] = (byte)(255 - Data[Index]);
			}
		}

		/// <summary>
		/// Negates the pixels of a matrix, creating the negative image.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		public static void Negate(this Matrix<uint> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int Index = M.Start;
			int Skip = M.Skip;
			uint[] Data = M.Data;
			uint ui;
			byte r, g, b;

			for (y = 0; y < h; y++, Index += Skip)
			{
				for (x = 0; x < w; x++)
				{
					ui = Data[Index];
					r = (byte)ui;
					ui >>= 8;
					g = (byte)ui;
					ui >>= 8;
					b = (byte)ui;

					ui &= 0xff00;
					ui |= (byte)(255 - b);
					ui <<= 8;
					ui |= (byte)(255 - g);
					ui <<= 8;
					ui |= (byte)(255 - r);

					Data[Index++] = ui;
				}
			}
		}

		/// <summary>
		/// Negates the pixels of a matrix, creating the negative image.
		/// </summary>
		/// <param name="M">Matrix of pixels.</param>
		public static void Negate(this IMatrix M)
		{
			if (M is Matrix<uint> M2)
				Negate(M2);
			else if (M is Matrix<byte> M3)
				Negate(M3);
			else if (M is Matrix<float> M4)
				Negate(M4);
			else if (M is Matrix<int> M5)
				Negate(M5);
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}
	}
}
