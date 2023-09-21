using IdApp.Cv.Basic;
using System;
using System.Collections.Generic;

namespace IdApp.Cv.Objects
{
	/// <summary>
	/// Contains an object map of contents in an image.
	/// </summary>
	public class ObjectMap : Matrix<ushort>
	{
		private readonly Dictionary<ushort, ObjectInformation> objectsById = new Dictionary<ushort, ObjectInformation>();
		private readonly ObjectInformation[] objects;
		private readonly IMatrix image;

		/// <summary>
		/// Contains an object map of contents in an image.
		/// </summary>
		/// <param name="M">Image matrix</param>
		/// <param name="Threshold">Threshold to use when identifying objects.</param>
		public ObjectMap(Matrix<float> M, float Threshold)
			: base(M.Width, M.Height)
		{
			this.image = Image;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;
			int DestIndex = 0;
			float[] Src = M.Data;
			ushort[] Dest = this.Data;
			float v;
			ObjectInformation Info;
			ushort Obj;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				for (x = 0; x < w; x++, DestIndex++)
				{
					v = Src[SrcIndex++];
					if (v >= Threshold)
					{
						Obj = Dest[DestIndex];
						if (Obj == 0)
						{
							Obj = (ushort)this.objectsById.Count;
							Info = new ObjectInformation(Obj, x, y, this);
							this.objectsById[Obj] = Info;
							this.FloodFill(x, y, M, Threshold, Info);
						}
					}
				}
			}

			this.objects = new ObjectInformation[this.objectsById.Count];
			this.objectsById.Values.CopyTo(this.objects, 0);
		}

