using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.XmlViewer
{
	/// <summary>
	/// A page that shows XML, such as a token definition or an XML note.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class XmlViewerPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="XmlViewerPage"/> class.
		/// </summary>
		public XmlViewerPage()
		{
			this.ContentPageModel = new XmlViewerViewModel(ServiceRef.NavigationService.PopLatestArgs<XmlViewerNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
