using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui.Services.Localization;

public class LanguageInfo : CultureInfo, INotifyPropertyChanged
{
	public string MyNativeName { get; private set; }

	public LanguageInfo(string Language, string ShownName) : base(Language)
	{
		this.MyNativeName = ShownName;
		LocalizationManager.CurrentCultureChanged += this.OnCurrentCultureChanged;
	}

	~LanguageInfo()
	{
		LocalizationManager.CurrentCultureChanged -= this.OnCurrentCultureChanged;
	}

	private void OnCurrentCultureChanged(object? sender, CultureInfo culture)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCurrent)));
	}

	public bool IsCurrent => this.Name == CurrentUICulture.Name;

	public event PropertyChangedEventHandler? PropertyChanged;
}
