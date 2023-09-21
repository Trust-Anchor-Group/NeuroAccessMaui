using System;
using Waher.Script.Objects;
using Waher.Script.Objects.Matrices;

namespace IdApp.Cv.Transformations.Linear
{
	/// <summary>
	/// Static class for Linear Transformation Operations, implemented as extensions.
	/// </summary>
	public static partial class LinearTransformationOperations
	{
		/// <summary>
		/// Rotates the image 180 degrees to the right.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<T> Rotate180<T>(this Matrix<T> M)
			where T : struct
		{
			int w = M.Width;
			int h = M.Height;
			Matrix<T> Result = new Matrix<T>(w, h);
			T[] Src = M.Data;
			int SrcIndex;
			T[] Dest = Result.Data;
			int DestIndex = 0;
			int x, y;

			for (y = 0; y < h; y++)
			{
				SrcIndex = M.StartIndex(w - 1, h - y - 1);
				for (x = 0; x < w; x++)
					Dest[DestIndex++] = Src[SrcIndex--];
			}

			return Result;
		}

		/// <summary>
		/// Rotates the image 180 degrees to the right.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static IMatrix Rotate180(this IMatrix M)
		{
			if (M is Matrix<float> M2)
				return M2.Rotate180();
			else if (M is Matrix<int> M3)
				return M3.Rotate180();
			else if (M is Matrix<uint> M4)
				return M4.Rotate180();
			else if (M is Matrix<byte> M5)
				return M5.Rotate180();
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}

	}
}
