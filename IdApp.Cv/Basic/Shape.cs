using System;

namespace IdApp.Cv.Basic
{
	/// <summary>
	/// Shape for morphological operations.
	/// </summary>
	public class Shape : Matrix<bool>
	{
		private readonly int pixelX;
		private readonly int pixelY;

		/// <summary>
		/// Shape for morphological operations.
		/// </summary>
		/// <param name="Width">Width of Matrix</param>
		/// <param name="Height">Height of Matrix</param>
		/// <param name="Points">Points in the shape.</param>
		/// <param name="PixelX">X-coordinate within matrix representing the pixel on which the morphlogical operation operates.</param>
		/// <param name="PixelY">Y-coordinate within matrix representing the pixel on which the morphlogical operation operates.</param>
		public Shape(int Width, int Height, bool[] Points, int PixelX, int PixelY)
			: base(Width, Height, Points)
		{
			if (PixelX < 0 || PixelX >= Width)
				throw new ArgumentOutOfRangeException(nameof(PixelX));

			if (PixelY < 0 || PixelY >= Height)
				throw new ArgumentOutOfRangeException(nameof(PixelY));

			this.pixelX = PixelX;
			this.pixelY = PixelY;
		}

		/// <summary>
		/// Implements a Matrix, basic component for computations in Image Processing and Computer Vision.
		/// </summary>
		/// <param name="Width">Width of Matrix</param>
		/// <param name="Height">Height of Matrix</param>
		/// <param name="Data">Underlying data.</param>
		/// <param name="Left">Left coordinate of matrix in underlying data.</param>
		/// <param name="Top">Top coordinate of matrix in underlying data.</param>
		/// <param name="RowSize">Size of one row.</param>
		/// <param name="Start">Start index of matrix in underlying data.</param>
		/// <param name="Skip">Number of elements to skip after last element in a row of the matrix to the first element of the next row.</param>
		/// <param name="PixelX">X-coordinate within matrix representing the pixel on which the morphlogical operation operates.</param>
		/// <param name="PixelY">Y-coordinate within matrix representing the pixel on which the morphlogical operation operates.</param>
		protected Shape(int Width, int Height, bool[] Data, int Left, int Top, int RowSize,
			int Start, int Skip, int PixelX, int PixelY)
			: base(Width, Height, Data, Left, Top, RowSize, Start, Skip)
		{
			this.pixelX = PixelX;
			this.pixelY = PixelY;
		}

		/// <summary>
		/// X-coordinate within matrix representing the pixel on which the morphlogical operation operates.
		/// </summary>
		public int PixelX => this.pixelX;

		/// <summary>
		/// Y-coordinate within matrix representing the pixel on which the morphlogical operation operates.
		/// </summary>
		public int PixelY => this.pixelY;

		/// <summary>
		/// Creates a shallow copy of the matrix. The shallow copy points to the same
		/// underlying pixel data.
		/// </summary>
		/// <returns>Shallow copy</returns>
		public override Matrix<bool> ShallowCopy()
		{
			return new Shape(this.Width, this.Height, this.Data, this.Left, this.Top,
				this.RowSize, this.Start, this.Skip, this.pixelX, this.pixelY);
		}

		private const bool X = true;
		private const bool _ = false;

		/// <summary>
		/// Cross shape
		/// </summary>
		public static readonly Shape Cross_3x3 = new Shape(3, 3, new bool[]
		{
			_, X, _,
			X, X, X,
			_, X, _,
		}, 1, 1);

		/// <summary>
		/// Cross shape
		/// </summary>
		public static readonly Shape Cross_5x5 = new Shape(5, 5, new bool[]
		{
			_, _, X, _, _,
			_, _, X, _, _,
			X, X, X, X, X,
			_, _, X, _, _,
			_, _, X, _, _,
		}, 2, 2);

		/// <summary>
		/// Cross shape
		/// </summary>
		public static readonly Shape Cross_7x7 = new Shape(7, 7, new bool[]
		{
			_, _, _, X, _, _, _,
			_, _, _, X, _, _, _,
			_, _, _, X, _, _, _,
			X, X, X, X, X, X, X,
			_, _, _, X, _, _, _,
			_, _, _, X, _, _, _,
			_, _, _, X, _, _, _,
		}, 3, 3);

		/// <summary>
		/// X shape
		/// </summary>
		public static readonly Shape X_3x3 = new Shape(3, 3, new bool[]
		{
			X, _, X,
			_, X, _,
			X, _, X,
		}, 1, 1);

		/// <summary>
		/// X shape
		/// </summary>
		public static readonly Shape X_5x5 = new Shape(5, 5, new bool[]
		{
			X, _, _, _, X,
			_, X, _, X, _,
			_, _, X, _, _,
			_, X, _, X, _,
			X, _, _, _, X,
		}, 2, 2);

		/// <summary>
		/// X shape
		/// </summary>
		public static readonly Shape X_7x7 = new Shape(7, 7, new bool[]
		{
			X, _, _, _, _, _, X,
			_, X, _, _, _, X, _,
			_, _, X, _, X, _, _,
			_, _, _, X, _, _, _,
			_, _, X, _, X, _, _,
			_, X, _, _, _, X, _,
			X, _, _, _, _, _, X,
		}, 3, 3);

		/// <summary>
		/// Diamond shape
		/// </summary>
		public static readonly Shape Diamond_3x3 = Cross_3x3;

		/// <summary>
		/// Diamond shape
		/// </summary>
		public static readonly Shape Diamond_5x5 = new Shape(5, 5, new bool[]
		{
			_, _, X, _, _,
			_, X, X, X, _,
			X, X, X, X, X,
			_, X, X, X, _,
			_, _, X, _, _,
		}, 2, 2);

		/// <summary>
		/// Diamond shape
		/// </summary>
		public static readonly Shape Diamond_7x7 = new Shape(7, 7, new bool[]
		{
			_, _, _, X, _, _, _,
			_, _, X, X, X, _, _,
			_, X, X, X, X, X, _,
			X, X, X, X, X, X, X,
			_, X, X, X, X, X, _,
			_, _, X, X, X, _, _,
			_, _, _, X, _, _, _,
		}, 3, 3);
	}
}
