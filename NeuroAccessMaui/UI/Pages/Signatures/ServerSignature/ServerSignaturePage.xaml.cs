using System.ComponentModel;

namespace IdApp.Pages.Signatures.ServerSignature
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
			this.ViewModel = new ServerSignatureViewModel();
			this.InitializeComponent();
		}
	}
}
