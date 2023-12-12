using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.AttachmentCache
{
	/// <summary>
	/// Contains information about a file in the local cache.
	/// </summary>
	[CollectionName("AttachmentCache")]
	[TypeName(TypeNameSerialization.FullName)]
	[Index("Expires")]
	[Index("Url")]
	[Index("ParentId")]
	public class CacheEntry
	{
		private string? objectId = null;
		private CaseInsensitiveString parentId = string.Empty;
		private CaseInsensitiveString localFileName = string.Empty;
		private CaseInsensitiveString url = string.Empty;
		private CaseInsensitiveString contentType = string.Empty;
		private DateTime expires = DateTime.MinValue;

		/// <summary>
		/// Contains information about a file in the local cache.
		/// </summary>
		public CacheEntry()
		{
		}

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
		/// Timestamp of entry
		/// </summary>
		public DateTime Expires
		{
			get => this.expires;
			set => this.expires = value;
		}

		/// <summary>
		/// Associated Legal or Contract ID (Parent ID)
		/// </summary>
		[DefaultValueStringEmpty]
		public CaseInsensitiveString ParentId
		{
			get => this.parentId;
			set => this.parentId = value;
		}

		/// <summary>
		/// Local file name.
		/// </summary>
		[DefaultValueStringEmpty]
		public CaseInsensitiveString LocalFileName
		{
			get => this.localFileName;
			set => this.localFileName = value;
		}

		/// <summary>
		/// Local file name.
		/// </summary>
		public CaseInsensitiveString Url
		{
			get => this.url;
			set => this.url = value;
		}

		/// <summary>
		/// Content-Type
		/// </summary>
		public CaseInsensitiveString ContentType
		{
			get => this.contentType;
			set => this.contentType = value;
		}
	}
}
