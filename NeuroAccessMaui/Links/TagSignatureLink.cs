using System.Web;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens TAG signature links.
	/// </summary>
	public class TagSignatureLink : ILinkOpener
	{
		/// <summary>
		/// Opens TAG signature links.
		/// </summary>
		public TagSignatureLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return string.Equals(Link.Scheme, Constants.UriSchemes.TagSign, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public bool RequiresXmppConnection => true;

		/// <inheritdoc/>
		/// <remarks>
		/// Only the request syntax is checked. Whether the opaque request key exists can only be established by sending the
		/// signature, which remains the user's explicit action.
		/// </remarks>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string? Request = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
			if (Request is null || !IsValidRequest(Request))
				return Task.FromResult<PreparedLink?>(null);

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link,
				() => ServiceRef.ContractOrchestratorService.TagSignature(Request)));
		}

		/// <summary>
		/// Checks that a signature request consists of a valid recipient address and a key.
		/// </summary>
		/// <param name="Request">The request, without the URI scheme.</param>
		/// <returns>Whether the request has valid syntax.</returns>
		private static bool IsValidRequest(string Request)
		{
			int Separator = Request.IndexOf(',');
			if (Separator <= 0 || Separator == Request.Length - 1 || Request.Any(char.IsControl))
				return false;

			string Jid = HttpUtility.UrlDecode(Request[..Separator]);
			string Key = Request[(Separator + 1)..];
			int QueryStart = Key.IndexOf('?');
			if (QueryStart >= 0)
				Key = Key[..QueryStart];

			return Key.Length > 0 && !Key.Any(char.IsWhiteSpace) && !Jid.Any(char.IsWhiteSpace) &&
				(XmppClient.FullJidRegEx.IsMatch(Jid) || XmppClient.DomainJidRegEx.IsMatch(Jid));
		}
	}
}
