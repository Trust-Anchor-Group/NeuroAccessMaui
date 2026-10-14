using System.Globalization;
using IdApp.Cv;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NeuroAccessMaui.OCR.Models;
using CvPoint = IdApp.Cv.Basic.Point;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Runs the DocsaidLab MRZScanner ONNX model used to locate MRZ polygons.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The detector searches one square region of the image at a time. The region is warped to the model input, with
	/// black outside the image like the padding the model was trained with, and normalized to 0..1. Searching a square
	/// that just encloses the document makes the MRZ cover many more heatmap cells than letterboxing the whole frame, and
	/// small MRZs are only found at that scale.
	/// </para>
	/// <para>
	/// The heatmap boundary is interpolated between cells and fitted with an exact minimum-area rectangle. The MRZ is
	/// located with a fixed threshold, while the crop is fitted at a threshold relative to the strength of the region,
	/// because the heatmap fades slowly past the ends of the MRZ and the recognizer only reads crops that hug the text.
	/// </para>
	/// </remarks>
	public sealed class MrzScannerMrzDetector : IDisposable
	{
		private const string modelAssetPath = "OcrModels/mrzscanner_detection_20250222_fp32.onnx";
		private const int defaultInputWidth = 256;
		private const int defaultInputHeight = 256;
		private const float detectionThreshold = 0.50f;
		private const int minimumComponentCells = 12;
		private const float strengthPercentile = 0.90f;
		private const float cropThresholdRatio = 0.88f;

		private readonly OnnxModelAssetLoader modelAssetLoader;
		private readonly MrzRegionRectifier rectifier;
		private readonly object sessionSyncRoot = new object();
		private DetectorSession? session;

		/// <summary>
		/// Initializes a new instance of the <see cref="MrzScannerMrzDetector"/> class.
		/// </summary>
		/// <param name="ModelAssetLoader">The packaged model asset loader.</param>
		/// <param name="Rectifier">The rectifier used to warp search regions to the model input.</param>
		public MrzScannerMrzDetector(OnnxModelAssetLoader ModelAssetLoader, MrzRegionRectifier Rectifier)
		{
			ArgumentNullException.ThrowIfNull(ModelAssetLoader);
			ArgumentNullException.ThrowIfNull(Rectifier);

			this.modelAssetLoader = ModelAssetLoader;
			this.rectifier = Rectifier;
		}

		/// <summary>
		/// Releases resources owned by the detector.
		/// </summary>
		public void Dispose()
		{
			lock (this.sessionSyncRoot)
			{
				this.session?.Session.Dispose();
				this.session = null;
			}
		}

		/// <summary>
		/// Locates the MRZ inside a square region of the image.
		/// </summary>
		/// <param name="Image">The image to search.</param>
		/// <param name="Region">The region to search, in upright image coordinates. Parts outside the image are black.</param>
		/// <param name="MetadataPrefix">The prefix of the metadata keys describing this search.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The detection result, with polygons in upright image coordinates.</returns>
		internal MrzScannerMrzDetectionResult Detect(
			UprightImage Image,
			DocumentQuad Region,
			string MetadataPrefix,
			CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Image);
			ArgumentNullException.ThrowIfNull(Region);
			CancellationToken.ThrowIfCancellationRequested();

			DetectorSession Session = this.GetSession(CancellationToken);
			Matrix<float> RegionImage = this.rectifier.Rectify(Image, Region, Session.InputWidth, Session.InputHeight, CancellationToken);
			float[] InputBuffer = CreateInputBuffer(RegionImage, Session.InputWidth, Session.InputHeight);
			DenseTensor<float> InputTensor = new DenseTensor<float>(InputBuffer, new[] { 1, 3, Session.InputHeight, Session.InputWidth });
			List<NamedOnnxValue> Inputs = new List<NamedOnnxValue>
			{
				NamedOnnxValue.CreateFromTensor(Session.InputName, InputTensor)
			};
			Dictionary<string, string> Metadata = new Dictionary<string, string>
			{
				["OnnxDetector"] = "DocsaidLab.MRZScanner",
				["OnnxDetectorModel"] = "mrz_detection_20250222_fp32.onnx",
				[MetadataPrefix + "Region"] = FormatQuad(Region)
			};

			using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Outputs = Session.Session.Run(Inputs);
			foreach (DisposableNamedOnnxValue Output in Outputs)
			{
				if (Output.Value is not Tensor<float> Tensor)
					continue;

				MrzScannerMrzDetection? Detection = ParseHeatmap(Tensor, MrzRegionRectifier.CreateUnitSquareMapping(Region), MetadataPrefix, Metadata);
				return new MrzScannerMrzDetectionResult(Detection, Metadata);
			}

			Metadata[MetadataPrefix + "FailureReason"] = "NoFloatHeatmapOutput";
			return new MrzScannerMrzDetectionResult(null, Metadata);
		}

		private DetectorSession GetSession(CancellationToken CancellationToken)
		{
			// Loaded before taking the lock, since loading may wait for the main thread.
			OnnxRuntimeNativeLoader.EnsureLoaded();
			lock (this.sessionSyncRoot)
			{
				if (this.session is not null)
					return this.session;

				string ModelPath = this.modelAssetLoader.GetCachedAssetPath(modelAssetPath, CancellationToken);
				SessionOptions SessionOptions = new SessionOptions
				{
					GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
				};
				InferenceSession InferenceSession = new InferenceSession(ModelPath, SessionOptions);
				string InputName = InferenceSession.InputMetadata.Keys.First();
				int[] Dimensions = InferenceSession.InputMetadata[InputName].Dimensions.ToArray();
				this.session = new DetectorSession(
					InferenceSession,
					InputName,
					ResolveInputDimension(Dimensions, 3, defaultInputWidth),
					ResolveInputDimension(Dimensions, 2, defaultInputHeight));
				return this.session;
			}
		}

		private static int ResolveInputDimension(int[] Dimensions, int Index, int Fallback)
		{
			if (Dimensions.Length > Index && Dimensions[Index] > 0)
				return Dimensions[Index];

			return Fallback;
		}

		/// <summary>
		/// Copies the region image into a 1x3xHxW buffer, repeating the gray value across the three color planes.
		/// </summary>
		private static float[] CreateInputBuffer(Matrix<float> RegionImage, int InputWidth, int InputHeight)
		{
			int PlaneSize = InputWidth * InputHeight;
			float[] Buffer = new float[PlaneSize * 3];
			int BufferIndex = 0;
			for (int y = 0; y < InputHeight; y++)
			{
				int SourceIndex = RegionImage.StartIndex(0, y);
				for (int x = 0; x < InputWidth; x++)
					Buffer[BufferIndex++] = RegionImage.Data[SourceIndex++];
			}

			Array.Copy(Buffer, 0, Buffer, PlaneSize, PlaneSize);
			Array.Copy(Buffer, 0, Buffer, PlaneSize * 2, PlaneSize);
			return Buffer;
		}

		private static MrzScannerMrzDetection? ParseHeatmap(
			Tensor<float> Tensor,
			Func<double, double, (double X, double Y)> MapToRegion,
			string Prefix,
			Dictionary<string, string> Metadata)
		{
			int[] Dimensions = Tensor.Dimensions.ToArray();
			if (!TryResolveHeatmapDimensions(Dimensions, out int HeatmapWidth, out int HeatmapHeight))
			{
				Metadata[Prefix + "FailureReason"] = "UnsupportedHeatmapShape";
				return null;
			}

			float[] Heatmap = Tensor.ToArray();
			int CellCount = HeatmapWidth * HeatmapHeight;
			if (Heatmap.Length < CellCount)
			{
				Metadata[Prefix + "FailureReason"] = "TruncatedHeatmap";
				return null;
			}

			float Maximum = 0f;
			for (int Index = 0; Index < CellCount; Index++)
				Maximum = Math.Max(Maximum, Heatmap[Index]);
			Metadata[Prefix + "HeatmapMaximum"] = Maximum.ToString("0.000", CultureInfo.InvariantCulture);

			List<int>? Component = FindLargestComponent(Heatmap, HeatmapWidth, HeatmapHeight, detectionThreshold, null, out float ScoreSum);
			if (Component is null || Component.Count < minimumComponentCells)
			{
				Metadata[Prefix + "FailureReason"] = "NoMrzHeatmapComponent";
				return null;
			}

			float Score = ScoreSum / Component.Count;
			float Strength = MeasureStrength(Heatmap, Component);
			DocumentQuad DetectedQuad = FitQuad(Heatmap, HeatmapWidth, HeatmapHeight, Component, detectionThreshold, MapToRegion);

			// The crop is fitted to the strong core of the detected region, which excludes the slowly fading ends.
			float CropThreshold = Math.Max(detectionThreshold, cropThresholdRatio * Strength);
			DocumentQuad CropQuad = DetectedQuad;
			if (CropThreshold > detectionThreshold)
			{
				bool[] InComponent = new bool[CellCount];
				foreach (int Index in Component)
					InComponent[Index] = true;

				List<int>? Core = FindLargestComponent(Heatmap, HeatmapWidth, HeatmapHeight, CropThreshold, InComponent, out _);
				if (Core is not null && Core.Count >= minimumComponentCells)
					CropQuad = FitQuad(Heatmap, HeatmapWidth, HeatmapHeight, Core, CropThreshold, MapToRegion);
			}

			Metadata[Prefix + "ComponentCells"] = Component.Count.ToString(CultureInfo.InvariantCulture);
			Metadata[Prefix + "Score"] = Score.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "Strength"] = Strength.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "CropThreshold"] = CropThreshold.ToString("0.000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "DetectedPolygon"] = FormatQuad(DetectedQuad);
			Metadata[Prefix + "MrzPolygon"] = FormatQuad(CropQuad);
			Metadata[Prefix + "MrzAngleDegrees"] = (Math.Atan2(
				CropQuad.TopRight.Y - CropQuad.TopLeft.Y,
				CropQuad.TopRight.X - CropQuad.TopLeft.X) * 180d / Math.PI).ToString("0.00", CultureInfo.InvariantCulture);

			return new MrzScannerMrzDetection(DetectedQuad, CropQuad, Score);
		}

		/// <summary>
		/// Measures the heatmap level reached by the strongest part of the component, ignoring the few highest cells.
		/// </summary>
		private static float MeasureStrength(float[] Heatmap, List<int> Component)
		{
			float[] Values = new float[Component.Count];
			for (int Index = 0; Index < Values.Length; Index++)
				Values[Index] = Heatmap[Component[Index]];

			Array.Sort(Values);
			return Values[(int)((Values.Length - 1) * strengthPercentile)];
		}

		/// <summary>
		/// Fits a text-aligned rectangle around the cells of a component and maps it from heatmap to image coordinates.
		/// </summary>
		private static DocumentQuad FitQuad(
			float[] Heatmap,
			int HeatmapWidth,
			int HeatmapHeight,
			List<int> Component,
			float Threshold,
			Func<double, double, (double X, double Y)> MapToRegion)
		{
			List<HeatmapPoint> Boundary = CollectBoundaryPoints(Heatmap, HeatmapWidth, HeatmapHeight, Component, Threshold);
			List<HeatmapPoint> Hull = ComputeConvexHull(Boundary);
			HeatmapPoint[] Corners = FitTextAlignedRectangle(Hull);
			CvPoint[] Polygon = Corners
				.Select(Corner =>
				{
					(double X, double Y) = MapToRegion(Corner.X / (double)HeatmapWidth, Corner.Y / (double)HeatmapHeight);
					return new CvPoint((int)Math.Round(X), (int)Math.Round(Y));
				})
				.ToArray();

			return new DocumentQuad(Polygon[0], Polygon[1], Polygon[2], Polygon[3]);
		}

		private static string FormatQuad(DocumentQuad Quad)
		{
			return string.Join(";", new[] { Quad.TopLeft, Quad.TopRight, Quad.BottomRight, Quad.BottomLeft }
				.Select(static Point => Point.X.ToString(CultureInfo.InvariantCulture) + "," + Point.Y.ToString(CultureInfo.InvariantCulture)));
		}

		private static bool TryResolveHeatmapDimensions(int[] Dimensions, out int HeatmapWidth, out int HeatmapHeight)
		{
			if (Dimensions.Length >= 2)
			{
				HeatmapHeight = Dimensions[^2];
				HeatmapWidth = Dimensions[^1];
				return HeatmapWidth > 0 && HeatmapHeight > 0;
			}

			HeatmapWidth = 0;
			HeatmapHeight = 0;
			return false;
		}

		/// <summary>
		/// Finds the largest four-connected group of cells at or above the threshold, optionally limited to allowed cells.
		/// </summary>
		private static List<int>? FindLargestComponent(
			float[] Heatmap,
			int Width,
			int Height,
			float Threshold,
			bool[]? Allowed,
			out float BestScoreSum)
		{
			int CellCount = Width * Height;
			bool[] Visited = new bool[CellCount];
			int[] Queue = new int[CellCount];
			List<int>? BestComponent = null;
			BestScoreSum = 0f;

			for (int StartIndex = 0; StartIndex < CellCount; StartIndex++)
			{
				if (Visited[StartIndex] || !IsInside(Heatmap, StartIndex, Threshold, Allowed))
					continue;

				List<int> Component = new List<int>();
				float ScoreSum = 0f;
				int Head = 0;
				int Tail = 0;
				Queue[Tail++] = StartIndex;
				Visited[StartIndex] = true;

				while (Head < Tail)
				{
					int Index = Queue[Head++];
					Component.Add(Index);
					ScoreSum += Heatmap[Index];
					int x = Index % Width;
					int y = Index / Width;
					if (x > 0)
						Enqueue(Heatmap, Index - 1, Threshold, Allowed, Visited, Queue, ref Tail);
					if (x < Width - 1)
						Enqueue(Heatmap, Index + 1, Threshold, Allowed, Visited, Queue, ref Tail);
					if (y > 0)
						Enqueue(Heatmap, Index - Width, Threshold, Allowed, Visited, Queue, ref Tail);
					if (y < Height - 1)
						Enqueue(Heatmap, Index + Width, Threshold, Allowed, Visited, Queue, ref Tail);
				}

				if (BestComponent is null || Component.Count > BestComponent.Count)
				{
					BestComponent = Component;
					BestScoreSum = ScoreSum;
				}
			}

			return BestComponent;
		}

		private static bool IsInside(float[] Heatmap, int Index, float Threshold, bool[]? Allowed)
		{
			return Heatmap[Index] >= Threshold && (Allowed is null || Allowed[Index]);
		}

		private static void Enqueue(float[] Heatmap, int Index, float Threshold, bool[]? Allowed, bool[] Visited, int[] Queue, ref int Tail)
		{
			if (Visited[Index] || !IsInside(Heatmap, Index, Threshold, Allowed))
				return;

			Visited[Index] = true;
			Queue[Tail++] = Index;
		}

		/// <summary>
		/// Places a point where the heatmap crosses the threshold on every side of a component cell that faces a cell
		/// below the threshold, interpolating linearly between the two cell centers. Sides on the heatmap border use the
		/// cell edge.
		/// </summary>
		private static List<HeatmapPoint> CollectBoundaryPoints(float[] Heatmap, int Width, int Height, List<int> Component, float Threshold)
		{
			List<HeatmapPoint> Points = new List<HeatmapPoint>();
			foreach (int Index in Component)
			{
				int x = Index % Width;
				int y = Index / Width;
				float CenterX = x + 0.5f;
				float CenterY = y + 0.5f;
				float Value = Heatmap[Index];

				if (x == 0)
					Points.Add(new HeatmapPoint(x, CenterY));
				else if (Heatmap[Index - 1] < Threshold)
					Points.Add(new HeatmapPoint(CenterX - CrossingOffset(Value, Heatmap[Index - 1], Threshold), CenterY));

				if (x == Width - 1)
					Points.Add(new HeatmapPoint(x + 1, CenterY));
				else if (Heatmap[Index + 1] < Threshold)
					Points.Add(new HeatmapPoint(CenterX + CrossingOffset(Value, Heatmap[Index + 1], Threshold), CenterY));

				if (y == 0)
					Points.Add(new HeatmapPoint(CenterX, y));
				else if (Heatmap[Index - Width] < Threshold)
					Points.Add(new HeatmapPoint(CenterX, CenterY - CrossingOffset(Value, Heatmap[Index - Width], Threshold)));

				if (y == Height - 1)
					Points.Add(new HeatmapPoint(CenterX, y + 1));
				else if (Heatmap[Index + Width] < Threshold)
					Points.Add(new HeatmapPoint(CenterX, CenterY + CrossingOffset(Value, Heatmap[Index + Width], Threshold)));
			}

			return Points;
		}

		private static float CrossingOffset(float InsideValue, float OutsideValue, float Threshold)
		{
			float Difference = InsideValue - OutsideValue;
			if (Difference <= float.Epsilon)
				return 0.5f;

			return Math.Clamp((InsideValue - Threshold) / Difference, 0f, 1f);
		}

		/// <summary>
		/// Computes the convex hull of the points using Andrew's monotone chain algorithm.
		/// </summary>
		private static List<HeatmapPoint> ComputeConvexHull(List<HeatmapPoint> Points)
		{
			List<HeatmapPoint> Sorted = Points
				.Distinct()
				.OrderBy(static Point => Point.X)
				.ThenBy(static Point => Point.Y)
				.ToList();
			if (Sorted.Count < 3)
				return Sorted;

			List<HeatmapPoint> Hull = new List<HeatmapPoint>(Sorted.Count * 2);
			foreach (HeatmapPoint Point in Sorted)
			{
				while (Hull.Count >= 2 && Cross(Hull[^2], Hull[^1], Point) <= 0f)
					Hull.RemoveAt(Hull.Count - 1);
				Hull.Add(Point);
			}

			int LowerCount = Hull.Count + 1;
			for (int Index = Sorted.Count - 2; Index >= 0; Index--)
			{
				HeatmapPoint Point = Sorted[Index];
				while (Hull.Count >= LowerCount && Cross(Hull[^2], Hull[^1], Point) <= 0f)
					Hull.RemoveAt(Hull.Count - 1);
				Hull.Add(Point);
			}

			Hull.RemoveAt(Hull.Count - 1);
			return Hull;
		}

		private static float Cross(HeatmapPoint Origin, HeatmapPoint First, HeatmapPoint Second)
		{
			return (First.X - Origin.X) * (Second.Y - Origin.Y) - (First.Y - Origin.Y) * (Second.X - Origin.X);
		}

		/// <summary>
		/// Finds the minimum-area rectangle around the hull (one side is always collinear with a hull edge) and returns its
		/// corners ordered top-left, top-right, bottom-right, bottom-left, with the long side taken as the text direction.
		/// </summary>
		private static HeatmapPoint[] FitTextAlignedRectangle(List<HeatmapPoint> Hull)
		{
			float AxisX = 1f;
			float AxisY = 0f;
			double BestArea = double.MaxValue;
			for (int Index = 0; Index < Hull.Count; Index++)
			{
				HeatmapPoint Start = Hull[Index];
				HeatmapPoint End = Hull[(Index + 1) % Hull.Count];
				float EdgeX = End.X - Start.X;
				float EdgeY = End.Y - Start.Y;
				float EdgeLength = MathF.Sqrt(EdgeX * EdgeX + EdgeY * EdgeY);
				if (EdgeLength < 1e-6f)
					continue;

				float ux = EdgeX / EdgeLength;
				float uy = EdgeY / EdgeLength;
				Extents Extents = MeasureExtents(Hull, ux, uy);
				double Area = (double)(Extents.MaximumAlong - Extents.MinimumAlong) * (Extents.MaximumAcross - Extents.MinimumAcross);
				if (Area < BestArea)
				{
					BestArea = Area;
					AxisX = ux;
					AxisY = uy;
				}
			}

			Extents BestExtents = MeasureExtents(Hull, AxisX, AxisY);
			if (BestExtents.MaximumAcross - BestExtents.MinimumAcross > BestExtents.MaximumAlong - BestExtents.MinimumAlong)
			{
				float PreviousAxisX = AxisX;
				AxisX = -AxisY;
				AxisY = PreviousAxisX;
			}

			if (AxisX < 0f || (AxisX == 0f && AxisY < 0f))
			{
				AxisX = -AxisX;
				AxisY = -AxisY;
			}

			// The across axis is the text axis rotated a quarter turn clockwise, which points down the image.
			float AcrossX = -AxisY;
			float AcrossY = AxisX;
			Extents TextExtents = MeasureExtents(Hull, AxisX, AxisY);
			return new[]
			{
				FromExtents(TextExtents.MinimumAlong, TextExtents.MinimumAcross, AxisX, AxisY, AcrossX, AcrossY),
				FromExtents(TextExtents.MaximumAlong, TextExtents.MinimumAcross, AxisX, AxisY, AcrossX, AcrossY),
				FromExtents(TextExtents.MaximumAlong, TextExtents.MaximumAcross, AxisX, AxisY, AcrossX, AcrossY),
				FromExtents(TextExtents.MinimumAlong, TextExtents.MaximumAcross, AxisX, AxisY, AcrossX, AcrossY)
			};
		}

		private static Extents MeasureExtents(List<HeatmapPoint> Points, float AxisX, float AxisY)
		{
			float AcrossX = -AxisY;
			float AcrossY = AxisX;
			float MinimumAlong = float.MaxValue;
			float MaximumAlong = float.MinValue;
			float MinimumAcross = float.MaxValue;
			float MaximumAcross = float.MinValue;
			foreach (HeatmapPoint Point in Points)
			{
				float Along = Point.X * AxisX + Point.Y * AxisY;
				float Across = Point.X * AcrossX + Point.Y * AcrossY;
				MinimumAlong = Math.Min(MinimumAlong, Along);
				MaximumAlong = Math.Max(MaximumAlong, Along);
				MinimumAcross = Math.Min(MinimumAcross, Across);
				MaximumAcross = Math.Max(MaximumAcross, Across);
			}

			return new Extents(MinimumAlong, MaximumAlong, MinimumAcross, MaximumAcross);
		}

		private static HeatmapPoint FromExtents(float Along, float Across, float AxisX, float AxisY, float AcrossX, float AcrossY)
		{
			return new HeatmapPoint(Along * AxisX + Across * AcrossX, Along * AxisY + Across * AcrossY);
		}

		private sealed record DetectorSession(InferenceSession Session, string InputName, int InputWidth, int InputHeight);

		private readonly record struct HeatmapPoint(float X, float Y);

		private readonly record struct Extents(float MinimumAlong, float MaximumAlong, float MinimumAcross, float MaximumAcross);
	}

	internal sealed class MrzScannerMrzDetectionResult
	{
		internal MrzScannerMrzDetectionResult(MrzScannerMrzDetection? Detection, IReadOnlyDictionary<string, string> Metadata)
		{
			this.Detection = Detection;
			this.Metadata = Metadata;
		}

		public MrzScannerMrzDetection? Detection { get; }

		public IReadOnlyDictionary<string, string> Metadata { get; }
	}

	/// <summary>
	/// Represents a located MRZ.
	/// </summary>
	internal sealed class MrzScannerMrzDetection
	{
		internal MrzScannerMrzDetection(DocumentQuad DetectedQuad, DocumentQuad CropQuad, float Score)
		{
			this.DetectedQuad = DetectedQuad;
			this.CropQuad = CropQuad;
			this.Score = Score;
		}

		/// <summary>
		/// Gets the whole detected region, fitted at the detection threshold, in upright image coordinates.
		/// </summary>
		public DocumentQuad DetectedQuad { get; }

		/// <summary>
		/// Gets the crop to read the MRZ from, fitted tightly around the text, in upright image coordinates.
		/// </summary>
		public DocumentQuad CropQuad { get; }

		/// <summary>
		/// Gets the mean heatmap score of the detected region.
		/// </summary>
		public float Score { get; }
	}
}
