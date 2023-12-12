namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Represents the current 'is loaded' changed state.
	/// </summary>
	/// <param name="isLoaded">The current loaded state.</param>
	public sealed class LoadedEventArgs(bool isLoaded) : EventArgs
	{
		/// <summary>
		/// The current loaded state.
		/// </summary>
		public bool IsLoaded { get; } = isLoaded;
	}
}
