using System;
using System.Collections.Generic;

namespace IdApp.Cv.Basic
{
	/// <summary>
	/// Static class for Basic Operations, implemented as extensions.
	/// </summary>
	public static partial class BasicOperations
	{
		/// <summary>
		/// Reduces a contour, by removing points that are already represented by lines between
		/// other points, within a distance defined by <paramref name="Threshold"/>.
		/// </summary>
		/// <param name="Contour">Contour</param>
		/// <param name="Threshold">Distance from a line deemed "within the same line".</param>
		/// <returns></returns>
		public static Point[] Reduce(this Point[] Contour, int Threshold)
		{
			List<Point> Result = new List<Point>();
			int i, c = Contour.Length;
			bool Added;

			if (c <= 1)
				return Contour;

			if (Contour[0] == Contour[c - 1])
			{
				Contour = (Point[])Contour.Clone();
				Added = false;
			}
			else
			{
				Point[] Contour2 = new Point[c + 1];
				Array.Copy(Contour, 0, Contour2, 0, c);
				Contour2[c] = Contour2[0];
				Contour = Contour2;
				c++;
				Added = true;
			}

			Reduce(Contour, 0, c - 1, Threshold);

			for (i = 0; i < c; i++)
			{
				if (Contour[i].X >= 0)
					Result.Add(Contour[i]);
			}

			c = Result.Count;
			if (Added && Result[c - 1] == Result[0])
			{
				Result.RemoveAt(c - 1);
				c--;
			}

			if (c > 2 && DistanceToLine(Result[c - 1], Result[1], Result[0]) < Threshold)
				Result.RemoveAt(0);

			return Result.ToArray();
		}

		private static void Reduce(Point[] Contour, int Start, int End, int Threshold)
		{
			if (Start < End)
			{
				float x1 = Contour[Start].X;
				float y1 = Contour[Start].Y;
				float x2 = Contour[End].X;
				float y2 = Contour[End].Y;
				int i;
				int MaxDistant = 0;
				float d;
				float MaxDistance = 0;

				for (i = Start + 1; i < End; i++)
				{
					d = DistanceToLine(x1, y1, x2, y2, Contour[i].X, Contour[i].Y);
					if (d > MaxDistance)
					{
						MaxDistant = i;
						MaxDistance = d;
					}
				}

				if (MaxDistance < Threshold)
				{
					for (i = Start + 1; i < End; i++)
					{
						Contour[i].X = int.MinValue;
						Contour[i].Y = int.MinValue;
					}
				}
				else
				{
					Reduce(Contour, Start, MaxDistant, Threshold);
					Reduce(Contour, MaxDistant, End, Threshold);
				}
			}
		}

		/// <summary>
		/// Calculates the shortest distance of a point (x,y) to a line defined by
		/// two points (x1,y1) and (x2,y2).
		/// </summary>
		/// <param name="P1">Coordinates of point 1 defining the line.</param>
		/// <param name="P2">Coordinates of point 2 defining the line.</param>
		/// <param name="P">Coordinates of point whose distance from the line is to be calculated.</param>
		/// <returns>Shortest distance.</returns>
		public static float DistanceToLine(Point P1, Point P2, Point P)
		{
			return DistanceToLine(P1.X, P1.Y, P2.X, P2.Y, P.X, P.Y);
		}

		/// <summary>
		/// Calculates the shortest distance of a point (x,y) to a line defined by
		/// two points (x1,y1) and (x2,y2).
		/// </summary>
		/// <param name="x1">X-Coordinate of point 1 defining the line.</param>
		/// <param name="y1">Y-Coordinate of point 1 defining the line.</param>
		/// <param name="x2">X-Coordinate of point 2 defining the line.</param>
		/// <param name="y2">Y-Coordinate of point 2 defining the line.</param>
		/// <param name="x">X-Coordinate of point whose distance from the line is to be calculated.</param>
		/// <param name="y">Y-Coordinate of point whose distance from the line is to be calculated.</param>
		/// <returns>Shortest distance.</returns>
		public static float DistanceToLine(float x1, float y1, float x2, float y2, float x, float y)
		{
			float dx = x1 - x2;
			float dy = y1 - y2;

			if (dx == 0 && dy == 0)
			{
				dx = x1 - x;
				dy = y1 - y;

				return (float)Math.Sqrt(dx * dx + dy * dy);
			}
			else
				return (float)(Math.Abs(dx * (y1 - y) - (x1 - x) * dy) / Math.Sqrt(dx * dx + dy * dy));
		}
	}
}
