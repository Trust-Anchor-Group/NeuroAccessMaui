using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Cache.AttachmentCache
{
	/// <summary>
	/// Caches attachment content (e.g., images) locally by URL.
	/// </summary>
	[Singleton]
	internal sealed class AttachmentCacheService : LoadableService, IAttachmentCacheService
	{
		private static readonly TimeSpan defaultExpiry = TimeSpan.FromHours(24);
		private readonly FileCacheManager cacheManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="AttachmentCacheService"/> class.
		/// </summary>
		public AttachmentCacheService()
		{
			this.cacheManager = new FileCacheManager("Attachments", defaultExpiry);
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
		public Task<(byte[]? Data, string ContentType)> TryGet(string Url)
		{
			return this.cacheManager.TryGet(Url);
		}

		/// <inheritdoc/>
		public Task Add(string Url, string ParentId, bool Permanent, byte[] Data, string ContentType)
		{
			return this.cacheManager.AddOrUpdate(Url, ParentId, Permanent, Data, ContentType);
		}

		/// <inheritdoc/>
		public Task<bool> Remove(string Url)
		{
			return this.cacheManager.Remove(Url);
		}

		/// <inheritdoc/>
		public async Task<bool> RemoveAttachments(Attachment[]? Attachments)
		{
			if (Attachments is null)
				return false;

			bool Removed = false;
			foreach (Attachment Attachment in Attachments)
			{
				if (Attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
				{
					bool EntryRemoved = await this.cacheManager.Remove(Attachment.Url);
					if (EntryRemoved)
						Removed = true;
				}
			}

			return Removed;
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
