using IdAppNeuroAccessMaui.Services;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links;

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
		return Link.Scheme.ToLower() == Constants.UriSchemes.TagSign ? Grade.Ok : Grade.NotAtAll;
	}

	/// <summary>
	/// Tries to open a link
	/// </summary>
	/// <param name="Link">Link to open</param>
	/// <returns>If the link was opened.</returns>
	public async Task<bool> TryOpenLink(Uri Link)
	{
		ServiceReferences Services = new();

		string request = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
		await Services.ContractOrchestratorService.TagSignature(request);

		return true;
	}
}
