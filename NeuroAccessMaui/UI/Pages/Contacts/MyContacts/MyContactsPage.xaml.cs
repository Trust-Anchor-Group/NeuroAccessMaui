namespace NeuroAccessMaui.UI.Pages.Contacts.MyContacts
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContactsPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MyContactsPage"/> class.
		/// </summary>
		public MyContactsPage()
		{
			this.ContentPageModel = new ContactListViewModel();
			this.InitializeComponent();
		}
	}
}
