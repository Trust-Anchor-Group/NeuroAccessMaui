using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.Export
{
	/// <summary>
	/// The page that exports contacts to a contact file.
	/// </summary>
	public partial class ExportContactsPage
	{
		/// <summary>
		/// Creates the page that exports contacts to a contact file.
		/// </summary>
		public ExportContactsPage()
		{
			this.ContentPageModel = new ExportContactsViewModel(ServiceRef.NavigationService.PopLatestArgs<ExportContactsNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
