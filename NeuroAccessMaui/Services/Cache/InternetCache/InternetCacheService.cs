using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Content;

namespace NeuroAccessMaui.Services.Cache.InternetCache
{
	/// <summary>
	/// Caches arbitrary Internet-fetchable content (images, JSON, blobs, etc.) locally.
	/// </summary>
	[Singleton]
	internal sealed class InternetCacheService : LoadableService, IInternetCacheService
	{
		private static readonly TimeSpan defaultExpiry = Constants.Cache.DefaultImageCache; // 7d
		private readonly FileCacheManager cacheManager;

		public InternetCacheService()
		{
			this.cacheManager = new FileCacheManager("InternetCache", defaultExpiry);
		}

		public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				try
				{
					this.EndLoad(true);
				}
				catch
				{
					this.EndLoad(false);
					throw;
				}
			}
		}

		public Task<(byte[]? Data, string ContentType)> TryGet(Uri Uri)
			=> this.cacheManager.TryGet(Uri.AbsoluteUri);

		public async Task<(byte[]? Data, string ContentType)> GetOrFetch(Uri Uri, string ParentId, bool Permanent)
		{
			// Check cache (allow expired entries as fallback)
			(byte[]? Cached, string CachedType, bool IsExpired) = await this.cacheManager.TryGetWithExpiry(Uri.AbsoluteUri);
			if (Cached is not null && !IsExpired)
				return (Cached, CachedType);

			ContentResponse Response = await InternetContent.GetAsync(
				Uri,
				null,
				App.ValidateCertificateCallback);

			if (Response.HasError || Response.Encoded is null || string.IsNullOrEmpty(Response.ContentType))
			{
				if (Cached is not null)
					return (Cached, string.IsNullOrEmpty(CachedType) ? "" : CachedType);

				return (null, string.Empty);
			}

			byte[] Bytes = Response.Encoded;
			string Type = Response.ContentType;

			// 🔽 NEW: Transparent decompression
			Bytes = TryDecompress(Bytes);

			await this.cacheManager.AddOrUpdate(Uri.AbsoluteUri, ParentId, Permanent, Bytes, Type);
			return (Bytes, Type);
		}

		public Task<bool> Remove(Uri Uri)
			=> this.cacheManager.Remove(Uri.AbsoluteUri);

		public Task MakeTemporary(string ParentId)
			=> this.cacheManager.MakeTemporary(ParentId);

		public Task MakePermanent(string ParentId)
			=> this.cacheManager.MakePermanent(ParentId);

		public Task<int> RemoveByParentId(string ParentId)
			=> this.cacheManager.RemoveByParentId(ParentId);

		#region 🔧 Decompression Helpers

		private static bool IsGzip(byte[] data)
			=> data.Length > 2 && data[0] == 0x1F && data[1] == 0x8B;

		private static byte[] TryDecompress(byte[] data)
		{
			try
			{
				if (IsGzip(data))
				{
					using var input = new MemoryStream(data);
					using var gzip = new GZipStream(input, CompressionMode.Decompress);
					using var output = new MemoryStream();
					gzip.CopyTo(output);
					return output.ToArray();
				}

				// Try Brotli
				try
				{
					using var input = new MemoryStream(data);
					using var br = new BrotliStream(input, CompressionMode.Decompress);
					using var output = new MemoryStream();
					br.CopyTo(output);
					return output.ToArray();
				}
				catch
				{
					// Not Brotli — return original
					return data;
				}
			}
			catch
			{
				// Failed decompression — return original
				return data;
			}
		}

		#endregion
	}
}
