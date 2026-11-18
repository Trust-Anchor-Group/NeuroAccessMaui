using NeuroAccessMaui.Services.Contacts.VCard;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Attributes;

namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// A person or organization in the user's contacts. The contact card (vCard 4.0) is the source of truth for what the
	/// user sees and exports. The other fields are derived from it when it is saved, or belong to the app only.
	/// </summary>
	[CollectionName("Contacts")]
	[TypeName(TypeNameSerialization.None)]
	[Index("SortKey")]
	[Index("ChatAddress")]
	[Index("LegalId")]
	[Index("Uid")]
	public sealed class Contact
	{
		/// <summary>
		/// A person or organization in the user's contacts.
		/// </summary>
		public Contact()
		{
		}

		/// <summary>
		/// Database ID.
		/// </summary>
		[ObjectId]
		public string? ObjectId { get; set; }

		/// <summary>
		/// The card's unique ID, as in its UID property. It matches the card when a file with it is imported again.
		/// </summary>
		[DefaultValueStringEmpty]
		public string Uid { get; set; } = string.Empty;

		/// <summary>
		/// The contact card, as vCard 4.0 text.
		/// </summary>
		[DefaultValueStringEmpty]
		public string VCardText { get; set; } = string.Empty;

		/// <summary>
		/// The name to show. Derived from the card.
		/// </summary>
		[DefaultValueStringEmpty]
		public string DisplayName { get; set; } = string.Empty;

		/// <summary>
		/// Key the contacts are sorted by. Derived from the card.
		/// </summary>
		[DefaultValueStringEmpty]
		public string SortKey { get; set; } = string.Empty;

		/// <summary>
		/// The address the contact chats at, if any. Derived from the card's xmpp: link.
		/// </summary>
		public CaseInsensitiveString ChatAddress { get; set; } = CaseInsensitiveString.Empty;

		/// <summary>
		/// The contact's legal ID, if known. Derived from the card.
		/// </summary>
		public CaseInsensitiveString LegalId { get; set; } = CaseInsensitiveString.Empty;

		/// <summary>
		/// Labels the user has given the contact. Derived from the card.
		/// </summary>
		[DefaultValueNull]
		public string[]? Labels { get; set; }

		/// <summary>
		/// If the app has checked the contact's ID. Never taken from a file.
		/// </summary>
		[DefaultValue(false)]
		public bool IsVerified { get; set; }

		/// <summary>
		/// If the user has blocked the contact. Their requests are turned down without asking.
		/// </summary>
		[DefaultValue(false)]
		public bool IsBlocked { get; set; }

		/// <summary>
		/// If the user has stopped letting the contact see when they are online. A later request from the contact then waits
		/// for the user to answer it, instead of being accepted without asking.
		/// </summary>
		[DefaultValue(false)]
		public bool StoppedSharing { get; set; }

		/// <summary>
		/// The contact's ID, as the app checked it, if any. It stays on the phone and is never exported.
		/// </summary>
		[DefaultValueNull]
		public LegalIdentity? LegalIdentity { get; set; }

		/// <summary>
		/// When the contact was added.
		/// </summary>
		public DateTime Created { get; set; } = DateTime.MinValue;

		/// <summary>
		/// When the contact was last changed.
		/// </summary>
		public DateTime Updated { get; set; } = DateTime.MinValue;

		/// <summary>
		/// If the contact has a chat address. Every saved contact has one, so this is false only for a contact being made.
		/// </summary>
		public bool CanChat => !string.IsNullOrEmpty(this.ChatAddress?.Value);

		/// <summary>
		/// Reads the contact card.
		/// </summary>
		/// <returns>The card. An empty card if the text can't be read.</returns>
		public VCard.VCard GetCard()
		{
			IReadOnlyList<VCard.VCard> Cards = VCardReader.Parse(this.VCardText);
			return Cards.Count > 0 ? Cards[0] : new VCard.VCard();
		}
	}
}
