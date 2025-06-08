using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;

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
			if (Identity is null)
				return false;

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
		/// Gets personal information from an identity.
		/// </summary>
		/// <param name="Properties">Enumerable set of identity properties.</param>
		/// <returns>Personal information</returns>
		internal static PersonalInformation GetPersonalInfo(this LegalIdentity legalIdentity)
		{
			PersonalInformation Result = new PersonalInformation();

			foreach (Property P in legalIdentity.Properties)
			{
				switch (P.Name)
				{
					case "FIRST":
						Result.FirstName = P.Value;
						break;

					case "MIDDLE":
						Result.MiddleNames = P.Value;
						break;

					case "LAST":
						Result.LastNames = P.Value;
						break;

					case "FULLNAME":
						Result.FullName = P.Value;
						break;

					case "ADDR":
						Result.Address = P.Value;
						break;

					case "ADDR2":
						Result.Address2 = P.Value;
						break;

					case "ZIP":
						Result.PostalCode = P.Value;
						break;

					case "AREA":
						Result.Area = P.Value;
						break;

					case "CITY":
						Result.City = P.Value;
						break;

					case "REGION":
						Result.Region = P.Value;
						break;

					case "COUNTRY":
						Result.Country = P.Value;
						break;

					case "NATIONALITY":
						Result.Nationality = P.Value;
						break;

					case "GENDER":
						switch (P.Value.ToLower())
						{
							case "m":
								Result.Gender = Gender.Male;
								break;

							case "f":
								Result.Gender = Gender.Female;
								break;

							case "x":
								Result.Gender = Gender.Other;
								break;
						}
						break;

					case "BDAY":
						if (int.TryParse(P.Value, out int i) && i >= 1 && i <= 31)
							Result.BirthDay = i;
						break;

					case "BMONTH":
						if (int.TryParse(P.Value, out i) && i >= 1 && i <= 12)
							Result.BirthMonth = i;
						break;

					case "BYEAR":
						if (int.TryParse(P.Value, out i) && i >= 1900 && i <= 2100)
							Result.BirthYear = i;
						break;

					case "PNR":
						Result.PersonalNumber = P.Value;
						break;

					case "ORGNAME":
						Result.OrgName = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGDEPT":
						Result.OrgDepartment = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGROLE":
						Result.OrgRole = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGADDR":
						Result.OrgAddress = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGADDR2":
						Result.OrgAddress2 = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGZIP":
						Result.OrgPostalCode = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGAREA":
						Result.OrgArea = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGCITY":
						Result.OrgCity = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGREGION":
						Result.OrgRegion = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGCOUNTRY":
						Result.OrgCountry = P.Value;
						Result.HasOrg = true;
						break;

					case "ORGNR":
						Result.OrgNumber = P.Value;
						Result.HasOrg = true;
						break;

					case "PHONE":
						Result.Phone = P.Value;
						break;

					case "EMAIL":
						Result.EMail = P.Value;
						break;

					case "JID":
						Result.Jid = P.Value;
						break;
				}
			}

			Result.HasBirthDate =
				Result.BirthDay.HasValue &&
				Result.BirthMonth.HasValue &&
				Result.BirthYear.HasValue &&
				Result.BirthDay.Value <= DateTime.DaysInMonth(Result.BirthYear.Value, Result.BirthMonth.Value);

			if (!Result.HasBirthDate)
			{
				Result.BirthDay = null;
				Result.BirthMonth = null;
				Result.BirthYear = null;
			}

			if (CaseInsensitiveString.IsNullOrEmpty(Result.FullName))
				Result.FullName = LegalIdentity.JoinNames(Result.FirstName, Result.MiddleNames, Result.LastNames);
			else if (CaseInsensitiveString.IsNullOrEmpty(Result.FirstName) &&
				CaseInsensitiveString.IsNullOrEmpty(Result.MiddleNames) &&
				CaseInsensitiveString.IsNullOrEmpty(Result.LastNames))
			{
				LegalIdentity.SeparateNames(Result.FullName, out Result.FirstName, out Result.MiddleNames, out Result.LastNames);
			}

			return Result;
		}


		/// <summary>
		/// Returns the domain if the <see cref="LegalIdentity"/> has one, or the empty string otherwise.
		/// </summary>
		/// <param name="legalIdentity"></param>
		/// <param name="defaultValueIfNotFound"></param>
		/// <returns></returns>
		public static string GetDomain(this LegalIdentity legalIdentity, string defaultValueIfNotFound = "")
		{
			string? Domain = null;

			if (legalIdentity is not null && legalIdentity.Properties?.Length > 0)
				Domain = legalIdentity.Properties.FirstOrDefault(x => x.Name == Constants.XmppProperties.Domain)?.Value;

			return !string.IsNullOrWhiteSpace(Domain) ? Domain : defaultValueIfNotFound;
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
