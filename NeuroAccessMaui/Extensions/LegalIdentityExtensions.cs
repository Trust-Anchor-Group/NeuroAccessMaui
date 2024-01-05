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
		/// <param name="Identity">The legal identity whose state to check.</param>
		/// <returns>If ID has been discarded.</returns>
		public static bool IsDiscarded(this LegalIdentity Identity)
		{
			return Identity is null ||
				Identity.State == IdentityState.Compromised ||
				Identity.State == IdentityState.Obsoleted ||
				Identity.State == IdentityState.Rejected;
		}

		/// <summary>
		/// Returns <c>true</c> if the legal identity has been approved.
		/// </summary>
		/// <param name="Identity">The legal identity whose state to check.</param>
		/// <returns>If ID has been approved.</returns>
		public static bool IsApproved(this LegalIdentity Identity)
		{
			return Identity is not null && Identity.State == IdentityState.Approved;
		}

		/// <summary>
		/// If the Legal Identity has an approved identity with personal information.
		/// </summary>
		/// <param name="Identity">Identity</param>
		/// <returns>If an approved identity with personal information.</returns>
		public static bool HasApprovedPersonalInformation(this LegalIdentity? Identity)
		{
			if (Identity?.Attachments is null)
				return false;

			if (!Identity.IsApproved())
				return false;

			bool HasFirstName = false;
			bool HasLastName = false;
			bool HasPersonalNumber = false;

			foreach (Property P in Identity.Properties)
			{
				switch (P.Name)
				{
					case Constants.XmppProperties.FirstName:
						HasFirstName = true;
						break;

					case Constants.XmppProperties.LastNames:
						HasLastName = true;
						break;

					case Constants.XmppProperties.PersonalNumber:
						HasPersonalNumber = true;
						break;
				}
			}

			if (!(HasFirstName && HasLastName && HasPersonalNumber))
				return false;

			Attachment? Photo = Identity.Attachments.GetFirstImageAttachment();
			if (Photo is null)
				return false;

			return true;
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

		/// <summary>
		/// Returns <c>true</c> if the legal identity has organizational properties.
		/// </summary>
		/// <param name="Identity">The legal identity whose state to check.</param>
		/// <returns>If ID is organizational.</returns>
		public static bool IsOrganizational(this LegalIdentity Identity)
		{
			if (Identity?.Properties is null)
				return false;

			foreach (Property P in Identity.Properties)
			{
				switch (P.Name)
				{
					case Constants.XmppProperties.OrgAddress:
					case Constants.XmppProperties.OrgAddress2:
					case Constants.XmppProperties.OrgArea:
					case Constants.XmppProperties.OrgCity:
					case Constants.XmppProperties.OrgCountry:
					case Constants.XmppProperties.OrgDepartment:
					case Constants.XmppProperties.OrgName:
					case Constants.XmppProperties.OrgNumber:
					case Constants.XmppProperties.OrgRegion:
					case Constants.XmppProperties.OrgRole:
					case Constants.XmppProperties.OrgZipCode:
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns <c>true</c> if the legal identity does not have organizational properties.
		/// </summary>
		/// <param name="Identity">The legal identity whose state to check.</param>
		/// <returns>If ID is personal.</returns>
		public static bool IsPersonal(this LegalIdentity Identity)
		{
			return !Identity.IsOrganizational();
		}
	}
}
