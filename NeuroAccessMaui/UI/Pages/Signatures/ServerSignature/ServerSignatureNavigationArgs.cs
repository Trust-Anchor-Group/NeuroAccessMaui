using NeuroAccessMaui.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Signatures.ServerSignature
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a server signature.
	/// </summary>
	public class ServerSignatureNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ServerSignatureNavigationArgs"/> class.
		/// </summary>
		public ServerSignatureNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ServerSignatureNavigationArgs"/> class.
		/// </summary>
		/// <param name="Contract">The contract to display.</param>
		public ServerSignatureNavigationArgs(Contract? Contract)
		{
			this.Contract = Contract;
		}

		/// <summary>
		/// The contract to display.
		/// </summary>
		public Contract? Contract { get; }
	}
}
