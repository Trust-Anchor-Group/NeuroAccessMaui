using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens XMPP links.
	/// </summary>
	public class XmppLink : ILinkOpener
	{
		/// <summary>
		/// Opens XMPP links.
		/// </summary>
		public XmppLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.Equals(Constants.UriSchemes.Xmpp, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <param name="ShowErrorIfUnable">If an error message should be displayed, in case the URI could not be opened.</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link, bool ShowErrorIfUnable)
		{
			return await ChatViewModel.ProcessXmppUri(Link.OriginalString);
		}
	}
}
