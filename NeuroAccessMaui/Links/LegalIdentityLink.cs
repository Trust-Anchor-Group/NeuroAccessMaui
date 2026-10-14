using NeuroAccessMaui.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens Legal Identity links.
	/// </summary>
	public class LegalIdentityLink : ILinkOpener
	{
		/// <summary>
		/// Opens Legal Identity links.
		/// </summary>
		public LegalIdentityLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return string.Equals(Link.Scheme, Constants.UriSchemes.IotId, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public bool RequiresXmppConnection => true;

		/// <inheritdoc/>
		public async Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			IdentityReference? Reference = await this.ReadAsync(Link, CancellationToken);
			if (Reference is null)
				return null;

			return new PreparedLink(Link, async () =>
			{
				if (Reference.Identity is not null)
					await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Reference.Identity));
				else
				{
					// The identity is protected, so the access petition is sent without downloading it again.
					await ServiceRef.ContractOrchestratorService.PetitionLegalIdentity(Reference.Identifier,
						ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);
				}
			});
		}

		/// <summary>
		/// Reads a legal identity link without opening it or requesting access.
		/// </summary>
		/// <param name="Link">The link to read.</param>
		/// <param name="CancellationToken">Cancellation for the lookup.</param>
		/// <returns>The identity reference, or <c>null</c> when the link is malformed.</returns>
		public async Task<IdentityReference?> ReadAsync(Uri Link, CancellationToken CancellationToken)
		{
			string? Identifier = TryParseIdentifier(Link);
			if (Identifier is null)
				return null;

			LegalIdentity? Identity;
			try
			{
				Identity = await ServiceRef.XmppService.GetLegalIdentity(Identifier).WaitAsync(CancellationToken);
			}
			catch (ForbiddenException)
			{
				// Viewing someone else's identity requires their permission, which the open action requests.
				Identity = null;
			}

			return new IdentityReference(Identifier, Identity);
		}

		/// <summary>
		/// Reads the legal identity identifier from a link, without looking the identity up.
		/// </summary>
		/// <param name="Link">The link to read.</param>
		/// <returns>The identifier, or <c>null</c> when the link is malformed.</returns>
		public static string? TryParseIdentifier(Uri Link)
		{
			// Identifiers may arrive percent-encoded, for example "%40" for "@", which some Android scanners require.
			string Identifier;
			try
			{
				Identifier = Uri.UnescapeDataString(Constants.UriSchemes.RemoveScheme(Link.OriginalString) ?? string.Empty);
			}
			catch (UriFormatException)
			{
				return null;
			}

			if (string.IsNullOrWhiteSpace(Identifier) || Identifier.Any(char.IsWhiteSpace) || Identifier.Contains('?'))
				return null;

			return Identifier;
		}
	}
}
