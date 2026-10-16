using NeuroAccessMaui.Services;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens Neuro-Feature links.
	/// </summary>
	public class NeuroFeatureLink : ILinkOpener
	{
		/// <summary>
		/// Opens Neuro-Feature links.
		/// </summary>
		public NeuroFeatureLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.Equals(Constants.UriSchemes.NeuroFeature, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public bool RequiresXmppConnection => true;

		/// <inheritdoc/>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string Url = Link.OriginalString.Replace("%40", "@"); // Android has problems with URIs containg @
			string TokenId = Constants.UriSchemes.RemoveScheme(Url) ?? string.Empty;
			if (string.IsNullOrWhiteSpace(TokenId) || TokenId.Any(char.IsWhiteSpace))
				return Task.FromResult<PreparedLink?>(null);

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link,
				() => ServiceRef.NeuroWalletOrchestratorService.OpenNeuroFeatureUri(Url)));
		}
	}
}
