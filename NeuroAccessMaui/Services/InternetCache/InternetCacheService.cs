using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Temporary;
using Waher.Content;
using NeuroAccessMaui.Services.AttachmentCache; // For CacheEntry
using NeuroAccessMaui.Services.InternetCache;    // For IInternetCacheService

namespace NeuroAccessMaui.Services.InternetCache
{
    /// <summary>
    /// Caches arbitrary Internet‐fetchable content (images, JSON, blobs, etc.) locally 
    /// so that repeated calls to the same URI return the cached bytes + ContentType, 
    /// until the entry expires. 
    /// </summary>
    [Singleton]
    internal sealed class InternetCacheService : LoadableService, IInternetCacheService
    {
        // By default, temporary entries expire 24 hours after creation:
        private static readonly TimeSpan expiryTemporary = TimeSpan.FromHours(24);

        private const string cacheFolderName = "InternetCache";

        /// <summary>
        /// Creates a new instance of the <see cref="InternetCacheService"/> class.
        /// </summary>
        public InternetCacheService()
        {
        }

        #region Load / Evict Old Entries

        /// <inheritdoc/>
        public override async Task Load(bool IsResuming, CancellationToken CancellationToken)
        {
            if (this.BeginLoad(IsResuming, CancellationToken))
            {
                try
                {
                    // Ensure the on‐disk folder exists:
                    CreateCacheFolderIfNeeded();

                    if (!IsResuming)
                    {
                        // On “cold start,” remove any entries that are already expired:
                        await EvictOldEntries();
                    }

                    this.EndLoad(true);
                }
                catch (Exception Ex)
                {
                    ServiceRef.LogService.LogException(Ex);
                    this.EndLoad(false);
                }
            }
        }

        /// <summary>
        /// Remove all database entries whose Expires ≤ DateTime.UtcNow, 
        /// and delete their files from disk if they still exist.
        /// </summary>
        private static async Task EvictOldEntries()
        {
            try
            {
				// Find all CacheEntry items where Expires ≤ now:
				IEnumerable<CacheEntry> ExpiredEntries = await Database.FindDelete<CacheEntry>(
                    new FilterFieldLesserOrEqualTo("Expires", DateTime.UtcNow));

                foreach (CacheEntry Entry in ExpiredEntries)
                {
                    try
                    {
                        if (File.Exists(Entry.LocalFileName))
                            File.Delete(Entry.LocalFileName);
                    }
                    catch (Exception Ex2)
                    {
                        ServiceRef.LogService.LogException(Ex2);
                    }
                }
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }
        }

        #endregion

        #region TryGet

        /// <summary>
        /// Tries to get a cached blob for the specified URI.
        /// If found and not expired, returns (byte[], contentType). Otherwise (null, "").
        /// </summary>
        /// <param name="Uri">The URI whose cached content we want.</param>
        public async Task<(byte[]?, string)> TryGet(Uri Uri)
        {
            try
            {
                if (Uri is null)
                    return (null, string.Empty);

                // Store URIs internally as their absolute string form:
                string UrlKey = Uri.AbsoluteUri;

				CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
                    new FilterFieldEqualTo("Url", UrlKey));

                if (Entry is null)
                    return (null, string.Empty);

                bool FileStillExists = File.Exists(Entry.LocalFileName);

                // If expired (or file missing), delete and return null:
                if (DateTime.UtcNow >= Entry.Expires || !FileStillExists)
                {
                    if (FileStillExists)
                        File.Delete(Entry.LocalFileName);

                    await Database.Delete(Entry);
                    await Database.Provider.Flush();
                    return (null, string.Empty);
                }

                // Otherwise, return the cached bytes + ContentType:
                byte[] Data = File.ReadAllBytes(Entry.LocalFileName);
                return (Data, Entry.ContentType);
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
                return (null, string.Empty);
            }
        }

        #endregion

        #region GetOrFetch

