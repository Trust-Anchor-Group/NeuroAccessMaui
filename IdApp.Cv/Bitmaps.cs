using SkiaSharp;
using System;
using System.IO;

namespace IdApp.Cv
{
	/// <summary>
	/// Static methods managing conversion to and from bitmap representations.
	/// </summary>
	public static class Bitmaps
	{
		/// <summary>
		/// Loads a bitmap from a file, and returns a matrix.
		/// </summary>
		/// <param name="FileName">File name of bitmap.</param>
		/// <returns>Matrix</returns>
		public static IMatrix FromBitmapFile(string FileName)
		{
			return FromBitmapFile(FileName, int.MaxValue, int.MaxValue);
		}

		/// <summary>
		/// Loads a bitmap from a file, and returns a matrix.
		/// </summary>
		/// <param name="FileName">File name of bitmap.</param>
		/// <param name="MaxHeight">Maximum height of loaded image. If image is larger,
		/// it will be scaled down.</param>
		/// <param name="MaxWidth">Maximum width of loaded image. If image is larger,
		/// it will be scaled down.</param>
		/// <returns>Matrix</returns>
		public static IMatrix FromBitmapFile(string FileName, int MaxWidth, int MaxHeight)
		{
			using (SKBitmap Bmp = SKBitmap.Decode(FileName))
			{
				return FromBitmap(Bmp, MaxWidth, MaxHeight);
			}
		}

		/// <summary>
		/// Loads a bitmap from a binary representation, and returns a matrix.
		/// </summary>
		/// <param name="Data">Binary representation of bitmap</param>
		/// <returns>Matrix</returns>
		public static IMatrix FromBitmapFile(byte[] Data)
		{
			return FromBitmapFile(Data, int.MaxValue, int.MaxValue);
		}

		/// <summary>
		/// Loads a bitmap from a binary representation, and returns a matrix.
		/// </summary>
		/// <param name="Data">Binary representation of bitmap</param>
		/// <param name="MaxWidth">Maximum width of matrix.</param>
		/// <param name="MaxHeight">Maximum Height of matrix.</param>
		/// <returns>Matrix</returns>
		public static IMatrix FromBitmapFile(byte[] Data, int MaxWidth, int MaxHeight)
		{
			using (SKBitmap Bmp = SKBitmap.Decode(Data))
			{
				return FromBitmap(Bmp, MaxWidth, MaxHeight);
			}
		}

		/// <summary>
		/// Craetes a matrix from a bitmap.
		/// </summary>
		/// <param name="Bmp">Bitmap</param>
		/// <returns>Matrix</returns>
		public static IMatrix FromBitmap(SKBitmap Bmp)
		{
			return FromBitmap(Bmp, int.MaxValue, int.MaxValue);
		}

