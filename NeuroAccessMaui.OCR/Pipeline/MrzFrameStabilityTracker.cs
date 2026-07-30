using System.Globalization;
using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Tracks a short preview-frame window and selects stable commit frames.
	/// </summary>
	public sealed class MrzFrameStabilityTracker
	{
		private const int MaximumFrames = 8;
		private const int DefaultMinimumReadyFrames = 3;
		private const int StrongCandidateMinimumReadyFrames = 2;
		private static readonly TimeSpan MaximumWindowAge = TimeSpan.FromSeconds(3);

		private readonly Queue<TrackedFrame> frames = new Queue<TrackedFrame>();
		private readonly object syncRoot = new object();

		/// <summary>
		/// Adds a preview analysis result and frame to the stability window.
		/// </summary>
		/// <param name="Frame">The preview frame.</param>
		/// <param name="AnalysisResult">The preview analysis result.</param>
		public void Add(MrzPreviewFrame Frame, MrzPreviewAnalysisResult AnalysisResult)
		{
			ArgumentNullException.ThrowIfNull(Frame);
			ArgumentNullException.ThrowIfNull(AnalysisResult);

			lock (this.syncRoot)
			{
				this.frames.Enqueue(new TrackedFrame(Frame, AnalysisResult));
				while (this.frames.Count > MaximumFrames)
					this.frames.Dequeue();

				this.TrimExpired(Frame.Timestamp);
			}
		}

		/// <summary>
		/// Clears the tracked preview window.
		/// </summary>
		public void Reset()
		{
			lock (this.syncRoot)
			{
				this.frames.Clear();
			}
		}

		/// <summary>
		/// Attempts to select the best stable commit frame.
		/// </summary>
		/// <param name="Frame">The selected frame, if stable.</param>
		/// <returns>If a stable frame was selected.</returns>
		public bool TryGetCommitFrame(out MrzPreviewFrame? Frame)
		{
			lock (this.syncRoot)
			{
				List<TrackedFrame> ReadyFrames = this.frames
					.Where(static Item => Item.AnalysisResult.IsCommitReady)
					.ToList();
				int RequiredReadyFrames = ResolveRequiredReadyFrameCount(ReadyFrames);
				if (ReadyFrames.Count < RequiredReadyFrames)
				{
					Frame = null;
					return false;
				}

				List<TrackedFrame> RecentReadyFrames = ReadyFrames
					.Skip(Math.Max(0, ReadyFrames.Count - RequiredReadyFrames))
					.ToList();
				if (!HasStableDocumentQuad(RecentReadyFrames, RequiredReadyFrames))
				{
					Frame = null;
					return false;
				}

				TrackedFrame BestFrame = RecentReadyFrames
					.OrderByDescending(static Item => Item.AnalysisResult.QualityMetrics.QualityScore)
					.ThenByDescending(static Item => Item.AnalysisResult.DocumentQuadCandidate?.Score ?? 0f)
					.First();
				Frame = BestFrame.Frame;
				this.frames.Clear();
				return true;
			}
		}

		/// <summary>
		/// Creates a diagnostic snapshot of the current preview stability window.
		/// </summary>
		/// <returns>A dictionary containing preview readiness and stability measurements.</returns>
		public IReadOnlyDictionary<string, string> CreateDiagnostics()
		{
			List<TrackedFrame> TrackedFrames;
			lock (this.syncRoot)
			{
				TrackedFrames = this.frames.ToList();
			}

			List<TrackedFrame> ReadyFrames = TrackedFrames
				.Where(static Item => Item.AnalysisResult.IsCommitReady)
				.ToList();
			int RequiredReadyFrames = ResolveRequiredReadyFrameCount(ReadyFrames);
			Dictionary<string, string> Diagnostics = new Dictionary<string, string>
			{
				["StabilityTrackedFrameCount"] = TrackedFrames.Count.ToString(CultureInfo.InvariantCulture),
				["StabilityReadyFrameCount"] = ReadyFrames.Count.ToString(CultureInfo.InvariantCulture),
				["StabilityRequiredReadyFrameCount"] = RequiredReadyFrames.ToString(CultureInfo.InvariantCulture),
				["StabilityMaximumWindowAgeMs"] = MaximumWindowAge.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)
			};
			AddWindowDiagnostics(Diagnostics, TrackedFrames);

			if (TrackedFrames.Count > 0)
			{
				AddFrameDiagnostics(Diagnostics, "LastFrame", TrackedFrames[^1]);
			}

			if (ReadyFrames.Count == 0)
			{
				Diagnostics["StabilityCommitBlockedReason"] = "NoReadyFrames";
				return Diagnostics;
			}

			AddFrameDiagnostics(Diagnostics, "LastReadyFrame", ReadyFrames[^1]);
			if (ReadyFrames.Count < RequiredReadyFrames)
			{
				Diagnostics["StabilityCommitBlockedReason"] = "InsufficientReadyFrames";
				return Diagnostics;
			}

			List<TrackedFrame> RecentReadyFrames = ReadyFrames
				.Skip(Math.Max(0, ReadyFrames.Count - RequiredReadyFrames))
				.ToList();
			StabilityMeasurement Measurement = MeasureStability(RecentReadyFrames, RequiredReadyFrames);
			Diagnostics["StabilityStableFrameCount"] = Measurement.StableCount.ToString(CultureInfo.InvariantCulture);
			Diagnostics["StabilityMaximumAverageCornerDistance"] = Measurement.MaximumAverageCornerDistance.ToString("0.000", CultureInfo.InvariantCulture);
			Diagnostics["StabilityAllowedCornerDistance"] = Measurement.AllowedCornerDistance.ToString("0.000", CultureInfo.InvariantCulture);
			Diagnostics["StabilityRatio"] = Measurement.StabilityRatio.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics["StabilityCommitBlockedReason"] = Measurement.IsStable ? "Ready" : "UnstableQuad";
			return Diagnostics;
		}

		private void TrimExpired(DateTimeOffset CurrentTimestamp)
		{
			while (this.frames.Count > 0 && CurrentTimestamp - this.frames.Peek().Frame.Timestamp > MaximumWindowAge)
				this.frames.Dequeue();
		}

		private static int ResolveRequiredReadyFrameCount(IReadOnlyList<TrackedFrame> ReadyFrames)
		{
			if (ReadyFrames.Count == 0)
				return DefaultMinimumReadyFrames;

			TrackedFrame LastFrame = ReadyFrames[^1];
			float QuadScore = LastFrame.AnalysisResult.DocumentQuadCandidate?.Score ?? 0f;
			float MrzPlausibilityScore = LastFrame.AnalysisResult.MrzPlausibilityScore;
			if (IsMrzAnchoredQuad(LastFrame.AnalysisResult)
				|| MrzPlausibilityScore >= 0.28f
				|| QuadScore >= 0.48f)
			{
				return StrongCandidateMinimumReadyFrames;
			}

			return DefaultMinimumReadyFrames;
		}

		private static bool HasStableDocumentQuad(IReadOnlyList<TrackedFrame> ReadyFrames, int RequiredReadyFrames)
		{
			return MeasureStability(ReadyFrames, RequiredReadyFrames).IsStable;
		}

		private static StabilityMeasurement MeasureStability(IReadOnlyList<TrackedFrame> ReadyFrames, int RequiredReadyFrames)
		{
			TrackedFrame LastFrame = ReadyFrames[^1];
			DocumentQuad? ReferenceQuad = LastFrame.AnalysisResult.DocumentQuadCandidate?.Quad;
			if (ReferenceQuad is null)
				return new StabilityMeasurement(0, 0d, 0d, 0d, false);

			int PreviewWidth = TryGetMetadataInt(LastFrame.AnalysisResult.Metadata, "MrzPreviewWidth", out int ParsedWidth)
				? ParsedWidth
				: LastFrame.Frame.Image.Width;
			int PreviewHeight = TryGetMetadataInt(LastFrame.AnalysisResult.Metadata, "MrzPreviewHeight", out int ParsedHeight)
				? ParsedHeight
				: LastFrame.Frame.Image.Height;
			double FrameDiagonal = Math.Sqrt((PreviewWidth * PreviewWidth) + (PreviewHeight * PreviewHeight));
			double StabilityRatio = ResolveStabilityRatio(LastFrame.AnalysisResult);
			double MaximumPointDistance = Math.Max(14d, FrameDiagonal * StabilityRatio);
			int StableCount = 0;
			double MaximumAverageCornerDistance = 0d;
			foreach (TrackedFrame Frame in ReadyFrames)
			{
				DocumentQuad? Quad = Frame.AnalysisResult.DocumentQuadCandidate?.Quad;
				if (Quad is null)
					continue;

				double Distance = (
					DistanceBetween(ReferenceQuad.TopLeft, Quad.TopLeft)
					+ DistanceBetween(ReferenceQuad.TopRight, Quad.TopRight)
					+ DistanceBetween(ReferenceQuad.BottomRight, Quad.BottomRight)
					+ DistanceBetween(ReferenceQuad.BottomLeft, Quad.BottomLeft)) * 0.25d;
				MaximumAverageCornerDistance = Math.Max(MaximumAverageCornerDistance, Distance);
				if (Distance <= MaximumPointDistance)
					StableCount++;
			}

			return new StabilityMeasurement(
				StableCount,
				MaximumAverageCornerDistance,
				MaximumPointDistance,
				StabilityRatio,
				StableCount >= RequiredReadyFrames);
		}

		private static double ResolveStabilityRatio(MrzPreviewAnalysisResult AnalysisResult)
		{
			if (IsMrzAnchoredQuad(AnalysisResult))
				return 0.060d;

			float QuadScore = AnalysisResult.DocumentQuadCandidate?.Score ?? 0f;
			if (AnalysisResult.MrzPlausibilityScore >= 0.28f || QuadScore >= 0.48f)
				return 0.050d;

			return 0.040d;
		}

		private static bool IsMrzAnchoredQuad(MrzPreviewAnalysisResult AnalysisResult)
		{
			DocumentQuadCandidate? Candidate = AnalysisResult.DocumentQuadCandidate;
			if (Candidate is null)
				return false;

			return string.Equals(
				Candidate.Metadata.TryGetValue("DocumentQuadSource", out string? Source) ? Source : null,
				"MrzAnchoredPreviewQuad",
				StringComparison.Ordinal);
		}

		private static double DistanceBetween(IdApp.Cv.Basic.Point Left, IdApp.Cv.Basic.Point Right)
		{
			double dx = Left.X - Right.X;
			double dy = Left.Y - Right.Y;
			return Math.Sqrt((dx * dx) + (dy * dy));
		}

		private static bool TryGetMetadataInt(IReadOnlyDictionary<string, string> Metadata, string Key, out int Value)
		{
			if (Metadata.TryGetValue(Key, out string? Text)
				&& int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out Value))
			{
				return true;
			}

			Value = 0;
			return false;
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

		private static void AddWindowDiagnostics(Dictionary<string, string> Diagnostics, IReadOnlyList<TrackedFrame> TrackedFrames)
		{
			Dictionary<string, int> CommitGateReasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
			int StrictPreviewValidationFrameCount = 0;
			int StrongMrzSignalFrameCount = 0;
			int MrzCropQualityFrameCount = 0;
			float BestMrzPlausibilityScore = 0f;
			float BestFrameQualityScore = 0f;
			float BestMrzCropQualityScore = 0f;
			foreach (TrackedFrame Frame in TrackedFrames)
			{
				MrzPreviewAnalysisResult AnalysisResult = Frame.AnalysisResult;
				BestMrzPlausibilityScore = Math.Max(BestMrzPlausibilityScore, AnalysisResult.MrzPlausibilityScore);
				BestFrameQualityScore = Math.Max(BestFrameQualityScore, AnalysisResult.QualityMetrics.QualityScore);
				if (AnalysisResult.Metadata.TryGetValue("MrzPreviewCommitGateReason", out string? CommitGateReason)
					&& !string.IsNullOrWhiteSpace(CommitGateReason))
				{
					CommitGateReasonCounts.TryGetValue(CommitGateReason, out int Count);
					CommitGateReasonCounts[CommitGateReason] = Count + 1;
				}

				if (string.Equals(ReadMetadata(AnalysisResult.Metadata, "MrzPreviewHasStrictValidation"), true.ToString(), StringComparison.OrdinalIgnoreCase))
					StrictPreviewValidationFrameCount++;
				if (string.Equals(ReadMetadata(AnalysisResult.Metadata, "MrzPreviewHasStrongMrzSignal"), true.ToString(), StringComparison.OrdinalIgnoreCase))
					StrongMrzSignalFrameCount++;
				if (string.Equals(ReadMetadata(AnalysisResult.Metadata, "MrzPreviewMrzCropQualityGatePassed"), true.ToString(), StringComparison.OrdinalIgnoreCase))
					MrzCropQualityFrameCount++;
				if (TryGetMetadataFloat(AnalysisResult.Metadata, "MrzPreviewCropQualityScore", out float MrzCropQualityScore))
					BestMrzCropQualityScore = Math.Max(BestMrzCropQualityScore, MrzCropQualityScore);
			}

			Diagnostics["StabilityStrictPreviewValidationFrameCount"] = StrictPreviewValidationFrameCount.ToString(CultureInfo.InvariantCulture);
			Diagnostics["StabilityStrongMrzSignalFrameCount"] = StrongMrzSignalFrameCount.ToString(CultureInfo.InvariantCulture);
			Diagnostics["StabilityMrzCropQualityFrameCount"] = MrzCropQualityFrameCount.ToString(CultureInfo.InvariantCulture);
			Diagnostics["StabilityBestMrzPlausibilityScore"] = BestMrzPlausibilityScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics["StabilityBestFrameQualityScore"] = BestFrameQualityScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics["StabilityBestMrzCropQualityScore"] = BestMrzCropQualityScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics["StabilityCommitGateReasonCounts"] = string.Join(
				",",
				CommitGateReasonCounts
					.OrderBy(static Pair => Pair.Key, StringComparer.Ordinal)
					.Select(static Pair => Pair.Key + "=" + Pair.Value.ToString(CultureInfo.InvariantCulture)));
		}

		private static string ReadMetadata(IReadOnlyDictionary<string, string> Metadata, string Key)
		{
			return Metadata.TryGetValue(Key, out string? Value) ? Value : string.Empty;
		}

		private static void AddFrameDiagnostics(Dictionary<string, string> Diagnostics, string Prefix, TrackedFrame Frame)
		{
			MrzPreviewAnalysisResult AnalysisResult = Frame.AnalysisResult;
			Diagnostics[Prefix + "TimestampUtc"] = Frame.Frame.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
			Diagnostics[Prefix + "IsCommitReady"] = AnalysisResult.IsCommitReady.ToString();
			Diagnostics[Prefix + "GuidanceState"] = AnalysisResult.GuidanceState.ToString();
			Diagnostics[Prefix + "FailureCategory"] = AnalysisResult.FailureCategory.ToString();
			Diagnostics[Prefix + "QualityScore"] = AnalysisResult.QualityMetrics.QualityScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics[Prefix + "Sharpness"] = AnalysisResult.QualityMetrics.Sharpness.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics[Prefix + "GlareRatio"] = AnalysisResult.QualityMetrics.GlareRatio.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics[Prefix + "MrzPlausibilityScore"] = AnalysisResult.MrzPlausibilityScore.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics[Prefix + "CommitGateReason"] = ReadMetadata(AnalysisResult.Metadata, "MrzPreviewCommitGateReason");
			Diagnostics[Prefix + "HasStrictValidation"] = ReadMetadata(AnalysisResult.Metadata, "MrzPreviewHasStrictValidation");
			Diagnostics[Prefix + "HasStrongMrzSignal"] = ReadMetadata(AnalysisResult.Metadata, "MrzPreviewHasStrongMrzSignal");
			Diagnostics[Prefix + "MrzCropQualityScore"] = ReadMetadata(AnalysisResult.Metadata, "MrzPreviewCropQualityScore");
			Diagnostics[Prefix + "MrzCropGlareRatio"] = ReadMetadata(AnalysisResult.Metadata, "MrzPreviewCropGlareRatio");
			Diagnostics[Prefix + "RecognitionAttempted"] = ReadMetadata(AnalysisResult.Metadata, "MrzPreviewRecognitionAttempted");
			Diagnostics[Prefix + "RecognitionStrictSuccess"] = ReadMetadata(AnalysisResult.Metadata, "MrzPreviewRecognitionStrictSuccess");

			DocumentQuadCandidate? Candidate = AnalysisResult.DocumentQuadCandidate;
			if (Candidate is null)
			{
				Diagnostics[Prefix + "HasDocumentQuad"] = false.ToString();
				return;
			}

			Diagnostics[Prefix + "HasDocumentQuad"] = true.ToString();
			Diagnostics[Prefix + "DocumentQuadScore"] = Candidate.Score.ToString("0.000000", CultureInfo.InvariantCulture);
			Diagnostics[Prefix + "DocumentQuadFailureCategory"] = Candidate.FailureCategory.ToString();
			Diagnostics[Prefix + "DocumentQuadSource"] = Candidate.Metadata.TryGetValue("DocumentQuadSource", out string? Source) ? Source : string.Empty;
			Diagnostics[Prefix + "DocumentQuadAspectScore"] = Candidate.Metadata.TryGetValue("DocumentQuadAspectScore", out string? AspectScore) ? AspectScore : string.Empty;
			Diagnostics[Prefix + "DocumentQuadAreaScore"] = Candidate.Metadata.TryGetValue("DocumentQuadAreaScore", out string? AreaScore) ? AreaScore : string.Empty;
			Diagnostics[Prefix + "DocumentQuadEdgeSupportScore"] = Candidate.Metadata.TryGetValue("DocumentQuadEdgeSupportScore", out string? EdgeSupportScore) ? EdgeSupportScore : string.Empty;
		}

		private readonly record struct StabilityMeasurement(
			int StableCount,
			double MaximumAverageCornerDistance,
			double AllowedCornerDistance,
			double StabilityRatio,
			bool IsStable);

		private readonly record struct TrackedFrame(MrzPreviewFrame Frame, MrzPreviewAnalysisResult AnalysisResult);
	}
}
