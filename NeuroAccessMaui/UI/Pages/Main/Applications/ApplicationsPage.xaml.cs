namespace NeuroAccessMaui.UI.Pages.Main.Applications
{
	/// <summary>
	/// Main page for App settings.
	/// </summary>
	public partial class ApplicationsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ApplicationsPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public ApplicationsPage(ApplicationsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
