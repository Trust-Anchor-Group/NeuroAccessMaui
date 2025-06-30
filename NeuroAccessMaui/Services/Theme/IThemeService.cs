using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Service for loading, applying, and retrieving themes and branding in the application.
	/// Exposes methods for fetching themed image URIs.
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
		Task<AppTheme> GetTheme();

		/// <summary>
		/// Sets the application theme.
		/// </summary>
		/// <param name="Theme">The <see cref="AppTheme"/> to apply.</param>
		void SetTheme(AppTheme Theme);

		/// <summary>
		/// Sets the active theme
		/// </summary>
		/// <param name="theme"></param>
		void SetLocalTheme(AppTheme theme);

		/// <summary>
		/// Gets the mapping of image identifiers to their URIs.
		/// </summary>
		IReadOnlyDictionary<string, Uri> ImageUris { get; }

		/// <summary>
		/// Returns the image URI for the given identifier, or empty string if not found.
		/// </summary>
		/// <param name="Id">The image identifier.</param>
		string GetImageUri(string Id);
	}
}
