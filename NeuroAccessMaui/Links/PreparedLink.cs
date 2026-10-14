namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Retains a validated link together with the deferred action that uses its prepared data.
	/// </summary>
	/// <remarks>
	/// Preparing a link never runs its action. The action runs only when <see cref="ExecuteAsync"/> is called,
	/// typically after the scanner that produced the link has closed.
	/// </remarks>
	public sealed class PreparedLink
	{
		private readonly Func<Task> execute;

		/// <summary>
		/// Initializes a new instance of the <see cref="PreparedLink"/> class.
		/// </summary>
		/// <param name="Uri">The original parsed URI.</param>
		/// <param name="Execute">The action to run once the prepared link is accepted.</param>
		public PreparedLink(Uri Uri, Func<Task> Execute)
		{
			ArgumentNullException.ThrowIfNull(Uri);
			ArgumentNullException.ThrowIfNull(Execute);

			this.Uri = Uri;
			this.execute = Execute;
		}

		/// <summary>
		/// Gets the original URI, preserving protocol-specific encoding.
		/// </summary>
		public Uri Uri { get; }

		/// <summary>
		/// Runs the prepared action on the main thread.
		/// </summary>
		/// <returns>A task representing the action, including any confirmation it requires.</returns>
		public Task ExecuteAsync() => MainThread.InvokeOnMainThreadAsync(this.execute);
	}
}
