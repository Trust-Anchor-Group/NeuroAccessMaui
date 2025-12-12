using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Main
{
	/// <summary>
	/// Navigation arguments for the <see cref="MainPage"/>, allowing callers to target a specific tab.
	/// </summary>
	public class MainNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Gets or sets the key of the tab to select (for example, "wallet").
		/// </summary>
		public string? TargetTabKey { get; set; }

		/// <summary>
		/// Gets or sets the zero-based tab index to select. Ignored if <see cref="TargetTabKey"/> resolves to a tab.
		/// </summary>
		public int? TargetTabIndex { get; set; }
	}
}
