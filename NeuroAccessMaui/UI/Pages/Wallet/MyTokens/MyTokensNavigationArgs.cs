using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens
{
	/// <summary>
	/// Holds navigation parameters for viewing tokens.
	/// </summary>
	public class MyTokensNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyTokensNavigationArgs"/> class.
		/// </summary>
		public MyTokensNavigationArgs()
		{
			this.TokenItemProvider = new();
		}

		/// <summary>
		/// Task completion source; can be used to wait for a result.
		/// </summary>
		public TaskCompletionSource<TokenItem?> TokenItemProvider { get; internal set; }
	}
}
