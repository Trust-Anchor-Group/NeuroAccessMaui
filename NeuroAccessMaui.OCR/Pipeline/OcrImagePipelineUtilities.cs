using System.Globalization;
using IdApp.Cv;
using IdApp.Cv.Basic;
using IdApp.Cv.ColorModels;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Convolutions;
using IdApp.Cv.Transformations.Linear;
using NeuroAccessMaui.OCR.Models;
using CvRect = IdApp.Cv.Basic.Rect;

namespace NeuroAccessMaui.OCR.Pipeline
{
	internal static class OcrImagePipelineUtilities
	{
		internal static IMatrix NormalizeSourceImage(IMatrix Image, int SourceRotationDegrees)
		{
			return SourceRotationDegrees switch
			{
				90 => Rotate90(Image),
				180 => Rotate180(Image),
				270 => Rotate270(Image),
				_ => Image
			};
		}

		internal static Matrix<float> ToGrayScaleFloat(IMatrix Image)
		{
			return Image switch
			{
				Matrix<float> FloatMatrix => Clone(FloatMatrix),
				Matrix<byte> ByteMatrix => ByteMatrix.GrayScale(),
				Matrix<uint> ColorMatrix => ColorMatrix.GrayScale(),
				Matrix<int> FixedMatrix => FixedMatrix.GrayScale(),
				_ => throw new ArgumentException("Unsupported matrix type: " + Image.GetType().FullName, nameof(Image))
			};
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

		internal static Matrix<float> DownscaleToWidth(Matrix<float> Image, int TargetWidth)
		{
			ArgumentNullException.ThrowIfNull(Image);
			if (TargetWidth <= 0)
				throw new ArgumentOutOfRangeException(nameof(TargetWidth));
			if (Image.Width <= TargetWidth)
				return Clone(Image);

			int TargetHeight = Math.Max(1, (int)Math.Round(Image.Height * (TargetWidth / (double)Image.Width)));
			return Resize(Image, TargetWidth, TargetHeight);
		}

		internal static CvRect ResolveBounds(IMatrix Image, DocumentRegionHint? Hint)
		{
			ArgumentNullException.ThrowIfNull(Image);
			if (Hint is null)
			{
				return ClampBounds(Image, new CvRect
				{
					Left = (int)Math.Round(Image.Width * 0.05f),
					Top = (int)Math.Round(Image.Height * 0.06f),
					Width = (int)Math.Round(Image.Width * 0.90f),
					Height = (int)Math.Round(Image.Height * 0.88f)
				});
			}

			return ClampBounds(Image, new CvRect
			{
				Left = (int)Math.Round(Image.Width * Hint.Left),
				Top = (int)Math.Round(Image.Height * Hint.Top),
				Width = (int)Math.Round(Image.Width * Hint.Width),
				Height = (int)Math.Round(Image.Height * Hint.Height)
			});
		}

		internal static CvRect ClampBounds(IMatrix Image, CvRect Bounds)
		{
			ArgumentNullException.ThrowIfNull(Image);
			int Left = Math.Clamp(Bounds.Left, 0, Math.Max(0, Image.Width - 1));
			int Top = Math.Clamp(Bounds.Top, 0, Math.Max(0, Image.Height - 1));
			int Right = Math.Clamp(Bounds.Left + Math.Max(1, Bounds.Width), Left + 1, Image.Width);
			int Bottom = Math.Clamp(Bounds.Top + Math.Max(1, Bounds.Height), Top + 1, Image.Height);

			return new CvRect
			{
				Left = Left,
				Top = Top,
				Width = Math.Max(1, Right - Left),
				Height = Math.Max(1, Bottom - Top)
			};
		}

		internal static Matrix<float> Crop(Matrix<float> Image, CvRect Bounds)
		{
			ArgumentNullException.ThrowIfNull(Image);
			CvRect ClampedBounds = ClampBounds(Image, Bounds);
			Matrix<float> Result = new Matrix<float>(ClampedBounds.Width, ClampedBounds.Height);
			for (int y = 0; y < ClampedBounds.Height; y++)
			{
				for (int x = 0; x < ClampedBounds.Width; x++)
					Result[x, y] = Image[ClampedBounds.Left + x, ClampedBounds.Top + y];
			}

			return Result;
		}

		internal static DocumentQualityMetrics EvaluateQuality(Matrix<float> Image)
		{
			ArgumentNullException.ThrowIfNull(Image);

			Matrix<float>? Laplacian = Image.Width >= 3 && Image.Height >= 3
				? Image.DetectEdgesLaplacian()
				: null;
			double Sum = 0d;
			double SumSquared = 0d;
			double EdgeEnergy = 0d;
			float Minimum = float.MaxValue;
			float Maximum = float.MinValue;
			int HighlightCount = 0;
			int ShadowCount = 0;
			int PixelCount = Image.Width * Image.Height;
			int ImageIndex = Image.Start;

			for (int y = 0; y < Image.Height; y++, ImageIndex += Image.Skip)
			{
				int RowImageIndex = ImageIndex;
				for (int x = 0; x < Image.Width; x++, RowImageIndex++)
				{
					float Value = Image.Data[RowImageIndex];
					float EdgeValue = Laplacian is not null && x > 0 && y > 0 && x - 1 < Laplacian.Width && y - 1 < Laplacian.Height
						? Math.Abs(Laplacian[x - 1, y - 1])
						: 0f;
					Sum += Value;
					SumSquared += Value * Value;
					EdgeEnergy += EdgeValue;
					Minimum = Math.Min(Minimum, Value);
					Maximum = Math.Max(Maximum, Value);
					if (Value >= 0.96f)
						HighlightCount++;
					if (Value <= 0.03f)
						ShadowCount++;
				}
			}

			if (PixelCount <= 0)
				return new DocumentQualityMetrics(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

			float Mean = (float)(Sum / PixelCount);
			float Variance = (float)Math.Max((SumSquared / PixelCount) - (Mean * Mean), 0d);
			float StandardDeviation = (float)Math.Sqrt(Variance);
			float DynamicRange = Maximum > Minimum ? Maximum - Minimum : 0f;
			float Sharpness = (float)(EdgeEnergy / PixelCount);
			float HighlightRatio = HighlightCount / (float)PixelCount;
			float ShadowRatio = ShadowCount / (float)PixelCount;
			float GlareRatio = EstimateGlareRatio(Image);
			float ContrastScore = Math.Clamp((StandardDeviation - 0.06f) / 0.18f, 0f, 1f);
			float SharpnessScore = Math.Clamp((Sharpness - 0.018f) / 0.13f, 0f, 1f);
			float RangeScore = Math.Clamp((DynamicRange - 0.18f) / 0.70f, 0f, 1f);
			float GlareScore = 1f - Math.Clamp(GlareRatio / 0.12f, 0f, 1f);
			float QualityScore = ContrastScore * 0.30f
				+ SharpnessScore * 0.42f
				+ RangeScore * 0.12f
				+ GlareScore * 0.16f;

			return new DocumentQualityMetrics(
				Mean,
				StandardDeviation,
				DynamicRange,
				Sharpness,
				HighlightRatio,
				ShadowRatio,
				GlareRatio,
				Math.Clamp(QualityScore, 0f, 1f));
		}

		internal static Dictionary<string, string> CreateQualityMetadata(DocumentQualityMetrics QualityMetrics, string Prefix)
		{
			ArgumentNullException.ThrowIfNull(QualityMetrics);
			return new Dictionary<string, string>
			{
				[Prefix + "Mean"] = QualityMetrics.Mean.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "StdDev"] = QualityMetrics.StandardDeviation.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "DynamicRange"] = QualityMetrics.DynamicRange.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "Sharpness"] = QualityMetrics.Sharpness.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "HighlightRatio"] = QualityMetrics.HighlightRatio.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "ShadowRatio"] = QualityMetrics.ShadowRatio.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "GlareRatio"] = QualityMetrics.GlareRatio.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "QualityScore"] = QualityMetrics.QualityScore.ToString("0.000000", CultureInfo.InvariantCulture)
			};
		}

		internal static Dictionary<string, string> CreateQualityScoreBreakdownMetadata(DocumentQualityMetrics QualityMetrics, string Prefix)
		{
			ArgumentNullException.ThrowIfNull(QualityMetrics);
			float ContrastScore = Math.Clamp((QualityMetrics.StandardDeviation - 0.06f) / 0.18f, 0f, 1f);
			float SharpnessScore = Math.Clamp((QualityMetrics.Sharpness - 0.018f) / 0.13f, 0f, 1f);
			float RangeScore = Math.Clamp((QualityMetrics.DynamicRange - 0.18f) / 0.70f, 0f, 1f);
			float GlareScore = 1f - Math.Clamp(QualityMetrics.GlareRatio / 0.12f, 0f, 1f);
			return new Dictionary<string, string>
			{
				[Prefix + "ContrastComponentScore"] = ContrastScore.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "SharpnessComponentScore"] = SharpnessScore.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "RangeComponentScore"] = RangeScore.ToString("0.000000", CultureInfo.InvariantCulture),
				[Prefix + "GlareComponentScore"] = GlareScore.ToString("0.000000", CultureInfo.InvariantCulture)
			};
		}

		internal static void MergeMetadata(Dictionary<string, string> Target, IReadOnlyDictionary<string, string> Source)
		{
			foreach (KeyValuePair<string, string> Pair in Source)
				Target[Pair.Key] = Pair.Value;
		}

		private static float EstimateGlareRatio(Matrix<float> Image)
		{
			int LowerStart = (int)Math.Round(Image.Height * 0.45d);
			int CandidateCount = 0;
			int ReflectionCandidateCount = 0;
			int ReflectionSampleCount = 0;
			int PixelCount = Math.Max(1, Image.Width * Math.Max(1, Image.Height - LowerStart));
			for (int y = LowerStart; y < Image.Height; y++)
			{
				for (int x = 0; x < Image.Width; x++)
				{
					if (Image[x, y] >= 0.97f)
						CandidateCount++;
				}
			}

			for (int y = Math.Max(6, LowerStart); y < Image.Height - 6; y += 2)
			{
				for (int x = 6; x < Image.Width - 6; x += 2)
				{
					ReflectionSampleCount++;
					float Value = Image[x, y];
					if (Value < 0.72f)
						continue;

					float LocalRingMean = CalculateLocalRingMean(Image, x, y, 5);
					if (Value - LocalRingMean >= 0.13f && LocalRingMean < 0.86f)
						ReflectionCandidateCount++;
				}
			}

			float SaturatedRatio = CandidateCount / (float)PixelCount;
			float ReflectionRatio = ReflectionSampleCount == 0 ? 0f : ReflectionCandidateCount / (float)ReflectionSampleCount;
			return Math.Max(SaturatedRatio, ReflectionRatio);
		}

		private static float CalculateLocalRingMean(Matrix<float> Image, int CenterX, int CenterY, int Radius)
		{
			float Sum = 0f;
			int Count = 0;
			for (int y = CenterY - Radius; y <= CenterY + Radius; y++)
			{
				for (int x = CenterX - Radius; x <= CenterX + Radius; x++)
				{
					if (x != CenterX - Radius
						&& x != CenterX + Radius
						&& y != CenterY - Radius
						&& y != CenterY + Radius)
					{
						continue;
					}

					Sum += Image[x, y];
					Count++;
				}
			}

			return Count == 0 ? 0f : Sum / Count;
		}

		private static IMatrix Rotate90(IMatrix Image)
		{
			return Image switch
			{
				Matrix<byte> ByteMatrix => ByteMatrix.Rotate90(),
				Matrix<float> FloatMatrix => FloatMatrix.Rotate90(),
				Matrix<uint> ColorMatrix => ColorMatrix.Rotate90(),
				Matrix<int> FixedMatrix => FixedMatrix.Rotate90(),
				_ => throw new ArgumentException("Unsupported matrix type: " + Image.GetType().FullName, nameof(Image))
			};
		}

		private static IMatrix Rotate180(IMatrix Image)
		{
			return Image switch
			{
				Matrix<byte> ByteMatrix => ByteMatrix.Rotate180(),
				Matrix<float> FloatMatrix => FloatMatrix.Rotate180(),
				Matrix<uint> ColorMatrix => ColorMatrix.Rotate180(),
				Matrix<int> FixedMatrix => FixedMatrix.Rotate180(),
				_ => throw new ArgumentException("Unsupported matrix type: " + Image.GetType().FullName, nameof(Image))
			};
		}

		private static IMatrix Rotate270(IMatrix Image)
		{
			return Image switch
			{
				Matrix<byte> ByteMatrix => ByteMatrix.Rotate270(),
				Matrix<float> FloatMatrix => FloatMatrix.Rotate270(),
				Matrix<uint> ColorMatrix => ColorMatrix.Rotate270(),
				Matrix<int> FixedMatrix => FixedMatrix.Rotate270(),
				_ => throw new ArgumentException("Unsupported matrix type: " + Image.GetType().FullName, nameof(Image))
			};
		}
	}
}
