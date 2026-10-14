using EDaler.Uris;
using NeuroAccessMaui.Services;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens eDaler links.
	/// </summary>
	public class EDalerLink : ILinkOpener
	{
		/// <summary>
		/// Opens eDaler links.
		/// </summary>
		public EDalerLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.Equals(Constants.UriSchemes.EDaler, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public bool RequiresXmppConnection => true;

		/// <inheritdoc/>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string Url = Link.OriginalString.Replace("%40", "@"); // Android has problems with URIs containg @

			// The same checks the wallet applies when opening the link, so invalid or expired codes are reported in the scanner.
			if (!ServiceRef.XmppService.TryParseEDalerUri(Url, out EDalerUri Parsed, out _) ||
				Parsed.Expires.AddDays(1) < DateTime.UtcNow)
			{
				return Task.FromResult<PreparedLink?>(null);
			}

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link,
				() => ServiceRef.NeuroWalletOrchestratorService.OpenEDalerUri(Url)));
		}
	}
}
