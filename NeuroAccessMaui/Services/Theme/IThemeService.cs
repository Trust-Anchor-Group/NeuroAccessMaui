using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Service for loading, applying, and retrieving themes and branding in the application.
	/// </summary>
	[DefaultImplementation(typeof(ThemeService))]
	public interface IThemeService
	{
		/// <summary>
		/// Loads and applies the provider-supplied theme, if available.
		/// </summary>
		Task ApplyProviderTheme();

		/// <summary>
		/// Retrieves the current application theme.
		/// </summary>
		/// <returns>The current <see cref="AppTheme"/>.</returns>
		Task<AppTheme> GetTheme();

		/// <summary>
		/// Sets the application theme.
		/// </summary>
		/// <param name="Type">The <see cref="AppTheme"/> to apply.</param>
		Task SetTheme(AppTheme Type);

		/// <summary>
		/// Exposes image resources loaded as part of the branding payload.
		/// </summary>
		IReadOnlyDictionary<string, ImageSource> Images { get; }
	}

}
