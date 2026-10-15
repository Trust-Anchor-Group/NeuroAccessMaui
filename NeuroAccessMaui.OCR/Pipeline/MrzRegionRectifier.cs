using IdApp.Cv;
using NeuroAccessMaui.OCR.Models;
using CvPoint = IdApp.Cv.Basic.Point;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Warps a quadrilateral of an image into an upright image of a fixed size.
	/// </summary>
	/// <remarks>
	/// Coordinates use the pixel-area convention: pixel <c>(x, y)</c> covers <c>[x, x + 1) × [y, y + 1)</c>, so the quad
	/// corners map to the outer corners of the output image. When the source region is larger than the output, each output
	/// pixel averages a grid of bilinear samples to avoid aliasing the thin MRZ strokes. Areas outside the image are black,
	/// like the padding the detection model was trained with.
	/// </remarks>
	public sealed class MrzRegionRectifier
	{
		private const int maximumSamplesPerAxis = 2;

		/// <summary>
		/// Warps a quadrilateral, given in upright coordinates, into an upright grayscale image.
		/// </summary>
		/// <param name="Image">The image to sample.</param>
		/// <param name="Quad">The quadrilateral in upright image coordinates.</param>
		/// <param name="OutputWidth">The width of the output image.</param>
		/// <param name="OutputHeight">The height of the output image.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The rectified image with values in the 0..1 range.</returns>
		internal Matrix<float> Rectify(
			UprightImage Image,
			DocumentQuad Quad,
			int OutputWidth,
			int OutputHeight,
			CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Image);
			ArgumentNullException.ThrowIfNull(Quad);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(OutputWidth);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(OutputHeight);
			CancellationToken.ThrowIfCancellationRequested();

			// Sampling through the corners mapped to the stored image produces the upright result without rotating pixels.
			double[] Homography = CreateHomography(Image.ToSource(Quad), OutputWidth, OutputHeight);
			float SourceWidth = Math.Max(Distance(Quad.TopLeft, Quad.TopRight), Distance(Quad.BottomLeft, Quad.BottomRight));
			float SourceHeight = Math.Max(Distance(Quad.TopLeft, Quad.BottomLeft), Distance(Quad.TopRight, Quad.BottomRight));
			float DownscaleRatio = Math.Max(SourceWidth / OutputWidth, SourceHeight / OutputHeight);
			int SamplesPerAxis = Math.Clamp((int)MathF.Ceiling(DownscaleRatio), 1, maximumSamplesPerAxis);
			double SampleStep = 1d / SamplesPerAxis;
			float SampleWeight = 1f / (SamplesPerAxis * SamplesPerAxis * 255f);
			SourcePixels Pixels = new SourcePixels(Image.Source);

			// Each pass walks one row of samples, stepping the homography numerators instead of recomputing them.
			float[] Result = new float[OutputWidth * OutputHeight];
			int SamplesPerRow = OutputWidth * SamplesPerAxis;
			double StepX = Homography[0] * SampleStep;
			double StepY = Homography[3] * SampleStep;
			double StepW = Homography[6] * SampleStep;
			for (int y = 0; y < OutputHeight; y++)
			{
				int RowStart = y * OutputWidth;
				for (int sy = 0; sy < SamplesPerAxis; sy++)
				{
					double u = 0.5d * SampleStep;
					double v = y + (sy + 0.5d) * SampleStep;
					double NumeratorX = Homography[0] * u + Homography[1] * v + Homography[2];
					double NumeratorY = Homography[3] * u + Homography[4] * v + Homography[5];
					double Denominator = Homography[6] * u + Homography[7] * v + 1d;
					for (int Sample = 0; Sample < SamplesPerRow; Sample++)
					{
						Result[RowStart + Sample / SamplesPerAxis] += Pixels.SampleBilinear(
							(float)(NumeratorX / Denominator),
							(float)(NumeratorY / Denominator));
						NumeratorX += StepX;
						NumeratorY += StepY;
						Denominator += StepW;
					}
				}

				for (int x = 0; x < OutputWidth; x++)
					Result[RowStart + x] *= SampleWeight;
			}

			return new Matrix<float>(OutputWidth, OutputHeight, Result);
		}

		/// <summary>
		/// Creates a function that maps normalized output coordinates, 0..1 on both axes, to the coordinates of the given
		/// quadrilateral.
		/// </summary>
		/// <param name="Quad">The quadrilateral.</param>
		/// <returns>The mapping function.</returns>
		internal static Func<double, double, (double X, double Y)> CreateUnitSquareMapping(DocumentQuad Quad)
		{
			ArgumentNullException.ThrowIfNull(Quad);

			double[] Homography = CreateHomography(Quad, 1, 1);
			return (u, v) =>
			{
				double w = Homography[6] * u + Homography[7] * v + 1d;
				return ((Homography[0] * u + Homography[1] * v + Homography[2]) / w, (Homography[3] * u + Homography[4] * v + Homography[5]) / w);
			};
		}

		/// <summary>
		/// Holds the pixel layout of an image so that samples avoid per-call property and index calculations.
		/// </summary>
		private readonly struct SourcePixels
		{
			private readonly byte[] data;
			private readonly int origin;
			private readonly int rowSize;
			private readonly int width;
			private readonly int height;

			public SourcePixels(Matrix<byte> Image)
			{
				this.data = Image.Data;
				this.origin = Image.StartIndex(0, 0);
				this.rowSize = Image.RowSize;
				this.width = Image.Width;
				this.height = Image.Height;
			}

			/// <summary>
			/// Samples the image bilinearly at a point in pixel-area coordinates. Points outside the image are black.
			/// </summary>
			public float SampleBilinear(float X, float Y)
			{
				if (X < 0f || Y < 0f || X > this.width || Y > this.height)
					return 0f;

				float SampleX = Math.Clamp(X - 0.5f, 0f, this.width - 1);
				float SampleY = Math.Clamp(Y - 0.5f, 0f, this.height - 1);
				int x0 = (int)SampleX;
				int y0 = (int)SampleY;
				int x1 = Math.Min(x0 + 1, this.width - 1);
				int Row0 = this.origin + y0 * this.rowSize;
				int Row1 = y0 + 1 < this.height ? Row0 + this.rowSize : Row0;
				float fx = SampleX - x0;
				float fy = SampleY - y0;
				byte[] Data = this.data;
				float Top = Data[Row0 + x0] + (Data[Row0 + x1] - Data[Row0 + x0]) * fx;
				float Bottom = Data[Row1 + x0] + (Data[Row1 + x1] - Data[Row1 + x0]) * fx;
				return Top + (Bottom - Top) * fy;
			}
		}

		/// <summary>
		/// Solves the homography that maps output coordinates to source coordinates.
		/// </summary>
		private static double[] CreateHomography(DocumentQuad Quad, int OutputWidth, int OutputHeight)
		{
			double[,] Equations = new double[8, 9];
			AddHomographyEquation(Equations, 0, 0d, 0d, Quad.TopLeft.X, Quad.TopLeft.Y);
			AddHomographyEquation(Equations, 2, OutputWidth, 0d, Quad.TopRight.X, Quad.TopRight.Y);
			AddHomographyEquation(Equations, 4, OutputWidth, OutputHeight, Quad.BottomRight.X, Quad.BottomRight.Y);
			AddHomographyEquation(Equations, 6, 0d, OutputHeight, Quad.BottomLeft.X, Quad.BottomLeft.Y);
			return SolveLinearSystem(Equations);
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
