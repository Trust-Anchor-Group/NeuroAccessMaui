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
		Photo,

		/// <summary>
		/// Review name and personal number
		/// </summary>
		NamePnr,

		/// <summary>
		/// Review other personal information
		/// </summary>
		PersonalInfo,

		/// <summary>
		/// Review organizational information
		/// </summary>
		OrganizationalInfo,

		/// <summary>
		/// Get consent by reviewer
		/// </summary>
		Consent,

		/// <summary>
		/// Authenticate reviewer
		/// </summary>
		Authenticate,

		/// <summary>
		/// Show approval information
		/// </summary>
		Approved
	}
}
