﻿using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens Things Discovery links.
	/// </summary>
	public class ThingsDiscoveryLink : ILinkOpener
	{
		/// <summary>
		/// Opens Things Discovery links.
		/// </summary>
		public ThingsDiscoveryLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.Equals(Constants.UriSchemes.IotDisco, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			string Url = Link.OriginalString;

			if (ServiceRef.XmppService.IsIoTDiscoClaimURI(Url))
				await ServiceRef.ThingRegistryOrchestratorService.OpenClaimDevice(Url);
			else if (ServiceRef.XmppService.IsIoTDiscoSearchURI(Url))
				await ServiceRef.ThingRegistryOrchestratorService.OpenSearchDevices(Url);
			else if (ServiceRef.XmppService.IsIoTDiscoDirectURI(Url))
				await ServiceRef.ThingRegistryOrchestratorService.OpenDeviceReference(Url);
			else
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.InvalidIoTDiscoveryCode)] + Environment.NewLine + Environment.NewLine + Url);

				return false;
			}

			return true;
		}
	}
}