		private void FloodFill(int x, int y, Matrix<float> M, float Threshold, ObjectInformation Info)
		{
			int SrcIndex = M.StartIndex(0, y);
			int DestIndex = this.StartIndex(0, y);
			float[] Src = M.Data;
			ushort[] Dest = this.Data;
			int x1 = x;
			int x2 = x;
			int w = M.Width;
			int h = M.Height - 1;
			int SrcRow = M.RowSize;
			int DestRow = this.RowSize;
			ushort Nr = (ushort)(Info.Nr + 1);
			int si, di;

			while (x1 > 0 && Src[SrcIndex + x1 - 1] >= Threshold && Dest[DestIndex + x1 - 1] == 0)
				x1--;

			while (x2 < w - 1 && Src[SrcIndex + x2 + 1] >= Threshold && Dest[DestIndex + x2 + 1] == 0)
				x2++;

			Info.AddPixels(x1, x2, y);

			x = x1;
			di = DestIndex + x;
			Dest[di] = (ushort)(Nr | 0x8000);
			x++;
			di++;

			while (x < x2)
			{
				Dest[di] = Nr;
				x++;
				di++;
			}

			Dest[di] = (ushort)(Nr | 0x8000);

			for (x = x1, si = SrcIndex + x, di = DestIndex + x; x <= x2; x++, si++, di++)
			{
				if (y > 0)
				{
					if (Src[si - SrcRow] >= Threshold)
					{
						if (Dest[di - DestRow] == 0)
							this.FloodFill(x, y - 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}

				if (y < h)
				{
					if (Src[si + SrcRow] >= Threshold)
					{
						if (Dest[di + DestRow] == 0)
							this.FloodFill(x, y + 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}
			}
		}


		/// <summary>
		/// Contains an object map of contents in an image.
		/// </summary>
		/// <param name="M">Image matrix</param>
		/// <param name="Threshold">Threshold to use when identifying objects.</param>
		public ObjectMap(Matrix<int> M, int Threshold)
			: base(M.Width, M.Height)
		{
			this.image = Image;

			int y, h = M.Height;
			int x, w = M.Width;
			int SrcIndex = M.Start;
			int SrcSkip = M.Skip;
			int DestIndex = 0;
			int[] Src = M.Data;
			ushort[] Dest = this.Data;
			int v;
			ObjectInformation Info;
			ushort Obj;

			for (y = 0; y < h; y++, SrcIndex += SrcSkip)
			{
				for (x = 0; x < w; x++, DestIndex++)
				{
					v = Src[SrcIndex++];
					if (v >= Threshold)
					{
						Obj = Dest[DestIndex];
						if (Obj == 0)
						{
							Obj = (ushort)this.objectsById.Count;
							Info = new ObjectInformation(Obj, x, y, this);
							this.objectsById[Obj] = Info;
							this.FloodFill(x, y, M, Threshold, Info);
						}
					}
				}
			}

			this.objects = new ObjectInformation[this.objectsById.Count];
			this.objectsById.Values.CopyTo(this.objects, 0);
		}

		private void FloodFill(int x, int y, Matrix<int> M, int Threshold, ObjectInformation Info)
		{
			int SrcIndex = M.StartIndex(0, y);
			int DestIndex = this.StartIndex(0, y);
			int[] Src = M.Data;
			ushort[] Dest = this.Data;
			int x1 = x;
			int x2 = x;
			int w = M.Width;
			int h = M.Height - 1;
			int SrcRow = M.RowSize;
			int DestRow = this.RowSize;
			ushort Nr = (ushort)(Info.Nr + 1);
			int si, di;

			while (x1 > 0 && Src[SrcIndex + x1 - 1] >= Threshold && Dest[DestIndex + x1 - 1] == 0)
				x1--;

			while (x2 < w - 1 && Src[SrcIndex + x2 + 1] >= Threshold && Dest[DestIndex + x2 + 1] == 0)
				x2++;

			Info.AddPixels(x1, x2, y);

			x = x1;
			di = DestIndex + x;
			Dest[di] = (ushort)(Nr | 0x8000);
			x++;
			di++;

			while (x < x2)
			{
				Dest[di] = Nr;
				x++;
				di++;
			}

			Dest[di] = (ushort)(Nr | 0x8000);

			for (x = x1, si = SrcIndex + x, di = DestIndex + x; x <= x2; x++, si++, di++)
			{
				if (y > 0)
				{
					if (Src[si - SrcRow] >= Threshold)
					{
						if (Dest[di - DestRow] == 0)
							this.FloodFill(x, y - 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}

				if (y < h)
				{
					if (Src[si + SrcRow] >= Threshold)
					{
						if (Dest[di + DestRow] == 0)
							this.FloodFill(x, y + 1, M, Threshold, Info);
					}
					else
						Dest[di] |= 0x4000;
				}
			}
		}

		/// <summary>
		/// Underlying image.
		/// </summary>
		public IMatrix Image => this.image;

		/// <summary>
		/// Number of objects in map.
		/// </summary>
		public int NrObjects => this.objectsById.Count;

		/// <summary>
		/// Gets a reference to information about an object in the object map.
		/// </summary>
		/// <param name="Nr">Zero-based object index.</param>
		/// <returns>Object information.</returns>
		public ObjectInformation this[ushort Nr] => this.objectsById[Nr];

		internal Point[] FindContour(int X0, int Y0, int Nr)
		{
			List<Point> Result = new List<Point>();
			LinkedList<Rec> History = new LinkedList<Rec>();
			Point P;
			int Dir = 0;    // Right
			int x = X0;
			int y = Y0;
			int i;
			int Index = this.StartIndex(X0, Y0);
			int Index1;
			int RowSize = this.RowSize;
			int w = this.Width;
			int h = this.Height;
			ushort[] Data = this.Data;
			ushort v;
			int dx, dy;
			int x1, y1;

			Nr++;

			Result.Add(P = new Point(X0, Y0));
			History.AddLast(new Rec(P, Dir));
			Data[Index] &= 0x3fff;

			while (true)
			{
				for (i = 0; i < 8; i++)
				{
					dx = DirectionX[Dir];
					dy = DirectionY[Dir];

					x1 = x + dx;
					y1 = y + dy;

					if (x1 >= 0 && x1 < w && y1 >= 0 && y1 < h)
					{
						Index1 = Index + dx + dy * RowSize;
						v = Data[Index1];
						if ((v & 0x3fff) == Nr && (v & 0xc000) > 0)
						{
							x = x1;
							y = y1;
							Index = Index1;
							Result.Add(P = new Point(x, y));
							History.AddLast(new Rec(P, Dir));
							Data[Index] &= 0x3fff;
							break;
						}
					}

					Dir = (Dir + 1) & 7;
				}

				if (i >= 8)
				{
					if (History.Last is null)
						break;
					else
					{
						Rec Rec = History.Last.Value;
						History.RemoveLast();
						x = Rec.P.X;
						y = Rec.P.Y;
						Dir = Rec.Dir;
						Index = this.StartIndex(x, y);
					}
				}
			}

			return Result.ToArray();
		}

		private static readonly int[] DirectionX = new int[] { 1, 1, 0, -1, -1, -1, 0, 1 };
		private static readonly int[] DirectionY = new int[] { 0, -1, -1, -1, 0, 1, 1, 1 };

		private class Rec
		{
			public Rec(Point P, int Dir)
			{
				this.P = P;
				this.Dir = Dir;
			}

			public Point P;
			public int Dir;
		}

		/// <summary>
		/// Objects found in map.
		/// </summary>
		public ObjectInformation[] Objects => this.objects;

		/// <summary>
		/// Extracts one or more objects in the form of image from the underlying image. 
		/// Only pixels pertaining to the objects will be copied.
		/// </summary>
		/// <param name="Nrs">Object Numbers</param>
		/// <returns>Object image.</returns>
		public IMatrix Extract(params ushort[] Nrs)
		{
			return this.Extract(Nrs, this.image);
		}

		/// <summary>
		/// Extracts one or more objects in the form of image from the underlying image. 
		/// Only pixels pertaining to the objects will be copied.
		/// </summary>
		/// <param name="Nrs">Object Numbers</param>
		/// <param name="Source">Source image to get the original pixels from.</param>
		/// <returns>Object image.</returns>
		public IMatrix Extract(ushort[] Nrs, IMatrix Source)
		{
			if (Source.Width != this.Width || Source.Height != this.Height)
				throw new ArgumentOutOfRangeException(nameof(Source));

			ObjectInformation[] Infos = this.GetInfo(Nrs);

			if (Source is Matrix<float> M)
				return this.Extract<float>(Nrs, M, Infos, float.MinValue);
			else if (Source is Matrix<int> M2)
				return this.Extract<int>(Nrs, M2, Infos, int.MinValue);
			else if (Source is Matrix<uint> M3)
				return this.Extract<uint>(Nrs, M3, Infos, 0);
			else if (Source is Matrix<byte> M4)
				return this.Extract<byte>(Nrs, M4, Infos, 0);
			else
				throw new InvalidOperationException("Underlying image type not supported.");
		}

		private ObjectInformation[] GetInfo(ushort[] Nrs)
		{
			int i, c = Nrs.Length;
			ObjectInformation[] Infos = new ObjectInformation[c];

			for (i = 0; i < c; i++)
			{
				if (!this.objectsById.TryGetValue(Nrs[i], out ObjectInformation Info))
					throw new ArgumentOutOfRangeException(nameof(Nrs));

				Infos[i] = Info;
			}

			return Infos;
		}

		/// <summary>
		/// Extracts one or more objects in the form of image from the underlying image. 
		/// Only pixels pertaining to the objects will be copied.
		/// </summary>
		/// <param name="Nrs">Object Numbers</param>
		/// <param name="Source">Source image to get the original pixels from.</param>
		/// <returns>Object image.</returns>
		public Matrix<T> Extract<T>(ushort[] Nrs, Matrix<T> Source)
			where T : struct
		{
			if (Source.Width != this.Width || Source.Height != this.Height)
				throw new ArgumentOutOfRangeException(nameof(Source));

			ObjectInformation[] Infos = this.GetInfo(Nrs);

			return this.Extract<T>(Nrs, Source, Infos, default);
		}

		private Matrix<T> Extract<T>(ushort[] Nrs, Matrix<T> Image, ObjectInformation[] Objects, T BackgroundValue)
			where T : struct
		{
			int x, y, c = Nrs.Length;
			if (c != Objects.Length)
				throw new ArgumentException("Array size mismatch.", nameof(Objects));

			if (c == 0)
				return new Matrix<T>(1, 1);

			int SrcX1 = Objects[0].MinX;
			int SrcY1 = Objects[0].MinY;
			int SrcX2 = Objects[0].MaxX;
			int SrcY2 = Objects[0].MaxY;
			y = Objects[0].Nr;

			for (x = 1; x < c; x++)
			{
				ObjectInformation Info = Objects[x];

				if (Info.MinX < SrcX1)
					SrcX1 = Info.MinX;

				if (Info.MaxX > SrcX2)
					SrcX2 = Info.MaxX;

				if (Info.MinY < SrcY1)
					SrcY1 = Info.MinY;

				if (Info.MaxY > SrcY2)
					SrcY2 = Info.MaxY;

				if (Info.Nr > y)
					y = Info.Nr;
			}

			bool[] Included = new bool[y + 2];
			for (x = 0; x < c; x++)
				Included[Objects[x].Nr + 1] = true;

			c = y + 2;

			int w = SrcX2 - SrcX1 + 1;
			int h = SrcY2 - SrcY1 + 1;
			int SrcIndex = Image.StartIndex(SrcX1, SrcY1);
			int SrcSkip = Image.Skip + Image.Width - w;
			int MaskIndex = this.StartIndex(SrcX1, SrcY1);
			int MaskSkip = Image.Width - w;
			ushort i;
			Matrix<T> Result = new Matrix<T>(w, h);
			ushort[] Mask = this.Data;
			T[] Src = Image.Data;
			T[] Dest = Result.Data;
			int DestIndex = 0;

			for (y = SrcY1; y <= SrcY2; y++, SrcIndex += SrcSkip, MaskIndex += MaskSkip)
			{
				for (x = SrcX1; x <= SrcX2; x++, SrcIndex++)
				{
					i = Mask[MaskIndex++];
					i &= 0x3fff;
					Dest[DestIndex++] = i < c && Included[i] ? Src[SrcIndex] : BackgroundValue;
				}
			}

			return Result;
		}
	}
}
