using System;
using System.Threading;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Cache.InternetCache
{

	/// <summary>
	/// Defines operations for caching arbitrary internet-fetchable content.
	/// </summary>
	[DefaultImplementation(typeof(InternetCacheService))]
	public interface IInternetCacheService : ILoadableService
	{
		/// <summary>
		/// Tries to retrieve a cached blob for the specified URI.
		/// </summary>
		/// <param name="Uri">The URI key of the content.</param>
		/// <returns>A tuple containing the cached bytes and content type, or (null, string.Empty).</returns>
		Task<(byte[]? Data, string ContentType)> TryGet(Uri Uri);

		/// <summary>
		/// Retrieves content for the specified URI from cache or fetches and caches it if missing.
		/// </summary>
		/// <param name="Uri">The URI key of the content.</param>
		/// <param name="ParentId">An optional parent ID to group related cache entries.</param>
		/// <param name="Permanent">If true, store with Expires = DateTime.MaxValue; if false, temporary expiry.</param>
		/// <returns>A tuple containing the file bytes and content type.</returns>
		Task<(byte[]? Data, string ContentType)> GetOrFetch(Uri Uri, string ParentId, bool Permanent);

		/// <summary>
		/// Removes the cached entry for the specified URI, if it exists.
		/// </summary>
		/// <param name="Uri">The URI key of the content.</param>
		/// <returns>True if the entry was found and removed; otherwise false.</returns>
		Task<bool> Remove(Uri Uri);

		/// <summary>
		/// Marks items in the cache with the given parent ID as temporary.
		/// </summary>
		/// <param name="ParentId">The parent ID of the items to mark.</param>
		Task MakeTemporary(string ParentId);

		/// <summary>
		/// Marks items in the cache with the given parent ID as permanent.
		/// </summary>
		/// <param name="ParentId">The parent ID of the items to mark.</param>
		Task MakePermanent(string ParentId);
	}
}
