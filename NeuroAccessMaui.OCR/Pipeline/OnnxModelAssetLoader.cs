using Microsoft.Maui.Storage;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Copies packaged ONNX model assets to a file-system location that ONNX Runtime can open.
	/// </summary>
	public sealed class OnnxModelAssetLoader : IDisposable
	{
		private const string CacheFolderName = "ocr-models";

		private readonly SemaphoreSlim syncRoot = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Releases resources owned by the asset loader.
		/// </summary>
		public void Dispose()
		{
			this.syncRoot.Dispose();
		}

		/// <summary>
		/// Resolves a packaged OCR model asset to a cached file path.
		/// </summary>
		/// <param name="AssetPath">The packaged MAUI asset path.</param>
		/// <param name="CancellationToken">The cancellation token.</param>
		/// <returns>The local file path that can be passed to ONNX Runtime.</returns>
		public async Task<string> GetCachedAssetPathAsync(string AssetPath, CancellationToken CancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(AssetPath);
			CancellationToken.ThrowIfCancellationRequested();

			string NormalizedAssetPath = AssetPath.Replace('\\', '/');
			string LocalPath = Path.Combine(
				FileSystem.CacheDirectory,
				CacheFolderName,
				NormalizedAssetPath.Replace('/', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(LocalPath) ?? FileSystem.CacheDirectory);

			await this.syncRoot.WaitAsync(CancellationToken).ConfigureAwait(false);
			try
			{
				if (File.Exists(LocalPath) && new FileInfo(LocalPath).Length > 0)
					return LocalPath;

				try
				{
					await using Stream Source = await FileSystem.OpenAppPackageFileAsync(NormalizedAssetPath).ConfigureAwait(false);
					await using FileStream Target = File.Create(LocalPath);
					await Source.CopyToAsync(Target, CancellationToken).ConfigureAwait(false);
				}
				catch (FileNotFoundException)
				{
					throw new FileNotFoundException("The packaged OCR model asset was not found. Add it under Resources/Raw/OcrModels and ensure it is included as a MauiAsset.", NormalizedAssetPath);
				}

				return LocalPath;
			}
			finally
			{
				this.syncRoot.Release();
			}
		}

		internal string GetCachedAssetPath(string AssetPath, CancellationToken CancellationToken)
		{
			return this.GetCachedAssetPathAsync(AssetPath, CancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
		}
	}
}
