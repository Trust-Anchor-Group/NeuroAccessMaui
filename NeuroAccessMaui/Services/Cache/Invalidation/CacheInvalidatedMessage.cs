using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Cache.Invalidation
{
    public sealed record CacheInvalidatedMessage(string Scope, string? ParentId = null, IReadOnlyList<string>? Keys = null);
}

