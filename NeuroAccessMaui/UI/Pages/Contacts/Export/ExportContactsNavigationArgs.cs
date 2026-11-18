using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Contacts.Export
{
	/// <summary>
	/// Navigation arguments of the page that exports contacts to a contact file.
	/// </summary>
	public class ExportContactsNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Navigation arguments of the page that exports contacts. All contacts that are not blocked start out chosen.
		/// </summary>
		public ExportContactsNavigationArgs()
		{
		}

		/// <summary>
		/// Navigation arguments of the page that exports contacts, with some contacts chosen to start with.
		/// </summary>
		/// <param name="ObjectIds">Database IDs of the contacts to choose.</param>
		public ExportContactsNavigationArgs(IEnumerable<string> ObjectIds)
		{
			this.ObjectIds = [.. ObjectIds];
		}

		/// <summary>
		/// Database IDs of the contacts chosen to start with, or null to choose all that are not blocked.
		/// </summary>
		public IReadOnlyList<string>? ObjectIds { get; }
	}
}
