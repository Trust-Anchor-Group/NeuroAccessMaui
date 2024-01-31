using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Xamarin.CommunityToolkit.Helpers;

namespace NeuroAccessMaui.UI.Pages.Things
{
	/// <summary>
	/// Class used to present a meta-data tag in a human interface.
	/// </summary>
	public class HumanReadableTag
	{
		private readonly string name;
		private readonly string value;

		/// <summary>
		/// Classed used to present a meta-data tag in a human interface.
		/// </summary>
		/// <param name="Tag">Meta-data tag.</param>
		public HumanReadableTag(MetaDataTag Tag)
		{
			this.name = Tag.Name;
			this.value = Tag.StringValue;
		}

		/// <summary>
		/// Classed used to present a meta-data tag in a human interface.
		/// </summary>
		/// <param name="Tag">Meta-data tag.</param>
		public HumanReadableTag(Property Tag)
		{
			this.name = Tag.Name;
			this.value = Tag.Value;
		}

		/// <summary>
		/// Tag name.
		/// </summary>
		public string Name => this.name;

		/// <summary>
		/// Tag value.
		/// </summary>
		public string Value => this.value;

		/// <summary>
		/// Human-readable tag name
		/// </summary>
		public string LocalizedName
		{
			get
			{
				return this.name switch
				{
					Constants.XmppProperties.Altitude => LocalizationResourceManager.Current["Altitude"],
					Constants.XmppProperties.Apartment => LocalizationResourceManager.Current["Apartment"],
					Constants.XmppProperties.Area => LocalizationResourceManager.Current["Area"],
					Constants.XmppProperties.Building => LocalizationResourceManager.Current["Building"],
					Constants.XmppProperties.City => LocalizationResourceManager.Current["City"],
					Constants.XmppProperties.Class => LocalizationResourceManager.Current["Class"],
					Constants.XmppProperties.Country => LocalizationResourceManager.Current["Country"],
					Constants.XmppProperties.Phone => LocalizationResourceManager.Current["Phone"],
					Constants.XmppProperties.Key => LocalizationResourceManager.Current["Key"],
					Constants.XmppProperties.Latitude => LocalizationResourceManager.Current["Latitude"],
					Constants.XmppProperties.Longitude => LocalizationResourceManager.Current["Longitude"],
					Constants.XmppProperties.Manufacturer => LocalizationResourceManager.Current["Manufacturer"],
					Constants.XmppProperties.MeterLocation => LocalizationResourceManager.Current["MeterLocation"],
					Constants.XmppProperties.MeterNumber => LocalizationResourceManager.Current["MeterNumber"],
					Constants.XmppProperties.Model => LocalizationResourceManager.Current["Model"],
					Constants.XmppProperties.Name => LocalizationResourceManager.Current["Name"],
					Constants.XmppProperties.ProductInformation => LocalizationResourceManager.Current["ProductInformation"],
					Constants.XmppProperties.Registry => LocalizationResourceManager.Current["Registry"],
					Constants.XmppProperties.Region => LocalizationResourceManager.Current["Region"],
					Constants.XmppProperties.Room => LocalizationResourceManager.Current["Room"],
					Constants.XmppProperties.SerialNumber => LocalizationResourceManager.Current["SerialNumber"],
					Constants.XmppProperties.StreetName => LocalizationResourceManager.Current["StreetName"],
					Constants.XmppProperties.StreetNumber => LocalizationResourceManager.Current["StreetNumber"],
					Constants.XmppProperties.Version => LocalizationResourceManager.Current["Version"],
					_ => this.name,
				};
			}
		}

		/// <summary>
		/// Unit associated with the tag.
		/// </summary>
		public string Unit
		{
			get
			{
				return this.name switch
				{
					"ALT" => "m",
					"LAT" => "°",
					"LON" => "°",
					_ => string.Empty,
				};
			}
		}

		/// <summary>
		/// String value of tag.
		/// </summary>
		public string LocalizedValue
		{
			get
			{
				string s = this.Unit;

				if (string.IsNullOrEmpty(s))
					return this.value;
				else
					return this.value + " " + s;
			}
		}
	}
}
