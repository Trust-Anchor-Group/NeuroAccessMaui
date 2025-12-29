namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Represents the outcome of a provider theme application attempt.
	/// </summary>
	public enum ThemeApplyOutcome
	{
		/// <summary>
		/// No provider domain is configured, so no provider theme was applied.
		/// </summary>
		SkippedNoDomain = 0,
		/// <summary>
		/// Provider branding was applied from a fresh network fetch.
		/// </summary>
		Applied = 1,
		/// <summary>
		/// Provider branding was applied from cached data.
		/// </summary>
		AppliedFromCache = 2,
		/// <summary>
		/// Provider branding is not supported by the domain.
		/// </summary>
		NotSupported = 3,
		/// <summary>
		/// The attempt failed due to a transient error and can be retried.
		/// </summary>
		FailedTransient = 4,
		/// <summary>
		/// The attempt failed due to a permanent error and should not be retried immediately.
		/// </summary>
		FailedPermanent = 5
	}
}
