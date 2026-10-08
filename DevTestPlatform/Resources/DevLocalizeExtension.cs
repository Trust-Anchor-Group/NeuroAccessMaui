using Microsoft.Maui.Controls.Xaml;

namespace DevTestPlatform.Resources;

/// <summary>Supplies localized developer labels in XAML.</summary>
[ContentProperty(nameof(Key))]
public sealed class DevLocalizeExtension : IMarkupExtension<string>
{
    /// <summary>Gets or sets the developer resource key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <inheritdoc/>
    public string ProvideValue(IServiceProvider ServiceProvider) => DevText.Get(this.Key);
    object IMarkupExtension.ProvideValue(IServiceProvider ServiceProvider) => this.ProvideValue(ServiceProvider);
}
