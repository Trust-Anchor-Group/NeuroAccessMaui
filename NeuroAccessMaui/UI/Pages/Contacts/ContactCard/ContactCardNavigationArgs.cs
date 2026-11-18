using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.ContactCard
{
	/// <summary>
	/// Says which contact the contact card page shows: the contact itself, or a chat address or legal ID to find it by.
	/// </summary>
	public class ContactCardNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Says which contact the contact card page shows.
		/// </summary>
		public ContactCardNavigationArgs()
		{
		}

		/// <summary>
		/// Shows a contact.
		/// </summary>
		/// <param name="Contact">The contact.</param>
		public ContactCardNavigationArgs(Contact Contact)
		{
			this.Contact = Contact;
			this.ChatAddress = Contact.ChatAddress;
			this.LegalId = Contact.LegalId;
		}

		/// <summary>
		/// Shows the contact with a chat address, or else with a legal ID.
		/// </summary>
		/// <param name="ChatAddress">The contact's chat address, if known.</param>
		/// <param name="LegalId">The contact's legal ID, if known.</param>
		public ContactCardNavigationArgs(string? ChatAddress, string? LegalId)
		{
			this.ChatAddress = ChatAddress;
			this.LegalId = LegalId;
		}

		/// <summary>
		/// The contact, if given.
		/// </summary>
		public Contact? Contact { get; }

		/// <summary>
		/// The contact's chat address, if known.
		/// </summary>
		public string? ChatAddress { get; }

		/// <summary>
		/// The contact's legal ID, if known.
		/// </summary>
		public string? LegalId { get; }
	}
}
