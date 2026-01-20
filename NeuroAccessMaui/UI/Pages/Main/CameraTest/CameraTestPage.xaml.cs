namespace NeuroAccessMaui.UI.Pages.Main.CameraTest
{
	/// <summary>
	/// A simple page used for testing camera preview.
	/// </summary>
	public partial class CameraTestPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CameraTestPage"/> class.
		/// </summary>
		public CameraTestPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new CameraTestViewModel();
		}
	}
}
