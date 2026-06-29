using System.Globalization;
using IdApp.Cv;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Linear;
using NeuroAccessMaui.OCR.Models;
using CvPoint = IdApp.Cv.Basic.Point;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Rectifies detected MRZ quadrilaterals into upright MRZ region images.
	/// </summary>
	public sealed class MrzRegionRectifier
	{
		private const int PreferredMrzWidth = 900;
		private const int MinimumMrzWidth = 480;
		private const int MaximumMrzWidth = 1200;
		private const float MinimumMrzAspectRatio = 3.0f;
		private const float MaximumMrzAspectRatio = 14.0f;

		/// <summary>
		/// Rectifies the MRZ region using the detected quadrilateral.
		/// </summary>
		/// <param name="Image">The upright grayscale source image.</param>
		/// <param name="QuadCandidate">The selected MRZ quadrilateral candidate.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The rectified MRZ region image.</returns>
		public RectifiedDocument Rectify(
			Matrix<float> Image,
			DocumentQuadCandidate QuadCandidate,
			CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Image);
			ArgumentNullException.ThrowIfNull(QuadCandidate);
			CancellationToken.ThrowIfCancellationRequested();

			DocumentQuad Quad = QuadCandidate.Quad;
			float TopWidth = Distance(Quad.TopLeft, Quad.TopRight);
			float BottomWidth = Distance(Quad.BottomLeft, Quad.BottomRight);
			float LeftHeight = Distance(Quad.TopLeft, Quad.BottomLeft);
			float RightHeight = Distance(Quad.TopRight, Quad.BottomRight);
			float SourceWidth = Math.Max(TopWidth, BottomWidth);
			float SourceHeight = Math.Max(LeftHeight, RightHeight);
			float AspectRatio = Math.Clamp(SourceWidth / Math.Max(1f, SourceHeight), MinimumMrzAspectRatio, MaximumMrzAspectRatio);
			int OutputWidth = Math.Clamp((int)Math.Round(SourceWidth), MinimumMrzWidth, MaximumMrzWidth);
			OutputWidth = Math.Min(OutputWidth, PreferredMrzWidth);
			int OutputHeight = Math.Max(24, (int)Math.Round(OutputWidth / AspectRatio));

			Matrix<float> Transform = CreateHomography(Quad, OutputWidth, OutputHeight);
			Matrix<float> RectifiedImage = Image.LinearTransform(Transform, OutputWidth, OutputHeight);
			DocumentQualityMetrics RawQualityMetrics = OcrImagePipelineUtilities.EvaluateQuality(RectifiedImage);
			RectifiedImage.Contrast();
			DocumentQualityMetrics QualityMetrics = OcrImagePipelineUtilities.EvaluateQuality(RectifiedImage);
			Dictionary<string, string> Metadata = new Dictionary<string, string>(QuadCandidate.Metadata)
			{
				["RectifiedMrzWidth"] = OutputWidth.ToString(CultureInfo.InvariantCulture),
				["RectifiedMrzHeight"] = OutputHeight.ToString(CultureInfo.InvariantCulture),
				["RectifiedMrzAspectRatio"] = AspectRatio.ToString("0.000000", CultureInfo.InvariantCulture),
				["RectifiedMrzSourceWidth"] = SourceWidth.ToString("0.000", CultureInfo.InvariantCulture),
				["RectifiedMrzSourceHeight"] = SourceHeight.ToString("0.000", CultureInfo.InvariantCulture)
			};
			OcrImagePipelineUtilities.MergeMetadata(
				Metadata,
				OcrImagePipelineUtilities.CreateQualityMetadata(RawQualityMetrics, "RectifiedMrzRaw"));
			OcrImagePipelineUtilities.MergeMetadata(
				Metadata,
				OcrImagePipelineUtilities.CreateQualityScoreBreakdownMetadata(RawQualityMetrics, "RectifiedMrzRaw"));
			OcrImagePipelineUtilities.MergeMetadata(
				Metadata,
				OcrImagePipelineUtilities.CreateQualityMetadata(QualityMetrics, "RectifiedMrz"));
			OcrImagePipelineUtilities.MergeMetadata(
				Metadata,
				OcrImagePipelineUtilities.CreateQualityScoreBreakdownMetadata(QualityMetrics, "RectifiedMrz"));

			return new RectifiedDocument(RectifiedImage, Quad, QualityMetrics, Metadata);
		}

		private static Matrix<float> CreateHomography(DocumentQuad Quad, int OutputWidth, int OutputHeight)
		{
			double[,] Equations = new double[8, 9];
			AddHomographyEquation(Equations, 0, Quad.TopLeft.X, Quad.TopLeft.Y, 0d, 0d);
			AddHomographyEquation(Equations, 2, Quad.TopRight.X, Quad.TopRight.Y, OutputWidth - 1d, 0d);
			AddHomographyEquation(Equations, 4, Quad.BottomRight.X, Quad.BottomRight.Y, OutputWidth - 1d, OutputHeight - 1d);
			AddHomographyEquation(Equations, 6, Quad.BottomLeft.X, Quad.BottomLeft.Y, 0d, OutputHeight - 1d);
			double[] Solution = SolveLinearSystem(Equations);

			return new Matrix<float>(3, 3, new[]
			{
				(float)Solution[0],
				(float)Solution[1],
				(float)Solution[2],
				(float)Solution[3],
				(float)Solution[4],
				(float)Solution[5],
				(float)Solution[6],
				(float)Solution[7],
				1f
			});
		}

		private static void AddHomographyEquation(double[,] Equations, int Row, double SourceX, double SourceY, double DestinationX, double DestinationY)
		{
			Equations[Row, 0] = SourceX;
			Equations[Row, 1] = SourceY;
			Equations[Row, 2] = 1d;
			Equations[Row, 6] = -DestinationX * SourceX;
			Equations[Row, 7] = -DestinationX * SourceY;
			Equations[Row, 8] = DestinationX;

			Equations[Row + 1, 3] = SourceX;
			Equations[Row + 1, 4] = SourceY;
			Equations[Row + 1, 5] = 1d;
			Equations[Row + 1, 6] = -DestinationY * SourceX;
			Equations[Row + 1, 7] = -DestinationY * SourceY;
			Equations[Row + 1, 8] = DestinationY;
		}

		private static double[] SolveLinearSystem(double[,] AugmentedMatrix)
		{
			int Size = 8;
			for (int PivotIndex = 0; PivotIndex < Size; PivotIndex++)
			{
				int BestRow = PivotIndex;
				double BestValue = Math.Abs(AugmentedMatrix[PivotIndex, PivotIndex]);
				for (int Row = PivotIndex + 1; Row < Size; Row++)
				{
					double CandidateValue = Math.Abs(AugmentedMatrix[Row, PivotIndex]);
					if (CandidateValue > BestValue)
					{
						BestValue = CandidateValue;
						BestRow = Row;
					}
				}

				if (BestValue <= double.Epsilon)
					throw new InvalidOperationException("Unable to solve MRZ homography.");

				if (BestRow != PivotIndex)
					SwapRows(AugmentedMatrix, PivotIndex, BestRow);

				double Pivot = AugmentedMatrix[PivotIndex, PivotIndex];
				for (int Column = PivotIndex; Column <= Size; Column++)
					AugmentedMatrix[PivotIndex, Column] /= Pivot;

				for (int Row = 0; Row < Size; Row++)
				{
					if (Row == PivotIndex)
						continue;

					double Factor = AugmentedMatrix[Row, PivotIndex];
					for (int Column = PivotIndex; Column <= Size; Column++)
						AugmentedMatrix[Row, Column] -= Factor * AugmentedMatrix[PivotIndex, Column];
				}
			}

			double[] Result = new double[Size];
			for (int Row = 0; Row < Size; Row++)
				Result[Row] = AugmentedMatrix[Row, Size];

			return Result;
		}

		private static void SwapRows(double[,] Matrix, int LeftRow, int RightRow)
		{
			for (int Column = 0; Column < 9; Column++)
			{
				double Value = Matrix[LeftRow, Column];
				Matrix[LeftRow, Column] = Matrix[RightRow, Column];
				Matrix[RightRow, Column] = Value;
			}
		}

		private static float Distance(CvPoint Left, CvPoint Right)
		{
			float dx = Left.X - Right.X;
			float dy = Left.Y - Right.Y;
			return (float)Math.Sqrt(dx * dx + dy * dy);
		}
	}
}