		/// <summary>
		/// Craetes a matrix from a bitmap.
		/// </summary>
		/// <param name="Bmp">Bitmap</param>
		/// <param name="MaxWidth">Maximum width of matrix.</param>
		/// <param name="MaxHeight">Maximum Height of matrix.</param>
		/// <returns>Matrix</returns>
		public static IMatrix FromBitmap(SKBitmap Bmp, int MaxWidth, int MaxHeight)
		{
			if (Bmp.Width > MaxWidth || Bmp.Height > MaxHeight)
			{
				double Scale = ((double)MaxWidth) / Bmp.Width;
				double Scale2 = ((double)MaxHeight) / Bmp.Height;

				if (Scale2 < Scale)
					Scale = Scale2;

				int Width = (int)(Bmp.Width * Scale + 0.5);
				int Height = (int)(Bmp.Height * Scale + 0.5);

				using (SKSurface Surface = SKSurface.Create(new SKImageInfo(Width, Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul)))
				{
					SKCanvas Canvas = Surface.Canvas;
					Canvas.DrawBitmap(Bmp, new SKRect(0, 0, Bmp.Width, Bmp.Height),
						new SKRect(0, 0, Width, Height), new SKPaint()
						{
							IsAntialias = true,
							FilterQuality = SKFilterQuality.High
						});


					using (SKImage ScaledIamge = Surface.Snapshot())
					{
						using (SKBitmap ScaledBitmap = SKBitmap.FromImage(ScaledIamge))
						{
							return FromBitmap(ScaledBitmap, int.MaxValue, int.MaxValue);
						}
					}
				}
			}
			else
			{
				byte[] Data = Bmp.Bytes;

				switch (Bmp.ColorType)
				{
					case SKColorType.Alpha8:
					case SKColorType.Gray8:
						return new Matrix<byte>(Bmp.Width, Bmp.Height, Data);

					case SKColorType.Bgra8888:
						int i, j, c = Data.Length;
						uint[] Bin32 = new uint[c / 4];
						uint ui32;

						i = j = 0;
						while (i < c)
						{
							ui32 = (uint)(Data[i++] << 16);
							ui32 |= (uint)(Data[i++] << 8);
							ui32 |= Data[i++];
							ui32 |= (uint)(Data[i++] << 24);
							Bin32[j++] = ui32;
						}

						return new Matrix<uint>(Bmp.Width, Bmp.Height, Bin32);

					case SKColorType.Rgba8888:
						c = Data.Length;
						Bin32 = new uint[c / 4];

						i = j = 0;
						while (i < c)
						{
							ui32 = (uint)(Data[i++] << 16);
							ui32 |= (uint)(Data[i++] << 24);
							ui32 |= Data[i++];
							ui32 |= (uint)(Data[i++] << 8);
							Bin32[j++] = ui32;
						}

						return new Matrix<uint>(Bmp.Width, Bmp.Height, Bin32);

					default:
						throw new ArgumentException("Color type not supported: " + Bmp.ColorType.ToString(), nameof(Bmp));
				}
			}
		}

		/// <summary>
		/// Saves a matrix as an image to a file.
		/// </summary>
		/// <param name="M">Matrix</param>
		/// <param name="FileName">Filename</param>
		public static void ToImageFile(IMatrix M, string FileName)
		{
			using (SKImage Image = ToBitmap(M))
			{
				ToImageFile(Image, FileName);
			}
		}

		/// <summary>
		/// Saves an image to a file.
		/// </summary>
		/// <param name="Image">Image</param>
		/// <param name="FileName">Filename</param>
		public static void ToImageFile(SKImage Image, string FileName)
		{
			ToImageFile(Image, FileName, 100);
		}

		/// <summary>
		/// Saves an image to a file.
		/// </summary>
		/// <param name="Image">Image</param>
		/// <param name="FileName">Filename</param>
		/// <param name="Quality">Encoding quality.</param>
		public static void ToImageFile(SKImage Image, string FileName, int Quality)
		{
			string Extension = Path.GetExtension(FileName).ToLower();
			if (Extension.StartsWith("."))
				Extension = Extension.Substring(1);

			if (string.IsNullOrEmpty(Extension))
				throw new ArgumentException("Missing file extension", nameof(FileName));

			Extension = Extension.Substring(0, 1).ToUpper() + Extension.Substring(1).ToLower();
			if (!Enum.TryParse<SKEncodedImageFormat>(Extension, out SKEncodedImageFormat Format))
				throw new ArgumentException("Unsupported image type/extension: " + Extension, nameof(FileName));

			string Folder = Path.GetDirectoryName(FileName);
			if (!(Directory.Exists(Folder)))
				Directory.CreateDirectory(Folder);

			using (SKData Data = Image.Encode(Format, Quality))
			{
				using (FileStream fs = File.Create(FileName))
				{
					Data.SaveTo(fs);
				}
			}
		}

