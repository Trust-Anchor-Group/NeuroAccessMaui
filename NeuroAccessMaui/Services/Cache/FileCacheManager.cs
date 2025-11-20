using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Cache;
using Waher.Content;
using Waher.Persistence;
using Waher.Persistence.Files.Searching;

namespace NeuroAccessMaui.Services.Cache
{
	internal class FileCacheManager
	{
		private readonly string folderPath;
		private readonly TimeSpan defaultExpiry;

		private static int evictionPerformed = 0;   // 0 = not yet, 1 = done


		public FileCacheManager(string folderName, TimeSpan defaultExpiry)
		{
			this.folderPath = Path.Combine(FileSystem.CacheDirectory, folderName);
			this.defaultExpiry = defaultExpiry;

			if (!Directory.Exists(this.folderPath))
				Directory.CreateDirectory(this.folderPath);
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
				{
					try { File.Delete(Entry.LocalFileName); }
					catch { /* log if desired */ }
				}
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

			if (Entry is null ||
				!File.Exists(Entry.LocalFileName))
			{
				if (Entry is not null)
				{
					try { File.Delete(Entry.LocalFileName); }
					catch { }
					await Database.Delete(Entry);
					await Database.Provider.Flush();
				}
				return (null, string.Empty);
			}

			// Only return non-Expired entries from this method.
			if (DateTime.UtcNow >= Entry.Expires)
				return (null, string.Empty);

			return (File.ReadAllBytes(Entry.LocalFileName), Entry.ContentType);
		}

		/// <summary>
		/// Tries to get a cached entry and indicates if it is past its expiry.
		/// Returns bytes for both fresh and Expired items (if file exists). Expired items are not deleted here.
		/// </summary>
		public async Task<(byte[]? Data, string ContentType, bool IsExpired)> TryGetWithExpiry(string Key)
		{
			if (string.IsNullOrEmpty(Key))
				return (null, string.Empty, false);

			CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
				new FilterFieldEqualTo("Url", Key));

			if (Entry is null)
				return (null, string.Empty, false);

			if (!File.Exists(Entry.LocalFileName))
			{
				try { await Database.Delete(Entry); }
				catch { }
				await Database.Provider.Flush();
				return (null, string.Empty, false);
			}

			bool Expired = DateTime.UtcNow >= Entry.Expires && Entry.Expires != DateTime.MaxValue;
			return (File.ReadAllBytes(Entry.LocalFileName), Entry.ContentType, Expired);
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

			string Ext = InternetContent.GetFileExtension(contentType);
			string Filename = Path.Combine(this.folderPath, $"{Guid.NewGuid():N}.{Ext}");

			await File.WriteAllBytesAsync(Filename, data);

			if (Existing is null)
			{
				await Database.Insert(new CacheEntry
				{
					Url = key,
					ParentId = parentId ?? string.Empty,
					LocalFileName = Filename,
					ContentType = contentType,
					Expires = Expires
				});
			}
			else
			{
				try { File.Delete(Existing.LocalFileName); }
				catch { }

				Existing.LocalFileName = Filename;
				Existing.ContentType = contentType;
				Existing.ParentId = parentId ?? string.Empty;
				Existing.Expires = Expires;

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

			try { File.Delete(Entry.LocalFileName); }
			catch
			{
				//Ignore as it may not exist or be accessible. (User clearing data etc)
			}


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
				try { File.Delete(Entry.LocalFileName); }
				catch { /* ignore */ }

				await Database.Delete(Entry);
				Removed++;
			}

			await Database.Provider.Flush();
			return Removed;
		}
	}

}
