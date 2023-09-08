using Microsoft.Extensions.Localization;
using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui;

[ContentProperty(nameof(Path))]
public class LocalizeExtension : IMarkupExtension<BindingBase>, INotifyPropertyChanged
{
    private IStringLocalizer? localizer;
#pragma warning disable CS8603 // Possible null reference return.
	public IStringLocalizer Localizer => this.localizer ??= LocalizationManager.GetStringLocalizer(this.StringResource);
#pragma warning restore CS8603 // Possible null reference return.

	public string Path { get; set; } = ".";
    public BindingMode Mode { get; set; } = BindingMode.OneWay;
    public IValueConverter? Converter { get; set; } = null;
    public string? ConverterParameter { get; set; } = null;
    public string? StringFormat { get; set; } = null;
    public Type? StringResource { get; set; } = null;

	public object ProvideValue(IServiceProvider ServiceProvider)
	{
        return (this as IMarkupExtension<BindingBase>).ProvideValue(ServiceProvider);
	}

	BindingBase IMarkupExtension<BindingBase>.ProvideValue(IServiceProvider serviceProvider)
	{
        return new Binding($"Localizer[{this.Path}]", this.Mode, this.Converter, this.ConverterParameter, this.StringFormat, this);
	}

	public LocalizeExtension()
	{
		LocalizationManager.CurrentCultureChanged += this.OnCurrentCultureChanged;
	}

	~LocalizeExtension()
	{
        LocalizationManager.CurrentCultureChanged -= this.OnCurrentCultureChanged;
	}

	private void OnCurrentCultureChanged(object? sender, CultureInfo culture)
	{
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Localizer)));
	}

    public event PropertyChangedEventHandler? PropertyChanged;
}
