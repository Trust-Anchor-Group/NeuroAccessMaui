namespace NeuroAccessMaui.UI.Pages.Wallet.TokenEvents
{
	/// <summary>
	/// The kinds of events a token's history can be narrowed to.
	/// </summary>
	public enum TokenEventFilter
	{
		/// <summary>
		/// Every event.
		/// </summary>
		All,

		/// <summary>
		/// Events about who owns the token: created, sold, given away and destroyed.
		/// </summary>
		Ownership,

		/// <summary>
		/// Notes added to the token, by the owner or by others.
		/// </summary>
		Notes,

		/// <summary>
		/// Steps the token moved to, seen live in the app.
		/// </summary>
		Steps
	}
}
