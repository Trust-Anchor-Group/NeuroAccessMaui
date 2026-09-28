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

		/// <summary>
		/// Gets the stable identifier used to select this language in UI automation.
		/// </summary>
		public string AutomationId => $"option_language_{this.Language.TwoLetterISOLanguageName}";
	}

}
