namespace NeuroAccessMaui.UI.Pages.Main.Settings
{
	/// <summary>
	/// Main page for App settings.
	/// </summary>
	public partial class SettingsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SettingsPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public SettingsPage(SettingsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
