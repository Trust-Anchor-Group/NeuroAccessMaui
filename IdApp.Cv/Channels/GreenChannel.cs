using System;

namespace IdApp.Cv.Channels
{
	/// <summary>
	/// Static class for Channel Operations, implemented as extensions.
	/// </summary>
	public static partial class ChannelOperations
	{
		/// <summary>
		/// Creates a matrix of pixel green channel values.
		/// </summary>
		/// <param name="M">Matrix of colored pixels.</param>
		/// <returns>Matrix of green-channel pixel component values.</returns>
		public static Matrix<byte> GreenChannel(this Matrix<uint> M)
		{
			int y, h = M.Height;
			int x, w = M.Width;
			int SrcOffset = M.Start;
			int DestOffset = 0;
			int SrcSkip = M.Skip;
			uint[] Src = M.Data;
			byte[] Dest = new byte[w * h];

			for (y = 0; y < h; y++, SrcOffset += SrcSkip)
			{
				for (x = 0; x < w; x++)
					Dest[DestOffset++] = (byte)(Src[SrcOffset++] >> 8);
			}

			return new Matrix<byte>(w, h, Dest);
		}

		/// <summary>
		/// Creates a matrix of pixel green channel values.
		/// </summary>
		/// <param name="M">Matrix of colored pixels.</param>
		/// <returns>Matrix of green-channel pixel component values.</returns>
		public static IMatrix GreenChannel(this IMatrix M)
		{
			if (M is Matrix<uint> M2)
				return GreenChannel(M2);
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}
	}
}
