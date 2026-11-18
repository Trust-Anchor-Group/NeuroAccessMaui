using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using Contact = NeuroAccessMaui.Services.Contacts.Contact;

namespace NeuroAccessMaui.UI.Pages.Contacts.EditContact
{
	/// <summary>
	/// Says which contact the edit page changes, or that it adds a new one.
	/// </summary>
	public class EditContactNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Adds a new contact, typed in by the user.
		/// </summary>
		public EditContactNavigationArgs()
		{
		}

		/// <summary>
		/// Changes a contact, or adds a new one.
		/// </summary>
		/// <param name="Contact">The contact to change, or null to add a new one.</param>
		public EditContactNavigationArgs(Contact? Contact)
		{
			this.Contact = Contact;
		}

		/// <summary>
		/// The contact to change, or null to add a new one.
		/// </summary>
		public Contact? Contact { get; }
	}
}
