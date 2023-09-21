using System;

namespace IdApp.Cv.Basic
{
	/// <summary>
	/// Static class for Basic Operations, implemented as extensions.
	/// </summary>
	public static partial class BasicOperations
	{
		/// <summary>
		/// Draws a line between two points.
		/// </summary>
		/// <typeparam name="T">Pixel type.</typeparam>
		/// <param name="M">Matrix of pixels</param>
		/// <param name="X1">X-coordinate of one of the endpoints.</param>
		/// <param name="Y1">Y-coordinate of one of the endpoints.</param>
		/// <param name="X2">X-coordinate of the other of the endpoints.</param>
		/// <param name="Y2">Y-coordinate of the other of the endpoints.</param>
		/// <param name="Value">Value to set.</param>
		public static void Line<T>(this Matrix<T> M, int X1, int Y1, int X2, int Y2, T Value)
			where T : struct
		{
			int dx = X2 - X1;
			int dy = Y2 - Y1;
			int i;

			if (Math.Abs(dx) >= Math.Abs(dy))
			{
				if (dx == 0)
					M[X1, Y1] = Value;
				else if (X1 > X2)
				{
					for (i = X2; i <= X1; i++)
						M[i, Y2 + (i - X2) * dy / dx] = Value;
				}
				else
				{
					for (i = X1; i <= X2; i++)
						M[i, Y1 + (i - X1) * dy / dx] = Value;
				}
			}
			else
			{
				if (Y1 > Y2)
				{
					for (i = Y2; i <= Y1; i++)
						M[X2 + (i - Y2) * dx / dy, i] = Value;
				}
				else
				{
					for (i = Y1; i <= Y2; i++)
						M[X1 + (i - Y1) * dx / dy, i] = Value;
				}
			}
		}
	}
}
