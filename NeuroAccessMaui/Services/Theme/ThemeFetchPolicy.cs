namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Defines how provider branding should be fetched and applied.
	/// </summary>
	public enum ThemeFetchPolicy
	{
		/// <summary>
		/// Requires provider branding before proceeding when no cached theme exists.
		/// </summary>
		BlockingFirstRun = 0,
		/// <summary>
		/// Applies cached branding immediately and refreshes in the background when needed.
		/// </summary>
		BackgroundRefresh = 1,
		/// <summary>
		/// Forces a network refresh and applies the latest branding when available.
		/// </summary>
		ManualRefresh = 2
	}
}
