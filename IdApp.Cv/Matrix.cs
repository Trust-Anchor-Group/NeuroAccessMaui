using SkiaSharp;
using System;

namespace IdApp.Cv
{
	/// <summary>
	/// Implements a Matrix, basic component for computations in Image Processing and Computer Vision.
	/// </summary>
	/// <typeparam name="T">Type of each pixel.</typeparam>
	public class Matrix<T> : IMatrix
		where T : struct
	{
		private T[] data;
		private int width;
		private int height;
		private int left;
		private int top;
		private int rowSize;
		private int dataSize;
		private int start;
		private int skip;

		private Matrix()
		{
		}

		/// <summary>
		/// Implements a Matrix, basic component for computations in Image Processing and Computer Vision.
		/// </summary>
		/// <param name="Width">Width of Matrix</param>
		/// <param name="Height">Height of Matrix</param>
		public Matrix(int Width, int Height)
			: this(Width, Height, new T[Width * Height])
		{
		}

		/// <summary>
		/// Implements a Matrix, basic component for computations in Image Processing and Computer Vision.
		/// </summary>
		/// <param name="Width">Width of Matrix</param>
		/// <param name="Height">Height of Matrix</param>
		/// <param name="Data">Underlying data.</param>
		public Matrix(int Width, int Height, T[] Data)
		{
			this.dataSize = Width * Height;
			if (this.dataSize != Data.Length)
				throw new ArgumentOutOfRangeException(nameof(Data));

			this.data = Data;
			this.width = Width;
			this.height = Height;
			this.left = 0;
			this.top = 0;
			this.start = 0;
			this.rowSize = Width;
			this.skip = 0;
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
		protected Matrix(int Width, int Height, T[] Data, int Left, int Top, int RowSize, 
			int Start, int Skip)
		{
			this.dataSize = Data.Length;
			this.data = Data;
			this.width = Width;
			this.height = Height;
			this.left = Left;
			this.top = Top;
			this.start = Start;
			this.rowSize = RowSize;
			this.skip = Skip;
		}

		/// <summary>
		/// Creates a shallow copy of the matrix. The shallow copy points to the same
		/// underlying pixel data.
		/// </summary>
		/// <returns>Shallow copy</returns>
		public virtual Matrix<T> ShallowCopy()
		{
			return new Matrix<T>()
			{
				data = this.data,
				width = this.width,
				height = this.height,
				left = this.left,
				top = this.top,
				start = this.start,
				rowSize = this.rowSize,
				dataSize = this.dataSize,
				skip = this.skip
			};
		}

		/// <summary>
		/// Type of elements in matrix.
		/// </summary>
		public Type ElementType => typeof(T);

		/// <summary>
		/// Underlying data on which the matrix is defined.
		/// </summary>
		public T[] Data => this.data;

		/// <summary>
		/// Width of matrix (number of columns)
		/// </summary>
		public int Width
		{
			get => this.width;
			set
			{
				if (value <= 0 || value >= this.rowSize - this.left)
					throw new ArgumentOutOfRangeException(nameof(Width));

				this.width = value;
				this.skip = this.rowSize - this.width;
			}
		}

		/// <summary>
		/// Height of matrix (number of rows)
		/// </summary>
		public int Height
		{
			get => this.height;
			set
			{
				if (value <= 0 || (this.top + value) * this.width >= this.dataSize)
					throw new ArgumentOutOfRangeException(nameof(Height));

				this.height = value;
			}
		}

		/// <summary>
		/// Left offset of matrix in underlying data array.
		/// </summary>
		public int Left
		{
			get => this.left;
			set
			{
				if (value < 0 || value >= this.rowSize - this.width)
					throw new ArgumentOutOfRangeException(nameof(Left));

				this.left = value;
				this.start = this.left + this.top * this.rowSize;
			}
		}

		/// <summary>
		/// Top offset of matrix in underlying data array.
		/// </summary>
		public int Top
		{
			get => this.top;
			set
			{
				if (value < 0 || (value + this.height) * this.width >= this.dataSize)
					throw new ArgumentOutOfRangeException(nameof(Top));

				this.top = value;
				this.start = this.left + this.top * this.rowSize;
			}
		}

		/// <summary>
		/// Sets <see cref="Left"/> and <see cref="Width"/> simultaneously.
		/// </summary>
		/// <param name="Left">New Left</param>
		/// <param name="Width">New Width</param>
		public void SetXSpan(int Left, int Width)
		{
			if (Left < 0 || Left + Width > this.rowSize)
				throw new ArgumentOutOfRangeException(nameof(Left));

			if (Width <= 0)
				throw new ArgumentOutOfRangeException(nameof(Width));

			this.left = Left;
			this.start = this.left + this.top * this.rowSize;
			this.width = Width;
			this.skip = this.rowSize - this.width;
		}

		/// <summary>
		/// Sets <see cref="Top"/> and <see cref="Height"/> simultaneously.
		/// </summary>
		/// <param name="Top">New Top</param>
		/// <param name="Height">New Height</param>
		public void SetYSpan(int Top, int Height)
		{
			if (Top < 0 || (Top + Height) * this.width > this.dataSize)
				throw new ArgumentOutOfRangeException(nameof(Top));

			if (Height <= 0)
				throw new ArgumentOutOfRangeException(nameof(Height));

			this.top = Top;
			this.height = Height;
			this.start = this.left + this.top * this.rowSize;
		}
		/// <summary>
		/// Number of elements per row in underlying data array.
		/// </summary>
		public int RowSize => this.rowSize;

		/// <summary>
		/// Total number of elements in underlying data array.
		/// </summary>
		public int DataSize => this.dataSize;

		/// <summary>
		/// Start offset of matrix in underlying data.
		/// </summary>
		public int Start => this.start;

		/// <summary>
		/// Number of elements to skip from the right edge in the underlying data to the left edge of the new row.
		/// </summary>
		public int Skip => this.skip;

		/// <summary>
		/// Direct access to underlying elements in the matix.
		/// </summary>
		/// <param name="X">Zero-based X-coordinate (column)</param>
		/// <param name="Y">Zero-based Y-coordinate (row)</param>
		/// <returns>Pixel/Element</returns>
		public T this[int X, int Y]
		{
			get => this.data[(X + this.left) + (Y + this.top) * this.rowSize];
			set => this.data[(X + this.left) + (Y + this.top) * this.rowSize] = value;
		}

		/// <summary>
		/// Gets the start index of a point in the image.
		/// </summary>
		/// <param name="X">X-Coordinate</param>
		/// <param name="Y">Y-Coordinate</param>
		/// <returns>Index into underlying data set.</returns>
		public int StartIndex(int X, int Y)
		{
			return (X + this.left) + (Y + this.top) * this.rowSize;
		}

		/// <summary>
		/// Returns a row vector as a matrix.
		/// </summary>
		/// <param name="RowIndex">Zero-based row index.</param>
		/// <returns>Row vector as a matrix.</returns>
		public Matrix<T> Row(int RowIndex)
		{
			if (RowIndex < 0 || RowIndex >= this.height)
				throw new ArgumentOutOfRangeException(nameof(RowIndex));

			return new Matrix<T>()
			{
				data = this.data,
				width = this.width,
				height = 1,
				left = this.left,
				top = this.top + RowIndex,
				rowSize = this.rowSize,
				dataSize = this.dataSize,
				start = this.start + (RowIndex * this.rowSize),
				skip = this.skip
			};
		}

		/// <summary>
		/// Returns a column vector as a matrix.
		/// </summary>
		/// <param name="ColumnIndex">Zero-based column index.</param>
		/// <returns>Column vector as a matrix.</returns>
		public Matrix<T> Column(int ColumnIndex)
		{
			if (ColumnIndex < 0 || ColumnIndex >= this.width)
				throw new ArgumentOutOfRangeException(nameof(ColumnIndex));

			return new Matrix<T>()
			{
				data = this.data,
				width = 1,
				height = this.height,
				left = this.left + ColumnIndex,
				top = this.top,
				rowSize = this.rowSize,
				dataSize = this.dataSize,
				start = this.start + ColumnIndex,
				skip = this.rowSize - 1
			};
		}

		/// <summary>
		/// Returns a region of the matrix, as a new matrix.
		/// </summary>
		/// <param name="Left">Zero-based left offset where the region begins.</param>
		/// <param name="Top">Zero-based top offset where the region begins.</param>
		/// <param name="Width">Width of region</param>
		/// <param name="Height">Height of region</param>
		/// <returns>Column vector as a matrix.</returns>
		public Matrix<T> Region(int Left, int Top, int Width, int Height)
		{
			if (Left < 0 || Left >= this.width)
				throw new ArgumentOutOfRangeException(nameof(Left));

			if (Top < 0 || Top >= this.height)
				throw new ArgumentOutOfRangeException(nameof(Top));

			if (Width <= 0 || Width > this.width - this.left - Left)
				throw new ArgumentOutOfRangeException(nameof(Width));

			if (Height <= 0 || Height > this.height - this.top - Top)
				throw new ArgumentOutOfRangeException(nameof(Height));

			return new Matrix<T>()
			{
				data = this.data,
				width = Width,
				height = Height,
				left = this.left + Left,
				top = this.top + Top,
				rowSize = this.rowSize,
				dataSize = this.dataSize,
				start = this.start + Left + (Top * this.rowSize),
				skip = this.rowSize - Width
			};
		}

		/// <summary>
		/// Loads an image into a matrix.
		/// </summary>
		/// <param name="FileName">Filename of image file.</param>
		/// <returns>Matrix iamge.</returns>
		public static IMatrix LoadFromFile(string FileName)
		{
			using (SKBitmap Bmp = SKBitmap.Decode(FileName))
			{
				byte[] Data = Bmp.Bytes;

				switch (Bmp.BytesPerPixel)
				{
					case 1:
						return new Matrix<byte>(Bmp.Width, Bmp.Height, Data);

					case 2:
						int i, j, c = Data.Length;
						ushort[] Bin16 = new ushort[c / 2];
						ushort u;

						i = j = 0;
						while (i < c)
						{
							u = Data[i++];
							u <<= 8;
							u |= Data[i++];
							Bin16[j++] = u;
						}

						return new Matrix<ushort>(Bmp.Width, Bmp.Height, Bin16);

					case 3:
						c = Data.Length;
						uint[] Bin32 = new uint[c / 3];
						uint v;

						i = j = 0;
						while (i < c)
						{
							v = Data[i++];
							v <<= 8;
							v |= Data[i++];
							v <<= 8;
							v |= Data[i++];
							Bin32[j++] = v;
						}

						return new Matrix<uint>(Bmp.Width, Bmp.Height, Bin32);

					case 4:
						c = Data.Length;
						Bin32 = new uint[c / 4];

						i = j = 0;
						while (i < c)
						{
							v = Data[i++];
							v <<= 8;
							v |= Data[i++];
							v <<= 8;
							v |= Data[i++];
							v <<= 8;
							v |= Data[i++];
							Bin32[j++] = v;
						}

						return new Matrix<uint>(Bmp.Width, Bmp.Height, Bin32);

					default:
						throw new ArgumentException("Color type not supported.", nameof(FileName));
				}
			}
		}

	}
}
