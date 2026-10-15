using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

namespace NeuroAccessMaui.Models
{
	/// <summary>
	/// A smart contract identifier read from a link, with the contract when it is accessible and the template parameters in the link.
	/// </summary>
	public sealed class ContractLinkData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ContractLinkData"/> class.
		/// </summary>
		/// <param name="Identifier">The contract identifier.</param>
		/// <param name="Contract">The resolved contract, or <c>null</c> when access must be requested.</param>
		/// <param name="Parameters">The template parameters parsed from the link's query.</param>
		public ContractLinkData(string Identifier, Contract? Contract, Dictionary<CaseInsensitiveString, object> Parameters)
		{
			this.Identifier = Identifier;
			this.Contract = Contract;
			this.Parameters = Parameters;
		}

		/// <summary>
		/// Gets the contract identifier.
		/// </summary>
		public string Identifier { get; }

		/// <summary>
		/// Gets the resolved contract, or <c>null</c> when access must be requested.
		/// </summary>
		public Contract? Contract { get; }

		/// <summary>
		/// Gets the template parameters parsed from the link's query.
		/// </summary>
		public Dictionary<CaseInsensitiveString, object> Parameters { get; }
	}
}
