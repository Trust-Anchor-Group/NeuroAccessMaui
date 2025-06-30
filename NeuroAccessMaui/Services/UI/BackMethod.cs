namespace NeuroAccessMaui.Services.UI
{
	/// <summary>
	/// Navigation Back Method
	/// </summary>
	public enum BackMethod
	{
		/// <summary>
		/// Takes in consideration the parent's navigation arguments
		/// By default it will be considered a <see cref="Pop"/>
		/// </summary>
		Inherited = 0,

		/// <summary>
		/// Goes back just one navigation level - the route ".."
		/// </summary>
		Pop = 1,

		/// <summary>
		/// Pop two pages
		/// </summary>
		Pop2 = 2,

		/// <summary>
		/// Pop until the current page is reached
		/// </summary>
		CurrentPage = 3,

		/// <summary>
		/// Pop 3 pages
		/// </summary>
		Pop3 = 4,
		PopToRoot = 5
	}
}
