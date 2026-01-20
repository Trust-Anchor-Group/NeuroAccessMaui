using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	/// <summary>
	/// Navigation arguments used when navigating to the embedded layout page.
	/// </summary>
	public class EmbeddedLayoutNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EmbeddedLayoutNavigationArgs"/> class.
		/// </summary>
		public EmbeddedLayoutNavigationArgs()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EmbeddedLayoutNavigationArgs"/> class.
		/// </summary>
		/// <param name="TokenItem">Token item containing the embedded layout to render.</param>
		public EmbeddedLayoutNavigationArgs(TokenItem TokenItem)
		{
			this.TokenItem = TokenItem;
		}

		/// <summary>
		/// Gets the token item containing the embedded layout to render.
		/// </summary>
		public TokenItem? TokenItem { get; }
	}
}
