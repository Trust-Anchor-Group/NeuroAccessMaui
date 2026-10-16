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

		/// <inheritdoc/>
		public bool RequiresXmppConnection => true;

		/// <inheritdoc/>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			string Url = Link.OriginalString.Replace("%40", "@"); // iOS has problems with URIs containg @
			Func<string, Task> Open;

			if (ServiceRef.XmppService.IsIoTDiscoClaimURI(Url))
				Open = ServiceRef.ThingRegistryOrchestratorService.OpenClaimDevice;
			else if (ServiceRef.XmppService.IsIoTDiscoSearchURI(Url))
				Open = ServiceRef.ThingRegistryOrchestratorService.OpenSearchDevices;
			else if (ServiceRef.XmppService.IsIoTDiscoDirectURI(Url))
				Open = ServiceRef.ThingRegistryOrchestratorService.OpenDeviceReference;
			else
				return Task.FromResult<PreparedLink?>(null);

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link, async () =>
			{
				await Open(Url);
				ServiceRef.TagProfile.HasThing = true;
			}));
		}
	}
}
