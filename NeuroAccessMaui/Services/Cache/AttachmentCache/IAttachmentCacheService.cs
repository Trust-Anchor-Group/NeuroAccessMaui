using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Cache.AttachmentCache
{

	/// <summary>
	/// Defines operations for caching attachment content (e.g., images) by URL.
	/// </summary>
	[DefaultImplementation(typeof(AttachmentCacheService))]
	public interface IAttachmentCacheService : ILoadableService
	{
		/// <summary>
		/// Tries to retrieve cached attachment data for the specified URL.
		/// </summary>
		/// <param name="Url">The URL key of the attachment.</param>
		/// <returns>A tuple containing the cached bytes and content type, or (null, string.Empty).</returns>
		Task<(byte[]? Data, string ContentType)> TryGet(string Url);

		/// <summary>
		/// Adds or updates an attachment in the cache.
		/// </summary>
		/// <param name="Url">The URL key of the attachment.</param>
		/// <param name="ParentId">Associated parent ID for grouping entries.</param>
		/// <param name="Permanent">True to never expire; false for default expiry.</param>
		/// <param name="Data">The attachment data bytes.</param>
		/// <param name="ContentType">The MIME content type of the data.</param>
		Task Add(string Url, string ParentId, bool Permanent, byte[] Data, string ContentType);

		/// <summary>
		/// Removes the cached entry for the specified URL, if it exists.
		/// </summary>
		/// <param name="Url">The URL key of the attachment.</param>
		/// <returns>True if the entry was found and removed; otherwise false.</returns>
		Task<bool> Remove(string Url);

		/// <summary>
		/// Removes any attachments found in the given array from the cache.
		/// </summary>
		/// <param name="Attachments">An array of attachments to remove.</param>
		/// <returns>True if at least one entry was removed; otherwise false.</returns>
		Task<bool> RemoveAttachments(Attachment[]? Attachments);

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
