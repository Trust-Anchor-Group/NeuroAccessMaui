namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	/// <summary>
	/// Whether the identity of a part in a contract can be seen.
	/// </summary>
	public enum PartIdentityState
	{
		/// <summary>
		/// The identity is loaded and can be shown.
		/// </summary>
		Visible,

		/// <summary>
		/// The identity is protected, and hasn't been requested.
		/// </summary>
		NotVisible,

		/// <summary>
		/// The identity has been requested, and the owner hasn't answered.
		/// </summary>
		Requested,

		/// <summary>
		/// The owner declined the request.
		/// </summary>
		Declined
	}
}
