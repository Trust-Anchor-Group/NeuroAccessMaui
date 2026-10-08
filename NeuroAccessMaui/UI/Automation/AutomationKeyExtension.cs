using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace NeuroAccessMaui.UI.Automation;

/// <summary>Assigns contextual automation identity from a stable bound schema or opaque record key.</summary>
public sealed class AutomationKeyExtension : IMarkupExtension<IValueConverter>, IValueConverter
{
    /// <summary>Gets or sets the production-catalog namespace for this logical collection.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Provides the converter without registering application services.</summary>
    /// <param name="ServiceProvider">The XAML service provider.</param>
    /// <returns>This converter.</returns>
    public IValueConverter ProvideValue(IServiceProvider ServiceProvider) => this;

    object IMarkupExtension.ProvideValue(IServiceProvider ServiceProvider) => this.ProvideValue(ServiceProvider);

    /// <summary>Encodes a stable key while leaving not-yet-bound controls unidentified.</summary>
    /// <param name="Value">The stable key, never user-entered content.</param>
    /// <param name="TargetType">The target property type.</param>
    /// <param name="Parameter">Unused converter parameter.</param>
    /// <param name="Culture">Unused culture; technical identity is invariant.</param>
    /// <returns>The contextual identifier, or null until a nonempty key exists.</returns>
    public object? Convert(object? Value, Type TargetType, object? Parameter, CultureInfo Culture) =>
        Value is string Key && Key.Length > 0 ? AutomationIds.ForKey(this.Prefix, Key) : null;

    /// <summary>Rejects reverse conversion because automation metadata never updates the source key.</summary>
    /// <param name="Value">The target value.</param>
    /// <param name="TargetType">The source type.</param>
    /// <param name="Parameter">Unused converter parameter.</param>
    /// <param name="Culture">Unused culture.</param>
    /// <returns>No value; reverse conversion is unsupported.</returns>
    public object ConvertBack(object? Value, Type TargetType, object? Parameter, CultureInfo Culture) =>
        throw new NotSupportedException("Automation identity is one-way metadata.");
}
