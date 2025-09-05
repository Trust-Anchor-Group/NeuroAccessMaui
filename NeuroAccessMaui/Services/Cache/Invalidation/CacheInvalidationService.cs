using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Services.Cache.InternetCache;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Cache.Invalidation
{
    [Singleton]
    internal sealed class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly IInternetCacheService cache;

        public CacheInvalidationService() : this(ServiceRef.InternetCacheService) { }
        public CacheInvalidationService(IInternetCacheService cache)
        {
            this.cache = cache;
        }

        public async Task<int> InvalidateByParentId(string parentId, string scope)
        {
            int n = await this.cache.RemoveByParentId(parentId).ConfigureAwait(false);
            WeakReferenceMessenger.Default.Send(new CacheInvalidatedMessage(scope, parentId, null));
            return n;
        }

        public async Task<int> InvalidateByKeys(IEnumerable<string> keys, string scope)
        {
            int count = 0;
            foreach (string key in keys ?? Enumerable.Empty<string>())
            {
                if (await this.cache.Remove(new System.Uri(key)).ConfigureAwait(false))
                    count++;
            }
            WeakReferenceMessenger.Default.Send(new CacheInvalidatedMessage(scope, null, keys?.ToList()));
            return count;
        }
    }
}

