namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Represents the current 'is loaded' changed state.
	/// </summary>
	public sealed class LoadedEventArgs : EventArgs
	{
		/// <summary>
		/// Creates an instance of <see cref="LoadedEventArgs"/>.
		/// </summary>
		/// <param name="isLoaded">The current loaded state.</param>
		public LoadedEventArgs(bool isLoaded)
		{
			this.IsLoaded = isLoaded;
		}

		/// <summary>
		/// The current loaded state.
		/// </summary>
		public bool IsLoaded { get; }
	}
}
