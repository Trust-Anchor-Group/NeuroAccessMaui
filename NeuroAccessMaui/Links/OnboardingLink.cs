using NeuroAccessMaui.Resources.Languages;
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
		return string.Equals(Link.Scheme, Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
	}

	/// <inheritdoc/>
	public async Task<bool> TryOpenLink(Uri Link)
	{
		await ServiceRef.UiSerializer.DisplayAlert(
			ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
			ServiceRef.Localizer[nameof(AppResources.ThisCodeCannotBeClaimedAtThisTime)]);

		return false;
	}
}
