namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Represents a parsed KYC application template along with optional PubSub metadata.
	/// </summary>
	public sealed class KycApplicationTemplate
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationTemplate"/> class.
		/// </summary>
		/// <param name="Reference">The parsed KYC reference constructed from the template XML.</param>
		/// <param name="Source">The optional PubSub metadata that produced the template.</param>
		public KycApplicationTemplate(KycReference Reference, KycApplicationItem? Source)
		{
			this.Reference = Reference;
			this.Source = Source;
		}

		/// <summary>
		/// Gets the parsed reference constructed from the template XML.
		/// </summary>
		public KycReference Reference { get; }

		/// <summary>
		/// Gets the originating PubSub metadata, if available.
		/// </summary>
		public KycApplicationItem? Source { get; }

		/// <summary>
		/// Gets the friendly template name for display purposes.
		/// </summary>
		public string DisplayName => this.Reference.FriendlyName;
	}
}
