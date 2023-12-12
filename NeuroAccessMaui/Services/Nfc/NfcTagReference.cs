using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.Services.Nfc
{
	/// <summary>
	/// Contains information about a contact.
	/// </summary>
	[CollectionName("NfcTags")]
	[TypeName(TypeNameSerialization.None)]
	[Index("TagId")]
	public class NfcTagReference
	{
		private string? objectId = null;
		private CaseInsensitiveString tagId = CaseInsensitiveString.Empty;
		private Property[]? metaData = null;
		private string friendlyName = string.Empty;

		/// <summary>
		/// Object ID
		/// </summary>
		[ObjectId]
		public string ObjectId
		{
			get => this.objectId;
			set => this.objectId = value;
		}

		/// <summary>
		/// Bare JID of contact.
		/// </summary>
		public CaseInsensitiveString TagId
		{
			get => this.tagId;
			set => this.tagId = value;
		}

		/// <summary>
		/// Friendly name.
		/// </summary>
		[DefaultValueStringEmpty]
		public string FriendlyName
		{
			get => this.friendlyName;
			set => this.friendlyName = value;
		}

		/// <summary>
		/// Meta-data related to a thing.
		/// </summary>
		[DefaultValueNull]
		public Property[] MetaData
		{
			get => this.metaData;
			set => this.metaData = value;
		}

		/// <summary>
		/// Finds information about a contact, given its Bare JID.
		/// </summary>
		/// <param name="TagId">Tag ID</param>
		/// <returns>Tag reference, if found.</returns>
		public static Task<NfcTagReference> FindByTagId(CaseInsensitiveString TagId)
		{
			return Database.FindFirstIgnoreRest<NfcTagReference>(new FilterFieldEqualTo("TagId", TagId));
		}

		/// <summary>
		/// Access to meta-data properties.
		/// </summary>
		/// <param name="PropertyName">Name of property</param>
		/// <returns>Property value.</returns>
		public string this[string PropertyName]
		{
			get
			{
				if (this.metaData is not null)
				{
					foreach (Property P in this.metaData)
					{
						if (string.Compare(P.Name, PropertyName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							return P.Value;
						}
					}
				}

				return string.Empty;
			}

		}
	}
}
