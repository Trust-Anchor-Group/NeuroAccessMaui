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

		/// <inheritdoc/>
		public bool RequiresXmppConnection => true;

		/// <inheritdoc/>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string Url = Link.OriginalString;
			int SchemeEnd = Url.IndexOf(':');
			string Address = SchemeEnd < 0 ? string.Empty : Url[(SchemeEnd + 1)..].Trim();
			int QueryStart = Address.IndexOf('?');
			if (QueryStart >= 0)
				Address = Address[..QueryStart].TrimEnd();

			if (string.IsNullOrEmpty(Address) || Address.Any(char.IsWhiteSpace))
				return Task.FromResult<PreparedLink?>(null);

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link, async () =>
			{
				if (!await ChatViewModel.ProcessXmppUri(Url))
					throw new InvalidOperationException("The XMPP link could not be processed.");
			}));
		}
	}
}
