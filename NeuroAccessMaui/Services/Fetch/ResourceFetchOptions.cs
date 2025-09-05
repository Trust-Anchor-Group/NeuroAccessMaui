using System;

namespace NeuroAccessMaui.Services.Fetch
{
    public sealed class ResourceFetchOptions
    {
        public string ParentId { get; init; } = string.Empty;
        public bool Permanent { get; init; } = false;
        /// <summary>
        /// Optional per-call timeout override. If null, defaults are used.
        /// </summary>
        public TimeSpan? Timeout { get; init; }
        /// <summary>
        /// Optional per-call retry attempts override. If null, defaults are used.
        /// </summary>
        public int? RetryAttempts { get; init; }
    }
}
