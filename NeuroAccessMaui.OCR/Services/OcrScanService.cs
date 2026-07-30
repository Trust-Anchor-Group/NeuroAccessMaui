using System.Globalization;
using System.Text.Json;
using IdApp.Cv;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.OCR.Models;
using NeuroAccessMaui.OCR.Pipeline;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Implements the OCR scan workflow used by the application.
	/// </summary>
	public sealed class OcrScanService : IOcrScanService
	{
		private const string ProviderId = "onnx-mrz";

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			IncludeFields = true
		};

		private readonly OnnxMrzScanEngine scanEngine;

		/// <summary>
		/// Initializes a new instance of the <see cref="OcrScanService"/> class.
		/// </summary>
		/// <param name="ScanEngine">The ONNX MRZ scan engine.</param>
		public OcrScanService(OnnxMrzScanEngine ScanEngine)
		{
			ArgumentNullException.ThrowIfNull(ScanEngine);

			this.scanEngine = ScanEngine;
		}

		/// <inheritdoc/>
		public Task<OcrScanResult> ScanAsync(OcrScanRequest Request, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Request);
			CancellationToken.ThrowIfCancellationRequested();

			Dictionary<string, string> Metadata = new Dictionary<string, string>(Request.Metadata)
			{
				["SourceRotationDegrees"] = Request.SourceRotationDegrees.ToString(CultureInfo.InvariantCulture),
				["OcrScanMode"] = Request.ScanMode.ToString(),
				["OcrDocumentKindHint"] = Request.DocumentKindHint.ToString(),
				["OcrDocumentSideHint"] = Request.DocumentSideHint.ToString()
			};

			using OcrArtifactSessionWriter ArtifactSession = OcrArtifactSessionWriter.Create(Request, JsonOptions);
			ArtifactSession.WriteRequest();
			ArtifactSession.CaptureImage("input-source", Request.Image, Metadata);

			if (!string.IsNullOrWhiteSpace(Request.ProviderId)
				&& !string.Equals(Request.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase))
			{
				OcrScanResult ProviderFailureResult = CreateFailureResult(
					Request,
					OcrScanValidationStatus.Failed,
					$"The OCR provider '{Request.ProviderId}' is no longer available for MRZ scanning. Use '{ProviderId}'.",
					MrzScanFailureCategory.OcrNoText,
					Metadata);
				return Task.FromResult(ArtifactSession.Complete(ProviderFailureResult));
			}

			if (Request.TargetKind == OcrScanTargetKind.QrCode)
			{
				OcrScanResult QrFailureResult = CreateFailureResult(
					Request,
					OcrScanValidationStatus.UnsupportedTarget,
					"QR scanning is not handled by the ONNX MRZ pipeline.",
					MrzScanFailureCategory.None,
					Metadata);
				return Task.FromResult(ArtifactSession.Complete(QrFailureResult));
			}

			OnnxMrzScanEngineResult EngineResult = this.scanEngine.Scan(Request, CancellationToken);
			Dictionary<string, string> ResultMetadata = new Dictionary<string, string>(Metadata);
			OcrImagePipelineUtilities.MergeMetadata(ResultMetadata, EngineResult.Metadata);
			CaptureArtifacts(ArtifactSession, EngineResult);
			OcrScanResult Result = CreateResult(Request, EngineResult, ResultMetadata);
			return Task.FromResult(ArtifactSession.Complete(Result));
		}

		private static void CaptureArtifacts(OcrArtifactSessionWriter ArtifactSession, OnnxMrzScanEngineResult EngineResult)
		{
			ArtifactSession.CaptureData("mrzscanner-detector-detections", EngineResult.Detections.Select(static Detection => new
			{
				Detection.Bounds,
				Detection.Score,
				Detection.TopLeft,
				Detection.TopRight,
				Detection.BottomRight,
				Detection.BottomLeft
			}).ToArray());

			if (EngineResult.RectifiedDocument is not null)
				ArtifactSession.CaptureImage("cv-corrected-mrz", EngineResult.RectifiedDocument.Image, EngineResult.RectifiedDocument.Metadata);

			if (EngineResult.MrzCandidate is not null)
			{
				ArtifactSession.CaptureData("cv-corrected-mrz-candidate", new
				{
					EngineResult.MrzCandidate.Name,
					EngineResult.MrzCandidate.Format,
					EngineResult.MrzCandidate.Bounds,
					EngineResult.MrzCandidate.Score,
					EngineResult.MrzCandidate.Metadata,
					Lines = EngineResult.MrzCandidate.Lines.Select(static Line => new
					{
						Line.Bounds,
						Line.Score,
						Line.BaselineY,
						Line.Metadata
					}).ToArray()
				});
			}

			foreach (MrzLineCrop LineCrop in EngineResult.LineCrops)
			{
				ArtifactSession.CaptureImage(
					"mrzscanner-recognition-line-" + LineCrop.LineIndex.ToString(CultureInfo.InvariantCulture),
					LineCrop.Image,
					LineCrop.Metadata);
			}

			if (EngineResult.Recognition is not null)
			{
				ArtifactSession.CaptureData("mrzscanner-recognition", new
				{
					EngineResult.Recognition.RawText,
					EngineResult.Recognition.Lines,
					EngineResult.Recognition.Confidence,
					EngineResult.Recognition.Metadata
				});
			}

			if (EngineResult.CandidateResult is not null)
			{
				ArtifactSession.CaptureData("mrz-decoded-result", new
				{
					EngineResult.CandidateResult.RawText,
					EngineResult.CandidateResult.NormalizedText,
					EngineResult.CandidateResult.Format,
					EngineResult.CandidateResult.Confidence,
					EngineResult.CandidateResult.IsStrictSuccess,
					EngineResult.CandidateResult.FailureCategory,
					EngineResult.CandidateResult.FailureReason,
					Document = EngineResult.CandidateResult.DocumentInformation,
					EngineResult.CandidateResult.Metadata
				});
			}
		}

		private static OcrScanResult CreateResult(
			OcrScanRequest Request,
			OnnxMrzScanEngineResult EngineResult,
			Dictionary<string, string> Metadata)
		{
			MrzRecognitionCandidateResult? CandidateResult = EngineResult.CandidateResult;
			if (CandidateResult is not null
				&& CandidateResult.IsStrictSuccess
				&& CandidateResult.DocumentInformation is not null
				&& CandidateResult.Format.HasValue)
			{
				Metadata["MrzFailureCategory"] = MrzScanFailureCategory.None.ToString();
				return new OcrScanResult(
					Request.TargetKind,
					Request.SourceKind,
					EngineResult.ProviderId,
					OcrScanValidationStatus.Succeeded,
					CandidateResult.RawText,
					CandidateResult.NormalizedText,
					new MrzScanResult(CandidateResult.Format.Value, CandidateResult.NormalizedText, CandidateResult.DocumentInformation),
					CandidateResult.Confidence,
					null,
					Metadata);
			}

			Metadata["MrzFailureCategory"] = EngineResult.FailureCategory.ToString();
			return new OcrScanResult(
				Request.TargetKind,
				Request.SourceKind,
				EngineResult.ProviderId,
				EngineResult.ValidationStatus,
				CandidateResult?.RawText ?? string.Empty,
				CandidateResult?.NormalizedText ?? string.Empty,
				null,
				CandidateResult?.Confidence ?? 0f,
				EngineResult.FailureReason ?? CandidateResult?.FailureReason ?? "MRZ validation failed.",
				Metadata);
		}

		private static OcrScanResult CreateFailureResult(
			OcrScanRequest Request,
			OcrScanValidationStatus ValidationStatus,
			string FailureReason,
			MrzScanFailureCategory FailureCategory,
			Dictionary<string, string> Metadata)
		{
			Metadata["MrzFailureCategory"] = FailureCategory.ToString();
			return new OcrScanResult(
				Request.TargetKind,
				Request.SourceKind,
				ProviderId,
				ValidationStatus,
				string.Empty,
				string.Empty,
				null,
				0f,
				FailureReason,
				Metadata);
		}

		private sealed class OcrArtifactSessionWriter : IDisposable
		{
			private readonly string sessionId;
			private readonly string sessionDirectoryPath;
			private readonly string manifestPath;
			private readonly OcrScanRequest request;
			private readonly JsonSerializerOptions jsonOptions;
			private int fileCounter;
			private bool enabled;

			private OcrArtifactSessionWriter(string SessionId, string SessionDirectoryPath, string ManifestPath, OcrScanRequest Request, JsonSerializerOptions JsonOptions)
			{
				this.sessionId = SessionId;
				this.sessionDirectoryPath = SessionDirectoryPath;
				this.manifestPath = ManifestPath;
				this.request = Request;
				this.jsonOptions = JsonOptions;
				this.enabled = true;
			}

			public static OcrArtifactSessionWriter Create(OcrScanRequest Request, JsonSerializerOptions JsonOptions)
			{
#if DEBUG || OCR_DEBUG_ARTIFACTS_TO_JID || OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
				if (Request.CaptureDebugArtifacts)
				{
					string SessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8];
					string Root = Path.Combine(FileSystem.CacheDirectory, "ocr-debug-sessions");
					string SessionDirectory = Path.Combine(Root, SessionId);
					Directory.CreateDirectory(SessionDirectory);
					string ManifestPath = Path.Combine(SessionDirectory, "manifest.json");
					return new OcrArtifactSessionWriter(SessionId, SessionDirectory, ManifestPath, Request, JsonOptions);
				}
#endif
				return new OcrArtifactSessionWriter(string.Empty, string.Empty, string.Empty, Request, JsonOptions)
				{
					enabled = false
				};
			}

			public void WriteRequest()
			{
				if (!this.enabled)
					return;

				object RequestPayload = new
				{
					this.request.TargetKind,
					this.request.ProviderId,
					this.request.SourceKind,
					this.request.ScanMode,
					this.request.SourceRotationDegrees,
					this.request.DocumentKindHint,
					this.request.DocumentSideHint,
					this.request.CaptureDebugArtifacts,
					Width = this.request.Image.Width,
					Height = this.request.Image.Height,
					this.request.Metadata
				};

				File.WriteAllText(Path.Combine(this.sessionDirectoryPath, "request.json"), JsonSerializer.Serialize(RequestPayload, this.jsonOptions));
			}

			public void CaptureImage(string Name, IMatrix Image, IReadOnlyDictionary<string, string>? Metadata = null)
			{
				if (!this.enabled)
					return;

				string SafeName = this.NextName(Name);
				File.WriteAllBytes(Path.Combine(this.sessionDirectoryPath, SafeName + ".png"), Bitmaps.EncodeAsPng(Image));
				File.WriteAllText(
					Path.Combine(this.sessionDirectoryPath, SafeName + ".json"),
					JsonSerializer.Serialize(new
					{
						Name,
						Image.Width,
						Image.Height,
						Metadata
					}, this.jsonOptions));
			}

			public void CaptureData(string Name, object Payload)
			{
				if (!this.enabled)
					return;

				File.WriteAllText(
					Path.Combine(this.sessionDirectoryPath, this.NextName(Name) + ".json"),
					JsonSerializer.Serialize(Payload, this.jsonOptions));
			}

			public OcrScanResult Complete(OcrScanResult Result)
			{
				if (!this.enabled)
					return Result;

				File.WriteAllText(
					Path.Combine(this.sessionDirectoryPath, "result.json"),
					JsonSerializer.Serialize(new
					{
						Result.TargetKind,
						Result.SourceKind,
						Result.ProviderId,
						Result.ValidationStatus,
						Result.RawText,
						Result.NormalizedText,
						Result.Confidence,
						Result.FailureReason,
						Result.Metadata,
						Result.Mrz
					}, this.jsonOptions));

				File.WriteAllText(
					this.manifestPath,
					JsonSerializer.Serialize(new
					{
						this.sessionId,
						CreatedUtc = DateTimeOffset.UtcNow,
						Request = "request.json",
						Result = "result.json"
					}, this.jsonOptions));

				return new OcrScanResult(
					Result.TargetKind,
					Result.SourceKind,
					Result.ProviderId,
					Result.ValidationStatus,
					Result.RawText,
					Result.NormalizedText,
					Result.Mrz,
					Result.Confidence,
					Result.FailureReason,
					Result.Metadata,
					new OcrArtifactBundle(this.sessionId, this.sessionDirectoryPath, this.manifestPath));
			}

			public void Dispose()
			{
			}

			private string NextName(string Name)
			{
				this.fileCounter++;
				return this.fileCounter.ToString("D2", CultureInfo.InvariantCulture) + "-" + Name;
			}
		}
	}
}
