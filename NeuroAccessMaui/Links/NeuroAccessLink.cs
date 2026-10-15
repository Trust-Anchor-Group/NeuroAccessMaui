using NeuroAccessMaui.Services;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Receives Neuro-Access App links, which are callbacks returning the user from external payment pages.
	/// </summary>
	/// <remarks>
	/// Callbacks are accepted but not acted on. Their tokens are signed by this device before the payment starts and are handed
	/// to the payment provider, so holding one does not prove the payment outcome. Acting on a callback would also settle the
	/// transaction before the authoritative outcome arrives over XMPP, which would then be ignored.
	/// </remarks>
	public class NeuroAccessLink : ILinkOpener
	{
		/// <summary>
		/// Receives Neuro-Access App links.
		/// </summary>
		public NeuroAccessLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.Equals(Constants.UriSchemes.NeuroAccess, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			CancellationToken.ThrowIfCancellationRequested();

			return Task.FromResult<PreparedLink?>(new PreparedLink(Link, () =>
			{
				ServiceRef.LogService.LogInformational("PaymentCallbackIgnored");
				return Task.CompletedTask;
			}));
		}
	}
}
