using System;
using System.IO;
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
		private static readonly TimeSpan defaultExpiry = TimeSpan.FromHours(24);
		private readonly FileCacheManager cacheManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="InternetCacheService"/> class.
		/// </summary>
		public InternetCacheService()
		{
			this.cacheManager = new FileCacheManager("InternetCache", defaultExpiry);
		}

		/// <inheritdoc/>
		public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				try
				{
					if (!IsResuming)
						await this.cacheManager.EvictOldEntries();

					this.EndLoad(true);
				}
				catch (Exception)
				{
					this.EndLoad(false);
					throw;
				}
			}
		}

		/// <inheritdoc/>
		public Task<(byte[]? Data, string ContentType)> TryGet(Uri Uri)
		{
			return this.cacheManager.TryGet(Uri.AbsoluteUri);
		}

		/// <inheritdoc/>
		public async Task<(byte[]? Data, string ContentType)> GetOrFetch(Uri Uri, string ParentId, bool Permanent)
		{
			(byte[]? Data, string ContentType) = await this.TryGet(Uri);
			if (Data is not null)
				return (Data, ContentType);

			ContentResponse Response = await InternetContent.GetAsync(
				Uri,
				null,                               // Certificate
				App.ValidateCertificateCallback);
			if(Response.HasError)
				return (null, string.Empty);
			if (Response.Encoded is null || string.IsNullOrEmpty(Response.ContentType))
				return (null, string.Empty);

			byte[] Bytes = Response.Encoded;
			string Type = Response.ContentType;

			await this.cacheManager.AddOrUpdate(Uri.AbsoluteUri, ParentId, Permanent, Bytes, Type);
			return (Bytes, Type);
		}

		/// <inheritdoc/>
		public Task<bool> Remove(Uri Uri)
		{
			return this.cacheManager.Remove(Uri.AbsoluteUri);
		}

		/// <inheritdoc/>
		public Task MakeTemporary(string ParentId)
		{
			return this.cacheManager.MakeTemporary(ParentId);
		}

		/// <inheritdoc/>
		public Task MakePermanent(string ParentId)
		{
			return this.cacheManager.MakePermanent(ParentId);
		}
	}
}
