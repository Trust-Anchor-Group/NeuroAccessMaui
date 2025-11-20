using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui.UI.Popups.Settings
{
	public sealed partial class ObservableLanguage : ObservableObject
	{
		public LanguageInfo Language { get; }
		[ObservableProperty] private bool isSelected;

		public IRelayCommand SelectCommand { get; }

		public ObservableLanguage(LanguageInfo language, Func<string, Task> select)
		{
			this.Language = language;
			this.SelectCommand = new AsyncRelayCommand(() => select(this.Language.Name));
		}
	}

}
