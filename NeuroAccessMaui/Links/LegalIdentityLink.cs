using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
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
		public async Task<bool> TryOpenLink(Uri Link)
		{
			string? LegalId = Constants.UriSchemes.RemoveScheme(Link.OriginalString);

			if (LegalId is null)
			{
				return false;
			}

			await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(LegalId,
				ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]);

			return true;
		}
	}
}
