namespace NeuroAccessMaui.Services.Navigation
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
		/// Pop until this page is reached
		/// </summary>
		ToThisPage = 2,

		/*
		/// <summary>
		/// Pop until this page's parent is reached
		/// </summary>
		ToParentPage = 3,

		/// <summary>
		/// Goes back to the main page - the route "///" + nameof(MainPage)
		/// </summary>
		ToMainPage = 4,
		*/
	}
}
