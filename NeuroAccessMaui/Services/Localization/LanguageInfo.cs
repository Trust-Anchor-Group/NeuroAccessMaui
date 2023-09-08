using System.ComponentModel;
using System.Globalization;

namespace NeuroAccessMaui.Services.Localization;

public class LanguageInfo : CultureInfo, INotifyPropertyChanged
{
	public LanguageInfo(string Language) : base(Language)
	{
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
