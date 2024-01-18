namespace NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview
{
	/// <summary>
	/// Steps in the peer-review process
	/// </summary>
	public enum ReviewStep
	{
		/// <summary>
		/// Review photo
		/// </summary>
		Photo = 0,

		/// <summary>
		/// Review name
		/// </summary>
		Name = 1,

		/// <summary>
		/// Review personal number
		/// </summary>
		Pnr = 2,

		/// <summary>
		/// Review nationality
		/// </summary>
		Nationality = 3,

		/// <summary>
		/// Review birth date
		/// </summary>
		BirthDate = 4,

		/// <summary>
		/// Review gender
		/// </summary>
		Gender = 5,

		/// <summary>
		/// Review personal address information
		/// </summary>
		PersonalAddressInfo = 6,

		/// <summary>
		/// Review organizational information
		/// </summary>
		OrganizationalInfo = 7,

		/// <summary>
		/// Get consent by reviewer
		/// </summary>
		Consent = 8,

		/// <summary>
		/// Authenticate reviewer
		/// </summary>
		Authenticate = 9,

		/// <summary>
		/// Show approval information
		/// </summary>
		Approved = 10
	}
}
