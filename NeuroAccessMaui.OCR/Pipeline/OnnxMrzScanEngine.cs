using System.Diagnostics;
using System.Globalization;
using IdApp.Cv;
using NeuroAccessMaui.OCR.Models;
using CvPoint = IdApp.Cv.Basic.Point;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Locates and reads the MRZ in a single image: MRZScanner detection in two passes, rectification from the
	/// full-resolution image, MRZScanner recognition and a decode with check-digit validation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Live scanning and still-image scanning share this path. Whether a read is trustworthy is decided by its check
	/// digits, never by image-quality heuristics; quality signals only choose the guidance shown when a read fails.
	/// </para>
	/// <para>
	/// The first detection pass searches centered squares, then the whole image, because the model only responds to an MRZ
	/// that covers enough of its input. The second pass searches a square around the first detection, where the MRZ fills
	/// most of the model input, and yields the tight crop the recognizer needs. The image is never rotated; regions are
	/// expressed in upright coordinates and sampled from the image as stored.
	/// </para>
	/// </remarks>
	public sealed class OnnxMrzScanEngine
	{
		/// <summary>
		/// The provider identifier reported in scan results produced by this engine.
		/// </summary>
		internal const string ProviderIdentifier = "onnx-mrz";

		private const float minimumMrzAspectRatio = 2.5f;
		private const float maximumMrzAspectRatio = 20f;
		private const float td1MaximumAspectRatio = 6.2f;
		private const float minimumReadableLineHeight = 8f;
		private const float comfortableLineHeight = 14f;
		private const float saturatedPixelThreshold = 0.97f;
		private const float maximumSaturatedRatio = 0.10f;
		private const float refinementRegionScale = 1.3f;

		/// <summary>
		/// Sides of the centered squares searched before the whole image, as fractions of the shorter image side.
		/// </summary>
		private static readonly float[] centeredSearchFractions = { 1f, 0.65f };

		private readonly MrzScannerMrzDetector detector;
		private readonly MrzRegionRectifier rectifier;
		private readonly MrzScannerMrzRecognizer recognizer;

		/// <summary>
		/// Initializes a new instance of the <see cref="OnnxMrzScanEngine"/> class.
		/// </summary>
		/// <param name="Detector">The MRZScanner detector.</param>
		/// <param name="Rectifier">The MRZ region rectifier.</param>
		/// <param name="Recognizer">The MRZScanner recognizer.</param>
		public OnnxMrzScanEngine(
			MrzScannerMrzDetector Detector,
			MrzRegionRectifier Rectifier,
			MrzScannerMrzRecognizer Recognizer)
		{
			ArgumentNullException.ThrowIfNull(Detector);
			ArgumentNullException.ThrowIfNull(Rectifier);
			ArgumentNullException.ThrowIfNull(Recognizer);

			this.detector = Detector;
			this.rectifier = Rectifier;
			this.recognizer = Recognizer;
		}

		/// <summary>
		/// Locates and reads the MRZ in one image.
		/// </summary>
		/// <param name="Image">The source image.</param>
		/// <param name="SourceRotationDegrees">The clockwise rotation that makes the image upright.</param>
		/// <param name="DocumentKindHint">The expected document kind, used only to break ties between MRZ layouts.</param>
		/// <param name="RequestMetadata">Metadata supplied by the caller.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The processing result.</returns>
		internal MrzFrameProcessingResult ProcessFrame(
			IMatrix Image,
			int SourceRotationDegrees,
			OcrDocumentKindHint DocumentKindHint,
			IReadOnlyDictionary<string, string> RequestMetadata,
			CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Image);
			ArgumentNullException.ThrowIfNull(RequestMetadata);
			CancellationToken.ThrowIfCancellationRequested();

			Stopwatch Stopwatch = Stopwatch.StartNew();
			Dictionary<string, string> Metadata = new Dictionary<string, string>(RequestMetadata)
			{
				["ProviderId"] = ProviderIdentifier,
				["MrzPipeline"] = "SingleFrameRead"
			};
			int ImageWidth = Image.Width;
			int ImageHeight = Image.Height;

			try
			{
				UprightImage Upright = new UprightImage(OcrImagePipelineUtilities.ToGrayScaleBytes(Image), SourceRotationDegrees);
				ImageWidth = Upright.Width;
				ImageHeight = Upright.Height;
				Metadata["MrzImageWidth"] = ImageWidth.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzImageHeight"] = ImageHeight.ToString(CultureInfo.InvariantCulture);

				long DetectionStarted = Stopwatch.ElapsedMilliseconds;
				MrzScannerMrzDetection? Detection = this.Locate(Upright, Metadata, CancellationToken);
				Metadata["MrzDetectionElapsedMs"] = (Stopwatch.ElapsedMilliseconds - DetectionStarted).ToString(CultureInfo.InvariantCulture);
				if (Detection is null)
				{
					return CreateResult(MrzCaptureGuidanceState.FindPage, MrzScanFailureCategory.MrzRegionNotFound,
						ImageWidth, ImageHeight, null, 0f, null, null, null, false, Metadata, Stopwatch);
				}

				long RefinementStarted = Stopwatch.ElapsedMilliseconds;
				MrzScannerMrzDetectionResult Refinement = this.detector.Detect(
					Upright,
					CreateRefinementRegion(Detection.DetectedQuad),
					"OnnxDetectorRefinement",
					CancellationToken);
				OcrImagePipelineUtilities.MergeMetadata(Metadata, Refinement.Metadata);
				Metadata["MrzRefinementElapsedMs"] = (Stopwatch.ElapsedMilliseconds - RefinementStarted).ToString(CultureInfo.InvariantCulture);
				Metadata["MrzRefined"] = (Refinement.Detection is not null).ToString();

				DocumentQuad Quad = (Refinement.Detection ?? Detection).CropQuad;
				MrzGeometry Geometry = MeasureGeometry(Quad);
				Metadata["MrzRegionWidth"] = Geometry.Width.ToString("0.0", CultureInfo.InvariantCulture);
				Metadata["MrzRegionHeight"] = Geometry.Height.ToString("0.0", CultureInfo.InvariantCulture);
				Metadata["MrzRegionAspectRatio"] = Geometry.AspectRatio.ToString("0.000", CultureInfo.InvariantCulture);
				Metadata["MrzEstimatedLineHeight"] = Geometry.LineHeight.ToString("0.0", CultureInfo.InvariantCulture);

				if (Geometry.Height < 1f
					|| Geometry.AspectRatio < minimumMrzAspectRatio
					|| Geometry.AspectRatio > maximumMrzAspectRatio)
				{
					Metadata["MrzSkippedReason"] = "ImplausibleGeometry";
					return CreateResult(MrzCaptureGuidanceState.FindPage, MrzScanFailureCategory.MrzRegionNotFound,
						ImageWidth, ImageHeight, null, Detection.Score, null, null, null, false, Metadata, Stopwatch);
				}

				if (Geometry.LineHeight < minimumReadableLineHeight)
				{
					Metadata["MrzSkippedReason"] = "TextTooSmall";
					return CreateResult(MrzCaptureGuidanceState.MoveCloser, MrzScanFailureCategory.MrzTooSmall,
						ImageWidth, ImageHeight, Quad, Detection.Score, null, null, null, false, Metadata, Stopwatch);
				}

				long RecognitionStarted = Stopwatch.ElapsedMilliseconds;
				(int InputWidth, int InputHeight) = this.recognizer.GetInputSize(CancellationToken);
				Matrix<float> Crop = this.rectifier.Rectify(Upright, Quad, InputWidth, InputHeight, CancellationToken);
				float SaturatedRatio = OcrImagePipelineUtilities.MeasureSaturatedRatio(Crop, saturatedPixelThreshold);
				OcrImagePipelineUtilities.StretchContrast(Crop);
				OcrTextRecognitionResult Recognition = this.recognizer.Recognize(Crop, CancellationToken);
				Metadata["MrzRecognitionElapsedMs"] = (Stopwatch.ElapsedMilliseconds - RecognitionStarted).ToString(CultureInfo.InvariantCulture);
				Metadata["MrzCropSaturatedRatio"] = SaturatedRatio.ToString("0.000000", CultureInfo.InvariantCulture);
				OcrImagePipelineUtilities.MergeMetadata(Metadata, Recognition.Metadata);
				Metadata["MrzRecognitionRawText"] = Recognition.RawText;

				long DecodeStarted = Stopwatch.ElapsedMilliseconds;
				MrzFrameRead? Read = MrzReadDecoder.Decode(Recognition, DocumentKindHint);
				Metadata["MrzDecodeElapsedMs"] = (Stopwatch.ElapsedMilliseconds - DecodeStarted).ToString(CultureInfo.InvariantCulture);
				AddReadMetadata(Metadata, Read);
				(MrzCaptureGuidanceState GuidanceState, MrzScanFailureCategory FailureCategory) =
					ResolveGuidance(Read, SaturatedRatio, Geometry.LineHeight);

				return CreateResult(GuidanceState, FailureCategory, ImageWidth, ImageHeight, Quad, Detection.Score,
					Read, Crop, Recognition, false, Metadata, Stopwatch);
			}
			catch (Exception Exception) when (Exception is not OperationCanceledException)
			{
				Metadata["MrzModelFailureType"] = Exception.GetType().Name;
				Metadata["MrzModelFailureReason"] = Exception.Message;
				return CreateResult(MrzCaptureGuidanceState.FindPage, MrzScanFailureCategory.OcrNoText,
					ImageWidth, ImageHeight, null, 0f, null, null, null, true, Metadata, Stopwatch);
			}
		}

		/// <summary>
		/// Searches centered squares of decreasing size, then a square covering the whole image, and returns the first
		/// detection. Documents are usually held near the center, and a smaller square makes a distant MRZ large enough
		/// for the model to respond to it.
		/// </summary>
		private MrzScannerMrzDetection? Locate(UprightImage Image, Dictionary<string, string> Metadata, CancellationToken CancellationToken)
		{
			int ShorterSide = Math.Min(Image.Width, Image.Height);
			IEnumerable<float> Sides = centeredSearchFractions
				.Select(Fraction => ShorterSide * Fraction)
				.Append(Math.Max(Image.Width, Image.Height));
			int Passes = 0;
			MrzScannerMrzDetectionResult? Result = null;
			foreach (float Side in Sides)
			{
				Passes++;
				Result = this.detector.Detect(Image, CreateCenteredSquare(Image.Width, Image.Height, Side), "OnnxDetector", CancellationToken);
				if (Result.Detection is not null)
					break;
			}

			if (Result is not null)
				OcrImagePipelineUtilities.MergeMetadata(Metadata, Result.Metadata);

			Metadata["MrzDetectionPasses"] = Passes.ToString(CultureInfo.InvariantCulture);
			return Result?.Detection;
		}

		/// <summary>
		/// Creates an axis-aligned square with the given side, centered in the image.
		/// </summary>
		private static DocumentQuad CreateCenteredSquare(int ImageWidth, int ImageHeight, float Side)
		{
			int Left = (int)MathF.Round((ImageWidth - Side) / 2f);
			int Top = (int)MathF.Round((ImageHeight - Side) / 2f);
			int Right = (int)MathF.Round((ImageWidth + Side) / 2f);
			int Bottom = (int)MathF.Round((ImageHeight + Side) / 2f);
			return new DocumentQuad(new CvPoint(Left, Top), new CvPoint(Right, Top), new CvPoint(Right, Bottom), new CvPoint(Left, Bottom));
		}

		/// <summary>
		/// Creates the square searched by the refinement pass: centered on the detected MRZ, aligned with its text
		/// direction, with sides a little longer than the MRZ is wide.
		/// </summary>
		private static DocumentQuad CreateRefinementRegion(DocumentQuad Quad)
		{
			float CenterX = (Quad.TopLeft.X + Quad.TopRight.X + Quad.BottomRight.X + Quad.BottomLeft.X) / 4f;
			float CenterY = (Quad.TopLeft.Y + Quad.TopRight.Y + Quad.BottomRight.Y + Quad.BottomLeft.Y) / 4f;
			float AxisX = (Quad.TopRight.X - Quad.TopLeft.X + Quad.BottomRight.X - Quad.BottomLeft.X) / 2f;
			float AxisY = (Quad.TopRight.Y - Quad.TopLeft.Y + Quad.BottomRight.Y - Quad.BottomLeft.Y) / 2f;
			float Width = MathF.Sqrt(AxisX * AxisX + AxisY * AxisY);
			if (Width < 1f)
				return Quad;

			float Half = refinementRegionScale * Width / 2f;
			float AlongX = AxisX / Width * Half;
			float AlongY = AxisY / Width * Half;
			float AcrossX = -AlongY;
			float AcrossY = AlongX;
			return new DocumentQuad(
				new CvPoint((int)MathF.Round(CenterX - AlongX - AcrossX), (int)MathF.Round(CenterY - AlongY - AcrossY)),
				new CvPoint((int)MathF.Round(CenterX + AlongX - AcrossX), (int)MathF.Round(CenterY + AlongY - AcrossY)),
				new CvPoint((int)MathF.Round(CenterX + AlongX + AcrossX), (int)MathF.Round(CenterY + AlongY + AcrossY)),
				new CvPoint((int)MathF.Round(CenterX - AlongX + AcrossX), (int)MathF.Round(CenterY - AlongY + AcrossY)));
		}

		/// <summary>
		/// Chooses the guidance for a frame whose MRZ was located and read. Image-quality signals only explain a failed
		/// read; a read that validates always shows progress.
		/// </summary>
		private static (MrzCaptureGuidanceState GuidanceState, MrzScanFailureCategory FailureCategory) ResolveGuidance(
			MrzFrameRead? Read,
			float SaturatedRatio,
			float LineHeight)
		{
			if (Read is not null && Read.IsStrictSuccess)
				return (MrzCaptureGuidanceState.HoldSteady, MrzScanFailureCategory.None);
			if (SaturatedRatio > maximumSaturatedRatio)
				return (MrzCaptureGuidanceState.ReduceGlare, MrzScanFailureCategory.GlareInMrzArea);
			if (LineHeight < comfortableLineHeight)
				return (MrzCaptureGuidanceState.MoveCloser, MrzScanFailureCategory.MrzTooSmall);

			return (MrzCaptureGuidanceState.HoldSteady, Read is null ? MrzScanFailureCategory.OcrNoText : MrzScanFailureCategory.MrzChecksumFailed);
		}

		/// <summary>
		/// Measures the detected region. The line count is estimated from the aspect ratio, since three-line TD1 zones are
		/// noticeably less elongated than two-line TD2 and TD3 zones.
		/// </summary>
		private static MrzGeometry MeasureGeometry(DocumentQuad Quad)
		{
			float Width = (Distance(Quad.TopLeft, Quad.TopRight) + Distance(Quad.BottomLeft, Quad.BottomRight)) * 0.5f;
			float Height = (Distance(Quad.TopLeft, Quad.BottomLeft) + Distance(Quad.TopRight, Quad.BottomRight)) * 0.5f;
			float AspectRatio = Width / Math.Max(1f, Height);
			int LineCount = AspectRatio < td1MaximumAspectRatio ? 3 : 2;
			return new MrzGeometry(Width, Height, AspectRatio, Height / LineCount);
		}

		private static void AddReadMetadata(Dictionary<string, string> Metadata, MrzFrameRead? Read)
		{
			Metadata["MrzReadDecoded"] = (Read is not null).ToString();
			if (Read is null)
				return;

			OcrImagePipelineUtilities.MergeMetadata(Metadata, Read.Metadata);
			Metadata["MrzReadFormat"] = Read.Format.ToString();
			Metadata["MrzReadStrictSuccess"] = Read.IsStrictSuccess.ToString();
			Metadata["MrzReadPassedCheckCount"] = Read.PassedCheckCount.ToString(CultureInfo.InvariantCulture);
			Metadata["MrzReadRepairCount"] = Read.RepairCount.ToString(CultureInfo.InvariantCulture);
		}

		private static MrzFrameProcessingResult CreateResult(
			MrzCaptureGuidanceState GuidanceState,
			MrzScanFailureCategory FailureCategory,
			int ImageWidth,
			int ImageHeight,
			DocumentQuad? Quad,
			float DetectionScore,
			MrzFrameRead? Read,
			Matrix<float>? RecognitionInput,
			OcrTextRecognitionResult? Recognition,
			bool IsModelFailure,
			Dictionary<string, string> Metadata,
			Stopwatch Stopwatch)
		{
			Stopwatch.Stop();
			Metadata["MrzTotalElapsedMs"] = Stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
			Metadata["MrzGuidanceState"] = GuidanceState.ToString();
			Metadata["MrzFailureCategory"] = FailureCategory.ToString();

			MrzPreviewAnalysisResult Analysis = new MrzPreviewAnalysisResult(
				GuidanceState,
				FailureCategory,
				ImageWidth,
				ImageHeight,
				Quad,
				DetectionScore,
				Read,
				Metadata);
			return new MrzFrameProcessingResult(Analysis, RecognitionInput, Recognition, IsModelFailure);
		}

		private static float Distance(CvPoint Left, CvPoint Right)
		{
			float dx = Left.X - Right.X;
			float dy = Left.Y - Right.Y;
			return MathF.Sqrt(dx * dx + dy * dy);
		}

		private readonly record struct MrzGeometry(float Width, float Height, float AspectRatio, float LineHeight);
	}

	/// <summary>
	/// Holds the result of processing one image, including the intermediate data used for debug artifacts.
	/// </summary>
	internal sealed class MrzFrameProcessingResult
	{
		internal MrzFrameProcessingResult(
			MrzPreviewAnalysisResult Analysis,
			Matrix<float>? RecognitionInput,
			OcrTextRecognitionResult? Recognition,
			bool IsModelFailure)
		{
			this.Analysis = Analysis;
			this.RecognitionInput = RecognitionInput;
			this.Recognition = Recognition;
			this.IsModelFailure = IsModelFailure;
		}

		/// <summary>
		/// Gets the analysis result reported to callers.
		/// </summary>
		public MrzPreviewAnalysisResult Analysis { get; }

		/// <summary>
		/// Gets the rectified MRZ image passed to the recognizer, if recognition ran.
		/// </summary>
		public Matrix<float>? RecognitionInput { get; }

		/// <summary>
		/// Gets the raw recognizer output, if recognition ran.
		/// </summary>
		public OcrTextRecognitionResult? Recognition { get; }

		/// <summary>
		/// Gets a value indicating whether a model or pipeline error prevented processing.
		/// </summary>
		public bool IsModelFailure { get; }
	}
}
