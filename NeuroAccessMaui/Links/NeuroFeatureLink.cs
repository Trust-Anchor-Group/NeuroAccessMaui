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

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			await ServiceRef.NeuroWalletOrchestratorService.OpenNeuroFeatureUri(Link.OriginalString);

			return true;
		}
	}
}
