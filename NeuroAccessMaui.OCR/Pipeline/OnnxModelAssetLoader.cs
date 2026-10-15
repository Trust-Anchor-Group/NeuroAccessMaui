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
				await using Stream Source = await OpenPackagedAssetAsync(NormalizedAssetPath).ConfigureAwait(false);
				if (IsCachedCopyComplete(LocalPath, TryGetLength(Source)))
					return LocalPath;

				// The model is copied next to the cached path and then moved into place, so an interrupted copy never
				// leaves a truncated model that every later load would fail on.
				string TemporaryPath = LocalPath + ".tmp";
				try
				{
					await using (FileStream Target = File.Create(TemporaryPath))
					{
						await Source.CopyToAsync(Target, CancellationToken).ConfigureAwait(false);
					}

					File.Move(TemporaryPath, LocalPath, true);
				}
				catch
				{
					TryDeleteFile(TemporaryPath);
					throw;
				}

				return LocalPath;
			}
			finally
			{
				this.syncRoot.Release();
			}
		}

		private static async Task<Stream> OpenPackagedAssetAsync(string AssetPath)
		{
			try
			{
				return await FileSystem.OpenAppPackageFileAsync(AssetPath).ConfigureAwait(false);
			}
			catch (FileNotFoundException)
			{
				throw new FileNotFoundException("The packaged OCR model asset was not found. Add it under Resources/Raw/OcrModels and ensure it is included as a MauiAsset.", AssetPath);
			}
		}

		/// <summary>
		/// Checks that a cached model exists and, when the packaged size is known, that it has that size.
		/// </summary>
		/// <param name="LocalPath">The cached model path.</param>
		/// <param name="ExpectedLength">The packaged model size, or <c>null</c> if the platform cannot report it cheaply.</param>
		/// <returns><c>true</c> if the cached model can be used; otherwise <c>false</c>.</returns>
		private static bool IsCachedCopyComplete(string LocalPath, long? ExpectedLength)
		{
			FileInfo CachedFile = new FileInfo(LocalPath);
			return CachedFile.Exists &&
				CachedFile.Length > 0 &&
				(!ExpectedLength.HasValue || CachedFile.Length == ExpectedLength.Value);
		}

		/// <summary>
		/// Gets the length of a packaged asset stream without reading it. Android asset streams cannot seek, so their length
		/// is unknown.
		/// </summary>
		/// <param name="Source">The packaged asset stream.</param>
		/// <returns>The stream length, or <c>null</c> if it is unknown.</returns>
		private static long? TryGetLength(Stream Source)
		{
			try
			{
				return Source.CanSeek ? Source.Length : null;
			}
			catch (NotSupportedException)
			{
				return null;
			}
		}

		private static void TryDeleteFile(string FilePath)
		{
			try
			{
				File.Delete(FilePath);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		internal string GetCachedAssetPath(string AssetPath, CancellationToken CancellationToken)
		{
			return this.GetCachedAssetPathAsync(AssetPath, CancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
		}
	}
}
