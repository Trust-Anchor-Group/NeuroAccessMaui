namespace DevTestPlatform.Catalog;

/// <summary>Describes a current production screen or reusable component independently of its scenarios.</summary>
/// <param name="Key">The immutable catalog key.</param>
/// <param name="NameKey">The localized display-name resource key.</param>
/// <param name="AreaKey">The application area or component group resource key.</param>
/// <param name="IsComponent">Whether the subject is an independently reusable component.</param>
/// <param name="Tags">Additional searchable metadata.</param>
public sealed record ScreenDefinition(string Key, string NameKey, string AreaKey, bool IsComponent, string Tags);
