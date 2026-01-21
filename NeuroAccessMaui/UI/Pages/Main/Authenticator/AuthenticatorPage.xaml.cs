using NeuroAccessMaui.UI.Pages;

namespace NeuroAccessMaui.UI.Pages.Main.Authenticator
{
	/// <summary>
	/// Page used for authenticator related functionality.
	/// </summary>
	public partial class AuthenticatorPage : BaseContentPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticatorPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model instance used as binding context.</param>
		public AuthenticatorPage(AuthenticatorViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}
	}
}
