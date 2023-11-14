using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links;

/// <summary>
/// Default link opener.
/// </summary>
public class DefaultLink : ILinkOpener
{
	/// <summary>
	/// Default link opener.
	/// </summary>
	public DefaultLink()
	{
	}

	/// <summary>
	/// How well the link opener supports a given link
	/// </summary>
	/// <param name="Link">Link that will be opened.</param>
	/// <returns>Support grade of opener for the given link.</returns>
	public Grade Supports(Uri Link)
	{
		return Grade.Barely;
	}

	/// <inheritdoc/>
	public async Task<bool> TryOpenLink(Uri Link)
	{
		if (await Launcher.TryOpenAsync(Link.OriginalString))
		{
			return true;
		}
		else
		{
			await ServiceRef.UiSerializer.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
				ServiceRef.Localizer[nameof(AppResources.QrCodeNotUnderstood)] +
				Environment.NewLine + Environment.NewLine + Link.OriginalString);

			return false;
		}
	}
}
