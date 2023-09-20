using NeuroAccessMaui.Services;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links;

/// <summary>
/// Opens onboarding links.
/// </summary>
public class OnboardingLink : ILinkOpener
{
	/// <summary>
	/// Opens onboarding links.
	/// </summary>
	public OnboardingLink()
	{
	}

	/// <summary>
	/// How well the link opener supports a given link
	/// </summary>
	/// <param name="Link">Link that will be opened.</param>
	/// <returns>Support grade of opener for the given link.</returns>
	public Grade Supports(Uri Link)
	{
		return Link.Scheme.ToLower() == Constants.UriSchemes.Onboarding ? Grade.Ok : Grade.NotAtAll;
	}

	/// <summary>
	/// Tries to open a link
	/// </summary>
	/// <param name="Link">Link to open</param>
	/// <returns>If the link was opened.</returns>
	public async Task<bool> TryOpenLink(Uri Link)
	{
		ServiceReferences Services = new();
		await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["ThisCodeCannotBeClaimedAtThisTime"]);

		return false;
	}
}
