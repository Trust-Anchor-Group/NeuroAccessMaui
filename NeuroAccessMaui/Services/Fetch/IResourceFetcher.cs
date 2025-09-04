using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Fetch
{
    public interface IResourceFetcher
    {
        Task<ResourceResult<byte[]>> GetBytesAsync(Uri uri, ResourceFetchOptions options, CancellationToken ct = default);
    }
}

