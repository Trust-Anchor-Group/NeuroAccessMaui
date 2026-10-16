namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// The different steps of viewing and signing a contract.
	/// </summary>
	public enum ViewContractStep
	{
		/// <summary>
		/// The overview step.
		/// </summary>
		Overview = 10,
		/// <summary>
		/// Loading screen.
		/// </summary>
		Loading = 20,
		/// <summary>
		/// Show parameters.
		/// </summary>
		Parameters = 30,
		/// <summary>
		/// Show roles.
		/// </summary>
		Roles = 40,
		/// <summary>
		/// View attachments.
		/// </summary>
		Attachment = 50,
		/// <summary>
		/// Review the contract.
		/// </summary>
		Review = 60,
		/// <summary>
		/// What the user is about to sign: the parties, the contract, the role on offer and the signing window.
		/// </summary>
		Summary = 55,
		/// <summary>
		/// Choose the role and sign.
		/// </summary>
		Sign = 70,
		/// <summary>
		/// The outcome of signing, and what happens next.
		/// </summary>
		Result = 80





	}
}
