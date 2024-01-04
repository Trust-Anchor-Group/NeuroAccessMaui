namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Represents the current 'is loaded' changed state.
	/// </summary>
	/// <param name="isLoaded">The current loaded state.</param>
	/// <param name="isResuming">If App is resuming service.</param>
	public sealed class LoadedEventArgs(bool isLoaded, bool isResuming) : EventArgs
	{
		/// <summary>
		/// The current loaded state.
		/// </summary>
		public bool IsLoaded { get; } = isLoaded;

		/// <summary>
		/// If App is resuming service.
		/// </summary>
		public bool IsResuming { get; } = isResuming;
	}
}
