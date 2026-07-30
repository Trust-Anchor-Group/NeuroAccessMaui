using System.IO.Compression;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Services
{
	/// <summary>
	/// Shares persisted OCR debug-artifact bundles as zip archives.
	/// </summary>
	public sealed class OcrArtifactExportService : IOcrArtifactExportService
	{
		/// <inheritdoc/>
		public async Task ShareAsync(OcrArtifactBundle Bundle, CancellationToken CancellationToken)
		{
			ArgumentNullException.ThrowIfNull(Bundle);
			CancellationToken.ThrowIfCancellationRequested();

#if DEBUG || OCR_DEBUG_ARTIFACTS_TO_JID || OCR_DEBUG_ARTIFACTS_NATIVE_SHARE
			if (!Directory.Exists(Bundle.SessionDirectoryPath))
				throw new DirectoryNotFoundException(Bundle.SessionDirectoryPath);

			string ExportDirectory = Path.Combine(FileSystem.CacheDirectory, "ocr-debug-exports");
			Directory.CreateDirectory(ExportDirectory);

			string ArchivePath = Path.Combine(ExportDirectory, Bundle.SessionId + ".zip");
			if (File.Exists(ArchivePath))
				File.Delete(ArchivePath);

			ZipFile.CreateFromDirectory(Bundle.SessionDirectoryPath, ArchivePath, CompressionLevel.Fastest, false);

			await Share.Default.RequestAsync(new ShareFileRequest
			{
				Title = "Share OCR debug artifacts",
				File = new ShareFile(ArchivePath)
			});
#else
			await Task.CompletedTask;
#endif
		}
	}
}
