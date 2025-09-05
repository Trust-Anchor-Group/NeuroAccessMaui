using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NeuroAccessMaui.UI.MVVM.Policies;

namespace NeuroAccessMaui.Services.Fetch
{
    public interface IResourceFetcher
    {
        Task<ResourceResult<byte[]>> GetBytesAsync(Uri uri, ResourceFetchOptions options, CancellationToken ct = default);

        /// <summary>
        /// Retrieves bytes for a URI with optional custom resilience policies. When <paramref name="policies"/> is null,
        /// the fetcher applies its default timeout and retry policies.
        /// </summary>
        Task<ResourceResult<byte[]>> GetBytesAsync(
            Uri uri,
            ResourceFetchOptions options,
            IEnumerable<IAsyncPolicy>? policies,
            CancellationToken ct = default);
    }
}
