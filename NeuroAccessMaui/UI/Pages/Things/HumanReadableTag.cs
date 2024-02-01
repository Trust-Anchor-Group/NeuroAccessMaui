using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;

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
					Constants.XmppProperties.Altitude => ServiceRef.Localizer[nameof(AppResources.Altitude)],
					Constants.XmppProperties.Apartment => ServiceRef.Localizer[nameof(AppResources.Apartment)],
					Constants.XmppProperties.Area => ServiceRef.Localizer[nameof(AppResources.Area)],
					Constants.XmppProperties.Building => ServiceRef.Localizer[nameof(AppResources.Building)],
					Constants.XmppProperties.City => ServiceRef.Localizer[nameof(AppResources.City)],
					Constants.XmppProperties.Class => ServiceRef.Localizer[nameof(AppResources.Class)],
					Constants.XmppProperties.Country => ServiceRef.Localizer[nameof(AppResources.Country)],
					Constants.XmppProperties.Phone => ServiceRef.Localizer[nameof(AppResources.Phone)],
					Constants.XmppProperties.Key => ServiceRef.Localizer[nameof(AppResources.Key)],
					Constants.XmppProperties.Latitude => ServiceRef.Localizer[nameof(AppResources.Latitude)],
					Constants.XmppProperties.Longitude => ServiceRef.Localizer[nameof(AppResources.Longitude)],
					Constants.XmppProperties.Manufacturer => ServiceRef.Localizer[nameof(AppResources.Manufacturer)],
					Constants.XmppProperties.MeterLocation => ServiceRef.Localizer[nameof(AppResources.MeterLocation)],
					Constants.XmppProperties.MeterNumber => ServiceRef.Localizer[nameof(AppResources.MeterNumber)],
					Constants.XmppProperties.Model => ServiceRef.Localizer[nameof(AppResources.Model)],
					Constants.XmppProperties.Name => ServiceRef.Localizer[nameof(AppResources.Name)],
					Constants.XmppProperties.ProductInformation => ServiceRef.Localizer[nameof(AppResources.ProductInformation)],
					Constants.XmppProperties.Registry => ServiceRef.Localizer[nameof(AppResources.Registry)],
					Constants.XmppProperties.Region => ServiceRef.Localizer[nameof(AppResources.Region)],
					Constants.XmppProperties.Room => ServiceRef.Localizer[nameof(AppResources.Room)],
					Constants.XmppProperties.SerialNumber => ServiceRef.Localizer[nameof(AppResources.SerialNumber)],
					Constants.XmppProperties.StreetName => ServiceRef.Localizer[nameof(AppResources.StreetName)],
					Constants.XmppProperties.StreetNumber => ServiceRef.Localizer[nameof(AppResources.StreetNumber)],
					Constants.XmppProperties.Version => ServiceRef.Localizer[nameof(AppResources.Version)],
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
