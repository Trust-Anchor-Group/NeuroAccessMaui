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
		/// Introductory step with the template's category.
		/// </summary>
		Intro = 25,
		/// <summary>
		/// Edit parameters.
		/// </summary>
		Parameters = 30,
		/// <summary>
		/// Choose who can sign, the signing window, validity, archiving and visibility.
		/// </summary>
		Settings = 35,
		/// <summary>
		/// Edit roles.
		/// </summary>
		Roles = 40,
		/// <summary>
		/// Preview the contract.
		/// </summary>
		Preview = 60,

	}
}
