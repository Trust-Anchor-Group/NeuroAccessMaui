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
		/// Introductory step with category and settings.
		/// </summary>
		Intro = 25,
		/// <summary>
		/// Edit parameters.
		/// </summary>
		Parameters = 30,
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
