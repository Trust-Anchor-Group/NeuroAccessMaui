namespace NeuroAccessMaui.UI.Pages.Main.Apps
{
	/// <summary>
	/// Main page for viewing apps.
	/// </summary>
	public partial class AppsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="AppsPage"/> class.
		/// </summary>
		/// <param name="ViewModel"></param>
		public AppsPage(AppsViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
