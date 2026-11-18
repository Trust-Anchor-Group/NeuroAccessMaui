using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.ContactCard
{
	/// <summary>
	/// A contact's card: who they are, how to reach them, what they share with the user, and what can be done with them.
	/// </summary>
	public partial class ContactCardPage
	{
		/// <summary>
		/// Creates the page of a contact's card.
		/// </summary>
		public ContactCardPage()
		{
			this.ContentPageModel = new ContactCardViewModel(ServiceRef.NavigationService.PopLatestArgs<ContactCardNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
