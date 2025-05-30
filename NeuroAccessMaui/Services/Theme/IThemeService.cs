using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Service for loading and applying themes/branding in the app.
	/// </summary>
	[DefaultImplementation(typeof(ThemeService))]
	public interface IThemeService
	{
		/// <summary>
		/// Loads and applies the provider theme (if exists).
		/// </summary>
		Task ApplyProviderTheme();

				Task ApplyProviderTheme2();


		/// <summary>
		/// Sets the theme for the app.
		/// </summary>
		/// <param name="Type">The theme type</param>
		Task SetTheme(AppTheme Type);

		/// <summary>
		/// Gets the current theme for the app.
		/// </summary>
		/// <returns></returns>
		Task<AppTheme> GetTheme();

	}

}
