using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.Import
{
	/// <summary>
	/// The page that imports a contact file.
	/// </summary>
	public partial class ImportContactsPage
	{
		/// <summary>
		/// Creates the page that imports a contact file.
		/// </summary>
		public ImportContactsPage()
		{
			this.ContentPageModel = new ImportContactsViewModel(ServiceRef.NavigationService.PopLatestArgs<ImportContactsNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
