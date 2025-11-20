using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Identity.TransferIdentity
{
	/// <summary>
	/// A page to display when the user wants to transfer an identity.
	/// </summary>
	public partial class TransferIdentityPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TransferIdentityPage"/> class.
		/// </summary>
		public TransferIdentityPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new TransferIdentityViewModel(ServiceRef.NavigationService.PopLatestArgs<TransferIdentityNavigationArgs>());
		}
	}
}
