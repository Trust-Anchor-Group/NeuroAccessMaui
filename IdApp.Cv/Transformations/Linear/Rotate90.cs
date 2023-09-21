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
		/// Rotates the image 90 degrees to the right.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static Matrix<T> Rotate90<T>(this Matrix<T> M)
			where T : struct
		{
			int w = M.Width;
			int h = M.Height;
			Matrix<T> Result = new Matrix<T>(h, w);
			T[] Src = M.Data;
			int SrcIndex;
			int SrcRowSize = M.RowSize;
			T[] Dest = Result.Data;
			int DestIndex = 0;
			int x, y;

			for (y = 0; y < w; y++)
			{
				SrcIndex = M.StartIndex(y, h - 1);
				for (x = 0; x < h; x++)
				{
					Dest[DestIndex++] = Src[SrcIndex];
					SrcIndex -= SrcRowSize;
				}
			}

			return Result;
		}

		/// <summary>
		/// Rotates the image 90 degrees to the right.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <returns>New matrix containing the convolution result.</returns>
		public static IMatrix Rotate90(this IMatrix M)
		{
			if (M is Matrix<float> M2)
				return M2.Rotate90();
			else if (M is Matrix<int> M3)
				return M3.Rotate90();
			else if (M is Matrix<uint> M4)
				return M4.Rotate90();
			else if (M is Matrix<byte> M5)
				return M5.Rotate90();
			else
				throw new ArgumentException("Unsupported type: " + M.GetType().FullName, nameof(M));
		}
	}
}
