using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.EditContact
{
	/// <summary>
	/// The page where a contact is typed in or changed.
	/// </summary>
	public partial class EditContactPage
	{
		/// <summary>
		/// Creates the page where a contact is typed in or changed.
		/// </summary>
		public EditContactPage()
		{
			this.ContentPageModel = new EditContactViewModel(ServiceRef.NavigationService.PopLatestArgs<EditContactNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
