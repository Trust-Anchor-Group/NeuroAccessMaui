namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Represents a part related to a token.
	/// </summary>
	public class PartItem
	{
		private readonly string legalId;
		private readonly string jid;
		private readonly string friendlyName;

		/// <summary>
		/// Represents a part related to a token.
		/// </summary>
		/// <param name="LegalId">Legal ID</param>
		/// <param name="Jid">JID of part.</param>
		/// <param name="FriendlyName">Friendly Name</param>
		public PartItem(string LegalId, string Jid, string FriendlyName)
		{
			this.legalId = LegalId;
			this.jid = Jid;
			this.friendlyName = FriendlyName;
		}

		/// <summary>
		/// Legal ID
		/// </summary>
		public string LegalId => this.legalId;

		/// <summary>
		/// JID of part
		/// </summary>
		public string Jid => this.jid;

		/// <summary>
		/// Friendly Name
		/// </summary>
		public string FriendlyName => this.friendlyName;
	}
}
