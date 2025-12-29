using System.Threading;
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
		/// Loads and applies the provider-supplied theme, if available, using the background refresh policy.
		/// </summary>
		Task ApplyProviderThemeAsync();

		/// <summary>
		/// Loads and applies the provider-supplied theme according to the specified policy.
		/// </summary>
		/// <param name="Policy">The fetch policy to apply.</param>
		/// <param name="CancellationToken">A token that can be used to cancel the operation.</param>
		/// <returns>An outcome describing how the provider theme was resolved.</returns>
		Task<ThemeApplyOutcome> ApplyProviderThemeAsync(ThemeFetchPolicy Policy, CancellationToken CancellationToken = default);

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
		/// Sets the active theme.
		/// </summary>
		/// <param name="Theme">The theme to apply locally.</param>
		void SetLocalTheme(AppTheme Theme);

		/// <summary>
		/// Gets the mapping of image identifiers to their URIs.
		/// </summary>
		IReadOnlyDictionary<string, Uri> ImageUris { get; }

		/// <summary>
		/// If the theme has been loaded.
		/// </summary>
		TaskCompletionSource ThemeLoaded { get; }

		/// <summary>
		/// Clears locally cached branding descriptors for the current provider domain.
		/// </summary>
		Task<int> ClearBrandingCacheForCurrentDomain();

		/// <summary>
		/// Returns the image URI for the given identifier, or empty string if not found.
		/// </summary>
		/// <param name="Id">The image identifier.</param>
		string GetImageUri(string Id);
	}
}
