namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Represents a parsed KYC application template along with optional PubSub metadata.
	/// </summary>
	public sealed class KycApplicationTemplate
	{
		public KycApplicationTemplate(KycReference reference, KycApplicationItem? source)
		{
			this.Reference = reference;
			this.Source = source;
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
