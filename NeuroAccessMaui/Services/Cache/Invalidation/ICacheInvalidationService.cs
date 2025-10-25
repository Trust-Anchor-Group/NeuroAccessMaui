using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Cache.Invalidation
{
    [DefaultImplementation(typeof(CacheInvalidationService))]
    public interface ICacheInvalidationService
    {
        Task<int> InvalidateByParentId(string parentId, string scope);
        Task<int> InvalidateByKeys(IEnumerable<string> keys, string scope);
    }
}

