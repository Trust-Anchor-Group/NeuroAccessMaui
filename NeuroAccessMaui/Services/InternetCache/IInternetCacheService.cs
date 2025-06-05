using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.InternetCache
{
    /// <summary>
    /// Defines a service that can cache arbitrary URI‐addressable content (e.g. images, JSON, blobs, etc.).
    /// </summary>
    public interface IInternetCacheService
    {
        /// <summary>
        /// Called when the service is starting up. Should create cache folder, evict old entries, etc.
        /// </summary>
        Task Load(bool IsResuming, CancellationToken CancellationToken);

        /// <summary>
        /// Try to get cached bytes + Content‐Type for a given URI. 
        /// If present and not expired, returns (byte[], contentType). Otherwise (null, string.Empty).
        /// </summary>
        Task<(byte[]?, string)> TryGet(Uri uri);

        /// <summary>
        /// Get the content for a URI. 
        /// If not already cached (or expired), fetches via InternetContent, caches it (under the given parentId),
        /// and returns the bytes+ContentType. 
        /// If it was already cached and valid, simply returns the cached bytes+ContentType.
        /// </summary>
        /// <param name="uri">The URI to fetch or retrieve from cache.</param>
        /// <param name="parentId">
        /// An optional parent‐ID you may use to group “related” entries (e.g. by screen/context). 
        /// </param>
        /// <param name="permanent">
        /// If true, the entry never expires (Expires = DateTime.MaxValue). 
        /// Otherwise, it expires after a default period (e.g. 24 hours).
        /// </param>
        Task<(byte[]?, string)> GetOrFetch(Uri uri, string parentId, bool permanent);

        /// <summary>
        /// Force‐remove a URI from cache (delete file + database entry). Returns true if it existed.
        /// </summary>
        Task<bool> Remove(Uri uri);

        /// <summary>
        /// Given a parentId, make all entries under that parent temporary (i.e. set Expires = UtcNow + 24 h if they were permanent).
        /// </summary>
        Task MakeTemporary(string parentId);

        /// <summary>
        /// Given a parentId, make all entries under that parent permanent (Expires = DateTime.MaxValue).
        /// </summary>
        Task MakePermanent(string parentId);
    }
}
