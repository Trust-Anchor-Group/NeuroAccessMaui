using IdApp.Cv;
using IdApp.Cv.ColorModels;

namespace NeuroAccessMaui.OCR.Pipeline
{
	internal static class OcrImagePipelineUtilities
	{
		/// <summary>
		/// Converts an image to 8-bit grayscale, returning byte images unchanged.
		/// </summary>
		/// <param name="Image">The source image.</param>
		/// <returns>The grayscale image.</returns>
		internal static Matrix<byte> ToGrayScaleBytes(IMatrix Image)
		{
			ArgumentNullException.ThrowIfNull(Image);
			if (Image is Matrix<byte> ByteImage)
				return ByteImage;

			if (Image.GrayScale() is not Matrix<float> GrayScale)
				throw new ArgumentException("Unsupported matrix type: " + Image.GetType().FullName, nameof(Image));

			byte[] Data = new byte[GrayScale.Width * GrayScale.Height];
			int Index = 0;
			for (int y = 0; y < GrayScale.Height; y++)
			{
				int SourceIndex = GrayScale.StartIndex(0, y);
				for (int x = 0; x < GrayScale.Width; x++)
					Data[Index++] = (byte)Math.Clamp((int)MathF.Round(GrayScale.Data[SourceIndex++] * 255f), 0, 255);
			}

			return new Matrix<byte>(GrayScale.Width, GrayScale.Height, Data);
		}

		internal static Matrix<float> Clone(Matrix<float> Image)
		{
			ArgumentNullException.ThrowIfNull(Image);
			Matrix<float> Result = new Matrix<float>(Image.Width, Image.Height);
			for (int y = 0; y < Image.Height; y++)
			{
				for (int x = 0; x < Image.Width; x++)
					Result[x, y] = Image[x, y];
			}

			return Result;
		}

		internal static Matrix<float> Resize(Matrix<float> Image, int Width, int Height)
		{
			ArgumentNullException.ThrowIfNull(Image);
			if (Width <= 0)
				throw new ArgumentOutOfRangeException(nameof(Width));
			if (Height <= 0)
				throw new ArgumentOutOfRangeException(nameof(Height));

			if (Image.Width == Width && Image.Height == Height)
				return Clone(Image);

			Matrix<float> Result = new Matrix<float>(Width, Height);
			float ScaleX = Image.Width / (float)Width;
			float ScaleY = Image.Height / (float)Height;
			for (int y = 0; y < Height; y++)
			{
				float SourceY = (y + 0.5f) * ScaleY - 0.5f;
				int y0 = Math.Clamp((int)Math.Floor(SourceY), 0, Image.Height - 1);
				int y1 = Math.Clamp(y0 + 1, 0, Image.Height - 1);
				float fy = SourceY - y0;
				for (int x = 0; x < Width; x++)
				{
					float SourceX = (x + 0.5f) * ScaleX - 0.5f;
					int x0 = Math.Clamp((int)Math.Floor(SourceX), 0, Image.Width - 1);
					int x1 = Math.Clamp(x0 + 1, 0, Image.Width - 1);
					float fx = SourceX - x0;
					float Top = Image[x0, y0] * (1f - fx) + Image[x1, y0] * fx;
					float Bottom = Image[x0, y1] * (1f - fx) + Image[x1, y1] * fx;
					Result[x, y] = Top * (1f - fy) + Bottom * fy;
				}
			}

			return Result;
		}

		/// <summary>
		/// Stretches the image values to the full 0..1 range. Images without contrast are left unchanged.
		/// </summary>
		/// <param name="Image">The image to stretch in place.</param>
		internal static void StretchContrast(Matrix<float> Image)
		{
			ArgumentNullException.ThrowIfNull(Image);

			float Minimum = float.MaxValue;
			float Maximum = float.MinValue;
			for (int y = 0; y < Image.Height; y++)
			{
				int Index = Image.StartIndex(0, y);
				for (int x = 0; x < Image.Width; x++, Index++)
				{
					float Value = Image.Data[Index];
					Minimum = Math.Min(Minimum, Value);
					Maximum = Math.Max(Maximum, Value);
				}
			}

			float Range = Maximum - Minimum;
			if (Range < 1e-4f)
				return;

			float Scale = 1f / Range;
			for (int y = 0; y < Image.Height; y++)
			{
				int Index = Image.StartIndex(0, y);
				for (int x = 0; x < Image.Width; x++, Index++)
					Image.Data[Index] = (Image.Data[Index] - Minimum) * Scale;
			}
		}

		/// <summary>
		/// Measures the share of pixels at or above a brightness threshold, used to spot clipped highlights such as glare.
		/// </summary>
		/// <param name="Image">The image to measure.</param>
		/// <param name="Threshold">The brightness threshold in the 0..1 range.</param>
		/// <returns>The ratio of pixels at or above the threshold.</returns>
		internal static float MeasureSaturatedRatio(Matrix<float> Image, float Threshold)
		{
			ArgumentNullException.ThrowIfNull(Image);

			int PixelCount = Image.Width * Image.Height;
			if (PixelCount <= 0)
				return 0f;

			int SaturatedCount = 0;
			for (int y = 0; y < Image.Height; y++)
			{
				int Index = Image.StartIndex(0, y);
				for (int x = 0; x < Image.Width; x++, Index++)
				{
					if (Image.Data[Index] >= Threshold)
						SaturatedCount++;
				}
			}

			return SaturatedCount / (float)PixelCount;
		}

		internal static void MergeMetadata(Dictionary<string, string> Target, IReadOnlyDictionary<string, string> Source)
		{
			foreach (KeyValuePair<string, string> Pair in Source)
				Target[Pair.Key] = Pair.Value;
		}
	}
}
