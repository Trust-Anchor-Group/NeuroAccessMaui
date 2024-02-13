namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Represents a part related to a token.
	/// </summary>
	/// <param name="LegalId">Legal ID</param>
	/// <param name="Jid">JID of part.</param>
	/// <param name="FriendlyName">Friendly Name</param>
	public class PartItem(string LegalId, string Jid, string FriendlyName)
	{
		private readonly string legalId = LegalId;
		private readonly string jid = Jid;
		private readonly string friendlyName = FriendlyName;

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
