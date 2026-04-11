using System.Collections.Generic;
using NeuroAccess.Nfc.TravelDocuments.Security;

namespace NeuroAccess.Nfc.TravelDocuments.Certificates
{
	/// <summary>
	/// Contains a collection of distinguished names.
	/// </summary>
	public class Names(Vector NamesVector)
	{
		private readonly Vector namesVector = NamesVector;
		private readonly Dictionary<string, string> names = [];

		/// <summary>
		/// Access to distinguished names, by property name.
		/// </summary>
		/// <param name="Name">Property name.</param>
		/// <returns>Distinguished name, if defined, empty string if not.</returns>
		public string this[string Name]
		{
			get
			{
				lock (this.names)
				{
					if (this.names.TryGetValue(Name, out string? Value))
						return Value;

					foreach (object Item in this.namesVector)
					{
						if (Item is SecurityString s)
						{
							if (Name == Item.GetType().Name)
							{
								string s2 = s.Value ?? string.Empty;

								this.names[Name] = s2;
								return s2;
							}
						}
						else if (Item is Vector v)
						{
							foreach (object Item2 in v)
							{
								if (Item2 is SecurityString s2 &&
									Name == Item2.GetType().Name)
								{
									string s3 = s2.Value ?? string.Empty;
									this.names[Name] = s3;
									return s3;
								}
							}
						}
					}

					this.names[Name] = string.Empty;
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// Aliased entry name
		/// </summary>
		public string AliasedEntryName => this[nameof(Security.Properties.DistinguishedNames.AliasedEntryName)];

		/// <summary>
		/// Business category
		/// </summary>
		public string BusinessCategory => this[nameof(Security.Properties.DistinguishedNames.BusinessCategory)];

		/// <summary>
		/// Common name
		/// </summary>
		public string CommonName => this[nameof(Security.Properties.DistinguishedNames.CommonName)];

		/// <summary>
		/// Country name
		/// </summary>
		public string CountryName => this[nameof(Security.Properties.DistinguishedNames.CountryName)];

		/// <summary>
		/// Description
		/// </summary>
		public string Description => this[nameof(Security.Properties.DistinguishedNames.Description)];

		/// <summary>
		/// Destination Indicator
		/// </summary>
		public string DestinationIndicator => this[nameof(Security.Properties.DistinguishedNames.DestinationIndicator)];

		/// <summary>
		/// Facsimile Telephone Number
		/// </summary>
		public string FacsimileTelephoneNumber => this[nameof(Security.Properties.DistinguishedNames.FacsimileTelephoneNumber)];

		/// <summary>
		/// International ISDN Number
		/// </summary>
		public string InternationalIsdnNumber => this[nameof(Security.Properties.DistinguishedNames.InternationalIsdnNumber)];

		/// <summary>
		/// Knowledge Information
		/// </summary>
		public string KnowledgeInformation => this[nameof(Security.Properties.DistinguishedNames.KnowledgeInformation)];

		/// <summary>
		/// Locality Name
		/// </summary>
		public string LocalityName => this[nameof(Security.Properties.DistinguishedNames.LocalityName)];

		/// <summary>
		/// Member
		/// </summary>
		public string Member => this[nameof(Security.Properties.DistinguishedNames.Member)];

		/// <summary>
		/// Organization Name
		/// </summary>
		public string OrganizationName => this[nameof(Security.Properties.DistinguishedNames.OrganizationName)];

		/// <summary>
		/// Organization Unit Name
		///	</summary>
		public string OrganizationUnitName => this[nameof(Security.Properties.DistinguishedNames.OrganizationUnitName)];

		/// <summary>
		/// Owner
		/// </summary>
		public string Owner => this[nameof(Security.Properties.DistinguishedNames.Owner)];

		/// <summary>
		/// Physical Delivery Office Name
		/// </summary>
		public string PhysicalDeliveryOfficeName => this[nameof(Security.Properties.DistinguishedNames.PhysicalDeliveryOfficeName)];

		/// <summary>
		/// Postal Address
		/// </summary>
		public string PostalAddress => this[nameof(Security.Properties.DistinguishedNames.PostalAddress)];

		/// <summary>
		/// Postal Code
		/// </summary>
		public string PostalCode => this[nameof(Security.Properties.DistinguishedNames.PostalCode)];

		/// <summary>
		/// Post Office Box
		/// </summary>
		public string PostOfficeBox => this[nameof(Security.Properties.DistinguishedNames.PostOfficeBox)];

		/// <summary>
		/// Preferred Delivery Method
		/// </summary>
		public string PreferredDeliveryMethod => this[nameof(Security.Properties.DistinguishedNames.PreferredDeliveryMethod)];

		/// <summary>
		/// Presentation Address
		/// </summary>
		public string PresentationAddress => this[nameof(Security.Properties.DistinguishedNames.PresentationAddress)];

		/// <summary>
		/// Registered Address
		/// </summary>
		public string RegisteredAddress => this[nameof(Security.Properties.DistinguishedNames.RegisteredAddress)];

		/// <summary>
		/// Search Guide
		/// </summary>
		public string SearchGuide => this[nameof(Security.Properties.DistinguishedNames.SearchGuide)];

		/// <summary>
		/// Serial Number
		/// </summary>
		public string SerialNumber => this[nameof(Security.Properties.DistinguishedNames.SerialNumber)];

		/// <summary>
		/// State or Province Name
		/// </summary>
		public string StateOrProvinceName => this[nameof(Security.Properties.DistinguishedNames.StateOrProvinceName)];

		/// <summary>
		/// Street Address
		/// </summary>
		public string StreetAddress => this[nameof(Security.Properties.DistinguishedNames.StreetAddress)];

		/// <summary>
		/// Supported Application Context
		/// </summary>
		public string SupportedApplicationContext => this[nameof(Security.Properties.DistinguishedNames.SupportedApplicationContext)];

		/// <summary>
		/// Surname
		/// </summary>
		public string Surname => this[nameof(Security.Properties.DistinguishedNames.Surname)];

		/// <summary>
		/// Telephone Number
		/// </summary>
		public string TelephoneNumber => this[nameof(Security.Properties.DistinguishedNames.TelephoneNumber)];

		/// <summary>
		/// Telex Number
		/// </summary>
		public string TelexNumber => this[nameof(Security.Properties.DistinguishedNames.TelexNumber)];

		/// <summary>
		/// Telex Terminal Identifier
		/// </summary>
		public string TelexTerminalIdentifier => this[nameof(Security.Properties.DistinguishedNames.TelexTerminalIdentifier)];

		/// <summary>
		/// Title
		/// </summary>
		public string Title => this[nameof(Security.Properties.DistinguishedNames.Title)];

		/// <summary>
		/// X.121 Address
		/// </summary>
		public string X121Address => this[nameof(Security.Properties.DistinguishedNames.X121Address)];
	}
}
