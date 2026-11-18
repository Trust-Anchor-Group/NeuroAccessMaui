using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Profiles
{
	/// <summary>
	/// The user's own card: the name and photo others see, and which of them the user has chosen to share. Nothing is shared
	/// until the user turns it on.
	/// </summary>
	[CollectionName("MyProfile")]
	[TypeName(TypeNameSerialization.None)]
	public class MyProfile
	{
		/// <summary>
		/// The object ID in the database.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// The name others see.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// If the name is shared with others.
		/// </summary>
		[DefaultValue(false)]
		public bool ShareName { get; set; }

		/// <summary>
		/// The photo, as a small JPEG image, if the user has chosen one.
		/// </summary>
		[DefaultValueNull]
		public byte[]? Photo { get; set; }

		/// <summary>
		/// If the photo is shared with others.
		/// </summary>
		[DefaultValue(false)]
		public bool SharePhoto { get; set; }

		/// <summary>
		/// When the card was last changed, in UTC.
		/// </summary>
		public DateTime Updated { get; set; } = DateTime.MinValue;
	}
}
