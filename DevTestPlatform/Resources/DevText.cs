using System.Globalization;
using System.Resources;

namespace DevTestPlatform.Resources;

/// <summary>Resolves developer-tooling labels independently of production localization resources.</summary>
public static class DevText
{
    private static readonly ResourceManager resourceManager = new ResourceManager("DevTestPlatform.Resources.Languages.DevResources", typeof(DevText).Assembly);
    /// <summary>Gets a developer label in the current UI culture.</summary>
    /// <param name="Key">The resource key.</param>
    /// <returns>The localized label, falling back to the key if missing.</returns>
    public static string Get(string Key) => resourceManager.GetString(Key, CultureInfo.CurrentUICulture) ?? Key;
}
