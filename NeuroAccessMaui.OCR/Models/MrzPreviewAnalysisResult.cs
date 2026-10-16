using System.Collections.Generic;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents the outcome of locating and reading the MRZ in a single camera frame.
	/// </summary>
	public sealed class MrzPreviewAnalysisResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MrzPreviewAnalysisResult"/> class.
		/// </summary>
		/// <param name="GuidanceState">The guidance to show the user for this frame.</param>
		/// <param name="FailureCategory">The main reason the frame did not produce a valid read, if any.</param>
		/// <param name="ImageWidth">The width of the upright frame that <paramref name="MrzQuad"/> refers to.</param>
		/// <param name="ImageHeight">The height of the upright frame that <paramref name="MrzQuad"/> refers to.</param>
		/// <param name="MrzQuad">The detected MRZ outline in upright frame coordinates, if any.</param>
		/// <param name="DetectionScore">The detector's mean heatmap score for the MRZ region.</param>
		/// <param name="Read">The MRZ text read from the frame, if recognition ran and produced a decodable candidate.</param>
		/// <param name="Metadata">Additional analysis metadata.</param>
		public MrzPreviewAnalysisResult(
			MrzCaptureGuidanceState GuidanceState,
			MrzScanFailureCategory FailureCategory,
			int ImageWidth,
			int ImageHeight,
			DocumentQuad? MrzQuad,
			float DetectionScore,
			MrzFrameRead? Read,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			this.GuidanceState = GuidanceState;
			this.FailureCategory = FailureCategory;
			this.ImageWidth = ImageWidth;
			this.ImageHeight = ImageHeight;
			this.MrzQuad = MrzQuad;
			this.DetectionScore = DetectionScore;
			this.Read = Read;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the guidance to show the user for this frame.
		/// </summary>
		public MrzCaptureGuidanceState GuidanceState { get; }

		/// <summary>
		/// Gets the main reason the frame did not produce a valid read, if any.
		/// </summary>
		public MrzScanFailureCategory FailureCategory { get; }

		/// <summary>
		/// Gets the width of the upright frame that <see cref="MrzQuad"/> refers to.
		/// </summary>
		public int ImageWidth { get; }

		/// <summary>
		/// Gets the height of the upright frame that <see cref="MrzQuad"/> refers to.
		/// </summary>
		public int ImageHeight { get; }

		/// <summary>
		/// Gets the detected MRZ outline in upright frame coordinates, if any.
		/// </summary>
		public DocumentQuad? MrzQuad { get; }

		/// <summary>
		/// Gets the detector's mean heatmap score for the MRZ region.
		/// </summary>
		public float DetectionScore { get; }

		/// <summary>
		/// Gets the MRZ text read from the frame, if recognition ran and produced a decodable candidate.
		/// </summary>
		public MrzFrameRead? Read { get; }

		/// <summary>
		/// Gets additional analysis metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }
	}
}
