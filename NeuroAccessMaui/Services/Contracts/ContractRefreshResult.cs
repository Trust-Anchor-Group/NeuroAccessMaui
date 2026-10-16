using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Contracts
{
	/// <summary>
	/// The result of refreshing one contract from the server.
	/// </summary>
	/// <param name="Status">The outcome.</param>
	/// <param name="Contract">The decrypted contract, when it could be read.</param>
	/// <param name="Reference">The stored row, when the contract is stored.</param>
	public class ContractRefreshResult(ContractRefreshStatus Status, Contract? Contract, ContractReference? Reference)
	{
		/// <summary>
		/// The outcome.
		/// </summary>
		public ContractRefreshStatus Status { get; } = Status;

		/// <summary>
		/// The decrypted contract, when it could be read.
		/// </summary>
		public Contract? Contract { get; } = Contract;

		/// <summary>
		/// The stored row, when the contract is stored.
		/// </summary>
		public ContractReference? Reference { get; } = Reference;
	}
}