        /// <summary>
        /// If the URI is already in cache and valid, returns the cached bytes + contentType.
        /// Otherwise, fetches from the Internet, adds to cache, and returns the new bytes+contentType.
        /// </summary>
        /// <param name="Uri">The URI to retrieve from cache or fetch anew.</param>
        /// <param name="ParentId">An optional parent ID to group related cache entries.</param>
        /// <param name="Permanent">
        /// If true, store with Expires = DateTime.MaxValue.
        /// If false, Expires = UtcNow + 24 hours (expiryTemporary).
        /// </param>
        public async Task<(byte[]?, string)> GetOrFetch(Uri Uri, string ParentId, bool Permanent)
        {
            if (Uri is null)
                return (null, string.Empty);

            // First try to get from cache:
            (byte[]? CachedData, string CachedContentType) = await this.TryGet(Uri);
            if (CachedData is not null)
                return (CachedData, CachedContentType);

            // Not in cache (or expired). Attempt to fetch from the Internet:
            try
            {
                ContentStreamResponse Response = await InternetContent.GetTempStreamAsync(Uri);
                Response.AssertOk();   

                // If Encoded is null or ContentType is null, treat as “no data”:
                if (Response.Encoded == null || string.IsNullOrEmpty(Response.ContentType))
                {
                    Response.Dispose();
                    return (null, string.Empty);
                }

                // Otherwise, copy the response.Encoded (TemporaryStream) to disk:
                string FetchedContentType = Response.ContentType;
                TemporaryStream TempStr = Response.Encoded;

                Response.Dispose();

                // Copy the TemporaryStream to a file in our cache folder:
                string CacheFolder = CreateCacheFolderIfNeeded();
                string Ext = InternetContent.GetFileExtension(FetchedContentType);
                string NewFileName = Path.Combine(CacheFolder, $"{Guid.NewGuid():N}.{Ext}");

                using (FileStream FileStream = File.Create(NewFileName))
                {
                    TempStr.Seek(0, SeekOrigin.Begin);
                    await TempStr.CopyToAsync(FileStream);
                }
                TempStr.Dispose();

                // Determine expiration:
                DateTime Expires = Permanent 
                    ? DateTime.MaxValue 
                    : (DateTime.UtcNow + expiryTemporary);

                // Check if an entry already exists (maybe in DB but file was missing):
                string UrlKey = Uri.AbsoluteUri;
                CacheEntry? ExistingEntry = await Database.FindFirstDeleteRest<CacheEntry>(
                    new FilterFieldEqualTo("Url", UrlKey));

                if (ExistingEntry is null)
                {
					// Create a brand‐new CacheEntry:
					CacheEntry Entry = new CacheEntry()
                    {
                        Url           = UrlKey,
                        ParentId      = ParentId ?? string.Empty,
                        LocalFileName = NewFileName,
                        ContentType   = FetchedContentType,
                        Expires       = Expires
                    };

                    await Database.Insert(Entry);
                }
                else
                {
                    // Replace file on disk if any, then update fields:
                    try
                    {
                        if (ExistingEntry.LocalFileName is not null 
                            && File.Exists(ExistingEntry.LocalFileName))
                        {
                            File.Delete(ExistingEntry.LocalFileName);
                        }
                    }
                    catch (Exception Ex2)
                    {
                        ServiceRef.LogService.LogException(Ex2);
                    }

                    ExistingEntry.LocalFileName = NewFileName;
                    ExistingEntry.ContentType   = FetchedContentType;
                    ExistingEntry.ParentId      = ParentId ?? string.Empty;
                    ExistingEntry.Expires       = Expires;
                    ExistingEntry.Url           = UrlKey;

                    await Database.Update(ExistingEntry);
                }

                // Flush so that the DB knows about it:
                await Database.Provider.Flush();

                // Return the file’s bytes + content type:
                byte[] FinalData = File.ReadAllBytes(NewFileName);
                return (FinalData, FetchedContentType);
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
                return (null, string.Empty);
            }
        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes the cached entry for this URI, if it exists.
        /// Deletes the on‐disk file & DB row.
        /// </summary>
        public async Task<bool> Remove(Uri Uri)
        {
            if (Uri is null)
                return false;

            string UrlKey = Uri.AbsoluteUri;
			CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
                new FilterFieldEqualTo("Url", UrlKey));

            if (Entry is null)
                return false;

            try
            {
                if (File.Exists(Entry.LocalFileName))
                    File.Delete(Entry.LocalFileName);

                await Database.Delete(Entry);
            }
            catch (Exception Ex)
            {
                ServiceRef.LogService.LogException(Ex);
            }

            return true;
        }

        #endregion

        #region MakeTemporary / MakePermanent

        /// <inheritdoc/>
        public async Task MakeTemporary(string parentId)
        {
			IEnumerable<CacheEntry> Entries = await Database.Find<CacheEntry>(
                new FilterFieldEqualTo("ParentId", parentId));

            foreach (CacheEntry Entry in Entries)
            {
                if (Entry.Expires == DateTime.MaxValue)
                {
                    Entry.Expires = DateTime.UtcNow + expiryTemporary;
                    await Database.Update(Entry);
                }
            }

            await Database.Provider.Flush();
        }

        /// <inheritdoc/>
        public async Task MakePermanent(string parentId)
        {
			IEnumerable<CacheEntry> Entries = await Database.Find<CacheEntry>(
                new FilterFieldEqualTo("ParentId", parentId));

            foreach (CacheEntry Entry in Entries)
            {
                if (Entry.Expires != DateTime.MaxValue)
                {
                    Entry.Expires = DateTime.MaxValue;
                    await Database.Update(Entry);
                }
            }

            await Database.Provider.Flush();
        }

        #endregion

        #region Helper: Cache Folder

        /// <summary>
        /// Ensures the cache folder exists; returns its full path.
        /// </summary>
        private static string CreateCacheFolderIfNeeded()
        {
            string CacheFolder = GetCacheFolder();
            if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);

            return CacheFolder;
        }

        /// <summary>
        /// Returns the full path to the cache directory for Internet‐fetched content.
        /// </summary>
        private static string GetCacheFolder()
        {
            // Use the platform’s cache directory + our own subfolder name:
            string OsCache = FileSystem.CacheDirectory; 
            return Path.Combine(OsCache, cacheFolderName);
        }

        #endregion
    }
}
