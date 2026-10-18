using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using NeuroAccessMaui.Services.Kyc.ViewModels;

namespace NeuroAccessMaui.UI.Automation;

/// <summary>Identifies KYC inputs by their identity-property mapping.</summary>
[AcceptEmptyServiceProvider]
public sealed class KycFieldKeyExtension : IMarkupExtension<IValueConverter>, IValueConverter
{
	/// <summary>Gets or sets the registered field namespace in the shared automation catalog.</summary>
	public string Prefix { get; set; } = string.Empty;

	/// <summary>Provides the one-way metadata converter.</summary>
	/// <param name="ServiceProvider">The unused XAML service provider.</param>
	/// <returns>This converter.</returns>
	public IValueConverter ProvideValue(IServiceProvider ServiceProvider) => this;

	object IMarkupExtension.ProvideValue(IServiceProvider ServiceProvider) => this.ProvideValue(ServiceProvider);

	/// <summary>Uses a stable property mapping, or the schema ID for an unmapped input, without reading its value.</summary>
	/// <param name="Value">The field schema and its mappings.</param>
	/// <param name="TargetType">The target metadata type.</param>
	/// <param name="Parameter">The unused converter parameter.</param>
	/// <param name="Culture">The unused culture; identifiers are invariant.</param>
	/// <returns>The field's automation identifier, or null for an unbound field.</returns>
	public object? Convert(object? Value, Type TargetType, object? Parameter, CultureInfo Culture)
	{
		if (Value is not ObservableKycField Field)
			return null;

		string[] Mappings = Field.Mappings.Select(Mapping => Mapping.Key.ToUpperInvariant()).ToArray();
		string Key = new[] { "BDAY", "BMONTH", "BYEAR" }.All(Mappings.Contains)
			? "BDATE" : Mappings.FirstOrDefault() ?? Field.Id;
		return Key.Length > 0 ? AutomationIds.ForKey(this.Prefix, Key) : null;
	}

	/// <summary>Rejects reverse conversion so metadata cannot change a KYC field.</summary>
	/// <param name="Value">The target value.</param>
	/// <param name="TargetType">The source type.</param>
	/// <param name="Parameter">The unused converter parameter.</param>
	/// <param name="Culture">The unused culture.</param>
	/// <returns>No value; reverse conversion is unsupported.</returns>
	public object ConvertBack(object? Value, Type TargetType, object? Parameter, CultureInfo Culture) =>
		throw new NotSupportedException("KYC automation metadata is one-way.");
}
