using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="LegalIdentity"/> class.
	/// </summary>
	public static class LegalIdentityExtensions
	{
		/// <summary>
		/// Returns <c>true</c> if the legal identity is either null or is in a 'bad' state (rejected, compromised or obsolete).
		/// </summary>
		/// <param name="legalIdentity">The legal identity whose state to check.</param>
		/// <returns>If ID has been discarded.</returns>
		public static bool IsDiscarded(this LegalIdentity legalIdentity)
		{
			return legalIdentity is null ||
				legalIdentity.State == IdentityState.Compromised ||
				legalIdentity.State == IdentityState.Obsoleted ||
				legalIdentity.State == IdentityState.Rejected;
		}

		/// <summary>
		/// Returns <c>true</c> if the legal identity has been approved.
		/// </summary>
		/// <param name="legalIdentity">The legal identity whose state to check.</param>
		/// <returns>If ID has been approved.</returns>
		public static bool IsApproved(this LegalIdentity legalIdentity)
		{
			return legalIdentity is not null && legalIdentity.State == IdentityState.Approved;
		}

		/// <summary>
		/// Returns the JID if the <see cref="LegalIdentity"/> has one, or the empty string otherwise.
		/// </summary>
		/// <param name="legalIdentity">The legal identity whose JID to get.</param>
		/// <param name="defaultValueIfNotFound">The default value to use if JID isn't found.</param>
		/// <returns>Gets the JID property of an identity object.</returns>
		public static string GetJid(this LegalIdentity legalIdentity, string defaultValueIfNotFound = "")
		{
			string? Jid = null;

			if (legalIdentity is not null && legalIdentity.Properties?.Length > 0)
				Jid = legalIdentity.Properties.FirstOrDefault(x => x.Name == Constants.XmppProperties.Jid)?.Value;

			return !string.IsNullOrWhiteSpace(Jid) ? Jid : defaultValueIfNotFound;
		}
	}
}
