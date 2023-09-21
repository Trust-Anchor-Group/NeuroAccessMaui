namespace IdApp.Cv.Basic
{
	/// <summary>
	/// Represents a point in an image.
	/// </summary>
	public struct Point
	{
		/// <summary>
		/// Represents a point in an image.
		/// </summary>
		/// <param name="X">X-Coordinate</param>
		/// <param name="Y">Y-Coordinate</param>
		public Point(int X, int Y)
		{
			this.X = X;
			this.Y = Y;
		}

		/// <summary>
		/// X-Coordinate
		/// </summary>
		public int X;

		/// <summary>
		/// Y-Coordinate
		/// </summary>
		public int Y;

		/// <inheritdoc/>
		public override string ToString()
		{
			return "(" + this.X + "," + this.Y + ")";
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is Point P &&
				this.X == P.X &&
				this.Y == P.Y;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int Result = this.X.GetHashCode();
			Result ^= Result << 5 ^ this.Y.GetHashCode();
			return Result;
		}

		/// <summary>
		/// Equals operator on points.
		/// </summary>
		/// <param name="P1">Point 1</param>
		/// <param name="P2">Point 2</param>
		/// <returns>If P1 represents the same point as P2.</returns>
		public static bool operator ==(Point P1, Point P2)
		{
			return P1.Equals(P2);
		}

		/// <summary>
		/// Not-Equals operator on points.
		/// </summary>
		/// <param name="P1">Point 1</param>
		/// <param name="P2">Point 2</param>
		/// <returns>If P1 represents a different point than P2.</returns>
		public static bool operator !=(Point P1, Point P2)
		{
			return !P1.Equals(P2);
		}
	}
}
