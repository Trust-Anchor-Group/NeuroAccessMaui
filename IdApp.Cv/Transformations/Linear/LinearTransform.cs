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
		/// Converts a CV Matrix to a script Matrix.
		/// </summary>
		/// <param name="M">CV Matrix</param>
		/// <returns>Script matrix.</returns>
		public static DoubleMatrix ToDoubleMatrix(this Matrix<float> M)
		{
			float[] v = M.Data;
			int i, c = M.Data.Length;
			DoubleNumber[] Elements = new DoubleNumber[c];

			for (i = 0; i < c; i++)
				Elements[i] = new DoubleNumber(v[i]);

			return new DoubleMatrix(M.Height, M.Width, Elements);
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Transform">Linear transform in homogeneous coordinates</param>
		/// <param name="Width">Width of new image.</param>
		/// <param name="Height">Height of new image.</param>
		/// <returns>New matrix containing the transformed result.</returns>
		public static Matrix<float> LinearTransform(this Matrix<float> M, Matrix<float> Transform, int Width, int Height)
		{
			return LinearTransform(M, Transform.ToDoubleMatrix(), Width, Height);
		}

		/// <summary>
		/// Performs an image convolution on a matrix.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Transform">Linear transform in homogeneous coordinates</param>
		/// <param name="Width">Width of new image.</param>
		/// <param name="Height">Height of new image.</param>
		/// <returns>New matrix containing the transformed result.</returns>
		public static Matrix<float> LinearTransform(this Matrix<float> M, DoubleMatrix Transform, int Width, int Height)
		{
			if (Transform.Columns != 3 || Transform.Rows != 3)
				throw new ArgumentException("Invalid transform matrix.", nameof(Transform));

			DoubleMatrix T = (DoubleMatrix)Transform.Invert();
			double[,] a = T.Values;
			float a00 = (float)a[0, 0];
			float a10 = (float)a[0, 1];
			float a20 = (float)a[0, 2];
			float a01 = (float)a[1, 0];
			float a11 = (float)a[1, 1];
			float a21 = (float)a[1, 2];
			float a02 = (float)a[2, 0];
			float a12 = (float)a[2, 1];
			float a22 = (float)a[2, 2];

			Matrix<float> Result = new Matrix<float>(Width, Height);
			float[] Dest = Result.Data;
			int DestIndex = 0;
			int x, y;
			int w = M.Width;
			int h = M.Height;
			float x0, y0, t0, v0, v1, v2, v3, w0, w1;
			int ix0, iy0;
			float fx0, fy0;

			for (y = 0; y < Height; y++)
			{
				for (x = 0; x < Width; x++)
				{
					x0 = a00 * x + a10 * y + a20;
					y0 = a01 * x + a11 * y + a21;
					t0 = a02 * x + a12 * y + a22;

					if (t0 != 0 && t0 != 1)
					{
						x0 /= t0;
						y0 /= t0;
					}

					if (x0 >= 0 && x0 < w && y0 >= 0 && y0 < h)
					{
						ix0 = (int)x0;
						iy0 = (int)y0;
						fx0 = x0 - ix0;
						fy0 = y0 - iy0;

						v0 = M[ix0, iy0];

						if (fx0 == 0 && fy0 == 0)
							Dest[DestIndex] = v0;
						else
						{
							if (ix0 + 1 < w)
								v1 = M[ix0 + 1, iy0];
							else
								v1 = v0;

							if (iy0 + 1 < h)
							{
								v2 = M[ix0, iy0 + 1];

								if (ix0 + 1 < w)
									v3 = M[ix0 + 1, iy0 + 1];
								else
									v3 = v0;
							}
							else
								v2 = v3 = v0;

							w0 = v1 * fx0 + v0 * (1 - fx0);
							w1 = v3 * fx0 + v2 * (1 - fx0);

							Dest[DestIndex] = w1 * fy0 + w0 * (1 - fy0);
						}
					}

					DestIndex++;
				}
			}

			return Result;
		}

	}
}
