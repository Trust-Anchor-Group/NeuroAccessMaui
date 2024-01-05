using System.Net.Mime;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.AttachmentCache
{
	///<inheritdoc cref="IAttachmentCacheService"/>
	[Singleton]
	internal sealed partial class AttachmentCacheService : LoadableService, IAttachmentCacheService
	{
		private static readonly TimeSpan expiryTemporary = TimeSpan.FromHours(24);
		private const string cacheFolderName = "Attachments";

		/// <summary>
		/// Creates a new instance of the <see cref="AttachmentCacheService"/> class.
		/// </summary>
		public AttachmentCacheService()
		{
		}

		/// <inheritdoc/>
		public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				try
				{
					CreateCacheFolderIfNeeded();

					if (!IsResuming)
						await EvictOldEntries();

					this.EndLoad(true);
				}
				catch (Exception e)
				{
					ServiceRef.LogService.LogException(e);
					this.EndLoad(false);
				}
			}
		}

		/// <summary>
		/// Tries to get a cached image given the specified url.
		/// </summary>
		/// <param name="Url">The url of the image to get.</param>
		/// <returns>If entry was found in the cache, the binary data of the image together with the Content-Type of the data.</returns>
		public async Task<(byte[]?, string)> TryGet(string Url)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(Url) || !Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
					return (null, string.Empty);

				CacheEntry Entry = await Database.FindFirstDeleteRest<CacheEntry>(new FilterFieldEqualTo("Url", Url));

				if (Entry is null)
					return (null, string.Empty);

				bool Exists = File.Exists(Entry.LocalFileName);

				if (DateTime.UtcNow >= Entry.Expires || !Exists)
				{
					if (Exists)
						File.Delete(Entry.LocalFileName);

					await Database.Delete(Entry);
					await Database.Provider.Flush();

					return (null, string.Empty);
				}

				return (File.ReadAllBytes(Entry.LocalFileName), Entry.ContentType);
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
				return (null, string.Empty);
			}
		}

		/// <summary>
		/// Adds an image to the cache.
		/// </summary>
		/// <param name="Url">The url, which is the key for accessing it later.</param>
		/// <param name="ParentId">Associated Legal or Contract ID (Parent ID)</param>
		/// <param name="Permanent">If attachment is permanent or temporary.</param>
		/// <param name="Data">Binary data of image</param>
		/// <param name="ContentType">Content-Type of data.</param>
		public async Task Add(string Url, string ParentId, bool Permanent, byte[] Data, string ContentType)
		{
			if (string.IsNullOrWhiteSpace(Url) ||
				!Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute) ||
				Data is null ||
				string.IsNullOrWhiteSpace(ContentType))
			{
				return;
			}

			string CacheFolder = CreateCacheFolderIfNeeded();

			CacheEntry Entry = await Database.FindFirstDeleteRest<CacheEntry>(new FilterFieldEqualTo("Url", Url));
			DateTime Expires = Permanent ? DateTime.MaxValue : (DateTime.UtcNow + expiryTemporary);

			if (Entry is null)
			{
				Entry = new CacheEntry()
				{
					Expires = Expires,
					ParentId = ParentId,
					LocalFileName = Path.Combine(CacheFolder, Guid.NewGuid().ToString() + ".bin"),
					Url = Url,
					ContentType = ContentType
				};

				await Database.Insert(Entry);
			}
			else
			{
				Entry.Expires = Expires;
				Entry.ParentId = ParentId;
				Entry.ContentType = ContentType;

				await Database.Update(Entry);
			}

			File.WriteAllBytes(Entry.LocalFileName, Data);

			await Database.Provider.Flush();
		}

		/// <summary>
		/// Removes an image from the cache.
		/// </summary>
		/// <param name="Url">URL of image.</param>
		/// <returns>If entry was found and removed.</returns>
		public async Task<bool> Remove(string Url)
		{
			if (string.IsNullOrWhiteSpace(Url) || !Uri.IsWellFormedUriString(Url, UriKind.RelativeOrAbsolute))
				return false;

			CacheEntry Entry = await Database.FindFirstDeleteRest<CacheEntry>(new FilterFieldEqualTo("Url", Url));
			if (Entry is null)
				return false;

			try
			{
				if (File.Exists(Entry.LocalFileName))
					File.Delete(Entry.LocalFileName);

				await Database.Delete(Entry);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			return true;
		}

		/// <summary>
		/// Removes any attachments found in an identity, from the cache.
		/// </summary>
		/// <param name="Attachments">Optional array of attachments to process.</param>
		/// <returns>If attachments were found and removed.</returns>
		public async Task<bool> RemoveAttachments(Attachment[]? Attachments)
		{
			if (Attachments is null)
				return false;

			bool Removed = false;

			foreach (Attachment Attachment in Attachments)
			{
				if (!Attachment.ContentType.StartsWith("image/", StringComparison.Ordinal))
					continue;

				if (await this.Remove(Attachment.Url))
					Removed = true;
			}

			return Removed;
		}

		private static string CreateCacheFolderIfNeeded()
		{
			string CacheFolder = GetCacheFolder();

			if (!Directory.Exists(CacheFolder))
				Directory.CreateDirectory(CacheFolder);

			return CacheFolder;
		}

		private static string GetCacheFolder()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), cacheFolderName);
		}

		private static async Task EvictOldEntries()
		{
			try
			{
				foreach (CacheEntry Entry in await Database.FindDelete<CacheEntry>(
					new FilterFieldLesserOrEqualTo("Expires", DateTime.UtcNow)))
				{
					try
					{
						if (File.Exists(Entry.LocalFileName))
							File.Delete(Entry.LocalFileName);
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
