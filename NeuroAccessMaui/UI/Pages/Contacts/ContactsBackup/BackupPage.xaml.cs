namespace NeuroAccessMaui.UI.Pages.Contacts.ContactsBackup
{
	/// <summary>
	/// The page where the user turns the contacts backup on or off, backs up now, restores the backup, or deletes it.
	/// </summary>
	public partial class BackupPage
	{
		/// <summary>
		/// Creates the contacts backup page.
		/// </summary>
		public BackupPage()
		{
			this.ContentPageModel = new BackupViewModel();
			this.InitializeComponent();
		}
	}
}
