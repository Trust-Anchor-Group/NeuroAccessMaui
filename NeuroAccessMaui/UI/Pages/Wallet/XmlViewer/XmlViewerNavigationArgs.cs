using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Wallet.XmlViewer
{
	/// <summary>
	/// Navigation arguments for the page that shows XML.
	/// </summary>
	public class XmlViewerNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="XmlViewerNavigationArgs"/> class.
		/// </summary>
		public XmlViewerNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="XmlViewerNavigationArgs"/> class.
		/// </summary>
		/// <param name="Title">Title shown above the XML.</param>
		/// <param name="Xml">The XML to show.</param>
		public XmlViewerNavigationArgs(string Title, string Xml)
		{
			this.Title = Title;
			this.Xml = Xml;
		}

		/// <summary>
		/// Title shown above the XML.
		/// </summary>
		public string? Title { get; }

		/// <summary>
		/// The XML to show.
		/// </summary>
		public string? Xml { get; }
	}
}
