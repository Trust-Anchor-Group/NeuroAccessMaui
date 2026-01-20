using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;

namespace NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout
{
	public class EmbeddedLayoutNavigationArgs : NavigationArgs
	{
		public EmbeddedLayoutNavigationArgs()
		{

		}

		public EmbeddedLayoutNavigationArgs(TokenItem TokenItem)
		{
			this.TokenItem = TokenItem;
		}

		public TokenItem TokenItem { get; }
	}
}
