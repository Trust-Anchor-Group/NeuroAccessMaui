using System;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.Services.Cache.InternetCache;
using NeuroAccessMaui.Services.Resilience;
using NeuroAccessMaui.UI.MVVM.Policies;

namespace NeuroAccessMaui.Services.Fetch
{
    /// <summary>
    /// Default resource fetcher: retrieves content using Internet cache and applies resilience policies.
    /// </summary>
    public sealed class ResourceFetcher : IResourceFetcher
    {
        private readonly IInternetCacheService cache;

        public ResourceFetcher() : this(ServiceRef.InternetCacheService) { }
        public ResourceFetcher(IInternetCacheService cache)
        {
            this.cache = cache;
        }

        public async Task<ResourceResult<byte[]>> GetBytesAsync(Uri uri, ResourceFetchOptions options, CancellationToken ct = default)
        {
            (byte[]? Data, string ContentType) = await this.cache.TryGet(uri).ConfigureAwait(false);
            if (Data is not null)
                return new ResourceResult<byte[]>(Data, ResourceOrigin.Disk, ContentType);

            IAsyncPolicy timeout = Policies.Timeout(Constants.Timeouts.DownloadFile);
            IAsyncPolicy retry = Policies.Retry(
                maxAttempts: 3,
                delayProvider: (attempt, ex) => JitterBackoff.DecorrelatedJitter(TimeSpan.FromMilliseconds(200), attempt),
                shouldRetry: Transient.IsTransient);

            // Fetch via cache service under policies
            await Services.Resilience.PolicyRunner.RunAsync(async cts =>
            {
                (byte[]? Bytes, string Type) = await this.cache.GetOrFetch(uri, options.ParentId, options.Permanent).ConfigureAwait(false);
                Data = Bytes;
                ContentType = Type;
            }, ct, timeout, retry).ConfigureAwait(false);

            return new ResourceResult<byte[]>(Data, ResourceOrigin.Network, ContentType);
        }
    }
}
