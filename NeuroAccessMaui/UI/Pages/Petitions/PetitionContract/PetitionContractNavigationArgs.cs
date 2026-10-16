using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.UI;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionContract
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a contract petition request.
	/// </summary>
	public class PetitionContractNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="PetitionContractNavigationArgs"/> class.
		/// </summary>
		public PetitionContractNavigationArgs() { }

		/// <summary>
		/// Creates an instance of the <see cref="PetitionContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Request">The request to see the contract.</param>
		/// <param name="RequestedContract">The requested contract, or null if it couldn't be read.</param>
		public PetitionContractNavigationArgs(ContractPetitionRequest Request, Contract? RequestedContract)
		{
			this.Request = Request;
			this.RequestedContract = RequestedContract;
		}

		/// <summary>
		/// The request to see the contract.
		/// </summary>
		public ContractPetitionRequest? Request { get; }

		/// <summary>
		/// The requested contract, or null if it couldn't be read.
		/// </summary>
		public Contract? RequestedContract { get; }
	}
}
