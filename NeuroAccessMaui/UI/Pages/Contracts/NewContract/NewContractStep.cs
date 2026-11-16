namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// The different steps of creating a new contract.
	/// </summary>
	public enum NewContractStep
	{
		/// <summary>
		/// Loading screen.
		/// </summary>
		Loading = 20,
		/// <summary>
		/// Edit parameters.
		/// </summary>
		Parameters = 30,
		/// <summary>
		/// The options the template fills in: who can see the contract, the signing window, validity and archiving.
		/// </summary>
		Settings = 35,
		/// <summary>
		/// Edit roles.
		/// </summary>
		Roles = 40,
		/// <summary>
		/// Check what will be created, and create it.
		/// </summary>
		Check = 50,
		/// <summary>
		/// The whole text of the contract.
		/// </summary>
		Preview = 60
	}
}
