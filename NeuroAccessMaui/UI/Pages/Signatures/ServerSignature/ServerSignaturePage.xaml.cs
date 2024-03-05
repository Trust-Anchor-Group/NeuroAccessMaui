using System.ComponentModel;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Signatures.ServerSignature
{
    /// <summary>
    /// A page that displays a server signature.
    /// </summary>
    [DesignTimeVisible(true)]
	public partial class ServerSignaturePage
	{
        /// <summary>
        /// Creates a new instance of the <see cref="ServerSignaturePage"/> class.
        /// </summary>
		public ServerSignaturePage()
		{
			this.ContentPageModel = new ServerSignatureViewModel(ServiceRef.UiService.PopLatestArgs<ServerSignatureNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
