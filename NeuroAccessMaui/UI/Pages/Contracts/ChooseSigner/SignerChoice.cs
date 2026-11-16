namespace NeuroAccessMaui.UI.Pages.Contracts.ChooseSigner
{
	/// <summary>
	/// Who the user chose to sign a role of a new contract.
	/// </summary>
	public enum SignerChoice
	{
		/// <summary>
		/// The user signs the role when the contract is created.
		/// </summary>
		Me,

		/// <summary>
		/// A person from the user's contacts.
		/// </summary>
		Contact,

		/// <summary>
		/// A person whose ID code the user scans.
		/// </summary>
		QrCode,

		/// <summary>
		/// Anyone who gets the link can sign.
		/// </summary>
		AnyoneWithLink
	}
}
