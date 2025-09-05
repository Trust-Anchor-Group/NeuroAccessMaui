namespace NeuroAccessMaui.Services.Fetch
{
    public enum ResourceOrigin { Memory, Disk, Network, NotModified, Fallback }

    public sealed record ResourceResult<T>(T? Value, ResourceOrigin Origin, string ContentType = "");
}

