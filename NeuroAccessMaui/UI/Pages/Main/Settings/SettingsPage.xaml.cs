namespace NeuroAccessMaui.UI.Pages.Main.Settings
{
	/// <summary>
	/// A page ...
	/// </summary>
	public partial class SettingsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SettingsPage"/> class.
		/// </summary>
		public SettingsPage(SettingsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
