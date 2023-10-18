using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Xmpp;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Registration;

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
		string? Value;

		if (!string.IsNullOrWhiteSpace(Value = this.FirstName?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.FirstName, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.MiddleNames?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.MiddleName, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.LastNames?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.LastName, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.PersonalNumber?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.PersonalNumber, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.Address?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.Address, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.Address2?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.Address2, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.ZipCode?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.ZipCode, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.Area?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.Area, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.City?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.City, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.Region?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.Region, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.Country?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.Country, ISO_3166_1.ToCode(Value)));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgName?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgName, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgNumber?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgNumber, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgDepartment?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgDepartment, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgRole?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgRole, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgAddress?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgAddress, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgAddress2?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgAddress2, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgZipCode?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgZipCode, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgArea?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgArea, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgCity?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgCity, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgRegion?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgRegion, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.OrgCountry?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.OrgCountry, ISO_3166_1.ToCode(Value)));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.PhoneNr?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.Phone, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.EMail?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.EMail, Value));
		}

		if (!string.IsNullOrWhiteSpace(Value = this.DeviceId?.Trim()))
		{
			Properties.Add(new Property(Constants.XmppProperties.DeviceId, Value));
		}

		Properties.Add(new Property(Constants.XmppProperties.Jid, XmppService.BareJid));

		return [.. Properties];
	}
}
