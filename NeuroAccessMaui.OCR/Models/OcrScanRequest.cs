using IdApp.Cv;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a single OCR scan request against a source image or preview frame.
	/// </summary>
	public sealed class OcrScanRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OcrScanRequest"/> class.
		/// </summary>
		/// <param name="Image">The source image matrix.</param>
		/// <param name="TargetKind">The requested scan target.</param>
		/// <param name="ProviderId">The explicitly selected provider identifier, if any.</param>
		/// <param name="SourceKind">The source kind.</param>
		/// <param name="ExpectedDocumentRegion">An optional normalized document-region hint.</param>
		/// <param name="SourceRotationDegrees">Clockwise source-rotation degrees to normalize before scanning.</param>
		/// <param name="DocumentKindHint">The expected document kind, if known.</param>
		/// <param name="DocumentSideHint">The expected document side, if known.</param>
		/// <param name="ScanMode">The intended scanner workload.</param>
		/// <param name="CaptureDebugArtifacts">If debug artifacts should be persisted.</param>
		/// <param name="Metadata">Optional caller metadata.</param>
		public OcrScanRequest(
			IMatrix Image,
			OcrScanTargetKind TargetKind,
			string? ProviderId = null,
			OcrScanSourceKind SourceKind = OcrScanSourceKind.StillImage,
			DocumentRegionHint? ExpectedDocumentRegion = null,
			int SourceRotationDegrees = 0,
			OcrDocumentKindHint DocumentKindHint = OcrDocumentKindHint.Unknown,
			OcrDocumentSideHint DocumentSideHint = OcrDocumentSideHint.Unknown,
			OcrScanMode? ScanMode = null,
			bool CaptureDebugArtifacts = false,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Image);

			this.Image = Image;
			this.TargetKind = TargetKind;
			this.ProviderId = ProviderId;
			this.SourceKind = SourceKind;
			this.ExpectedDocumentRegion = ExpectedDocumentRegion;
			this.SourceRotationDegrees = NormalizeSourceRotationDegrees(SourceRotationDegrees);
			this.DocumentKindHint = DocumentKindHint;
			this.DocumentSideHint = DocumentSideHint;
			this.ScanMode = ScanMode ?? (SourceKind == OcrScanSourceKind.PreviewFrame ? OcrScanMode.Commit : OcrScanMode.StillImage);
			this.CaptureDebugArtifacts = CaptureDebugArtifacts;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the source image matrix.
		/// </summary>
		public IMatrix Image { get; }

		/// <summary>
		/// Gets the requested scan target.
		/// </summary>
		public OcrScanTargetKind TargetKind { get; }

		/// <summary>
		/// Gets the explicitly selected provider identifier, if any.
		/// </summary>
		public string? ProviderId { get; }

		/// <summary>
		/// Gets the source kind.
		/// </summary>
		public OcrScanSourceKind SourceKind { get; }

		/// <summary>
		/// Gets the optional normalized document-region hint.
		/// </summary>
		public DocumentRegionHint? ExpectedDocumentRegion { get; }

		/// <summary>
		/// Gets clockwise source-rotation degrees to normalize before scanning.
		/// </summary>
		public int SourceRotationDegrees { get; }

		/// <summary>
		/// Gets the expected document kind, if known.
		/// </summary>
		public OcrDocumentKindHint DocumentKindHint { get; }

		/// <summary>
		/// Gets the expected document side, if known.
		/// </summary>
		public OcrDocumentSideHint DocumentSideHint { get; }

		/// <summary>
		/// Gets the intended scanner workload.
		/// </summary>
		public OcrScanMode ScanMode { get; }

		/// <summary>
		/// Gets a value indicating whether debug artifacts should be persisted.
		/// </summary>
		public bool CaptureDebugArtifacts { get; }

		/// <summary>
		/// Gets caller-supplied metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata { get; }

		private static int NormalizeSourceRotationDegrees(int SourceRotationDegrees)
		{
			int NormalizedRotationDegrees = SourceRotationDegrees % 360;
			if (NormalizedRotationDegrees < 0)
				NormalizedRotationDegrees += 360;

			return NormalizedRotationDegrees switch
			{
				90 => 90,
				180 => 180,
				270 => 270,
				_ => 0
			};
		}
	}
}
