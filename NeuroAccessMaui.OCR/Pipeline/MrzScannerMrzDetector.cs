using System.Globalization;
using IdApp.Cv;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NeuroAccessMaui.OCR.Models;
using CvPoint = IdApp.Cv.Basic.Point;
using CvRect = IdApp.Cv.Basic.Rect;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Runs the DocsaidLab MRZScanner ONNX model used to locate MRZ polygons.
	/// </summary>
	public sealed class MrzScannerMrzDetector : IDisposable
	{
		private const string ModelAssetPath = "OcrModels/mrzscanner_detection_20250222_fp32.onnx";
		private const int DefaultInputWidth = 256;
		private const int DefaultInputHeight = 256;
		private const float DefaultHeatmapThreshold = 0.50f;
		private const int MinimumComponentPixels = 12;

		private readonly OnnxModelAssetLoader modelAssetLoader;
		private readonly object sessionSyncRoot = new object();
		private InferenceSession? session;

		/// <summary>
		/// Initializes a new instance of the <see cref="MrzScannerMrzDetector"/> class.
		/// </summary>
		/// <param name="ModelAssetLoader">The packaged model asset loader.</param>
		public MrzScannerMrzDetector(OnnxModelAssetLoader ModelAssetLoader)
		{
			ArgumentNullException.ThrowIfNull(ModelAssetLoader);

			this.modelAssetLoader = ModelAssetLoader;
		}

		/// <summary>
		/// Releases resources owned by the detector.
		/// </summary>
		public void Dispose()
		{
			lock (this.sessionSyncRoot)
			{
				this.session?.Dispose();
				this.session = null;
			}
		}

		internal MrzScannerMrzDetectionResult Detect(IMatrix Image, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Image);
			CancellationToken.ThrowIfCancellationRequested();

			InferenceSession Session = this.GetSession(CancellationToken);
			string InputName = Session.InputMetadata.Keys.First();
			NodeMetadata InputMetadata = Session.InputMetadata[InputName];
			int InputWidth = ResolveInputDimension(InputMetadata, 3, DefaultInputWidth);
			int InputHeight = ResolveInputDimension(InputMetadata, 2, DefaultInputHeight);
			MrzScannerPreprocessInfo PreprocessInfo = CreatePreprocessInfo(Image, InputWidth, InputHeight);
			DenseTensor<float> InputTensor = CreateInputTensor(Image, PreprocessInfo);
			List<NamedOnnxValue> Inputs = new List<NamedOnnxValue>
			{
				NamedOnnxValue.CreateFromTensor(InputName, InputTensor)
			};

			Dictionary<string, string> Metadata = new Dictionary<string, string>
			{
				["OnnxDetector"] = "DocsaidLab.MRZScanner",
				["OnnxDetectorModel"] = "mrz_detection_20250222_fp32.onnx",
				["OnnxDetectorInputName"] = InputName,
				["OnnxDetectorInputWidth"] = InputWidth.ToString(CultureInfo.InvariantCulture),
				["OnnxDetectorInputHeight"] = InputHeight.ToString(CultureInfo.InvariantCulture),
				["OnnxDetectorPadLeft"] = PreprocessInfo.PadLeft.ToString(CultureInfo.InvariantCulture),
				["OnnxDetectorPadTop"] = PreprocessInfo.PadTop.ToString(CultureInfo.InvariantCulture),
				["OnnxDetectorPaddedSize"] = PreprocessInfo.PaddedSize.ToString(CultureInfo.InvariantCulture)
			};

			using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Outputs = Session.Run(Inputs);
			foreach (DisposableNamedOnnxValue Output in Outputs)
			{
				if (Output.Value is not Tensor<float> Tensor)
					continue;

				Metadata["OnnxDetectorOutputName"] = Output.Name;
				Metadata["OnnxDetectorOutputShape"] = string.Join("x", Tensor.Dimensions.ToArray().Select(static Dimension => Dimension.ToString(CultureInfo.InvariantCulture)));
				MrzScannerMrzDetection? Detection = ParseHeatmap(Tensor, Image, PreprocessInfo, Metadata);
				Metadata["OnnxDetectorDetectionCount"] = Detection is null ? "0" : "1";
				return new MrzScannerMrzDetectionResult(Detection, Metadata);
			}

			Metadata["OnnxDetectorDetectionCount"] = "0";
			Metadata["OnnxDetectorFailureReason"] = "NoFloatHeatmapOutput";
			return new MrzScannerMrzDetectionResult(null, Metadata);
		}

		private InferenceSession GetSession(CancellationToken CancellationToken)
		{
			lock (this.sessionSyncRoot)
			{
				if (this.session is not null)
					return this.session;

				OnnxRuntimeNativeLoader.EnsureLoaded();
				string ModelPath = this.modelAssetLoader.GetCachedAssetPath(ModelAssetPath, CancellationToken);
				SessionOptions SessionOptions = new SessionOptions
				{
					GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
				};
				this.session = new InferenceSession(ModelPath, SessionOptions);
				return this.session;
			}
		}

		private static int ResolveInputDimension(NodeMetadata Metadata, int Index, int Fallback)
		{
			int[] Dimensions = Metadata.Dimensions.ToArray();
			if (Dimensions.Length > Index && Dimensions[Index] > 0)
				return Dimensions[Index];

			return Fallback;
		}

		private static MrzScannerPreprocessInfo CreatePreprocessInfo(IMatrix Image, int InputWidth, int InputHeight)
		{
			int PaddedSize = Math.Max(Image.Width, Image.Height);
			int PadLeft = Image.Width < PaddedSize ? (PaddedSize - Image.Width) / 2 : 0;
			int PadTop = Image.Height < PaddedSize ? (PaddedSize - Image.Height) / 2 : 0;
			return new MrzScannerPreprocessInfo(InputWidth, InputHeight, PaddedSize, PadLeft, PadTop);
		}

		private static DenseTensor<float> CreateInputTensor(IMatrix Image, MrzScannerPreprocessInfo PreprocessInfo)
		{
			DenseTensor<float> Tensor = new DenseTensor<float>(new[] { 1, 3, PreprocessInfo.InputHeight, PreprocessInfo.InputWidth });
			for (int y = 0; y < PreprocessInfo.InputHeight; y++)
			{
				float PaddedY = (y + 0.5f) * PreprocessInfo.PaddedSize / PreprocessInfo.InputHeight - 0.5f;
				float SourceY = PaddedY - PreprocessInfo.PadTop;
				for (int x = 0; x < PreprocessInfo.InputWidth; x++)
				{
					float PaddedX = (x + 0.5f) * PreprocessInfo.PaddedSize / PreprocessInfo.InputWidth - 0.5f;
					float SourceX = PaddedX - PreprocessInfo.PadLeft;
					(float Red, float Green, float Blue) = SampleRgb(Image, SourceX, SourceY);
					Tensor[0, 0, y, x] = Red;
					Tensor[0, 1, y, x] = Green;
					Tensor[0, 2, y, x] = Blue;
				}
			}

			return Tensor;
		}

		private static (float Red, float Green, float Blue) SampleRgb(IMatrix Image, float SourceX, float SourceY)
		{
			if (SourceX < 0f || SourceY < 0f || SourceX >= Image.Width || SourceY >= Image.Height)
				return (0f, 0f, 0f);

			int x = Math.Clamp((int)Math.Round(SourceX), 0, Image.Width - 1);
			int y = Math.Clamp((int)Math.Round(SourceY), 0, Image.Height - 1);
			return Image switch
			{
				Matrix<uint> ColorImage => SampleColor(ColorImage, x, y),
				Matrix<byte> ByteImage => SampleGray(ByteImage[x, y] / 255f),
				Matrix<float> FloatImage => SampleGray(Math.Clamp(FloatImage[x, y], 0f, 1f)),
				Matrix<int> FixedImage => SampleGray(Math.Clamp(FixedImage[x, y] / 65535f, 0f, 1f)),
				_ => throw new ArgumentException("Unsupported matrix type: " + Image.GetType().FullName, nameof(Image))
			};
		}

		private static (float Red, float Green, float Blue) SampleColor(Matrix<uint> Image, int x, int y)
		{
			uint Pixel = Image[x, y];
			float Red = (Pixel & 0xff) / 255f;
			float Green = ((Pixel >> 8) & 0xff) / 255f;
			float Blue = ((Pixel >> 16) & 0xff) / 255f;
			return (Red, Green, Blue);
		}

		private static (float Red, float Green, float Blue) SampleGray(float Value)
		{
			return (Value, Value, Value);
		}

		private static MrzScannerMrzDetection? ParseHeatmap(
			Tensor<float> Tensor,
			IMatrix SourceImage,
			MrzScannerPreprocessInfo PreprocessInfo,
			Dictionary<string, string> Metadata)
		{
			int[] Dimensions = Tensor.Dimensions.ToArray();
			if (!TryResolveHeatmapDimensions(Dimensions, out int HeatmapWidth, out int HeatmapHeight))
			{
				Metadata["OnnxDetectorFailureReason"] = "UnsupportedHeatmapShape";
				return null;
			}

			float[] Buffer = Tensor.ToArray();
			float[] Heatmap = ExtractHeatmap(Buffer, Dimensions, HeatmapWidth, HeatmapHeight);
			ConnectedComponent? Component = FindLargestComponent(Heatmap, HeatmapWidth, HeatmapHeight);
			if (Component is null || Component.PixelCount < MinimumComponentPixels)
			{
				Metadata["OnnxDetectorFailureReason"] = "NoMrzHeatmapComponent";
				return null;
			}

			FloatPoint[] HeatmapPolygon = CreateMinimumAreaRectangle(Component.Pixels);
			CvPoint[] Polygon = HeatmapPolygon
				.Select(Point => MapHeatmapPointToSource(Point, HeatmapWidth, HeatmapHeight, SourceImage, PreprocessInfo))
				.ToArray();
			Polygon = OrderPolygon(Polygon);
			CvRect Bounds = CreateBounds(SourceImage, Polygon);
			float Score = Component.ScoreSum / Math.Max(1, Component.PixelCount);

			Metadata["OnnxDetectorHeatmapWidth"] = HeatmapWidth.ToString(CultureInfo.InvariantCulture);
			Metadata["OnnxDetectorHeatmapHeight"] = HeatmapHeight.ToString(CultureInfo.InvariantCulture);
			Metadata["OnnxDetectorHeatmapThreshold"] = DefaultHeatmapThreshold.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata["OnnxDetectorComponentPixels"] = Component.PixelCount.ToString(CultureInfo.InvariantCulture);
			Metadata["OnnxDetectorScore"] = Score.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata["OnnxDetectorMrzBounds"] = FormatBounds(Bounds);
			Metadata["OnnxDetectorMrzPolygon"] = FormatPolygon(Polygon);

			return new MrzScannerMrzDetection(Polygon[0], Polygon[1], Polygon[2], Polygon[3], Bounds, Score);
		}

		private static bool TryResolveHeatmapDimensions(int[] Dimensions, out int HeatmapWidth, out int HeatmapHeight)
		{
			if (Dimensions.Length == 4)
			{
				HeatmapHeight = Dimensions[2];
				HeatmapWidth = Dimensions[3];
				return HeatmapWidth > 0 && HeatmapHeight > 0;
			}

			if (Dimensions.Length == 3)
			{
				HeatmapHeight = Dimensions[1];
				HeatmapWidth = Dimensions[2];
				return HeatmapWidth > 0 && HeatmapHeight > 0;
			}

			if (Dimensions.Length == 2)
			{
				HeatmapHeight = Dimensions[0];
				HeatmapWidth = Dimensions[1];
				return HeatmapWidth > 0 && HeatmapHeight > 0;
			}

			HeatmapWidth = 0;
			HeatmapHeight = 0;
			return false;
		}

		private static float[] ExtractHeatmap(float[] Buffer, int[] Dimensions, int HeatmapWidth, int HeatmapHeight)
		{
			float[] Heatmap = new float[HeatmapWidth * HeatmapHeight];
			int SourceOffset = 0;
			if (Dimensions.Length == 4 && Dimensions[1] > 1)
				SourceOffset = 0;

			Array.Copy(Buffer, SourceOffset, Heatmap, 0, Math.Min(Heatmap.Length, Buffer.Length - SourceOffset));
			return Heatmap;
		}

		private static ConnectedComponent? FindLargestComponent(float[] Heatmap, int Width, int Height)
		{
			bool[] Visited = new bool[Heatmap.Length];
			ConnectedComponent? BestComponent = null;
			int[] Queue = new int[Heatmap.Length];
			for (int Index = 0; Index < Heatmap.Length; Index++)
			{
				if (Visited[Index] || Heatmap[Index] < DefaultHeatmapThreshold)
					continue;

				ConnectedComponent Component = FloodFill(Heatmap, Width, Height, Index, Visited, Queue);
				if (BestComponent is null || Component.PixelCount > BestComponent.PixelCount)
					BestComponent = Component;
			}

			return BestComponent;
		}

		private static ConnectedComponent FloodFill(float[] Heatmap, int Width, int Height, int StartIndex, bool[] Visited, int[] Queue)
		{
			ConnectedComponent Component = new ConnectedComponent();
			int Head = 0;
			int Tail = 0;
			Queue[Tail++] = StartIndex;
			Visited[StartIndex] = true;

			while (Head < Tail)
			{
				int Index = Queue[Head++];
				int x = Index % Width;
				int y = Index / Width;
				float Score = Heatmap[Index];
				Component.Add(new FloatPoint(x + 0.5f, y + 0.5f), Score);

				Enqueue(Heatmap, Width, Height, x - 1, y, Visited, Queue, ref Tail);
				Enqueue(Heatmap, Width, Height, x + 1, y, Visited, Queue, ref Tail);
				Enqueue(Heatmap, Width, Height, x, y - 1, Visited, Queue, ref Tail);
				Enqueue(Heatmap, Width, Height, x, y + 1, Visited, Queue, ref Tail);
			}

			return Component;
		}

		private static void Enqueue(float[] Heatmap, int Width, int Height, int x, int y, bool[] Visited, int[] Queue, ref int Tail)
		{
			if (x < 0 || y < 0 || x >= Width || y >= Height)
				return;

			int Index = y * Width + x;
			if (Visited[Index] || Heatmap[Index] < DefaultHeatmapThreshold)
				return;

			Visited[Index] = true;
			Queue[Tail++] = Index;
		}

		private static FloatPoint[] CreateMinimumAreaRectangle(IReadOnlyList<FloatPoint> Points)
		{
			if (Points.Count == 0)
				return Array.Empty<FloatPoint>();

			double BestArea = double.MaxValue;
			FloatPoint[] BestRectangle = CreateAxisAlignedRectangle(Points);
			for (int Degrees = 0; Degrees < 180; Degrees += 3)
			{
				double Radians = Degrees * Math.PI / 180d;
				float ux = (float)Math.Cos(Radians);
				float uy = (float)Math.Sin(Radians);
				float vx = -uy;
				float vy = ux;
				float MinU = float.MaxValue;
				float MaxU = float.MinValue;
				float MinV = float.MaxValue;
				float MaxV = float.MinValue;
				foreach (FloatPoint Point in Points)
				{
					float u = Point.X * ux + Point.Y * uy;
					float v = Point.X * vx + Point.Y * vy;
					MinU = Math.Min(MinU, u);
					MaxU = Math.Max(MaxU, u);
					MinV = Math.Min(MinV, v);
					MaxV = Math.Max(MaxV, v);
				}

				double Area = Math.Max(0.1f, MaxU - MinU) * Math.Max(0.1f, MaxV - MinV);
				if (Area >= BestArea)
					continue;

				BestArea = Area;
				BestRectangle = new[]
				{
					FromProjection(MinU, MinV, ux, uy, vx, vy),
					FromProjection(MaxU, MinV, ux, uy, vx, vy),
					FromProjection(MaxU, MaxV, ux, uy, vx, vy),
					FromProjection(MinU, MaxV, ux, uy, vx, vy)
				};
			}

			return BestRectangle;
		}

		private static FloatPoint[] CreateAxisAlignedRectangle(IReadOnlyList<FloatPoint> Points)
		{
			float MinX = Points.Min(static Point => Point.X);
			float MaxX = Points.Max(static Point => Point.X);
			float MinY = Points.Min(static Point => Point.Y);
			float MaxY = Points.Max(static Point => Point.Y);
			return new[]
			{
				new FloatPoint(MinX, MinY),
				new FloatPoint(MaxX, MinY),
				new FloatPoint(MaxX, MaxY),
				new FloatPoint(MinX, MaxY)
			};
		}

		private static FloatPoint FromProjection(float u, float v, float ux, float uy, float vx, float vy)
		{
			return new FloatPoint(u * ux + v * vx, u * uy + v * vy);
		}

		private static CvPoint MapHeatmapPointToSource(
			FloatPoint Point,
			int HeatmapWidth,
			int HeatmapHeight,
			IMatrix SourceImage,
			MrzScannerPreprocessInfo PreprocessInfo)
		{
			float PaddedX = Point.X * PreprocessInfo.PaddedSize / Math.Max(1, HeatmapWidth);
			float PaddedY = Point.Y * PreprocessInfo.PaddedSize / Math.Max(1, HeatmapHeight);
			int SourceX = (int)Math.Round(PaddedX - PreprocessInfo.PadLeft);
			int SourceY = (int)Math.Round(PaddedY - PreprocessInfo.PadTop);
			return new CvPoint(
				Math.Clamp(SourceX, 0, Math.Max(0, SourceImage.Width - 1)),
				Math.Clamp(SourceY, 0, Math.Max(0, SourceImage.Height - 1)));
		}

		private static CvPoint[] OrderPolygon(IReadOnlyList<CvPoint> Polygon)
		{
			CvPoint TopLeft = Polygon.OrderBy(static Point => Point.X + Point.Y).First();
			CvPoint BottomRight = Polygon.OrderByDescending(static Point => Point.X + Point.Y).First();
			CvPoint TopRight = Polygon.OrderByDescending(static Point => Point.X - Point.Y).First();
			CvPoint BottomLeft = Polygon.OrderBy(static Point => Point.X - Point.Y).First();
			return new[] { TopLeft, TopRight, BottomRight, BottomLeft };
		}

		private static CvRect CreateBounds(IMatrix Image, IReadOnlyList<CvPoint> Polygon)
		{
			int Left = Polygon.Min(static Point => Point.X);
			int Top = Polygon.Min(static Point => Point.Y);
			int Right = Polygon.Max(static Point => Point.X);
			int Bottom = Polygon.Max(static Point => Point.Y);
			return OcrImagePipelineUtilities.ClampBounds(Image, new CvRect
			{
				Left = Left,
				Top = Top,
				Width = Math.Max(1, Right - Left),
				Height = Math.Max(1, Bottom - Top)
			});
		}

		private static string FormatBounds(CvRect Bounds)
		{
			return Bounds.Left.ToString(CultureInfo.InvariantCulture) + ","
				+ Bounds.Top.ToString(CultureInfo.InvariantCulture) + ","
				+ Bounds.Width.ToString(CultureInfo.InvariantCulture) + ","
				+ Bounds.Height.ToString(CultureInfo.InvariantCulture);
		}

		private static string FormatPolygon(IReadOnlyList<CvPoint> Polygon)
		{
			return string.Join(";", Polygon.Select(static Point => Point.X.ToString(CultureInfo.InvariantCulture) + "," + Point.Y.ToString(CultureInfo.InvariantCulture)));
		}

		private readonly record struct FloatPoint(float X, float Y);

		private readonly record struct MrzScannerPreprocessInfo(int InputWidth, int InputHeight, int PaddedSize, int PadLeft, int PadTop);

		private sealed class ConnectedComponent
		{
			private readonly List<FloatPoint> pixels = new List<FloatPoint>();

			public IReadOnlyList<FloatPoint> Pixels => this.pixels;

			public int PixelCount => this.pixels.Count;

			public float ScoreSum { get; private set; }

			public void Add(FloatPoint Point, float Score)
			{
				this.pixels.Add(Point);
				this.ScoreSum += Score;
			}
		}
	}

	internal sealed class MrzScannerMrzDetectionResult
	{
		private readonly IReadOnlyList<MrzScannerMrzDetection> detections;

		internal MrzScannerMrzDetectionResult(MrzScannerMrzDetection? Detection, IReadOnlyDictionary<string, string> Metadata)
		{
			this.Detection = Detection;
			this.detections = Detection is null
				? Array.Empty<MrzScannerMrzDetection>()
				: new[] { Detection };
			this.Metadata = Metadata;
		}

		public MrzScannerMrzDetection? Detection { get; }

		public IReadOnlyList<MrzScannerMrzDetection> Detections => this.detections;

		public IReadOnlyDictionary<string, string> Metadata { get; }
	}

	internal sealed class MrzScannerMrzDetection
	{
		internal MrzScannerMrzDetection(CvPoint TopLeft, CvPoint TopRight, CvPoint BottomRight, CvPoint BottomLeft, CvRect Bounds, float Score)
		{
			this.TopLeft = TopLeft;
			this.TopRight = TopRight;
			this.BottomRight = BottomRight;
			this.BottomLeft = BottomLeft;
			this.Bounds = Bounds;
			this.Score = Score;
		}

		public CvPoint TopLeft { get; }

		public CvPoint TopRight { get; }

		public CvPoint BottomRight { get; }

		public CvPoint BottomLeft { get; }

		public CvRect Bounds { get; }

		public float Score { get; }
	}
}
