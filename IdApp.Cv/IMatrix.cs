using System;

namespace IdApp.Cv
{
	/// <summary>
	/// Interface for matrices.
	/// </summary>
	public interface IMatrix
	{
		/// <summary>
		/// Type of elements in matrix.
		/// </summary>
		Type ElementType { get; }

		/// <summary>
		/// Width of matrix (number of columns)
		/// </summary>
		int Width { get; }

		/// <summary>
		/// Height of matrix (number of rows)
		/// </summary>
		int Height { get; }

		/// <summary>
		/// Left offset of matrix in underlying data array.
		/// </summary>
		int Left { get; }

		/// <summary>
		/// Top offset of matrix in underlying data array.
		/// </summary>
		int Top { get; }

		/// <summary>
		/// Number of elements per row in underlying data array.
		/// </summary>
		int RowSize { get; }

		/// <summary>
		/// Total number of elements in underlying data array.
		/// </summary>
		int DataSize { get; }

		/// <summary>
		/// Start offset of matrix in underlying data.
		/// </summary>
		int Start { get; }

		/// <summary>
		/// Number of elements to skip from the right edge in the underlying data to the left edge of the new row.
		/// </summary>
		int Skip { get; }

		/// <summary>
		/// Gets the start index of a point in the image.
		/// </summary>
		/// <param name="X">X-Coordinate</param>
		/// <param name="Y">Y-Coordinate</param>
		/// <returns>Index into underlying data set.</returns>
		int StartIndex(int X, int Y);
	}
}