		/// <summary>
		/// Converts a matrix to an image.
		/// </summary>
		/// <param name="M">Matrix</param>
		/// <returns>Image</returns>
		public static SKImage ToBitmap(IMatrix M)
		{
			int x, w = M.Width;
			int y, h = M.Height;
			int RowBytes;
			byte[] Dest;
			SKColorType ColorType;

			if (M is Matrix<uint> ColorMatrix)
			{
				uint[] Src = ColorMatrix.Data;
				int SrcIndex = ColorMatrix.Start;
				int SrcSkip = ColorMatrix.Skip;
				Dest = new byte[w * h * 4];
				int DestIndex = 0;
				uint ui32;

				for (y = 0; y < h; y++, SrcIndex += SrcSkip)
				{
					for (x = 0; x < w; x++)
					{
						ui32 = Src[SrcIndex++];

						Dest[DestIndex++] = (byte)(ui32 >> 16);
						Dest[DestIndex++] = (byte)(ui32 >> 8);
						Dest[DestIndex++] = (byte)ui32;
						Dest[DestIndex++] = (byte)(ui32 >> 24);
					}
				}

				RowBytes = w << 2;
				ColorType = SKColorType.Bgra8888;
			}
			else if (M is Matrix<byte> GrayScaleMatrix)
			{
				byte[] Src = GrayScaleMatrix.Data;
				int SrcIndex = GrayScaleMatrix.Start;
				int SrcSkip = GrayScaleMatrix.Skip;
				Dest = new byte[w * h];
				int DestIndex = 0;

				for (y = 0; y < h; y++, SrcIndex += SrcSkip)
				{
					for (x = 0; x < w; x++)
						Dest[DestIndex++] = Src[SrcIndex++];
				}

				RowBytes = w;
				ColorType = SKColorType.Gray8;
			}
			else if (M is Matrix<float> ComputationMatrix)
			{
				float[] Src = ComputationMatrix.Data;
				int SrcIndex = ComputationMatrix.Start;
				int SrcSkip = ComputationMatrix.Skip;
				Dest = new byte[w * h];
				int DestIndex = 0;
				float f;

				for (y = 0; y < h; y++, SrcIndex += SrcSkip)
				{
					for (x = 0; x < w; x++)
					{
						f = Src[SrcIndex++];
						Dest[DestIndex++] = f < 0f ? (byte)0 : f > 1f ? (byte)255 : (byte)(f * 255f + 0.5f);
					}
				}

				RowBytes = w;
				ColorType = SKColorType.Gray8;
			}
			else if (M is Matrix<int> FixedPointMatrix)
			{
				int[] Src = FixedPointMatrix.Data;
				int SrcIndex = FixedPointMatrix.Start;
				int SrcSkip = FixedPointMatrix.Skip;
				Dest = new byte[w * h];
				int DestIndex = 0;
				int f;

				for (y = 0; y < h; y++, SrcIndex += SrcSkip)
				{
					for (x = 0; x < w; x++)
					{
						f = Src[SrcIndex++] + 32768;
						Dest[DestIndex++] = f < 0f ? (byte)0 : f >= 0x01000000 ? (byte)255 : (byte)(f >> 16);
					}
				}

				RowBytes = w;
				ColorType = SKColorType.Gray8;
			}
			else
				throw new ArgumentException("Matrix type not supported: " + M.GetType().FullName);

			using (MemoryStream ms = new MemoryStream(Dest))
			{
				using (SKData Data = SKData.Create(ms))
				{
					return SKImage.FromPixels(new SKImageInfo(w, h, ColorType), Data, RowBytes);
				}
			}
		}


		/// <summary>
		/// Encodes an image in a matrix using PNG.
		/// </summary>
		/// <param name="M">Matrix</param>
		/// <returns>Binary PNG representation of image.</returns>
		public static byte[] EncodeAsPng(IMatrix M)
		{
			using (SKImage Image = ToBitmap(M))
			{
				using (SKData Data = Image.Encode(SKEncodedImageFormat.Png, 100))
				{
					return Data.ToArray();
				}
			}
		}
	}
}
