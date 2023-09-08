namespace NeuroAccessMaui.Services.Localization;

[ContentProperty(nameof(Path))]
public class FlowDirectionExtension : IMarkupExtension<BindingBase>
{
	public string Path { get; set; } = nameof(LocalizationManager.FlowDirection);
	public BindingMode Mode { get; set; } = BindingMode.OneWay;
	public IValueConverter? Converter { get; set; } = null;
	public string? ConverterParameter { get; set; } = null;
	public string? StringFormat { get; set; } = null;

	public object ProvideValue(IServiceProvider serviceProvider)
	{
		return (this as IMarkupExtension<BindingBase>).ProvideValue(serviceProvider);
	}

	BindingBase IMarkupExtension<BindingBase>.ProvideValue(IServiceProvider serviceProvider)
	{
		return new Binding(this.Path, this.Mode, this.Converter, this.ConverterParameter, this.StringFormat, LocalizationManager.Current);
	}
}
