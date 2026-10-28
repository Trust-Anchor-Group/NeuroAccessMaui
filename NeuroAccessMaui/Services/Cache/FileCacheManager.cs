using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Cache
{
	/// <summary>
	/// Stores cached content in the encrypted object database, with expiry and grouping by parent ID.
	/// </summary>
	/// <remarks>
	/// Older versions wrote the content as plain files in a cache folder and kept only the file name in the database.
	/// Such entries are moved into the database when read, and all at once by <see cref="MigrateLegacyFilesAsync"/>.
	/// </remarks>
	internal class FileCacheManager
	{
		private readonly string folderPath;
		private readonly TimeSpan defaultExpiry;

		private static int evictionPerformed = 0;   // 0 = not yet, 1 = done

		/// <summary>
		/// Creates a cache manager for one cache.
		/// </summary>
		/// <param name="folderName">Name of the folder older versions stored the cached files in.</param>
		/// <param name="defaultExpiry">How long entries that are not permanent are kept.</param>
		public FileCacheManager(string folderName, TimeSpan defaultExpiry)
		{
			this.folderPath = Path.Combine(FileSystem.CacheDirectory, folderName);
			this.defaultExpiry = defaultExpiry;
		}

		public async Task EvictOldEntries()
		{
			try
			{
				// Only allow the first caller to actually perform eviction
				// This is not really needed,
				// but added justin case there ever is a situations where the cache contains a ridicoulus amount of files
				if (Interlocked.Exchange(ref evictionPerformed, 1) == 1)
					return;

				IEnumerable<CacheEntry> Expired = await Database.FindDelete<CacheEntry>(
					new FilterFieldLesserOrEqualTo("Expires", DateTime.UtcNow));

				foreach (CacheEntry Entry in Expired)
					DeleteLegacyFile(Entry);
			}
			catch
			{
				// Ignore
			}
		}

		public async Task<(byte[]?, string)> TryGet(string Key)
		{
			if (string.IsNullOrEmpty(Key))
				return (null, string.Empty);

			CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
				new FilterFieldEqualTo("Url", Key));

			if (Entry is not null)
				await this.TryMigrateLegacyEntryAsync(Entry);

			if (Entry is null || Entry.Data is null || Entry.Data.Length == 0)
			{
				if (Entry is not null)
				{
					DeleteLegacyFile(Entry);
					await Database.Delete(Entry);
					await Database.Provider.Flush();
				}
				return (null, string.Empty);
			}

			// Only return non-Expired entries from this method.
			if (DateTime.UtcNow >= Entry.Expires)
				return (null, string.Empty);

			return (Entry.Data, Entry.ContentType);
		}

		/// <summary>
		/// Tries to get a cached entry and indicates if it is past its expiry.
		/// Returns bytes for both fresh and Expired items (if data exists). Expired items are not deleted here.
		/// </summary>
		public async Task<(byte[]? Data, string ContentType, bool IsExpired)> TryGetWithExpiry(string Key)
		{
			if (string.IsNullOrEmpty(Key))
				return (null, string.Empty, false);

			CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
				new FilterFieldEqualTo("Url", Key));

			if (Entry is null)
				return (null, string.Empty, false);

			await this.TryMigrateLegacyEntryAsync(Entry);

			if (Entry.Data is null || Entry.Data.Length == 0)
			{
				DeleteLegacyFile(Entry);

				try { await Database.Delete(Entry); }
				catch { }
				await Database.Provider.Flush();
				return (null, string.Empty, false);
			}

			bool Expired = DateTime.UtcNow >= Entry.Expires && Entry.Expires != DateTime.MaxValue;
			return (Entry.Data, Entry.ContentType, Expired);
		}

		public async Task AddOrUpdate(string key, string parentId, bool permanent,
									  byte[] data, string contentType)
		{
			if (string.IsNullOrEmpty(key) || data is null || string.IsNullOrEmpty(contentType))
				return;

			DateTime Expires = permanent
				? DateTime.MaxValue
				: DateTime.UtcNow + this.defaultExpiry;

			CacheEntry? Existing = await Database.FindFirstDeleteRest<CacheEntry>(
				new FilterFieldEqualTo("Url", key));

			if (Existing is null)
			{
				await Database.Insert(new CacheEntry
				{
					Url = key,
					ParentId = parentId ?? string.Empty,
					LocalFileName = string.Empty,
					ContentType = contentType,
					Expires = Expires,
					Data = data
				});
			}
			else
			{
				DeleteLegacyFile(Existing);

				Existing.LocalFileName = string.Empty;
				Existing.ContentType = contentType;
				Existing.ParentId = parentId ?? string.Empty;
				Existing.Expires = Expires;
				Existing.Data = data;

				await Database.Update(Existing);
			}

			await Database.Provider.Flush();
		}

		public async Task<bool> Remove(string Key)
		{
			if (string.IsNullOrEmpty(Key))
				return false;

			CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
				new FilterFieldEqualTo("Url", Key));

			if (Entry is null)
				return false;

			DeleteLegacyFile(Entry);

			await Database.Delete(Entry);
			await Database.Provider.Flush();
			return true;
		}

		public async Task MakeTemporary(string parentId)
		{
			foreach (CacheEntry Entry in await Database.Find<CacheEntry>(
				new FilterFieldEqualTo("ParentId", parentId)))
			{
				if (Entry.Expires == DateTime.MaxValue)
				{
					Entry.Expires = DateTime.UtcNow + this.defaultExpiry;
					await Database.Update(Entry);
				}
			}
			await Database.Provider.Flush();
		}

		public async Task MakePermanent(string parentId)
		{
			foreach (CacheEntry Entry in await Database.Find<CacheEntry>(
				new FilterFieldEqualTo("ParentId", parentId)))
			{
				if (Entry.Expires != DateTime.MaxValue)
				{
					Entry.Expires = DateTime.MaxValue;
					await Database.Update(Entry);
				}
			}
			await Database.Provider.Flush();
		}

		public async Task<int> RemoveByParentId(string parentId)
		{
			int Removed = 0;
			foreach (CacheEntry Entry in await Database.Find<CacheEntry>(
				new FilterFieldEqualTo("ParentId", parentId)))
			{
				DeleteLegacyFile(Entry);

				await Database.Delete(Entry);
				Removed++;
			}

			await Database.Provider.Flush();
			return Removed;
		}

		/// <summary>
		/// Moves cache entries that older versions stored as plain files on disk into the database, deletes those files and
		/// then the old cache folder. Safe to call more than once.
		/// </summary>
		/// <remarks>
		/// The scan reads the whole cache table, so once it has completed for this cache folder a flag is persisted and later
		/// calls return at once. Entries read before the scan has run are migrated one by one by the read methods.
		/// </remarks>
		public async Task MigrateLegacyFilesAsync()
		{
			string CompletedFlag = Constants.Settings.LegacyCacheMigrationCompletedPrefix + Path.GetFileName(this.folderPath);

			try
			{
				if (await RuntimeSettings.GetAsync(CompletedFlag, false))
					return;

				IEnumerable<CacheEntry> LegacyEntries = await Database.Find<CacheEntry>(
					new FilterNot(new FilterFieldEqualTo("LocalFileName", string.Empty)));

				foreach (CacheEntry Entry in LegacyEntries)
					await this.MigrateLegacyEntryAsync(Entry);

				await Database.Provider.Flush();

				if (Directory.Exists(this.folderPath))
				{
					try { Directory.Delete(this.folderPath, true); }
					catch { }
				}

				await RuntimeSettings.SetAsync(CompletedFlag, true);
			}
			catch (Exception Ex)
			{
				// Best effort: it is tried again at the next start.
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Migrates a single entry if it still refers to a plain file, without letting failures escape.
		/// </summary>
		/// <param name="Entry">Entry that was just read from the database.</param>
		private async Task TryMigrateLegacyEntryAsync(CacheEntry Entry)
		{
			if (string.IsNullOrEmpty(Entry.LocalFileName) || (Entry.Data is not null && Entry.Data.Length > 0))
				return;

			try
			{
				await this.MigrateLegacyEntryAsync(Entry);
				await Database.Provider.Flush();
			}
			catch
			{
				// The caller treats an entry without data as missing.
			}
		}

		private async Task MigrateLegacyEntryAsync(CacheEntry Entry)
		{
			if (string.IsNullOrEmpty(Entry.LocalFileName))
				return;

			if (File.Exists(Entry.LocalFileName))
			{
				Entry.Data = await File.ReadAllBytesAsync(Entry.LocalFileName);
				File.Delete(Entry.LocalFileName);
			}
			else
			{
				Entry.Data = null;
			}

			Entry.LocalFileName = string.Empty;
			await Database.Update(Entry);
		}

		private static void DeleteLegacyFile(CacheEntry Entry)
		{
			if (string.IsNullOrEmpty(Entry.LocalFileName))
				return;

			try { File.Delete(Entry.LocalFileName); }
			catch
			{
				// Ignore as it may not exist or be accessible. (User clearing data etc)
			}
		}
	}

}
