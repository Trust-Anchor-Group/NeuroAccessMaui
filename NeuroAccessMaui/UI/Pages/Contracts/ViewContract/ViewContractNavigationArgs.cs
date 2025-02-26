using NeuroAccessMaui.Services.UI;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying a contract.
	/// </summary>
	/// <param name="Contract">The contract to display.</param>
	/// <param name="IsReadOnly"><c>true</c> if the contract is readonly, <c>false</c> otherwise.</param>
	/// <param name="Role">Contains proposed role, if a proposal, null if not a proposal.</param>
	/// <param name="Proposal">Proposal text.</param>
	public class ViewContractNavigationArgs(Contract? Contract, bool IsReadOnly, string? Role, string? Proposal, string? FromJID = null) : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="ViewContractNavigationArgs"/> class.
		/// </summary>
		public ViewContractNavigationArgs()
			: this(null, false)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractNavigationArgs"/> class.
		/// </summary>
		/// <param name="Contract">The contract to display.</param>
		/// <param name="IsReadOnly"><c>true</c> if the contract is readonly, <c>false</c> otherwise.</param>
		public ViewContractNavigationArgs(Contract? Contract, bool IsReadOnly)
			: this(Contract, IsReadOnly, null, string.Empty)
		{
		}

		/// <summary>
		/// The contract to display.
		/// </summary>
		public Contract? Contract { get; } = Contract;

		/// <summary>
		/// <c>true</c> if the contract is readonly, <c>false</c> otherwise.
		/// </summary>
		public bool IsReadOnly { get; } = IsReadOnly;

		/// <summary>
		/// Contains proposed role, if a proposal, null if not a proposal.
		/// </summary>
		public string? Role { get; } = Role;

		/// <summary>
		/// Proposal text.
		/// </summary>
		public string? Proposal { get; } = Proposal;

		/// <summary>
		/// The JID of the sender of proposal.
		/// </summary>
		public string? FromJID { get; } = FromJID;
	}
}
