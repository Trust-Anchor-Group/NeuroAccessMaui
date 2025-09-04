using System;

namespace NeuroAccessMaui.Services.Fetch
{
    public sealed class ResourceFetchOptions
    {
        public string ParentId { get; init; } = string.Empty;
        public bool Permanent { get; init; } = false;
    }
}

