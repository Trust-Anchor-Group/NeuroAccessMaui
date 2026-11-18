using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Contacts.Import
{
	/// <summary>
	/// Navigation arguments of the page that imports a contact file.
	/// </summary>
	public class ImportContactsNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Navigation arguments of the page that imports a contact file. The page asks the user to pick a file.
		/// </summary>
		public ImportContactsNavigationArgs()
		{
		}

		/// <summary>
		/// Navigation arguments of the page that imports a contact file that has been read already, such as one shared with
		/// the app.
		/// </summary>
		/// <param name="FileName">The file's name, as shown.</param>
		/// <param name="Text">The file's text.</param>
		public ImportContactsNavigationArgs(string FileName, string Text)
		{
			this.FileName = FileName;
			this.Text = Text;
		}

		/// <summary>
		/// The name of a file that has been read already, if any.
		/// </summary>
		public string? FileName { get; }

		/// <summary>
		/// The text of a file that has been read already, or null for the page to ask the user to pick a file.
		/// </summary>
		public string? Text { get; }
	}
}
