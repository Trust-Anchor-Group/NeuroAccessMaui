using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Main
{
	/// <summary>
	/// Provides the tab definitions and selection state for the main view-switcher surface.
	/// </summary>
	public partial class MainViewModel : BaseViewModel
	{
		private readonly ObservableCollection<TabDefinition> tabs;

		/// <summary>
		/// Initializes a new instance of the <see cref="MainViewModel"/> class.
		/// </summary>
		public MainViewModel()
		{
			this.tabs = new ObservableCollection<TabDefinition>
			{
				this.CreateTab("home", "home.svg", ServiceRef.Localizer[nameof(AppResources.Home)]),
				this.CreateTab("wallet", "wallet.svg", ServiceRef.Localizer[nameof(AppResources.Wallet)], isProminent: true),
				this.CreateTab("apps", "apps.svg", ServiceRef.Localizer[nameof(AppResources.Apps)])
			};

			this.Tabs = new ReadOnlyObservableCollection<TabDefinition>(this.tabs);
			this.SelectedTabIndex = 0;
		}

		/// <summary>
		/// Gets the collection of tabs displayed in the navigation host.
		/// </summary>
		public ReadOnlyObservableCollection<TabDefinition> Tabs { get; }

		/// <summary>
		/// Gets or sets the selected tab index.
		/// </summary>
		[ObservableProperty]
		private int selectedTabIndex;

		private TabDefinition CreateTab(string key, string icon, string title, bool isProminent = false)
		{
			return new TabDefinition
			{
				Key = key,
				Icon = icon,
				Title = title,
				IsProminent = isProminent
			};
		}
	}
}
