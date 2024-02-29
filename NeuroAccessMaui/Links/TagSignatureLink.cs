using NeuroAccessMaui.Services;
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
		public async Task<bool> TryOpenLink(Uri Link, bool ShowErrorIfUnable)
		{
			string? request = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
			if (request is null)
				return false;

			await ServiceRef.ContractOrchestratorService.TagSignature(request);

			return true;
		}
	}
}
