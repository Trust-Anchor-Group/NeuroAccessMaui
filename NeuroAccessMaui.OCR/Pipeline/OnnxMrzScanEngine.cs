using System.Diagnostics;
using System.Globalization;
using IdApp.Cv;
using IdApp.Cv.Arithmetics;
using IdApp.Cv.Transformations;
using IdApp.Cv.Transformations.Thresholds;
using NeuroAccessMaui.OCR.Models;
using CvPoint = IdApp.Cv.Basic.Point;
using CvRect = IdApp.Cv.Basic.Rect;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Orchestrates MRZScanner detection, CV correction, MRZScanner recognition, and MRZ validation.
	/// </summary>
	public sealed class OnnxMrzScanEngine
	{
		private const int PreviewWorkingWidth = 640;
		private const float MinimumUsableSharpness = 0.014f;
		private const float MaximumPreviewGlareRatio = 0.10f;
		private const float MaximumCommitGlareRatio = 0.16f;
		private const float MinimumCommitQualityScore = 0.30f;
		private const float MinimumPreviewMrzCropQualityScore = 0.14f;
		private const float MinimumMrzScore = 0.30f;
		private const float StrongPreviewMrzScore = 0.85f;
		private const string ProviderId = "onnx-mrz";

		private readonly MrzScannerMrzDetector detector;
		private readonly MrzRegionRectifier mrzRectifier;
		private readonly MrzScannerMrzRecognizer recognizer;

		/// <summary>
		/// Initializes a new instance of the <see cref="OnnxMrzScanEngine"/> class.
		/// </summary>
		/// <param name="Detector">The MRZScanner detector.</param>
		/// <param name="MrzRectifier">The CV MRZ region rectifier.</param>
		/// <param name="Recognizer">The MRZScanner recognizer.</param>
		public OnnxMrzScanEngine(
			MrzScannerMrzDetector Detector,
			MrzRegionRectifier MrzRectifier,
			MrzScannerMrzRecognizer Recognizer)
		{
			ArgumentNullException.ThrowIfNull(Detector);
			ArgumentNullException.ThrowIfNull(MrzRectifier);
			ArgumentNullException.ThrowIfNull(Recognizer);

			this.detector = Detector;
			this.mrzRectifier = MrzRectifier;
			this.recognizer = Recognizer;
		}

		internal MrzPreviewAnalysisResult AnalyzePreview(MrzPreviewAnalysisRequest Request, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Request);
			CancellationToken.ThrowIfCancellationRequested();

			Stopwatch Stopwatch = Stopwatch.StartNew();
			Dictionary<string, string> Metadata = new Dictionary<string, string>(Request.Metadata);
			try
			{
				IMatrix UprightImage = OcrImagePipelineUtilities.NormalizeSourceImage(Request.Image, Request.SourceRotationDegrees);
				Matrix<float> GrayScale = OcrImagePipelineUtilities.ToGrayScaleFloat(UprightImage);
				Matrix<float> PreviewImage = OcrImagePipelineUtilities.DownscaleToWidth(GrayScale, PreviewWorkingWidth);
				DocumentQualityMetrics QualityMetrics = OcrImagePipelineUtilities.EvaluateQuality(PreviewImage);
				MrzScannerMrzDetectionResult DetectionResult = this.detector.Detect(PreviewImage, CancellationToken);
				OcrImagePipelineUtilities.MergeMetadata(Metadata, DetectionResult.Metadata);

				MrzScannerMrzDetection? MrzDetection = DetectionResult.Detection;
				DocumentQuadCandidate? QuadCandidate = MrzDetection is null
					? null
					: CreateMrzQuadCandidate(PreviewImage, MrzDetection, "MrzScannerPreviewMrzPolygon");
				float MrzPlausibilityScore = MrzDetection?.Score ?? 0f;
				RectifiedDocument? PreviewRectifiedMrz = this.TryRectifyPreviewMrz(PreviewImage, QuadCandidate, Metadata, CancellationToken);
				float? PreviewRawMrzGlareRatio = TryGetMetadataFloat(Metadata, "MrzPreviewCropRawGlareRatio", out float RawMrzGlareRatio)
					? RawMrzGlareRatio
					: null;
				MrzDocumentFormat PreviewFormat = InferPrimaryFormat(Request.DocumentKindHint, MrzDetection?.Bounds, Metadata);
				MrzRegionCandidate? MrzCandidate = MrzDetection is null
					? null
					: CreateMrzRegionCandidate(PreviewImage, PreviewFormat, MrzDetection.Bounds, MrzDetection.Score, "MrzScannerPreviewMrzPolygon");
				bool HasUsableSharpness = HasUsablePreviewSharpness(QualityMetrics, PreviewRectifiedMrz?.QualityMetrics);
				bool HasAcceptableGlare = HasAcceptablePreviewGlare(QualityMetrics, PreviewRawMrzGlareRatio);
				bool HasUsableQuality = HasUsableSharpness && HasAcceptableGlare;
				bool HasMrz = MrzDetection is not null && MrzPlausibilityScore >= MinimumMrzScore;
				bool HasQualityGate = QualityMetrics.QualityScore >= MinimumCommitQualityScore;
				bool HasMrzCropQualityGate = PreviewRectifiedMrz is not null
					&& PreviewRectifiedMrz.QualityMetrics.QualityScore >= MinimumPreviewMrzCropQualityScore;
				bool HasStrongMrzSignal = HasMrz && MrzPlausibilityScore >= StrongPreviewMrzScore;
				MrzScanFailureCategory FailureCategory = ResolveFailureCategory(HasUsableSharpness, HasAcceptableGlare, HasMrz);
				MrzCaptureGuidanceState GuidanceState = ResolveGuidanceState(FailureCategory);
				bool IsCommitReady = HasMrz
					&& HasUsableQuality
					&& (HasStrongMrzSignal || HasMrzCropQualityGate || HasQualityGate);
				Stopwatch.Stop();

				Metadata["MrzPreviewElapsedMs"] = Stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzPreviewGuidanceState"] = GuidanceState.ToString();
				Metadata["MrzPreviewFailureCategory"] = FailureCategory.ToString();
				Metadata["MrzPreviewOverlayKind"] = "MrzPolygon";
				Metadata["MrzPreviewPlausibilityScore"] = MrzPlausibilityScore.ToString("0.000000", CultureInfo.InvariantCulture);
				Metadata["MrzPreviewCandidateCount"] = HasMrz ? "1" : "0";
				Metadata["MrzPreviewIsCommitReady"] = IsCommitReady.ToString();
				Metadata["MrzPreviewHasUsableSharpness"] = HasUsableSharpness.ToString();
				Metadata["MrzPreviewHasAcceptableGlare"] = HasAcceptableGlare.ToString();
				Metadata["MrzPreviewHasUsableQuality"] = HasUsableQuality.ToString();
				Metadata["MrzPreviewQualityGatePassed"] = HasQualityGate.ToString();
				Metadata["MrzPreviewMrzCropQualityGatePassed"] = HasMrzCropQualityGate.ToString();
				Metadata["MrzPreviewHasStrongMrzSignal"] = HasStrongMrzSignal.ToString();
				Metadata["MrzPreviewHasStrictValidation"] = false.ToString();
				Metadata["MrzPreviewRecognitionAttempted"] = false.ToString();
				Metadata["MrzPreviewRecognitionSkippedReason"] = "PreviewHotPath";
				Metadata["MrzPreviewHasStrongQuad"] = HasMrz.ToString();
				Metadata["MrzPreviewHasMrzSignal"] = HasMrz.ToString();
				Metadata["MrzPreviewHasReliableDocumentEvidence"] = HasMrz.ToString();
				Metadata["MrzPreviewQuadRectificationSafe"] = HasMrz.ToString();
				Metadata["MrzPreviewQuadUnsafeReason"] = HasMrz ? string.Empty : "MrzNotDetected";
				Metadata["MrzPreviewCommitGateReason"] = ResolveCommitGateReason(
					IsCommitReady,
					HasMrz,
					HasUsableSharpness,
					HasAcceptableGlare,
					HasQualityGate,
					HasMrzCropQualityGate,
					HasStrongMrzSignal);
				AddMrzThresholdMetadata(Metadata, "MrzPreview");
				Metadata["MrzPreviewWidth"] = PreviewImage.Width.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzPreviewHeight"] = PreviewImage.Height.ToString(CultureInfo.InvariantCulture);
				OcrImagePipelineUtilities.MergeMetadata(Metadata, OcrImagePipelineUtilities.CreateQualityMetadata(QualityMetrics, "MrzPreview"));
				OcrImagePipelineUtilities.MergeMetadata(Metadata, OcrImagePipelineUtilities.CreateQualityScoreBreakdownMetadata(QualityMetrics, "MrzPreview"));

				return new MrzPreviewAnalysisResult(
					GuidanceState,
					QualityMetrics,
					QuadCandidate,
					MrzPlausibilityScore,
					IsCommitReady,
					FailureCategory,
					Metadata,
					MrzCandidate);
			}
			catch (Exception Exception) when (Exception is not OperationCanceledException)
			{
				Stopwatch.Stop();
				DocumentQualityMetrics EmptyQuality = new DocumentQualityMetrics(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
				Metadata["MrzPreviewElapsedMs"] = Stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzPreviewFailureCategory"] = MrzScanFailureCategory.MrzRegionNotFound.ToString();
				Metadata["MrzPreviewModelFailureType"] = Exception.GetType().Name;
				Metadata["MrzPreviewModelFailureReason"] = Exception.Message;
				Metadata["MrzPreviewOverlayKind"] = "MrzPolygon";
				Metadata["MrzPreviewIsCommitReady"] = false.ToString();
				Metadata["MrzPreviewWidth"] = Request.Image.Width.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzPreviewHeight"] = Request.Image.Height.ToString(CultureInfo.InvariantCulture);
				return new MrzPreviewAnalysisResult(
					MrzCaptureGuidanceState.FindPage,
					EmptyQuality,
					null,
					0f,
					false,
					MrzScanFailureCategory.MrzRegionNotFound,
					Metadata);
			}
		}

		internal OnnxMrzScanEngineResult Scan(OcrScanRequest Request, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Request);
			CancellationToken.ThrowIfCancellationRequested();

			Stopwatch Stopwatch = Stopwatch.StartNew();
			Dictionary<string, string> Metadata = new Dictionary<string, string>(Request.Metadata)
			{
				["ProviderId"] = ProviderId,
				["MrzPipeline"] = "MrzScannerCvRecognizerOnnx"
			};

			try
			{
				IMatrix UprightSource = OcrImagePipelineUtilities.NormalizeSourceImage(Request.Image, Request.SourceRotationDegrees);
				Matrix<float> GrayScale = OcrImagePipelineUtilities.ToGrayScaleFloat(UprightSource);
				DocumentQualityMetrics SourceQualityMetrics = OcrImagePipelineUtilities.EvaluateQuality(GrayScale);
				OcrImagePipelineUtilities.MergeMetadata(Metadata, OcrImagePipelineUtilities.CreateQualityMetadata(SourceQualityMetrics, "SourceImage"));
				OcrImagePipelineUtilities.MergeMetadata(Metadata, OcrImagePipelineUtilities.CreateQualityScoreBreakdownMetadata(SourceQualityMetrics, "SourceImage"));
				AddMrzThresholdMetadata(Metadata, "MrzScan");
				if (SourceQualityMetrics.Sharpness < MinimumUsableSharpness)
					return CreateFailure(Metadata, Stopwatch, OcrScanValidationStatus.Invalid, MrzScanFailureCategory.DocumentTooBlurred, "The document image was too blurred for reliable recognition.");

				MrzScannerMrzDetectionResult DetectionResult = this.detector.Detect(UprightSource, CancellationToken);
				OcrImagePipelineUtilities.MergeMetadata(Metadata, DetectionResult.Metadata);
				MrzScannerMrzDetection? MrzDetection = DetectionResult.Detection;
				if (MrzDetection is null || MrzDetection.Score < MinimumMrzScore)
					return CreateFailure(Metadata, Stopwatch, OcrScanValidationStatus.Invalid, MrzScanFailureCategory.MrzRegionNotFound, "No plausible MRZ region was localized.", DetectionResult.Detections);

				DocumentQuadCandidate MrzQuadCandidate = CreateMrzQuadCandidate(GrayScale, MrzDetection, "MrzScannerCommitMrzPolygon");
				RectifiedDocument RectifiedMrz = this.mrzRectifier.Rectify(GrayScale, MrzQuadCandidate, CancellationToken);

				IReadOnlyList<MrzRegionCandidate> Candidates = CreateCandidateFormats(RectifiedMrz.Image, Request.DocumentKindHint, MrzDetection.Score);
				MrzRecognitionAttempt? BestAttempt = this.RecognizeBestAttempt(
					RectifiedMrz.Image,
					Candidates,
					Metadata,
					true,
					CancellationToken);

				if (BestAttempt is null)
					return CreateFailure(Metadata, Stopwatch, OcrScanValidationStatus.Invalid, MrzScanFailureCategory.OcrNoText, "No OCR candidates were generated.", DetectionResult.Detections, RectifiedMrz);

				OcrImagePipelineUtilities.MergeMetadata(Metadata, BestAttempt.Result.Metadata);
				Metadata["MrzRecognitionCandidateCount"] = Candidates.Count.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzRecognitionSelectedCandidate"] = BestAttempt.Candidate.Name;
				IReadOnlyList<MrzLineCrop> SelectedLineCrops = ExtractLineCrops(RectifiedMrz.Image, BestAttempt.Candidate);
				Stopwatch.Stop();
				Metadata["MrzTotalElapsedMs"] = Stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);

				return new OnnxMrzScanEngineResult(
					ProviderId,
					BestAttempt.Result.IsStrictSuccess ? OcrScanValidationStatus.Succeeded : OcrScanValidationStatus.Invalid,
					BestAttempt.Result.FailureCategory,
					BestAttempt.Result.FailureReason,
					Metadata,
					DetectionResult.Detections,
					RectifiedMrz,
					BestAttempt.Candidate,
					SelectedLineCrops,
					BestAttempt.Recognition,
					BestAttempt.Result);
			}
			catch (Exception Exception) when (Exception is not OperationCanceledException)
			{
				Metadata["MrzModelFailureType"] = Exception.GetType().Name;
				Metadata["MrzModelFailureReason"] = Exception.Message;
				return CreateFailure(Metadata, Stopwatch, OcrScanValidationStatus.Failed, MrzScanFailureCategory.OcrNoText, "The ONNX MRZ models are unavailable or failed to run.");
			}
		}

		private RectifiedDocument? TryRectifyPreviewMrz(
			Matrix<float> PreviewImage,
			DocumentQuadCandidate? QuadCandidate,
			Dictionary<string, string> Metadata,
			CancellationToken CancellationToken)
		{
			if (QuadCandidate is null)
			{
				Metadata["MrzPreviewRectificationAttempted"] = false.ToString();
				return null;
			}

			try
			{
				Metadata["MrzPreviewRectificationAttempted"] = true.ToString();
				RectifiedDocument RectifiedMrz = this.mrzRectifier.Rectify(PreviewImage, QuadCandidate, CancellationToken);
				Metadata["MrzPreviewCropWidth"] = RectifiedMrz.Image.Width.ToString(CultureInfo.InvariantCulture);
				Metadata["MrzPreviewCropHeight"] = RectifiedMrz.Image.Height.ToString(CultureInfo.InvariantCulture);
				if (RectifiedMrz.Metadata.TryGetValue("RectifiedMrzAspectRatio", out string? AspectRatio))
					Metadata["MrzPreviewCropAspectRatio"] = AspectRatio;
				CopyPrefixedMetadata(RectifiedMrz.Metadata, Metadata, "RectifiedMrzRaw", "MrzPreviewCropRaw");
				OcrImagePipelineUtilities.MergeMetadata(
					Metadata,
					OcrImagePipelineUtilities.CreateQualityMetadata(RectifiedMrz.QualityMetrics, "MrzPreviewCrop"));
				OcrImagePipelineUtilities.MergeMetadata(
					Metadata,
					OcrImagePipelineUtilities.CreateQualityScoreBreakdownMetadata(RectifiedMrz.QualityMetrics, "MrzPreviewCrop"));
				return RectifiedMrz;
			}
			catch (Exception Exception) when (Exception is not OperationCanceledException)
			{
				Metadata["MrzPreviewRectificationFailureType"] = Exception.GetType().Name;
				Metadata["MrzPreviewRectificationFailureReason"] = Exception.Message;
				return null;
			}
		}

		private static bool HasUsablePreviewSharpness(DocumentQualityMetrics PreviewQualityMetrics, DocumentQualityMetrics? MrzCropQualityMetrics)
		{
			return PreviewQualityMetrics.Sharpness >= MinimumUsableSharpness
				|| (MrzCropQualityMetrics is not null && MrzCropQualityMetrics.Sharpness >= MinimumUsableSharpness);
		}

		private static bool HasAcceptablePreviewGlare(DocumentQualityMetrics PreviewQualityMetrics, float? RawMrzCropGlareRatio)
		{
			return RawMrzCropGlareRatio.HasValue
				? RawMrzCropGlareRatio.Value <= MaximumCommitGlareRatio
				: PreviewQualityMetrics.GlareRatio <= MaximumPreviewGlareRatio;
		}

		private static MrzScanFailureCategory ResolveFailureCategory(bool HasUsableSharpness, bool HasAcceptableGlare, bool HasMrz)
		{
			if (!HasUsableSharpness)
				return MrzScanFailureCategory.DocumentTooBlurred;
			if (!HasAcceptableGlare)
				return MrzScanFailureCategory.GlareInMrzArea;
			if (!HasMrz)
				return MrzScanFailureCategory.MrzRegionNotFound;

			return MrzScanFailureCategory.None;
		}

		private static MrzCaptureGuidanceState ResolveGuidanceState(MrzScanFailureCategory FailureCategory)
		{
			return FailureCategory switch
			{
				MrzScanFailureCategory.DocumentTooBlurred => MrzCaptureGuidanceState.HoldSteady,
				MrzScanFailureCategory.GlareInMrzArea => MrzCaptureGuidanceState.ReduceGlare,
				MrzScanFailureCategory.MrzRegionNotFound => MrzCaptureGuidanceState.FindPage,
				_ => MrzCaptureGuidanceState.HoldSteady
			};
		}

		private static string ResolveCommitGateReason(
			bool IsCommitReady,
			bool HasMrz,
			bool HasUsableSharpness,
			bool HasAcceptableGlare,
			bool HasQualityGate,
			bool HasMrzCropQualityGate,
			bool HasStrongMrzSignal)
		{
			if (!HasMrz)
				return "MrzNotDetected";
			if (!HasUsableSharpness)
				return "SharpnessTooLow";
			if (!HasAcceptableGlare)
				return "GlareTooHigh";
			if (HasStrongMrzSignal)
				return "StrongMrzSignal";
			if (HasMrzCropQualityGate)
				return "MrzCropQualityPassed";
			if (HasQualityGate)
				return "FrameQualityPassed";

			return IsCommitReady ? "Ready" : "NotReady";
		}

		private static void AddMrzThresholdMetadata(Dictionary<string, string> Metadata, string Prefix)
		{
			Metadata[Prefix + "MinimumUsableSharpness"] = MinimumUsableSharpness.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "MaximumFrameGlareRatio"] = MaximumPreviewGlareRatio.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "MaximumCropGlareRatio"] = MaximumCommitGlareRatio.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "MinimumFrameQualityScore"] = MinimumCommitQualityScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "MinimumCropQualityScore"] = MinimumPreviewMrzCropQualityScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "MinimumMrzScore"] = MinimumMrzScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Metadata[Prefix + "StrongMrzScore"] = StrongPreviewMrzScore.ToString("0.000000", CultureInfo.InvariantCulture);
		}

		private static bool TryGetMetadataFloat(IReadOnlyDictionary<string, string> Metadata, string Key, out float Value)
		{
			if (Metadata.TryGetValue(Key, out string? Text)
				&& float.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out Value))
			{
				return true;
			}

			Value = 0f;
			return false;
		}

		private static void CopyPrefixedMetadata(
			IReadOnlyDictionary<string, string> Source,
			Dictionary<string, string> Target,
			string SourcePrefix,
			string TargetPrefix)
		{
			foreach (KeyValuePair<string, string> Pair in Source)
			{
				if (Pair.Key.StartsWith(SourcePrefix, StringComparison.Ordinal))
					Target[TargetPrefix + Pair.Key[SourcePrefix.Length..]] = Pair.Value;
			}
		}

		private static DocumentQuadCandidate CreateMrzQuadCandidate(Matrix<float> Image, MrzScannerMrzDetection Detection, string Source)
		{
			DocumentQuad Quad = new DocumentQuad(Detection.TopLeft, Detection.TopRight, Detection.BottomRight, Detection.BottomLeft);
			float AreaRatio = Detection.Bounds.Width * Detection.Bounds.Height / (float)Math.Max(1, Image.Width * Image.Height);
			Dictionary<string, string> Metadata = new Dictionary<string, string>
			{
				["DocumentQuadSource"] = Source,
				["DocumentQuadSemantic"] = "MrzPolygon",
				["DocumentQuadScore"] = Detection.Score.ToString("0.000000", CultureInfo.InvariantCulture),
				["DocumentQuadAreaRatio"] = AreaRatio.ToString("0.000000", CultureInfo.InvariantCulture),
				["DocumentQuadLeft"] = Detection.Bounds.Left.ToString(CultureInfo.InvariantCulture),
				["DocumentQuadTop"] = Detection.Bounds.Top.ToString(CultureInfo.InvariantCulture),
				["DocumentQuadWidth"] = Detection.Bounds.Width.ToString(CultureInfo.InvariantCulture),
				["DocumentQuadHeight"] = Detection.Bounds.Height.ToString(CultureInfo.InvariantCulture),
				["DocumentQuadTopLeft"] = FormatPoint(Quad.TopLeft),
				["DocumentQuadTopRight"] = FormatPoint(Quad.TopRight),
				["DocumentQuadBottomRight"] = FormatPoint(Quad.BottomRight),
				["DocumentQuadBottomLeft"] = FormatPoint(Quad.BottomLeft)
			};
			return new DocumentQuadCandidate(
				Quad,
				Detection.Score,
				Detection.Score,
				0f,
				Math.Clamp(AreaRatio / 0.18f, 0f, 1f),
				MrzScanFailureCategory.None,
				Metadata);
		}

		private static MrzDocumentFormat InferPrimaryFormat(
			OcrDocumentKindHint DocumentKindHint,
			CvRect? Bounds,
			Dictionary<string, string>? Metadata = null)
		{
			if (Metadata is not null)
				Metadata["MrzPreviewFormatHintSource"] = "DocumentKindOrGeometry";
			if (DocumentKindHint == OcrDocumentKindHint.IdentityCard)
				return MrzDocumentFormat.Td1;
			if (DocumentKindHint == OcrDocumentKindHint.Passport)
				return MrzDocumentFormat.Td3;
			if (Bounds.HasValue)
			{
				float AspectRatio = Bounds.Value.Width / (float)Math.Max(1, Bounds.Value.Height);
				if (Metadata is not null)
				{
					Metadata["MrzPreviewFormatHintAspectRatio"] = AspectRatio.ToString("0.000000", CultureInfo.InvariantCulture);
					Metadata["MrzPreviewFormatHintTd1MaximumAspectRatio"] = "6.200000";
				}
				if (AspectRatio < 6.2f)
					return MrzDocumentFormat.Td1;
			}

			return MrzDocumentFormat.Td3;
		}

		private static IReadOnlyList<MrzRegionCandidate> CreateCandidateFormats(Matrix<float> Image, OcrDocumentKindHint DocumentKindHint, float Score)
		{
			List<MrzDocumentFormat> Formats = DocumentKindHint switch
			{
				OcrDocumentKindHint.IdentityCard => new List<MrzDocumentFormat> { MrzDocumentFormat.Td1, MrzDocumentFormat.Td3, MrzDocumentFormat.Td2 },
				OcrDocumentKindHint.Passport => new List<MrzDocumentFormat> { MrzDocumentFormat.Td3, MrzDocumentFormat.Td2, MrzDocumentFormat.Td1 },
				_ => new List<MrzDocumentFormat> { MrzDocumentFormat.Td3, MrzDocumentFormat.Td2, MrzDocumentFormat.Td1 }
			};
			CvRect Bounds = new CvRect
			{
				Left = 0,
				Top = 0,
				Width = Image.Width,
				Height = Image.Height
			};
			return Formats
				.Select(Format => CreateMrzRegionCandidate(Image, Format, Bounds, Score, "MrzScannerRectifiedMrz"))
				.ToArray();
		}

		private static MrzRegionCandidate CreateMrzRegionCandidate(Matrix<float> Image, MrzDocumentFormat Format, CvRect Bounds, float Score, string Source)
		{
			CvRect ClampedBounds = OcrImagePipelineUtilities.ClampBounds(Image, Bounds);
			List<MrzLineCandidate> Lines = CreateLineCandidates(ClampedBounds, Format, Score, Source);
			Dictionary<string, string> Metadata = CreateMrzCandidateMetadata(ClampedBounds, Format, Score, Source);
			MrzCandidateScore CandidateScore = new MrzCandidateScore(
				Score,
				Score,
				1f,
				1f,
				1f,
				1f,
				0f,
				Metadata);
			return new MrzRegionCandidate("mrzscanner-" + Format.ToString().ToLowerInvariant(), Format, ClampedBounds, Lines, CandidateScore, Metadata);
		}

		private static List<MrzLineCandidate> CreateLineCandidates(CvRect Bounds, MrzDocumentFormat Format, float Score, string Source)
		{
			int LineCount = Format == MrzDocumentFormat.Td1 ? 3 : 2;
			int Gap = Math.Max(1, (int)Math.Round(Bounds.Height * 0.05d));
			int TotalGap = Gap * (LineCount - 1);
			int LineHeight = Math.Max(1, (Bounds.Height - TotalGap) / LineCount);
			List<MrzLineCandidate> Lines = new List<MrzLineCandidate>(LineCount);
			for (int Index = 0; Index < LineCount; Index++)
			{
				int Top = Bounds.Top + (Index * (LineHeight + Gap));
				CvRect LineBounds = new CvRect
				{
					Left = Bounds.Left,
					Top = Top,
					Width = Bounds.Width,
					Height = Index == LineCount - 1 ? Math.Max(1, Bounds.Top + Bounds.Height - Top) : LineHeight
				};
				Dictionary<string, string> Metadata = new Dictionary<string, string>
				{
					["MrzLineSource"] = Source,
					["MrzLineFormat"] = Format.ToString(),
					["MrzLineScore"] = Score.ToString("0.000000", CultureInfo.InvariantCulture)
				};
				Lines.Add(new MrzLineCandidate(LineBounds, Score, LineBounds.Top + (LineBounds.Height * 0.72f), Metadata));
			}

			return Lines;
		}

		private static Dictionary<string, string> CreateMrzCandidateMetadata(CvRect Bounds, MrzDocumentFormat Format, float Score, string Source)
		{
			return new Dictionary<string, string>
			{
				["MrzCropSource"] = Source,
				["MrzCropFormatHint"] = Format.ToString(),
				["MrzVisualScore"] = Score.ToString("0.000000", CultureInfo.InvariantCulture),
				["MrzCropLeft"] = Bounds.Left.ToString(CultureInfo.InvariantCulture),
				["MrzCropTop"] = Bounds.Top.ToString(CultureInfo.InvariantCulture),
				["MrzCropWidth"] = Bounds.Width.ToString(CultureInfo.InvariantCulture),
				["MrzCropHeight"] = Bounds.Height.ToString(CultureInfo.InvariantCulture)
			};
		}

		private MrzRecognitionAttempt? RecognizeBestAttempt(
			Matrix<float> RectifiedMrz,
			IReadOnlyList<MrzRegionCandidate> Candidates,
			Dictionary<string, string> Metadata,
			bool IncludeImageVariants,
			CancellationToken CancellationToken)
		{
			IReadOnlyList<MrzRecognitionImageVariant> Variants = CreateRecognitionImageVariants(RectifiedMrz, IncludeImageVariants);
			MrzRecognitionAttempt? BestAttempt = null;
			Metadata["MrzRecognitionVariantCount"] = Variants.Count.ToString(CultureInfo.InvariantCulture);
			foreach (MrzRecognitionImageVariant Variant in Variants)
			{
				OcrTextRecognitionResult Recognition = this.recognizer.Recognize(Variant.Image, CancellationToken);
				foreach (MrzRegionCandidate Candidate in Candidates)
				{
					Dictionary<string, string> CandidateMetadata = new Dictionary<string, string>(Metadata)
					{
						["MrzRecognitionVariantName"] = Variant.Name,
						["MrzRecognitionCandidateName"] = Candidate.Name,
						["MrzRecognitionCandidateFormat"] = Candidate.Format.ToString()
					};
					OcrImagePipelineUtilities.MergeMetadata(CandidateMetadata, Recognition.Metadata);
					MrzRecognitionCandidateResult CandidateResult = Decode(Candidate, Recognition, CandidateMetadata);
					MrzRecognitionAttempt Attempt = new MrzRecognitionAttempt(Variant.Name, Candidate, Recognition, CandidateResult);
					if (BestAttempt is null || CompareAttempts(Attempt, BestAttempt) > 0)
						BestAttempt = Attempt;
				}

				if (BestAttempt?.Result.IsStrictSuccess == true)
					break;
			}

			if (BestAttempt is not null)
				Metadata["MrzRecognitionSelectedVariant"] = BestAttempt.VariantName;

			return BestAttempt;
		}

		private static IReadOnlyList<MrzRecognitionImageVariant> CreateRecognitionImageVariants(Matrix<float> RectifiedMrz, bool IncludeImageVariants)
		{
			if (!IncludeImageVariants)
				return new[] { new MrzRecognitionImageVariant("rectified", RectifiedMrz) };

			List<MrzRecognitionImageVariant> Variants = new List<MrzRecognitionImageVariant>
			{
				new MrzRecognitionImageVariant("rectified", RectifiedMrz)
			};
			Matrix<float> HighContrast = OcrImagePipelineUtilities.Clone(RectifiedMrz);
			HighContrast.Contrast();
			Variants.Add(new MrzRecognitionImageVariant("high-contrast", HighContrast));
			if (RectifiedMrz.Width >= 32 && RectifiedMrz.Height >= 32)
			{
				Matrix<float> LocalThreshold = OcrImagePipelineUtilities.Clone(RectifiedMrz);
				int NeighborhoodWidth = Math.Clamp(Math.Min(RectifiedMrz.Width, RectifiedMrz.Height) / 3, 15, Math.Min(RectifiedMrz.Width, RectifiedMrz.Height) - 1);
				if (NeighborhoodWidth % 2 == 0)
					NeighborhoodWidth--;
				if (NeighborhoodWidth > 1)
				{
					LocalThreshold.AdaptiveThreshold(0.035f, NeighborhoodWidth);
					Variants.Add(new MrzRecognitionImageVariant("adaptive-threshold", LocalThreshold));
				}
			}

			Matrix<float> LiftedContrast = OcrImagePipelineUtilities.Clone(RectifiedMrz);
			LiftedContrast.ScalarLinearTransform(1.18f, -0.06f);
			LiftedContrast.Cap(0f, 1f);
			Variants.Add(new MrzRecognitionImageVariant("lifted-contrast", LiftedContrast));
			return Variants;
		}

		private static IReadOnlyList<MrzLineCrop> ExtractLineCrops(Matrix<float> Image, MrzRegionCandidate Candidate)
		{
			List<MrzLineCrop> Results = new List<MrzLineCrop>(Candidate.Lines.Count);
			for (int Index = 0; Index < Candidate.Lines.Count; Index++)
			{
				MrzLineCandidate Line = Candidate.Lines[Index];
				CvRect CropBounds = ExpandLineBounds(Image, Line.Bounds);
				Matrix<float> Crop = OcrImagePipelineUtilities.Crop(Image, CropBounds);
				Crop.Contrast();
				Matrix<float> BorderedCrop = AddWhiteBorder(Crop, Math.Max(4, Crop.Height / 3), Math.Max(8, Crop.Height / 2));
				Dictionary<string, string> Metadata = new Dictionary<string, string>(Line.Metadata)
				{
					["MrzLineCropIndex"] = Index.ToString(CultureInfo.InvariantCulture),
					["MrzLineCropFormat"] = Candidate.Format.ToString(),
					["MrzLineCropLeft"] = CropBounds.Left.ToString(CultureInfo.InvariantCulture),
					["MrzLineCropTop"] = CropBounds.Top.ToString(CultureInfo.InvariantCulture),
					["MrzLineCropWidth"] = CropBounds.Width.ToString(CultureInfo.InvariantCulture),
					["MrzLineCropHeight"] = CropBounds.Height.ToString(CultureInfo.InvariantCulture)
				};
				Results.Add(new MrzLineCrop(Index, BorderedCrop, CropBounds, Metadata));
			}

			return Results;
		}

		private static CvRect ExpandLineBounds(Matrix<float> Image, CvRect Bounds)
		{
			int HorizontalPadding = Math.Max(6, (int)Math.Round(Bounds.Width * 0.020d));
			int VerticalPadding = Math.Max(3, (int)Math.Round(Bounds.Height * 0.22d));
			return OcrImagePipelineUtilities.ClampBounds(Image, new CvRect
			{
				Left = Bounds.Left - HorizontalPadding,
				Top = Bounds.Top - VerticalPadding,
				Width = Bounds.Width + HorizontalPadding * 2,
				Height = Bounds.Height + VerticalPadding * 2
			});
		}

		private static Matrix<float> AddWhiteBorder(Matrix<float> Image, int VerticalBorder, int HorizontalBorder)
		{
			int Width = Image.Width + HorizontalBorder * 2;
			int Height = Image.Height + VerticalBorder * 2;
			Matrix<float> Result = new Matrix<float>(Width, Height);
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
					Result[x, y] = 1f;
			}

			for (int y = 0; y < Image.Height; y++)
			{
				for (int x = 0; x < Image.Width; x++)
					Result[x + HorizontalBorder, y + VerticalBorder] = Image[x, y];
			}

			return Result;
		}

		private static MrzRecognitionCandidateResult Decode(
			MrzRegionCandidate Candidate,
			OcrTextRecognitionResult Recognition,
			Dictionary<string, string> Metadata)
		{
			List<StructuredTextObservation> Observations = CreateRecognitionObservations(Recognition);
			IEnumerable<MrzDecodingEngine.MrzDecodedCandidate> DecodedCandidates = MrzDecodingEngine
				.Decode(Observations)
				.Where(DecodedCandidate => MapFormat(DecodedCandidate.Format) == Candidate.Format);
			MrzDecodingEngine.MrzDecodedCandidate? BestCandidate = DecodedCandidates.FirstOrDefault();
			Metadata["MrzRecognitionRawText"] = Recognition.RawText;
			Metadata["MrzRecognitionLineCount"] = Recognition.Lines.Count.ToString(CultureInfo.InvariantCulture);

			if (BestCandidate is null)
			{
				Metadata["MrzFailureCategory"] = MrzScanFailureCategory.OcrNoText.ToString();
				Metadata["MrzFailureReason"] = "NoMrzCandidate";
				return new MrzRecognitionCandidateResult(
					Candidate,
					Recognition.RawText,
					string.Empty,
					null,
					null,
					Recognition.Confidence,
					false,
					MrzScanFailureCategory.OcrNoText,
					"No MRZ candidate could be normalized from the OCR text.",
					Metadata);
			}

			OcrImagePipelineUtilities.MergeMetadata(Metadata, BestCandidate.Metadata);
			bool IsStrictSuccess = BestCandidate.ParserValidated
				&& BestCandidate.AcceptanceClassification == MrzDecodingEngine.MrzAcceptanceClassification.Accepted
				&& BestCandidate.ParsedInformation is not null;
			MrzScanFailureCategory FailureCategory = IsStrictSuccess ? MrzScanFailureCategory.None : MrzScanFailureCategory.MrzChecksumFailed;
			if (!IsStrictSuccess)
			{
				Metadata["MrzFailureCategory"] = FailureCategory.ToString();
				Metadata["MrzFailureReason"] = "ValidationFailed";
			}

			return new MrzRecognitionCandidateResult(
				Candidate,
				Recognition.RawText,
				BestCandidate.Text,
				BestCandidate.ParsedInformation,
				MapFormat(BestCandidate.Format),
				Math.Max(Recognition.Confidence, BestCandidate.Score),
				IsStrictSuccess,
				FailureCategory,
				IsStrictSuccess ? null : "MRZ validation failed.",
				Metadata);
		}

		private static List<StructuredTextObservation> CreateRecognitionObservations(OcrTextRecognitionResult Recognition)
		{
			List<string> Lines = Recognition.Lines.Select(NormalizeRecognizedMrzLine).Where(static Line => Line.Length > 0).ToList();
			List<StructuredTextObservation> Observations = new List<StructuredTextObservation>
			{
				new StructuredTextObservation(
					StructuredTextObservationSourceKind.Block,
					string.Join('\n', Lines),
					Lines,
					Recognition.Confidence,
					null,
					Recognition.Metadata)
			};

			for (int Index = 0; Index < Lines.Count; Index++)
			{
				Observations.Add(new StructuredTextObservation(
					StructuredTextObservationSourceKind.Line,
					Lines[Index],
					new[] { Lines[Index] },
					Recognition.Confidence,
					Index,
					Recognition.Metadata));
			}

			return Observations;
		}

		private static string NormalizeRecognizedMrzLine(string Text)
		{
			if (string.IsNullOrWhiteSpace(Text))
				return string.Empty;

			char[] Buffer = new char[Text.Length];
			int Count = 0;
			foreach (char Character in Text)
			{
				if (char.IsWhiteSpace(Character))
					continue;

				char Upper = char.ToUpperInvariant(Character);
				Buffer[Count++] = Upper == '«' ? '<' : Upper;
			}

			return new string(Buffer, 0, Count);
		}

		private static int CompareAttempts(MrzRecognitionAttempt Left, MrzRecognitionAttempt Right)
		{
			int StrictComparison = Left.Result.IsStrictSuccess.CompareTo(Right.Result.IsStrictSuccess);
			if (StrictComparison != 0)
				return StrictComparison;

			int ConfidenceComparison = Left.Result.Confidence.CompareTo(Right.Result.Confidence);
			if (ConfidenceComparison != 0)
				return ConfidenceComparison;

			return Left.Recognition.Confidence.CompareTo(Right.Recognition.Confidence);
		}

		private static MrzDocumentFormat MapFormat(MrzDecodingEngine.MrzFormat Format)
		{
			return Format switch
			{
				MrzDecodingEngine.MrzFormat.Td1 => MrzDocumentFormat.Td1,
				MrzDecodingEngine.MrzFormat.Td2 => MrzDocumentFormat.Td2,
				_ => MrzDocumentFormat.Td3
			};
		}

		private static string FormatPoint(CvPoint Point)
		{
			return Point.X.ToString(CultureInfo.InvariantCulture) + "," + Point.Y.ToString(CultureInfo.InvariantCulture);
		}

		private static OnnxMrzScanEngineResult CreateFailure(
			Dictionary<string, string> Metadata,
			Stopwatch Stopwatch,
			OcrScanValidationStatus ValidationStatus,
			MrzScanFailureCategory FailureCategory,
			string FailureReason,
			IReadOnlyList<MrzScannerMrzDetection>? Detections = null,
			RectifiedDocument? RectifiedMrz = null)
		{
			Stopwatch.Stop();
			Metadata["MrzTotalElapsedMs"] = Stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
			Metadata["MrzFailureCategory"] = FailureCategory.ToString();
			return new OnnxMrzScanEngineResult(
				ProviderId,
				ValidationStatus,
				FailureCategory,
				FailureReason,
				Metadata,
				Detections ?? Array.Empty<MrzScannerMrzDetection>(),
				RectifiedMrz,
				null,
				Array.Empty<MrzLineCrop>(),
				null,
				null);
		}

		private sealed record MrzRecognitionAttempt(
			string VariantName,
			MrzRegionCandidate Candidate,
			OcrTextRecognitionResult Recognition,
			MrzRecognitionCandidateResult Result);

		private sealed record MrzRecognitionImageVariant(string Name, Matrix<float> Image);
	}

	internal sealed class OnnxMrzScanEngineResult
	{
		internal OnnxMrzScanEngineResult(
			string ProviderId,
			OcrScanValidationStatus ValidationStatus,
			MrzScanFailureCategory FailureCategory,
			string? FailureReason,
			IReadOnlyDictionary<string, string> Metadata,
			IReadOnlyList<MrzScannerMrzDetection> Detections,
			RectifiedDocument? RectifiedDocument,
			MrzRegionCandidate? MrzCandidate,
			IReadOnlyList<MrzLineCrop> LineCrops,
			OcrTextRecognitionResult? Recognition,
			MrzRecognitionCandidateResult? CandidateResult)
		{
			this.ProviderId = ProviderId;
			this.ValidationStatus = ValidationStatus;
			this.FailureCategory = FailureCategory;
			this.FailureReason = FailureReason;
			this.Metadata = Metadata;
			this.Detections = Detections;
			this.RectifiedDocument = RectifiedDocument;
			this.MrzCandidate = MrzCandidate;
			this.LineCrops = LineCrops;
			this.Recognition = Recognition;
			this.CandidateResult = CandidateResult;
		}

		public string ProviderId { get; }

		public OcrScanValidationStatus ValidationStatus { get; }

		public MrzScanFailureCategory FailureCategory { get; }

		public string? FailureReason { get; }

		public IReadOnlyDictionary<string, string> Metadata { get; }

		public IReadOnlyList<MrzScannerMrzDetection> Detections { get; }

		public RectifiedDocument? RectifiedDocument { get; }

		public MrzRegionCandidate? MrzCandidate { get; }

		public IReadOnlyList<MrzLineCrop> LineCrops { get; }

		public OcrTextRecognitionResult? Recognition { get; }

		public MrzRecognitionCandidateResult? CandidateResult { get; }
	}
}
