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
		private const string ProviderId = OnnxMrzScanEngine.ProviderIdentifier;

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

			MrzFrameProcessingResult Processing = this.scanEngine.ProcessFrame(
				Request.Image,
				Request.SourceRotationDegrees,
				Request.DocumentKindHint,
				Metadata,
				CancellationToken);
			CaptureArtifacts(ArtifactSession, Processing);
			OcrScanResult Result = CreateResult(Request, Processing);
			return Task.FromResult(ArtifactSession.Complete(Result));
		}

		private static void CaptureArtifacts(OcrArtifactSessionWriter ArtifactSession, MrzFrameProcessingResult Processing)
		{
			MrzPreviewAnalysisResult Analysis = Processing.Analysis;
			ArtifactSession.CaptureData("mrzscanner-detection", new
			{
				Analysis.ImageWidth,
				Analysis.ImageHeight,
				Analysis.DetectionScore,
				TopLeft = Analysis.MrzQuad?.TopLeft,
				TopRight = Analysis.MrzQuad?.TopRight,
				BottomRight = Analysis.MrzQuad?.BottomRight,
				BottomLeft = Analysis.MrzQuad?.BottomLeft,
				Analysis.GuidanceState,
				Analysis.FailureCategory
			});

			if (Processing.RecognitionInput is not null)
				ArtifactSession.CaptureImage("mrzscanner-recognition-input", Processing.RecognitionInput);

			if (Processing.Recognition is not null)
			{
				ArtifactSession.CaptureData("mrzscanner-recognition", new
				{
					Processing.Recognition.RawText,
					Processing.Recognition.Lines,
					Processing.Recognition.Confidence,
					Processing.Recognition.Metadata
				});
			}

			if (Analysis.Read is not null)
			{
				ArtifactSession.CaptureData("mrz-decoded-result", new
				{
					Analysis.Read.RawText,
					Analysis.Read.NormalizedText,
					Analysis.Read.Format,
					Analysis.Read.RecognitionConfidence,
					Analysis.Read.DecodeScore,
					Analysis.Read.IsStrictSuccess,
					Analysis.Read.PassedCheckCount,
					Analysis.Read.RepairCount,
					Analysis.Read.Document,
					Analysis.Read.Metadata
				});
			}
		}

		private static OcrScanResult CreateResult(OcrScanRequest Request, MrzFrameProcessingResult Processing)
		{
			MrzPreviewAnalysisResult Analysis = Processing.Analysis;
			Dictionary<string, string> Metadata = new Dictionary<string, string>(Analysis.Metadata);
			MrzFrameRead? Read = Analysis.Read;
			if (Read is not null && Read.IsStrictSuccess && Read.Document is not null)
				return MrzReadDecoder.CreateSuccessResult(Read, Request.TargetKind, Request.SourceKind, Metadata);

			Metadata["MrzFailureCategory"] = Analysis.FailureCategory.ToString();
			return new OcrScanResult(
				Request.TargetKind,
				Request.SourceKind,
				ProviderId,
				Processing.IsModelFailure ? OcrScanValidationStatus.Failed : OcrScanValidationStatus.Invalid,
				Read?.RawText ?? Processing.Recognition?.RawText ?? string.Empty,
				Read?.NormalizedText ?? string.Empty,
				null,
				Read?.RecognitionConfidence ?? 0f,
				ResolveFailureReason(Processing),
				Metadata);
		}

		private static string ResolveFailureReason(MrzFrameProcessingResult Processing)
		{
			if (Processing.IsModelFailure)
				return "The ONNX MRZ models are unavailable or failed to run.";

			return Processing.Analysis.FailureCategory switch
			{
				MrzScanFailureCategory.MrzRegionNotFound => "No plausible MRZ region was localized.",
				MrzScanFailureCategory.MrzTooSmall => "The MRZ text was too small to read reliably.",
				MrzScanFailureCategory.GlareInMrzArea => "Glare in the MRZ area prevented a valid read.",
				MrzScanFailureCategory.OcrNoText => "No MRZ candidate could be normalized from the OCR text.",
				_ => "MRZ validation failed."
			};
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
