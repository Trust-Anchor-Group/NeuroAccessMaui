using NeuroAccessMaui.Services.UI;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts
{
	/// <summary>
	/// What list of contracts to display
	/// </summary>
	public enum ContractsListMode
	{
		/// <summary>
		/// Contracts that has been created or signed by the account
		/// </summary>
		Contracts,

		/// <summary>
		/// Contract templates that have been used to create new contracts by the account
		/// </summary>
		ContractTemplates,

		/// <summary>
		/// Contract templates for creating new tokens
		/// </summary>
		TokenCreationTemplates
	}

	/// <summary>
	/// Actions to take when a contact has been selected.
	/// </summary>
	public enum SelectContractAction
	{
		/// <summary>
		/// View the identity.
		/// </summary>
		ViewContract,

		/// <summary>
		/// Embed link to ID in chat
		/// </summary>
		Select
	}

	/// <summary>
	/// Holds navigation parameters specific to views displaying a list of contacts.
	/// </summary>
	public class MyContractsNavigationArgs : NavigationArgs
	{
		private readonly SelectContractAction action;
		private readonly TaskCompletionSource<Contract?>? selection;
		private readonly ContractsListMode mode;

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsNavigationArgs"/> class.
		/// </summary>
		public MyContractsNavigationArgs()
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsNavigationArgs"/> class.
		/// </summary>
		/// <param name="Mode">What list of contracts to display.</param>
		public MyContractsNavigationArgs(ContractsListMode Mode)
		{
			this.mode = Mode;
			this.action = SelectContractAction.ViewContract;
			this.selection = null;
		}

		/// <summary>
		/// Creates an instance of the <see cref="MyContractsNavigationArgs"/> class.
		/// </summary>
		/// <param name="Mode">What list of contracts to display.</param>
		/// <param name="Selection">Selection source, where selected item will be stored, or null if cancelled.</param>
		public MyContractsNavigationArgs(ContractsListMode Mode, TaskCompletionSource<Contract?> Selection)
		{
			this.selection = Selection;
			this.action = SelectContractAction.Select;
			this.mode = Mode;
		}

		/// <summary>
		/// Action to take when a contact has been selected.
		/// </summary>
		public SelectContractAction Action => this.action;

		/// <summary>
		/// What list of contracts to display.
		/// </summary>
		public ContractsListMode Mode => this.mode;

		/// <summary>
		/// Selection source, if selecting identity.
		/// </summary>
		public TaskCompletionSource<Contract?>? Selection => this.selection;
	}
}
