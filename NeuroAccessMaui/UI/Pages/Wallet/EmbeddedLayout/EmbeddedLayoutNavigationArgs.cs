using NeuroAccessMaui.Services.UI;
using NeuroFeatures;

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
		/// Initializes a new instance that renders an authoritative token directly.
		/// </summary>
		/// <param name="Token">Token containing the embedded layout definition.</param>
		public EmbeddedLayoutNavigationArgs(Token Token)
		{
			this.Token = Token;
		}

		/// <summary>
		/// Gets the authoritative token containing the embedded layout definition.
		/// </summary>
		public Token? Token { get; }
	}
}
