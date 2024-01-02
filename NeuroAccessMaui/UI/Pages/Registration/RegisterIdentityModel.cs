using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Xmpp;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration
{
	/// <summary>
	/// The data model for registering an identity.
	/// </summary>
	public class RegisterIdentityModel
	{
		/// <summary>
		/// First name
		/// </summary>
		public string? FirstName { get; set; }

		/// <summary>
		/// Middle name(s) as one string
		/// </summary>
		public string? MiddleNames { get; set; }

		/// <summary>
		/// Last name(s) as one string
		/// </summary>
		public string? LastNames { get; set; }

		/// <summary>
		/// Personal number
		/// </summary>
		public string? PersonalNumber { get; set; }

		/// <summary>
		/// Address, line 1
		/// </summary>
		public string? Address { get; set; }

		/// <summary>
		/// Address, line 2
		/// </summary>
		public string? Address2 { get; set; }

		/// <summary>
		/// Zip code (postal code)
		/// </summary>
		public string? ZipCode { get; set; }

		/// <summary>
		/// Area
		/// </summary>
		public string? Area { get; set; }

		/// <summary>
		/// City
		/// </summary>
		public string? City { get; set; }

		/// <summary>
		/// Region
		/// </summary>
		public string? Region { get; set; }

		/// <summary>
		/// Country
		/// </summary>
		public string? Country { get; set; }

		/// <summary>
		/// Organization name
		/// </summary>
		public string? OrgName { get; set; }

		/// <summary>
		/// Organization number
		/// </summary>
		public string? OrgNumber { get; set; }

		/// <summary>
		/// Organization Address, line 1
		/// </summary>
		public string? OrgAddress { get; set; }

		/// <summary>
		/// Organization Address, line 2
		/// </summary>
		public string? OrgAddress2 { get; set; }

		/// <summary>
		/// Organization Zip code (postal code)
		/// </summary>
		public string? OrgZipCode { get; set; }

		/// <summary>
		/// Organization Area
		/// </summary>
		public string? OrgArea { get; set; }

		/// <summary>
		/// Organization City
		/// </summary>
		public string? OrgCity { get; set; }

		/// <summary>
		/// Organization Region
		/// </summary>
		public string? OrgRegion { get; set; }

		/// <summary>
		/// Organization Country
		/// </summary>
		public string? OrgCountry { get; set; }

		/// <summary>
		/// Organization Department
		/// </summary>
		public string? OrgDepartment { get; set; }

		/// <summary>
		/// Organization Role
		/// </summary>
		public string? OrgRole { get; set; }

		/// <summary>
		/// Phone Number
		/// </summary>
		public string? PhoneNr { get; set; }

		/// <summary>
		/// EMail
		/// </summary>
		public string? EMail { get; set; }

		/// <summary>
		/// Device Id
		/// </summary>
		public string? DeviceId { get; set; }

		/// <summary>
		/// Jabber Id
		/// </summary>
		public string? Jid { get; set; }

		/// <summary>
		/// Converts the <see cref="RegisterIdentityModel"/> to an array of <inheritdoc cref="Property"/>.
		/// </summary>
		/// <param name="XmppService">The XMPP service to use for accessing the Bare Jid.</param>
		/// <returns>The <see cref="RegisterIdentityModel"/> as a list of properties.</returns>
		public Property[] ToProperties(IXmppService XmppService)
		{
			List<Property> Properties = [];

			AddProperty(Properties, Constants.XmppProperties.FirstName, this.FirstName);
			AddProperty(Properties, Constants.XmppProperties.MiddleName, this.MiddleNames);
			AddProperty(Properties, Constants.XmppProperties.LastName, this.LastNames);
			AddProperty(Properties, Constants.XmppProperties.PersonalNumber, this.PersonalNumber);
			AddProperty(Properties, Constants.XmppProperties.Address, this.Address);
			AddProperty(Properties, Constants.XmppProperties.Address2, this.Address2);
			AddProperty(Properties, Constants.XmppProperties.ZipCode, this.ZipCode);
			AddProperty(Properties, Constants.XmppProperties.Area, this.Area);
			AddProperty(Properties, Constants.XmppProperties.City, this.City);
			AddProperty(Properties, Constants.XmppProperties.Region, this.Region);
			AddProperty(Properties, Constants.XmppProperties.Country, ISO_3166_1.ToCode(this.Country));
			AddProperty(Properties, Constants.XmppProperties.OrgName, this.OrgName);
			AddProperty(Properties, Constants.XmppProperties.OrgNumber, this.OrgNumber);
			AddProperty(Properties, Constants.XmppProperties.OrgDepartment, this.OrgDepartment);
			AddProperty(Properties, Constants.XmppProperties.OrgRole, this.OrgRole);
			AddProperty(Properties, Constants.XmppProperties.OrgAddress, this.OrgAddress);
			AddProperty(Properties, Constants.XmppProperties.OrgAddress2, this.OrgAddress2);
			AddProperty(Properties, Constants.XmppProperties.OrgZipCode, this.OrgZipCode);
			AddProperty(Properties, Constants.XmppProperties.OrgArea, this.OrgArea);
			AddProperty(Properties, Constants.XmppProperties.OrgCity, this.OrgCity);
			AddProperty(Properties, Constants.XmppProperties.OrgRegion, this.OrgRegion);
			AddProperty(Properties, Constants.XmppProperties.OrgCountry, ISO_3166_1.ToCode(this.OrgCountry));
			AddProperty(Properties, Constants.XmppProperties.Phone, this.PhoneNr);
			AddProperty(Properties, Constants.XmppProperties.EMail, this.EMail);
			AddProperty(Properties, Constants.XmppProperties.DeviceId, this.DeviceId);
			AddProperty(Properties, Constants.XmppProperties.Jid, XmppService.BareJid);

			return [.. Properties];
		}

		private static void AddProperty(List<Property> Properties, string PropertyName, string? PropertyValue)
		{
			string? s = PropertyValue?.Trim();

			if (!string.IsNullOrWhiteSpace(s))
				Properties.Add(new Property(PropertyName, s));
		}
	}
}
