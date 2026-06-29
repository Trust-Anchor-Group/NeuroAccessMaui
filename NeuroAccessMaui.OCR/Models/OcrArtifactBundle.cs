namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Describes a persisted debug-artifact session for a scan.
	/// </summary>
	public sealed class OcrArtifactBundle
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OcrArtifactBundle"/> class.
		/// </summary>
		/// <param name="SessionId">The unique session identifier.</param>
		/// <param name="SessionDirectoryPath">The persisted session directory.</param>
		/// <param name="ManifestPath">The manifest file path.</param>
		public OcrArtifactBundle(string SessionId, string SessionDirectoryPath, string ManifestPath)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(SessionId);
			ArgumentException.ThrowIfNullOrWhiteSpace(SessionDirectoryPath);
			ArgumentException.ThrowIfNullOrWhiteSpace(ManifestPath);

			this.SessionId = SessionId;
			this.SessionDirectoryPath = SessionDirectoryPath;
			this.ManifestPath = ManifestPath;
		}

		/// <summary>
		/// Gets the unique session identifier.
		/// </summary>
		public string SessionId { get; }

		/// <summary>
		/// Gets the persisted session directory path.
		/// </summary>
		public string SessionDirectoryPath { get; }

		/// <summary>
		/// Gets the manifest file path.
		/// </summary>
		public string ManifestPath { get; }
	}
}
